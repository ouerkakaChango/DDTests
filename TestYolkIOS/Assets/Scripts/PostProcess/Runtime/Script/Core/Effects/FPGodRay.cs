using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;

namespace CenturyGame.PostProcess
{
    public class FPGodRay : IPostProcess
    {
        public Color ColorThreshold = Color.gray;
        public Transform Light;
        [Range(0.0f, 5.0f)]
        public float LightRadius = 2.0f;
        [Range(1.0f, 4.0f)]
        public float LightPowFactor = 3.0f;

        public float LightDistance = 50.0f;
        public Color LightColor = Color.white;
        [Range(0.0f, 20.0f)]
        public float LightFactor = 0.5f;
        [Range(0.0f, 10.0f)]
        public float SamplerScale = 1;
        [Range(1, 3)]
        public int BlurIteration = 2;
        [Range(0, 3)]
        public int DownSample = 1;


        private Shader m_curShader = null;
        private Material m_curMat = null;
        private CenturyGame.PostProcess.PostProcessHandle para;
        public override void Init()
        {
            Title = "FPGodRay";
            Propertys = new string[] {
            "ColorThreshold",
            "Light",
            "LightRadius",
            "LightPowFactor",
            "LightDistance",
            "LightColor",
            "LightFactor",
            "SamplerScale",
            "BlurIteration",
            "DownSample"
            };
            checkSupport();
        }

        public override void DoEnable(CenturyGame.PostProcess.PostProcessHandle _para)
        {
            para = _para;
            checkSupport();
        }

        public override void DoDisable()
        {
            if (m_curMat != null)
            {
                GameObject.DestroyImmediate(m_curMat);
            }
        }

        public override void OnRenderHandle(ref RenderTexture source, ref RenderTexture destination, ref RenderTexture depth, ref int count)
        {
            if (m_curMat != null)
            {
                int rtWidth = source.width >> DownSample;
                int rtHeight = source.height >> DownSample;

                RenderTexture temp1 = FPRenderTextureManager.Instance.Get(rtWidth, rtHeight, 0, source.format);
                RenderTexture temp2 = FPRenderTextureManager.Instance.Get(rtWidth, rtHeight, 0, source.format);
                Vector3 viewPortLightPos = Light == null ? new Vector3(.5f, .5f, 0) : para.MainCamera.WorldToViewportPoint(Light.position);

                m_curMat.SetVector("_ColorThreshold", ColorThreshold);
                m_curMat.SetVector("_ViewPortLightPos", new Vector4(viewPortLightPos.x, viewPortLightPos.y, viewPortLightPos.z, 0));
                m_curMat.SetFloat("_LightDistance", focalDistance01(LightDistance));
                m_curMat.SetFloat("_LightRadius", LightRadius);
                m_curMat.SetFloat("_PowFactor", LightPowFactor);
                Graphics.Blit(source, temp1, m_curMat, 0);

                m_curMat.SetVector("_ViewPortLightPos", new Vector4(viewPortLightPos.x, viewPortLightPos.y, viewPortLightPos.z, 0));
                m_curMat.SetFloat("_LightRadius", LightRadius);

                float samplerOffset = SamplerScale / source.width;
                for (int i = 0; i < BlurIteration; i++)
                {
                    float offset = samplerOffset * (i * 2 + 1);
                    m_curMat.SetVector("_offsets", new Vector4(offset, offset, 0, 0));
                    Graphics.Blit(temp1, temp2, m_curMat, 1);

                    offset = samplerOffset * (i * 2 + 2);
                    m_curMat.SetVector("_offsets", new Vector4(offset, offset, 0, 0));
                    Graphics.Blit(temp2, temp1, m_curMat, 1);
                }

                m_curMat.SetTexture("_BlurTex", temp1);
                m_curMat.SetVector("_LightColor", LightColor);
                m_curMat.SetFloat("_LightFactor", LightFactor);
                Graphics.Blit(source, destination, m_curMat, 2);

                FPRenderTextureManager.Instance.Release(temp1);
                FPRenderTextureManager.Instance.Release(temp2);
            }
            base.OnRenderHandle(ref source, ref destination, ref depth, ref count);
        }

        void checkSupport()
        {
            if (m_curShader == null)
            {
                m_curShader = CenturyGame.PostProcess.PostProcessHandle.LoadShader("Shaders/Post/FPGodRay");
            }
            if (!SystemInfo.supportsImageEffects || m_curShader == null || !m_curShader.isSupported)
            {
                Enable = false;
                return;
            }
            if (Enable && m_curMat == null)
            {
                m_curMat = new Material(m_curShader);
                m_curMat.hideFlags = HideFlags.HideAndDontSave;
            }
        }

        private float focalDistance01(float distance)
        {
            return para.MainCamera.WorldToViewportPoint((distance - para.MainCamera.nearClipPlane) * para.MainCamera.transform.forward + para.MainCamera.transform.position).z / (para.MainCamera.farClipPlane - para.MainCamera.nearClipPlane);
        }
    }
}