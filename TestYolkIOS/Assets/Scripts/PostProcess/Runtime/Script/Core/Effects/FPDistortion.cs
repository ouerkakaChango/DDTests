using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;
using System.Collections.Generic;
namespace CenturyGame.PostProcess
{
    public class FPDistortion : IPostProcess
    {
        private const string _DistortionAlpha = "_GrabTexture";
        private const string _DistortionOpaque = "_GrabOpaque";
        private const string _DistortionSSR = "_GrabSSR";
        public RenderTexture distortionOpaqueTexture = null, distortionAlphaTexture = null, distortionSSRTexture = null, ssrBlurRT = null;
        public bool distortionOpaque = false, distortionAlpha = true, distortionSSR = true;
        public float sampleOpaqueScale = 0.2f;
        public float sampleAlphaScale = 0.3f;
        public bool reflectUpSurface = true;

        private bool lastDistortionOpaque = false, lastDistortionAlpha = false, lastdistortionSSR = false;
        private CommandBuffer m_distortionAlphaCmd = null, m_distortionOpaqueCmd = null, m_rtCmd = null, drawSSR = null;
        private List<Renderer> m_renderList = null;
        private int _DistortionAlphaID = 0, _DistortionOpaqueID = 0;
        private CenturyGame.PostProcess.PostProcessHandle para;

        private Texture2D ditherMap = null;
        [Range(0, 1)]
        public float maxReflectionDistance = 0.5f;

        [Range(0, 100)]
        public int maxSampleCount = 10;

        [Range(0, 0.5f)]
        public float depthThickness = 0.25f;

        public float ditherFactor = 8;

        [Range(0, 4)]
        public int downSample = 1;

        public Renderer Plan = null;

        public override void Init()
        {
            Title = "FPDistortion";
            Propertys = new string[] { "distortionOpaque", "sampleOpaqueScale", "distortionOpaqueTexture", "distortionAlpha", "sampleAlphaScale", "distortionAlphaTexture", "distortionSSR", "maxReflectionDistance", "maxSampleCount", "depthThickness", "ditherFactor", "downSample", "reflectUpSurface", "distortionSSRTexture", "Plan" };
            CheckSupport();
        }

        public void AddRenderer(Renderer renderer)
        {
            if (null == m_renderList)
            {
                m_renderList = new List<Renderer>();
            }
            if (!m_renderList.Contains(renderer))
            {
                m_renderList.Add(renderer);
            }
            if (m_distortionAlphaCmd != null)
            {
                DrawRenderList();
            }
        }

        public void RemoveRenderer(Renderer renderer)
        {
            if (null == m_renderList)
            {
                return;
            }
            if (m_renderList.Contains(renderer))
            {
                m_renderList.Remove(renderer);
            }
            if (m_distortionAlphaCmd != null)
            {
                DrawRenderList();
            }
        }

        public override void DoEnable(CenturyGame.PostProcess.PostProcessHandle cam)
        {
            lastDistortionOpaque = !distortionOpaque;
            lastDistortionAlpha = !distortionAlpha;
            _DistortionAlphaID = Shader.PropertyToID(_DistortionAlpha);
            _DistortionOpaqueID = Shader.PropertyToID(_DistortionOpaque);
            para = cam;
            CheckSupport();
            ditherMap = OrderedDithering.GetDitherMap(4);
            //GenerateDitherMap();
        }

        public override void DoDisable()
        {
            addDistortionOpaque(true);
            addDistortionApha(true);
            addDistortionSSR(true);
            GameObject.DestroyImmediate(ditherMap);
        }

