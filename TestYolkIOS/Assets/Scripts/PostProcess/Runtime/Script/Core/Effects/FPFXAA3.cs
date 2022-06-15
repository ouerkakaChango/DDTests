using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CenturyGame.PostProcess
{
    public class FPFXAA3 : IPostProcess
    {
        public override void Clear()
        {
            Enable = false;
            contrastThreshold = 0.0312f;
            relativeThreshold = 0.063f;
            pointScale = 1.0f;
            sharpness = 1.1f;
        }

        public override void Blend(IPostProcess other, float factor)
        {
            var o = other as FPFXAA3;
            if (!o) return;
            Enable |= o.Enable;
            contrastThreshold = Mathf.Lerp(contrastThreshold, o.contrastThreshold, factor);
            relativeThreshold = Mathf.Lerp(relativeThreshold, o.relativeThreshold, factor);
            pointScale = Mathf.Lerp(pointScale, o.pointScale, factor);
            sharpness = Mathf.Lerp(sharpness, o.sharpness, factor);
        }


        public enum LuminanceMode { Alpha, Green, Calculate }
        public enum Quality { Fast, Mid, Beatuty }

        public LuminanceMode luminanceSource = LuminanceMode.Green;
        const int luminancePass = 0;
        const int fxaaPass = 1;

        // Trims the algorithm from processing darks.
        //   0.0833 - upper limit (default, the start of visible unfiltered edges)
        //   0.0625 - high quality (faster)
        //   0.0312 - visible limit (slower)
        [EffectProperty]
        [Range(0.0312f, 0.0833f)]
        public float contrastThreshold = 0.0312f;
        // The minimum amount of local contrast required to apply algorithm.
        //   0.333 - too little (faster)
        //   0.250 - low quality
        //   0.166 - default
        //   0.125 - high quality 
        //   0.063 - overkill (slower)
        [EffectProperty]
        [Range(0.063f, 0.333f)]
        public float relativeThreshold = 0.063f;

        [EffectProperty]
        [Range(0.1f, 3.0f)]
        public float pointScale = 1.0f;

        [EffectProperty]
        [Range(1.0f, 2.0f)]
        public float sharpness = 1.1f;

        public bool isMobile = true;

        private float lastContrastThreshold = 0, lastRelativeThreshold = 0, lastSharpness = 0, lastPointScale = 0;
        private bool lastIsMobile = false;
        public override void Init()
        {
            Title = "FPFXAA3";
            Propertys = new string[] { "luminanceSource", "contrastThreshold", "relativeThreshold", "pointScale", "sharpness", "isMobile" };
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
            lastIsMobile = !isMobile;
            lastContrastThreshold = contrastThreshold - 0.1f;
            lastRelativeThreshold = relativeThreshold - 0.1f;
            lastPointScale = pointScale - 0.1f;
            lastSharpness = sharpness - 0.1f;
        }

        public override void Update()
        {
            if (null == m_curMat)
                return;
        }
        private Shader m_curShader;
        private Material m_curMat;

        void checkSupport()
        {
            if (m_curShader == null)
            {
                m_curShader = PostProcessHandle.LoadShader("Shaders/Post/FPFXAA3");
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

        public override void OnRenderHandle(ref RenderTexture source, ref RenderTexture destination, ref RenderTexture depth, ref int count)
        {
            if (m_curMat != null)
            {
                if (lastContrastThreshold != contrastThreshold)
                {
                    lastContrastThreshold = contrastThreshold;
                    m_curMat.SetFloat("_ContrastThreshold", contrastThreshold);
                }
                if (lastRelativeThreshold != relativeThreshold)
                {
                    lastRelativeThreshold = relativeThreshold;
                    m_curMat.SetFloat("_RelativeThreshold", relativeThreshold);
                }
                if (lastPointScale != pointScale)
                {
                    lastPointScale = pointScale;
                    m_curMat.SetFloat("_PointScale", pointScale);
                }

                if (lastSharpness != sharpness)
                {
                    lastSharpness = sharpness;
                    m_curMat.SetFloat("_Sharpness", sharpness);
                }

                if (lastIsMobile != isMobile)
                {
                    lastIsMobile = isMobile;
                    if (isMobile)
                    {
                        m_curMat.EnableKeyword("ISMOBILE");
                    }
                    else
                    {
                        m_curMat.DisableKeyword("ISMOBILE");
                    }
                }

                if (luminanceSource == LuminanceMode.Calculate)
                {
                    m_curMat.DisableKeyword("LUMINANCE_GREEN");
                    RenderTexture luminanceTex = RenderTexture.GetTemporary(
                        source.width, source.height, 0, RenderTextureFormat.ARGBHalf
                    );
                    Graphics.Blit(source, luminanceTex, m_curMat, luminancePass);
                    Graphics.Blit(luminanceTex, destination, m_curMat, fxaaPass);
                    RenderTexture.ReleaseTemporary(luminanceTex);
                }
                else
                {
                    if (luminanceSource == LuminanceMode.Green)
                    {
                        m_curMat.EnableKeyword("LUMINANCE_GREEN");
                    }
                    else
                    {
                        m_curMat.DisableKeyword("LUMINANCE_GREEN");
                    }
                    Graphics.Blit(source, destination, m_curMat, fxaaPass);
                }
            }
            base.OnRenderHandle(ref source, ref destination, ref depth, ref count);
        }
    }
}