using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;
using System.Collections.Generic;
namespace CenturyGame.PostProcess
{
    public class FPToneMapping : IPostProcess
    {
        public float Lum = 1.0f;
        public bool UseSharpness = false;
        public float Sharpness = 0.2f;
        private float lastLum = 0.0f, lastSharpness = 0.0f;
        private bool lastUseSharpness = false;
        public override void Init()
        {
            Title = "FPToneMapping";
            //这里是需要暴露到编辑器面板的属性名称
            Propertys = new string[] { "Type", "Lum", "UseSharpness", "Sharpness" };
            checkSupport();
        }

        public override void DoEnable(CenturyGame.PostProcess.PostProcessHandle cam)
        {
            checkSupport();
            lastSharpness = Sharpness - 1;
            lastLum = Lum - 1;
            lastUseSharpness = UseSharpness;
            if (Type == ToneMapping.ACES)
            {
                lastType = ToneMapping.HABLE;
            }
            else
            {
                lastType = ToneMapping.ACES;
            }
        }
        public enum ToneMapping
        {
            ACES,
            HABLE
        }
        public ToneMapping Type = ToneMapping.ACES;
        private ToneMapping lastType = ToneMapping.HABLE;
        public override void DoDisable()
        {
            if (m_curMat != null)
            {
#if UNITY_EDITOR
                DestroyImmediate(m_curMat, true);
#else
            Destroy(m_curMat);
#endif
                m_curMat = null;
            }
        }

        public override void OnRenderHandle(ref RenderTexture source, ref RenderTexture destination, ref RenderTexture depth, ref int count)
        {
            if (m_curMat != null)
            {
                Graphics.Blit(source, destination, m_curMat, 0);
            }
            base.OnRenderHandle(ref source, ref destination, ref depth, ref count);
        }

        public override void Update()
        {
            if (lastType != Type)
            {
                lastType = Type;
                if (Type == ToneMapping.ACES)
                {
                    m_curMat.EnableKeyword("ACES");
                    m_curMat.DisableKeyword("HABLE");
                }
                else
                {
                    m_curMat.DisableKeyword("ACES");
                    m_curMat.EnableKeyword("HABLE");
                }
            }

            if (lastLum != Lum)
            {
                lastLum = Lum;
                m_curMat.SetFloat("_Lum", lastLum);
            }

            if (lastSharpness != Sharpness)
            {
                lastSharpness = Sharpness;
                m_curMat.SetFloat("_CentralFactor", 1.0f + (3.2f * lastSharpness));
                m_curMat.SetFloat("_SideFactor", 0.8f * lastSharpness);
            }
            if (lastUseSharpness != UseSharpness)
            {
                lastUseSharpness = UseSharpness;
                if (lastUseSharpness)
                {
                    m_curMat.EnableKeyword("SHARPEN_ON");
                }
                else
                {
                    m_curMat.DisableKeyword("SHARPEN_ON");
                }
            }
        }

        void checkSupport()
        {
            if (m_curShader == null)
            {
                m_curShader = CenturyGame.PostProcess.PostProcessHandle.LoadShader("Shaders/Post/FPTonemapping");
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
        private Shader m_curShader;
        private Material m_curMat;
    }
}