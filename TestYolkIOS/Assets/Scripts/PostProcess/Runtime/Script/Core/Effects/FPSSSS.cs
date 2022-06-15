using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine;

namespace CenturyGame.PostProcess
{
    public class FPSSSS : IPostProcess
    {
        [Range(0, 6)]
        public float SubsurfaceScaler = 0.25f;
        public int Stencil = 128;
        public Color SubsurfaceColor, SubsurfaceFalloff;

        private float lastSubsurfaceScaler = 0;
        private Color lastSubsurfaceColor, lastSubsurfaceFalloff;
        private int lastStencil = 0;

        private CenturyGame.PostProcess.PostProcessHandle para;
        private CommandBuffer SubsurfaceBuffer;
        private Material m_curMat = null;
        private Vector4[] KernelArray = null;
        //private RenderTexture depthRT, colorRT;
        static int SceneColorID = Shader.PropertyToID("_SceneColor");
        static int Kernel = Shader.PropertyToID("_Kernel");
        static int SSSScaler = Shader.PropertyToID("_SSSScale");
        static int StencilTexID = Shader.PropertyToID("_StencilTex");
        static int StencilID = Shader.PropertyToID("_Stencil");
        static readonly int PickSample = 7;
        public override void Init()
        {
            Title = "FPSSSS";
            Propertys = new string[] { "SubsurfaceScaler", "Stencil", "SubsurfaceColor", "SubsurfaceFalloff" };
            checkSupport();
        }
        public override void DoEnable(CenturyGame.PostProcess.PostProcessHandle cam)
        {
            checkSupport();
            if (m_curMat == null)
                return;
            KernelArray = new Vector4[PickSample];
            para = cam;

            SubsurfaceBuffer = new CommandBuffer();

            SubsurfaceBuffer.name = "Separable Subsurface Scatter";
            para.MainCamera.AddCommandBuffer(CameraEvent.BeforeForwardAlpha, SubsurfaceBuffer);
            updateSubsurface();

            SubsurfaceBuffer.Clear();

            BlitStencil(SubsurfaceBuffer, para.ColorTex.colorBuffer, para.DesTex.colorBuffer, para.DepthTex.depthBuffer, m_curMat, 0);
            BlitStencil(SubsurfaceBuffer, para.DesTex.colorBuffer, para.ColorTex.colorBuffer, para.DepthTex.depthBuffer, m_curMat, 1);

            //SubsurfaceBuffer.GetTemporaryRT(SceneColorID, -1, -1, 0, FilterMode.Trilinear, RenderTextureFormat.RGB111110Float);
            //BlitStencil(SubsurfaceBuffer, BuiltinRenderTextureType.CurrentActive, SceneColorID, para.DepthTex.depthBuffer, m_curMat, 0);
            //BlitStencil(SubsurfaceBuffer, SceneColorID, para.ColorTex.colorBuffer, para.DepthTex.depthBuffer, m_curMat, 1);
            //SubsurfaceBuffer.ReleaseTemporaryRT(SceneColorID);

            lastSubsurfaceColor.a = SubsurfaceColor.a + 0.1f;
            lastSubsurfaceFalloff.a = SubsurfaceFalloff.a + 0.1f;
            lastStencil = Stencil - 1;
            lastSubsurfaceScaler = SubsurfaceScaler - 1;
        }

