using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CenturyGame.PostProcess
{
    [RegisteredEffect]
    public class FPBloom : IPostProcess
    {
        public override void Clear()
        {
            Enable = false;
            threshold = 1.0f;
            softKnee = 0.5f;
            radius = 7.0f;
            intensity = 0.0f;
            bloomColor = Color.white;
        }

        public override void Blend(IPostProcess other, float factor)
        {
            var o = other as FPBloom;
            if (!o) return;
            Enable |= o.Enable;
            threshold = Mathf.Lerp(threshold, o.threshold, factor);
            softKnee = Mathf.Lerp(softKnee, o.softKnee, factor);
            radius = Mathf.Lerp(radius, o.radius, factor);
            intensity = Mathf.Lerp(intensity, o.intensity, factor);
            bloomColor = Color.Lerp(bloomColor, o.bloomColor, factor);
        }

        public override void Init()
        {
            Title = "FPBloom";
            Propertys = new string[] { "threshold", "softKnee", "radius", "intensity", "bloomColor" };
            checkSupport();
        }

        public override void DoEnable(CenturyGame.PostProcess.PostProcessHandle cam)
        {
            checkSupport();

            rtformat = RenderTextureFormat.Default;
            if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RGB111110Float))
            {
                rtformat = RenderTextureFormat.RGB111110Float;
            }

            m_threshold = 0.0f;
            m_softKnee = 0.0f;
            m_radius = 0.0f;
            m_intensity = 0.0f;
        }

        public override void ReSize(Resolution size)
        {

        }

        public override void DoDisable()
        {
            if (m_curMat != null)
            {
                GameObject.DestroyImmediate(m_curMat);
            }
        }

        public void AddRenderer(Renderer renderer)
        {

        }

        public void RemoveRenderer(Renderer renderer)
        {

        }

        public override void OnRenderHandle(ref RenderTexture source, ref RenderTexture destination, ref RenderTexture depth, ref int count)
        {
            if (m_curMat == null)
            {
                return;
            }
            width = source.width / 2;
            height = source.height / 2;

            if (radius != m_radius)
            {
                int s = Mathf.Max(width, height);
                float logh = Mathf.Log(s, 2) + Mathf.Min(radius, 10f) - 10f;
                int nLogh = Mathf.FloorToInt(logh);
                m_iterations = Mathf.Clamp(nLogh, 1, m_maxIteration);
                m_curMat.SetFloat("_SampleScale", 0.5f + logh - nLogh);
                m_radius = radius;
            }

            for (int i = 0; i < m_iterations; ++i)
            {
                var down = RenderTexture.GetTemporary(width, height, 0, rtformat, RenderTextureReadWrite.Default);
                down.filterMode = FilterMode.Bilinear;
                down.wrapMode = TextureWrapMode.Clamp;
                m_burBuffer1[i] = down;

                var up = RenderTexture.GetTemporary(width, height, 0, rtformat, RenderTextureReadWrite.Default);
                up.filterMode = FilterMode.Bilinear;
                up.wrapMode = TextureWrapMode.Clamp;
                m_burBuffer2[i] = up;

                width = Mathf.Max(width / 2, 1);
                height = Mathf.Max(height / 2, 1);
            }

            var _last = source;
            for (int i = 0; i < m_iterations; ++i)
            {
                var down = m_burBuffer1[i];
                int pass = i == 0 ? 0 : 1;
                Graphics.Blit(_last, down, m_curMat, pass);
                _last = down;
            }

            _last = m_burBuffer1[m_iterations - 1];
            for (int i = m_iterations - 2; i >= 0; --i)
            {
                var down = m_burBuffer1[i];
                var up = m_burBuffer2[i];
                m_curMat.SetTexture(baseTexID, down);
                Graphics.Blit(_last, up, m_curMat, 2);
                _last = up;
            }

            m_curMat.SetColor(ID_BloomColor, bloomColor);
            m_curMat.SetTexture(baseTexID, source);
            Graphics.Blit(_last, destination, m_curMat, 3);

            for (int i = 0; i < m_iterations; ++i)
            {
                RenderTexture.ReleaseTemporary(m_burBuffer1[i]);
                RenderTexture.ReleaseTemporary(m_burBuffer2[i]);

                m_burBuffer1[i] = null;
                m_burBuffer2[i] = null;
            }

            base.OnRenderHandle(ref source, ref destination, ref depth, ref count);
        }

        public override void Update()
        {
            if (null == m_curMat)
            {
                return;
            }

            if (threshold != m_threshold || softKnee != m_softKnee)
            {
                float linearThreshold = threshold;
                //m_curMat.SetFloat("_Threshold", linearThreshold);
                float knee = linearThreshold * softKnee + 1e-5f;
                Vector4 curve = new Vector4(linearThreshold - knee, knee * 2f, 0.25f / knee, linearThreshold);
                m_curMat.SetVector("_Curve", curve);
                m_threshold = threshold;
                m_softKnee = softKnee;
            }
            if (intensity != m_intensity)
            {
                float tempIntensity = Mathf.Exp(intensity * 0.69314718055994530941723212145818f) - 1f;
                m_curMat.SetFloat("_Intensity", tempIntensity);
                m_intensity = intensity;
            }
        }
        int width = 0, height = 0;
        private List<Renderer> m_renderList = null;
        private CommandBuffer m_FxHdrCmdColor = null, m_FxHdrCmdRender = null;
        private static readonly string baseTexID = "_BaseTex";
        private readonly int ID_BloomColor = Shader.PropertyToID("_BloomColor");
        private RenderTextureFormat rtformat;

        void checkSupport()
        {

            if (m_curShader == null)
            {
                m_curShader = PostProcessHandle.LoadShader("Shaders/Post/FPBloom");
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
                m_threshold = m_softKnee = m_radius = m_intensity = 0.0f;
                m_burBuffer1 = new RenderTexture[m_maxIteration];
                m_burBuffer2 = new RenderTexture[m_maxIteration];
            }
        }

        private Shader m_curShader;
        private Material m_curMat;

        [EffectProperty]
        public float threshold = 1.0f;
        private float m_threshold = 0.0f;

        [EffectProperty]
        [Range(0.0f, 1.0f)]
        public float softKnee = 0.5f;
        private float m_softKnee = 0.0f;

        [EffectProperty]
        [Range(1.0f, 10.0f)]
        public float radius = 7f;
        private float m_radius = 0.0f;

        [EffectProperty]
        public float intensity = 0.0f;
        private float m_intensity = 0.0f;

        [EffectProperty]
        public Color bloomColor = Color.white;

        private int m_iterations;
        private const int m_maxIteration = 10;

        RenderTexture[] m_burBuffer1 = new RenderTexture[m_maxIteration];
        RenderTexture[] m_burBuffer2 = new RenderTexture[m_maxIteration];
    }
}