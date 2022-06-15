using UnityEngine;
using UnityEngine.Rendering;
using System;
namespace CenturyGame.PostProcess
{
    public class FPFinal : IPostProcess
    {
        public string colorTexName = "_ColorTex";
        public string depthTexName = "_DepthTex";
        public bool outputColor = false, outputDepth = false, lastOutputDepth = true;
        public RenderTexture depthTex;
        public override void Init()
        {
            Title = "FPFinal";
            Propertys = new string[] { "outputColor", "outputDepth", "colorTexName", "depthTexName" };
        }

        public override void DoEnable(CenturyGame.PostProcess.PostProcessHandle cam)
        {
            para = cam;
            colorTexId = Shader.PropertyToID(colorTexName);
            lastOutputDepth = !outputDepth;
        }

        public override void Update()
        {
            if (lastOutputDepth != outputDepth)
            {
                lastOutputDepth = outputDepth;
                initRT(true, 0, 0);
                if (outputDepth)
                {
                    initRT(false, para.PostResolution.width, para.PostResolution.height);
                }
            }
        }

        void initRT(bool destory, int w, int h)
        {
            if (destory)
            {
                if (outputDepthCmd != null)
                {
                    if (para != null)
                    {
                        para.MainCamera.RemoveCommandBuffer(CameraEvent.AfterForwardOpaque, outputDepthCmd);
                    }
                    outputDepthCmd.Dispose();
                    outputDepthCmd = null;
                }
                if (depthTex != null)
                {
                    GameObject.DestroyImmediate(depthTex);
                    depthTex = null;
                }
            }
            else
            {
                outputDepthCmd = new CommandBuffer();
                outputDepthCmd.name = "outputDepthCmd";
                para.MainCamera.AddCommandBuffer(CameraEvent.AfterForwardOpaque, outputDepthCmd);

                depthTex = new RenderTexture(w
                , h
                , 0
                , RenderTextureFormat.RFloat
                , RenderTextureReadWrite.Linear);
                depthTex.filterMode = FilterMode.Point;
                depthTex.name = depthTexName;

                outputDepthCmd.Clear();
                outputDepthCmd.Blit(para.DepthTex.depthBuffer, depthTex.colorBuffer);
                outputDepthCmd.SetGlobalTexture(depthTexName, depthTex);
            }
        }
        private CommandBuffer outputDepthCmd = null;
        private CenturyGame.PostProcess.PostProcessHandle para;
        private int colorTexId = 0;

        public Mesh Quad = null;
        public override void DoDisable()
        {
            initRT(true, 0, 0);
        }

        public override void OnRenderHandle(ref RenderTexture source, ref RenderTexture destination, ref RenderTexture depth, ref int count)
        {
            if (para == null)
            {
                return;
            }
            if (para.GetUITarget == null)
            {
                if (outputColor)
                {
                    Shader.SetGlobalTexture(colorTexId, source);
                }
                else
                {
                    Graphics.Blit(source, null as RenderTexture);
                }
            }
            else
            {
                RenderTexture target = null;
                Material linearMat = para.GetUITarget(ref target);
                Graphics.Blit(source, target, linearMat, 0);
                if (outputColor)
                {
                    Shader.SetGlobalTexture(colorTexId, target);
                }
            }
            base.OnRenderHandle(ref source, ref destination, ref depth, ref count);
        }

        private Mesh getMesh(Camera renderCam, float distance)
        {
            Vector3[] vertices = new Vector3[4];
            vertices[0] = renderCam.ScreenToWorldPoint(new Vector3(0, 0, distance));
            vertices[1] = renderCam.ScreenToWorldPoint(new Vector3(0, renderCam.pixelHeight, distance));
            vertices[2] = renderCam.ScreenToWorldPoint(new Vector3(renderCam.pixelWidth, renderCam.pixelHeight, distance));
            vertices[3] = renderCam.ScreenToWorldPoint(new Vector3(renderCam.pixelWidth, 0, distance));

            Vector2[] uvs = new Vector2[4];
            uvs[0] = new Vector2(0, 0);
            uvs[1] = new Vector2(0, 1);
            uvs[2] = new Vector2(1, 1);
            uvs[3] = new Vector2(1, 0);

            int[] triangleID = new int[6];
            triangleID[0] = 0;
            triangleID[1] = 1;
            triangleID[2] = 2;
            triangleID[3] = 2;
            triangleID[4] = 3;
            triangleID[5] = 0;

            Mesh drawMesh = new Mesh();
            drawMesh.name = "quad_ui";
            drawMesh.vertices = vertices;
            drawMesh.uv = uvs;
            drawMesh.triangles = triangleID;

            return drawMesh;

            /*
            //Mesh Quad = getMesh(Cam, 2.0f);
            //m_curMat.SetTexture("_MainTex", PostProcessManager.DesTex);
            //cmd.DrawMesh(Quad, Matrix4x4.identity, m_curMat, 0, 1);
            Cam.AddCommandBuffer(CameraEvent.BeforeForwardAlpha, cmd);
            */
        }
    }

    public delegate bool BoolHandle();
}