        void checkSupport()
        {
            Shader m_curShader = null;
            if (m_curShader == null)
            {
                m_curShader = CenturyGame.PostProcess.PostProcessHandle.LoadShader("Shaders/Post/FPSSSS");
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
        public override void DoDisable()
        {
            clearSubsurfaceBuffer();
            KernelArray = null;
        }

        public override void Update()
        {
#if UNITY_EDITOR
            updateSubsurface();
#endif
        }
        public override void OnRenderHandle(ref RenderTexture source, ref RenderTexture destination, ref RenderTexture depth, ref int count)
        {

        }
        void updateSubsurface()
        {
#if !UNITY_EDITOR
        if (SubsurfaceColor != lastSubsurfaceColor || SubsurfaceFalloff != lastSubsurfaceFalloff)
        {
#endif
            lastSubsurfaceColor = SubsurfaceColor;
            lastSubsurfaceFalloff = SubsurfaceFalloff;

            Vector3 SSSC = Vector3.Normalize(new Vector3(SubsurfaceColor.r, SubsurfaceColor.g, SubsurfaceColor.b));
            Vector3 SSSFC = Vector3.Normalize(new Vector3(SubsurfaceFalloff.r, SubsurfaceFalloff.g, SubsurfaceFalloff.b));

            CalculateKernel(KernelArray, PickSample, SSSC, SSSFC);
            m_curMat.SetVectorArray(Kernel, KernelArray);
#if !UNITY_EDITOR
        }

        if (lastStencil != Stencil)
        {
#endif
            lastStencil = Stencil;
            m_curMat.SetInt(StencilID, Stencil);
#if !UNITY_EDITOR
        }
        if (lastSubsurfaceScaler != SubsurfaceScaler)
        {
#endif
            lastSubsurfaceScaler = SubsurfaceScaler;
            m_curMat.SetFloat(SSSScaler, SubsurfaceScaler);
#if !UNITY_EDITOR
        }
#endif
        }
        public void BlitStencil(CommandBuffer buffer, RenderTargetIdentifier colorSrc, RenderTargetIdentifier colorBuffer, RenderTargetIdentifier depthStencilBuffer, Material mat, int pass)
        {
            buffer.SetGlobalTexture(StencilTexID, colorSrc);
            buffer.SetRenderTarget(colorBuffer, depthStencilBuffer);
            buffer.DrawMesh(m_Mesh, Matrix4x4.identity, mat, 0, pass);
        }

        void clearSubsurfaceBuffer()
        {
            if (m_Mesh != null)
            {
                GameObject.DestroyImmediate(m_Mesh);
                m_mesh = null;
            }
            if (SubsurfaceBuffer != null)
            {
                para.MainCamera.RemoveCommandBuffer(CameraEvent.BeforeForwardAlpha, SubsurfaceBuffer);
                SubsurfaceBuffer.Release();
                SubsurfaceBuffer = null;
            }

            if (m_curMat != null)
            {
                GameObject.DestroyImmediate(m_curMat);
                m_curMat = null;
            }
        }

        private Mesh m_Mesh
        {
            get
            {
                if (m_mesh != null)
                    return m_mesh;
                m_mesh = new Mesh();
                m_mesh.vertices = new Vector3[] {
                              new Vector3(-1,-1,0),
                              new Vector3(-1,1,0),
                              new Vector3(1,1,0),
                              new Vector3(1,-1,0)
            };
                m_mesh.uv = new Vector2[] {
                        new Vector2(0,1),
                        new Vector2(0,0),
                        new Vector2(1,0),
                        new Vector2(1,1)
            };
                m_mesh.SetIndices(new int[] { 0, 1, 2, 3 }, MeshTopology.Quads, 0);
                return m_mesh;
            }
        }
        private Mesh m_mesh;

        public void CalculateKernel(Vector4[] kernel, int nSamples, Vector3 strength, Vector3 falloff)
        {
            float RANGE = nSamples > 20 ? 3.0f : 2.0f;
            float EXPONENT = 2.0f;

            // Calculate the SSS_Offset_UV:
            float step = 2.0f * RANGE / (nSamples - 1);
            for (int i = 0; i < nSamples; i++)
            {
                float o = -RANGE + i * step;
                float sign = o < 0.0f ? -1.0f : 1.0f;
                float w = RANGE * sign * Mathf.Abs(Mathf.Pow(o, EXPONENT)) / Mathf.Pow(RANGE, EXPONENT);
                kernel[i] = new Vector4(0, 0, 0, w);
            }
            // Calculate the SSS_Scale:
            for (int i = 0; i < nSamples; i++)
            {
                float w0 = i > 0 ? Mathf.Abs(kernel[i].w - kernel[i - 1].w) : 0.0f;
                float w1 = i < nSamples - 1 ? Mathf.Abs(kernel[i].w - kernel[i + 1].w) : 0.0f;
                float area = (w0 + w1) / 2.0f;
                Vector3 temp = profile(kernel[i].w, falloff);
                Vector4 tt = new Vector4(area * temp.x, area * temp.y, area * temp.z, kernel[i].w);
                kernel[i] = tt;
            }
            Vector4 t = kernel[nSamples / 2];
            for (int i = nSamples / 2; i > 0; i--)
                kernel[i] = kernel[i - 1];
            kernel[0] = t;
            Vector4 sum = Vector4.zero;

            for (int i = 0; i < nSamples; i++)
            {
                sum.x += kernel[i].x;
                sum.y += kernel[i].y;
                sum.z += kernel[i].z;
            }

            for (int i = 0; i < nSamples; i++)
            {
                Vector4 vecx = kernel[i];
                vecx.x /= sum.x;
                vecx.y /= sum.y;
                vecx.z /= sum.z;
                kernel[i] = vecx;
            }

            Vector4 vec = kernel[0];
            vec.x = (1.0f - strength.x) * 1.0f + strength.x * vec.x;
            vec.y = (1.0f - strength.y) * 1.0f + strength.y * vec.y;
            vec.z = (1.0f - strength.z) * 1.0f + strength.z * vec.z;
            kernel[0] = vec;

            for (int i = 1; i < nSamples; i++)
            {
                var vect = kernel[i];
                vect.x *= strength.x;
                vect.y *= strength.y;
                vect.z *= strength.z;
                kernel[i] = vect;
            }
        }

        private Vector3 gaussian(float variance, float r, Vector3 falloff)
        {
            Vector3 g;

            float rr1 = r / (0.001f + falloff.x);
            g.x = Mathf.Exp((-(rr1 * rr1)) / (2.0f * variance)) / (2.0f * 3.14f * variance);

            float rr2 = r / (0.001f + falloff.y);
            g.y = Mathf.Exp((-(rr2 * rr2)) / (2.0f * variance)) / (2.0f * 3.14f * variance);

            float rr3 = r / (0.001f + falloff.z);
            g.z = Mathf.Exp((-(rr3 * rr3)) / (2.0f * variance)) / (2.0f * 3.14f * variance);

            return g;
        }
        private Vector3 profile(float r, Vector3 falloff)
        {
            return 0.100f * gaussian(0.0484f, r, falloff) +
                    0.118f * gaussian(0.187f, r, falloff) +
                    0.113f * gaussian(0.567f, r, falloff) +
                    0.358f * gaussian(1.99f, r, falloff) +
                    0.078f * gaussian(7.41f, r, falloff);
        }
    }
}