        public override void Update()
        {
            if (lastDistortionOpaque != distortionOpaque)
            {
                lastDistortionOpaque = distortionOpaque;
                addDistortionOpaque(true);
                if (distortionOpaque)
                {
                    addDistortionOpaque(false);
                }
            }
            if (lastDistortionAlpha != distortionAlpha)
            {
                lastDistortionAlpha = distortionAlpha;
                addDistortionApha(true);
                if (distortionAlpha)
                {
                    addDistortionApha(false);
                }
            }
            if (lastdistortionSSR != distortionSSR)
            {
                lastdistortionSSR = distortionSSR;
                addDistortionSSR(true);
                if (distortionSSR)
                {
                    addDistortionSSR(false);
                }
            }
            float width = para.PostResolution.width;
            float height = para.PostResolution.height;
            var screenSize = new Vector4(1.0f / width, 1.0f / height, width, height);
            var clipToScreenMatrix = new Matrix4x4();
            clipToScreenMatrix.SetRow(0, new Vector4(width * 0.5f, 0, 0, width * 0.5f));
            clipToScreenMatrix.SetRow(1, new Vector4(0, height * 0.5f, 0, height * 0.5f));
            clipToScreenMatrix.SetRow(2, new Vector4(0, 0, 1.0f, 0));
            clipToScreenMatrix.SetRow(3, new Vector4(0, 0, 0, 1.0f));
            var projectionMatrix = GL.GetGPUProjectionMatrix(para.MainCamera.projectionMatrix, true);
            var viewToScreenMatrix = clipToScreenMatrix * projectionMatrix;

            var vpMatrix = projectionMatrix * para.MainCamera.worldToCameraMatrix;

            Shader.SetGlobalMatrix("_InverseVPMatrix", vpMatrix.inverse);

            Shader.SetGlobalMatrix("_ViewToScreenMatrix", viewToScreenMatrix);
            Shader.SetGlobalVector("_ScreenSize", screenSize);
            Shader.SetGlobalMatrix("_WorldToCameraMatrix", para.MainCamera.worldToCameraMatrix);
            Shader.SetGlobalFloat("_maxReflectionDistance", maxReflectionDistance);
            Shader.SetGlobalFloat("_maxSampleCount", maxSampleCount);
            Shader.SetGlobalFloat("_depthThickness", depthThickness);
            Shader.SetGlobalFloat("_ditherFactor", ditherFactor);
            Shader.SetGlobalTexture("_ditherMap", ditherMap);

            if (reflectUpSurface)
                Shader.DisableKeyword("SSR_DONT_REFLECT_UP_SURFACE");
            else
                Shader.EnableKeyword("SSR_DONT_REFLECT_UP_SURFACE");
        }
        void CheckSupport()
        {

        }

        public override void OnRenderHandle(ref RenderTexture source, ref RenderTexture destination, ref RenderTexture depth, ref int count)
        {

        }

        void addDistortionOpaque(bool destory)
        {
            if (para != null && para.MainCamera != null)
            {
                if (destory)
                {
                    if (m_distortionOpaqueCmd != null)
                    {
                        para.MainCamera.RemoveCommandBuffer(CameraEvent.AfterForwardOpaque, m_distortionOpaqueCmd);
                        m_distortionOpaqueCmd.Dispose();
                        m_distortionOpaqueCmd = null;
                    }
                    if (distortionOpaqueTexture != null)
                    {
                        GameObject.DestroyImmediate(distortionOpaqueTexture);
                    }
                }
                else
                {
                    m_distortionOpaqueCmd = new CommandBuffer();
                    m_distortionOpaqueCmd.name = "DistortionOpaqueCmd_RT";
                    para.MainCamera.AddCommandBuffer(CameraEvent.AfterForwardOpaque, m_distortionOpaqueCmd);
                    if (distortionOpaqueTexture == null)
                    {
                        distortionOpaqueTexture = new RenderTexture((int)(para.PostResolution.width * sampleOpaqueScale), (int)(para.PostResolution.height * sampleOpaqueScale), 0, para.HDRFormat, RenderTextureReadWrite.Linear);
                        distortionOpaqueTexture.name = _DistortionOpaque;
                        m_distortionOpaqueCmd.Blit(BuiltinRenderTextureType.CurrentActive, distortionOpaqueTexture);
                        m_distortionOpaqueCmd.SetGlobalTexture(_DistortionOpaqueID, distortionOpaqueTexture);
                    }
                }
            }
        }

