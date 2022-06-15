using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CenturyGame.PostProcess
{
    [RegisteredEffect]
    public class FPGaussianBlur : IPostProcess
    {
        public override void Clear()
        {
            Enable = false;
            iterationCount = 0;
            sampleScale = 0;
        }

        public override void Blend(IPostProcess other, float factor)
        {
            var o = other as FPGaussianBlur;
            if (!o) return;
            Enable |= o.Enable;
            iterationCount = (int)Mathf.Lerp((float)iterationCount, (float)o.iterationCount, factor);
            sampleScale = Mathf.Lerp(sampleScale, o.sampleScale, factor);
        }

        Shader m_curShader;
        Material m_curMat;
        RenderTextureFormat rtformat;

        [EffectProperty]
        [Range(0, MaxIterationCount)]
        public int iterationCount = 2;
        int m_iterationCount;
        public const int MaxIterationCount = 8;

        [EffectProperty]
        public float sampleScale = 2;
        float m_sampleScale;

        RenderTexture[] m_blurBuffers = new RenderTexture[MaxIterationCount + 1];

        public override void Init()
        {
            Title = "FPGaussianBlur";
            Propertys = new string[] { "iterationCount", "sampleScale" };
            checkSupport();
        }

        public override void DoEnable(CenturyGame.PostProcess.PostProcessHandle parameter)
        {
            checkSupport();

            rtformat = RenderTextureFormat.Default;
            if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RGB111110Float))
            {
                rtformat = RenderTextureFormat.RGB111110Float;
            }
        }

        public override void DoDisable()
        {
            if (m_curMat != null)
                GameObject.DestroyImmediate(m_curMat);
            m_curMat = null;
        }

        public override void OnRenderHandle(ref RenderTexture source, ref RenderTexture destination, ref RenderTexture depth, ref int count)
        {
            if (m_curMat == null)
                return;

            int width = source.width / 2;
            int height = source.height / 2;

            int iteration = (int)(Mathf.Log(width, 2));
            iteration = Mathf.Clamp(iteration, 0, Mathf.Min(m_iterationCount, MaxIterationCount));

            if (iteration == 0)
                Graphics.Blit(source, destination, m_curMat, 2);
            else
            {
                for (int i = 1; i <= iteration; ++i)
                {
                    m_blurBuffers[i] = FPRenderTextureManager.Instance.Get(width, height, 0, rtformat);
                    width /= 2;
                    height /= 2;
                }

                m_blurBuffers[0] = source;
                for (int i = 1; i <= iteration; ++i)
                {
                    Graphics.Blit(m_blurBuffers[i - 1], m_blurBuffers[i], m_curMat, 0);
                }

                m_blurBuffers[0] = destination;
                for (int i = iteration - 1; i >= 0; --i)
                {
                    Graphics.Blit(m_blurBuffers[i + 1], m_blurBuffers[i], m_curMat, 1);
                }

                m_blurBuffers[0] = null;
                for (int i = 1; i <= iteration; ++i)
                {
                    FPRenderTextureManager.Instance.Release(m_blurBuffers[i]);
                    m_blurBuffers[i] = null;
                }
            }

            base.OnRenderHandle(ref source, ref destination, ref depth, ref count);
        }

        public override void Update()
        {
            if (m_curMat == null)
                return;

            if (m_sampleScale != sampleScale)
            {
                sampleScale = Mathf.Max(sampleScale, 0);
                m_sampleScale = sampleScale;
                m_curMat.SetFloat("_SampleScale", m_sampleScale);
            }

            if (m_iterationCount != iterationCount)
            {
                iterationCount = Mathf.Clamp(iterationCount, 0, MaxIterationCount);
                m_iterationCount = iterationCount;
            }
        }

        void checkSupport()
        {
            if (m_curShader == null)
                m_curShader = PostProcessHandle.LoadShader("Shaders/Post/FPGaussianBlur");

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
    }
}