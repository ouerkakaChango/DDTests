using UnityEngine;
using System.Collections;

namespace CenturyGame.PostProcess
{
    [RegisteredEffect]
    public class FPRadialBlur : IPostProcess
    {
        public override void Clear()
        {
            Enable = false;
            blurStrength = 0;
            sampleStrength = 0;
        }

        public override void Blend(IPostProcess other, float factor)
        {
            var o = other as FPRadialBlur;
            if (!o) return;
            Enable |= o.Enable;
            blurStrength = Mathf.Lerp(blurStrength, o.blurStrength, factor);
            sampleStrength = Mathf.Lerp(sampleStrength, o.sampleStrength, factor);
        }

        public override void Init()
        {
            Title = "FPRadialBlur";
            //这里是需要暴露到编辑器面板的属性名称
            Propertys = new string[] { "blurStrength", "sampleStrength" };
            checkSupport();
        }

        public override void DoEnable(CenturyGame.PostProcess.PostProcessHandle cam)
        {
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
            if (m_curMat == null)
            {
                Graphics.Blit(source, null as RenderTexture);
            }
            else
            {
                //Graphics.Blit(source, destination, m_curMat);
                Graphics.Blit(source, destination, m_curMat);
                //Graphics.Blit(destination, null as RenderTexture);
                //Graphics.Blit(source, null as RenderTexture, m_curMat);
            }
            base.OnRenderHandle(ref source, ref destination, ref depth, ref count);
        }

        public override void Update()
        {
            if (m_curMat != null && (m_blurStrength != blurStrength || m_sampleStrength != sampleStrength))
            {
                m_blurStrength = blurStrength;
                m_sampleStrength = sampleStrength;
                m_curMat.SetFloat("_BlurStrength", blurStrength);
                m_curMat.SetFloat("_SampleStrength", sampleStrength);
            }
        }

        private Shader m_curShader;
        private Material m_curMat;
        [EffectProperty]
        public float blurStrength = 0.12f;
        [EffectProperty]
        public float sampleStrength = 3.0f;
        private float m_blurStrength = 0.0f, m_sampleStrength = 0.0f;

        void checkSupport()
        {
            if (m_curShader == null)
            {
                m_curShader = PostProcessHandle.LoadShader("Shaders/Post/FPRadialBlur");
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
                m_blurStrength = m_sampleStrength = 0.0f;
            }
        }
    }
}