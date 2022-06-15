using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;

namespace CenturyGame.PostProcess
{
    [RegisteredEffect]
    public class FPDepthOfField : IPostProcess
    {
        public override void Clear()
        {
            Enable = false;
            farDistance = 0.5f;
            nearDistance = 0.0f;
            blurScale = 50;
            offset = 1f;
        }

        public override void Blend(IPostProcess other, float factor)
        {
            var o = other as FPDepthOfField;
            if (!o) return;
            Enable |= o.Enable;
            farDistance = Mathf.Lerp(farDistance, o.farDistance, factor);
            nearDistance = Mathf.Lerp(nearDistance, o.nearDistance, factor);
            blurScale = Mathf.Lerp(blurScale, o.blurScale, factor);
            offset = Mathf.Lerp(offset, o.offset, factor);
        }

        [EffectProperty]
        [Range(0.0f, 100.0f)]
        public float farDistance = 0.5f;
        [EffectProperty]
        [Range(0.0f, 100.0f)]
        public float nearDistance = 0.0f;
        [EffectProperty]
        [Range(0.0f, 1000)]
        public float blurScale = 50;
        [EffectProperty]
        public float offset = 1f;
        private Shader m_curShader = null;
        private Material m_curMat;
        public GameObject target = null;
        public float aniTime = 1.3f;
        public float aniFromValue = 1.0f;
        public float aniToValue = 0.8f;
        private Renderer[] m_targetRenderer = null;
        private CommandBuffer cmd = null;
        private float m_aniValue = 1, m_lastAbs = 0, m_aniSpeed = 0;
        private CenturyGame.PostProcess.PostProcessHandle para;
        public override void Init()
        {
            Title = "FPDepthOfField";
            Propertys = new string[]
            {
            "farDistance",
            "nearDistance",
            "blurScale",
            "offset",
            "target",
            "aniTime",
            "aniFromValue",
            "aniToValue"
            };
            checkSupport();
        }

        public override void DoEnable(CenturyGame.PostProcess.PostProcessHandle cam)
        {
            para = cam;
            checkSupport();
            m_aniValue = aniFromValue;
            m_aniSpeed = (aniToValue - aniFromValue) / aniTime;
            m_lastAbs = Mathf.Abs(aniToValue - aniFromValue);
            if (target != null)
            {
                cmd = new CommandBuffer();
                cmd.name = "Dof";
                m_targetRenderer = target.GetComponentsInChildren<Renderer>();

                for (int i = 0; i < m_targetRenderer.Length; i++)
                {
                    Renderer r = m_targetRenderer[i];
                    cmd.DrawRenderer(r, r.sharedMaterial);
                }
                para.MainCamera.AddCommandBuffer(UnityEngine.Rendering.CameraEvent.BeforeImageEffects, cmd);
            }
        }

        public override void DoDisable()
        {
            if (m_curMat != null)
            {
                GameObject.DestroyImmediate(m_curMat);
            }
            if (target != null)
            {
                para.MainCamera.RemoveCommandBuffer(UnityEngine.Rendering.CameraEvent.BeforeImageEffects, cmd);
                if (cmd != null)
                {
                    cmd.Clear();
                    cmd.Release();
                }
            }
        }

        public override void OnRenderHandle(ref RenderTexture source, ref RenderTexture destination, ref RenderTexture depth, ref int count)
        {
            m_aniValue += m_aniSpeed * Time.deltaTime;
            float abs = Mathf.Abs(m_aniValue - aniToValue);
            if (abs < m_lastAbs)
            {
                m_lastAbs = abs;
            }
            else
            {
                m_aniValue = aniToValue;
            }
            if (m_curMat != null)
            {
                float far = FocalDistance01(farDistance);
                float near = FocalDistance01(nearDistance);
                //far = Mathf.Clamp(far, para.MainCamera.nearClipPlane, para.MainCamera.farClipPlane);
                //near = Mathf.Clamp(near, para.MainCamera.nearClipPlane, para.MainCamera.farClipPlane);
                m_curMat.SetVector("_parameter", new Vector4(far, near, blurScale, m_aniValue));
                RenderTexture temp1 = FPRenderTextureManager.Instance.Get(source.width, source.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default, FilterMode.Bilinear, TextureWrapMode.Clamp, "Dof_temp1");
                RenderTexture temp2 = FPRenderTextureManager.Instance.Get(source.width, source.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default, FilterMode.Bilinear, TextureWrapMode.Clamp, "Dof_temp2");

                Graphics.Blit(source, temp1, m_curMat, 2);
                m_curMat.SetVector("_offsets", new Vector4(0, offset, 0, 0));
                Graphics.Blit(temp1, temp2, m_curMat, 0);
                m_curMat.SetVector("_offsets", new Vector4(offset, 0, 0, 0));
                Graphics.Blit(temp2, temp1, m_curMat, 0);
                m_curMat.SetTexture("_BlurTex", temp1);

                Graphics.Blit(source, destination, m_curMat, 1);
                FPRenderTextureManager.Instance.Release(temp1);
                FPRenderTextureManager.Instance.Release(temp2);
            }
            base.OnRenderHandle(ref source, ref destination, ref depth, ref count);
        }

        void checkSupport()
        {
            if (m_curShader == null)
            {
                m_curShader = PostProcessHandle.LoadShader("Shaders/Post/FPDepthOfField");
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

        private float FocalDistance01(float distance)
        {
            return para.MainCamera.WorldToViewportPoint((distance - para.MainCamera.nearClipPlane) * para.MainCamera.transform.forward + para.MainCamera.transform.position).z / (para.MainCamera.farClipPlane - para.MainCamera.nearClipPlane);
        }
    }
}