        void addDistortionSSR(bool destory)
        {
            if (para != null && para.MainCamera != null)
            {
                if (destory)
                {
                    if (drawSSR != null)
                    {
                        para.MainCamera.RemoveCommandBuffer(CameraEvent.BeforeForwardAlpha, drawSSR);
                        drawSSR.Dispose();
                        drawSSR = null;
                    }
                    if (distortionSSRTexture != null)
                    {
                        GameObject.DestroyImmediate(distortionSSRTexture);
                    }
                    if (ssrBlurRT != null)
                    {
                        DestroyImmediate(ssrBlurRT);
                    }
                }
                else
                {
                    drawSSR = new CommandBuffer();
                    drawSSR.name = "Distortion_SSR";
                    para.MainCamera.AddCommandBuffer(CameraEvent.BeforeForwardAlpha, drawSSR);
                    if (distortionSSRTexture == null)
                    {
                        distortionSSRTexture = new RenderTexture((int)(para.PostResolution.width >> downSample), (int)(para.PostResolution.height >> downSample), 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
                        distortionSSRTexture.name = _DistortionSSR;
                    }
                    if (ssrBlurRT == null)
                    {
                        ssrBlurRT = new RenderTexture((int)(para.PostResolution.width >> downSample), (int)(para.PostResolution.height >> downSample), 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
                        ssrBlurRT.name = "_SSRBlur";
                    }

                    var mat = Plan.sharedMaterial;
                    var pass = mat.FindPass("SSR");
                    drawSSR.SetRenderTarget(distortionSSRTexture);
                    drawSSR.SetGlobalTexture("_ColorTex2", para.ColorTex);
                    drawSSR.DrawRenderer(Plan, Plan.sharedMaterial, 0, pass);

                    pass = mat.FindPass("FASTBLUR");

                    drawSSR.SetGlobalVector("_SSRTargetSize", new Vector2((int)(para.PostResolution.width >> downSample), (int)(para.PostResolution.height >> downSample)));
                    drawSSR.SetGlobalVector("_Blur_Dir", new Vector2(0, 1));
                    drawSSR.Blit(distortionSSRTexture, ssrBlurRT, mat, pass);

                    drawSSR.SetGlobalTexture("_SSR_Texture", distortionSSRTexture);
                    drawSSR.SetGlobalTexture("_SSR_Texture_Blur", ssrBlurRT);
                }
            }
        }

        void addDistortionApha(bool destory)
        {
            if (para != null && para.MainCamera != null)
            {
                if (destory)
                {
                    if (m_distortionAlphaCmd != null)
                    {
                        para.MainCamera.RemoveCommandBuffer(CameraEvent.AfterForwardAlpha, m_distortionAlphaCmd);
                        m_distortionAlphaCmd.Dispose();
                        m_distortionAlphaCmd = null;
                    }
                    if (distortionAlphaTexture != null)
                    {
                        GameObject.DestroyImmediate(distortionAlphaTexture);
                    }


                    if (m_rtCmd != null)
                    {
                        para.MainCamera.RemoveCommandBuffer(CameraEvent.AfterForwardAlpha, m_rtCmd);
                        m_rtCmd.Clear();
                        m_rtCmd.Dispose();
                        m_rtCmd = null;
                    }
                    if (m_renderList != null)
                    {
                        m_renderList.Clear();
                    }
                }
                else
                {
                    m_distortionAlphaCmd = new CommandBuffer();
                    m_distortionAlphaCmd.name = "DistortionOpaqueCmd_RT";
                    para.MainCamera.AddCommandBuffer(CameraEvent.AfterForwardOpaque, m_distortionAlphaCmd);
                    if (distortionAlphaTexture == null)
                    {
                        distortionAlphaTexture = new RenderTexture((int)(para.PostResolution.width * sampleOpaqueScale), (int)(para.PostResolution.height * sampleOpaqueScale), 0, para.HDRFormat, RenderTextureReadWrite.Linear);
                        distortionAlphaTexture.name = _DistortionAlpha;
                        m_distortionAlphaCmd.Blit(BuiltinRenderTextureType.CurrentActive, distortionAlphaTexture);
                        m_distortionAlphaCmd.SetGlobalTexture(_DistortionAlphaID, distortionAlphaTexture);
                    }

                    //m_rtCmd = new CommandBuffer();
                    //m_rtCmd.name = "DistortionCmd_Renders";
                    //para.MainCamera.AddCommandBuffer(CameraEvent.AfterForwardAlpha, m_rtCmd);
                }
            }
        }

        void DrawRenderList()
        {
            if (m_rtCmd != null && m_renderList != null)
            {
                m_rtCmd.Clear();
                //m_distortionCmd.SetRenderTarget(BuiltinRenderTextureType.CurrentActive, BuiltinRenderTextureType.CurrentActive);
                int length = m_renderList.Count;

                for (int i = 0; i < length; i++)
                {
                    var _tempRenderer = m_renderList[i];
                    if (_tempRenderer != null && _tempRenderer.sharedMaterial != null)
                    {
                        m_rtCmd.DrawRenderer(_tempRenderer, _tempRenderer.sharedMaterial, 0, 0);
                    }
                }
                Debug.Log(m_renderList.Count);
            }
        }
    }
}