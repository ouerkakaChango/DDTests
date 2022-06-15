using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CenturyGame.PostProcess
{
    [RegisteredEffect]
    public class FPHBAO : IPostProcess
    {
        public override void Clear()
        {
            Enable = false;
            DownSample = 1;
            RayMarchingDirectionCount = 3;
            RayMarchingStepCount = 2;
            RayMarchingRadius = 0;
            MaxPixelRadius = 128;
            AngleBiasValue = 0;
            Strength = 0;
            MaxDistance = 150;
            DistanceFalloff = 50;
            BlurRadius = 2.0f;
        }

        public override void Blend(IPostProcess other, float factor)
        {
            var o = other as FPHBAO;
            if (!o) return;
            Enable |= o.Enable;
            DownSample = (int)Mathf.Lerp(DownSample, o.DownSample, factor);
            RayMarchingDirectionCount = (int)Mathf.Lerp(RayMarchingDirectionCount, o.RayMarchingDirectionCount, factor);
            RayMarchingStepCount = (int)Mathf.Lerp(RayMarchingStepCount, o.RayMarchingStepCount, factor);
            RayMarchingRadius = Mathf.Lerp(RayMarchingRadius, o.RayMarchingRadius, factor);
            MaxPixelRadius = (int)Mathf.Lerp(MaxPixelRadius, o.MaxPixelRadius, factor);
            AngleBiasValue = Mathf.Lerp(AngleBiasValue, o.AngleBiasValue, factor);
            Strength = Mathf.Lerp(Strength, o.Strength, factor);
            MaxDistance = Mathf.Lerp(MaxDistance, o.MaxDistance, factor);
            DistanceFalloff = Mathf.Lerp(DistanceFalloff, o.DistanceFalloff, factor);
            BlurRadius = Mathf.Lerp(BlurRadius, o.BlurRadius, factor);
        }

        //水平基准环境光遮蔽HBAO+

        public override void Init()
        {
            Title = "FPHBAO";
            Propertys = new string[] { "DownSample", "RayMarchingDirectionCount", "RayMarchingStepCount",
            "RayMarchingRadius", "MaxPixelRadius", "AngleBiasValue", "Strength",
            "MaxDistance", "DistanceFalloff",
            "BlurRadius"
            };
            checkSupport();
        }
        private CenturyGame.PostProcess.PostProcessHandle para;
        private Camera mainCamera;
        public override void DoEnable(CenturyGame.PostProcess.PostProcessHandle cam)
        {
            para = cam;
            mainCamera = cam.MainCamera;
            checkSupport();

            if (Enable == false)
                return;

            noiseTex = CreateRandomTexture();
            BuildCommandBuffer();
        }

        [EffectProperty]
        [Range(0, 4)]
        public int DownSample = 2;
        [EffectProperty]
        [Range(3, 8)]
        public int RayMarchingDirectionCount = 3;
        [EffectProperty]
        [Range(2, 6)]
        public int RayMarchingStepCount = 2;
        [EffectProperty]
        [Range(0, 2)]
        public float RayMarchingRadius = 0.8f;
        [EffectProperty]
        [Range(64, 512)]
        public int MaxPixelRadius = 128;
        [EffectProperty]
        [Range(0, 0.5f)]
        public float AngleBiasValue = 0.05f;
        [EffectProperty]
        [Range(0, 10)]
        public float Strength = 1;

        [EffectProperty]
        public float MaxDistance = 150;
        [EffectProperty]
        public float DistanceFalloff = 50;
        [EffectProperty]
        [Range(0, 3)]
        public float BlurRadius = 1.5f;

        Texture2D noiseTex;
        CommandBuffer m_commandBuffer;
        int lastDownSample;

        public override void DoDisable()
        {
            if (m_curMat != null)
                DestroyImmediate(m_curMat);
            if (noiseTex != null)
                DestroyImmediate(noiseTex);
            DisposeCommandBuffer();
        }

        public override void Update()
        {
            if (null == m_curMat)
                return;

#if UNITY_EDITOR
            UpdateMaterial();

            if (DownSample != lastDownSample)
            {
                DisposeCommandBuffer();
                BuildCommandBuffer();
            }
#endif
        }
        private Shader m_curShader;
        private Material m_curMat;

        void checkSupport()
        {
            if (m_curShader == null)
            {
                m_curShader = PostProcessHandle.LoadShader("Shaders/Post/FPHBAO");
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

        public override void ReSize(Resolution size)
        {
            DisposeCommandBuffer();
            BuildCommandBuffer();
        }

        void BuildCommandBuffer()
        {
            if (m_curMat == null)
                return;

            m_commandBuffer = new CommandBuffer();
            m_commandBuffer.name = "HBAO Pass";

            int sourceWidth = para.PostResolution.width;
            int sourceHeight = para.PostResolution.height;

            m_curMat.SetFloat("_Intensity", Mathf.Max(Strength, 0.001f));
            m_curMat.SetFloat("_MaxPixelRadius", MaxPixelRadius);
            m_curMat.SetInt("_RayMarchingStepCount", RayMarchingStepCount);
            m_curMat.SetInt("_RayMarchingDirectionCount", RayMarchingDirectionCount);
            m_curMat.SetFloat("_AngleBiasValue", AngleBiasValue);
            m_curMat.SetFloat("_AOmultiplier", 2.0f * (1.0f / (1.0f - AngleBiasValue)));
            m_curMat.SetFloat("_MaxDistance", Mathf.Max(MaxDistance, 0.01f));
            m_curMat.SetFloat("_DistanceFalloff", Mathf.Max(DistanceFalloff, 0.01f));

            float tanHalfFovY = Mathf.Tan(0.5f * mainCamera.fieldOfView * Mathf.Deg2Rad);
            float invFocalLenX = 1.0f / (1.0f / tanHalfFovY * (sourceHeight / (float)sourceWidth));
            float invFocalLenY = 1.0f / (1.0f / tanHalfFovY);
            m_curMat.SetVector("_UVToView", new Vector4(2.0f * invFocalLenX, -2.0f * invFocalLenY, -1.0f * invFocalLenX, 1.0f * invFocalLenY));

            m_curMat.SetFloat("_Radius", RayMarchingRadius * 0.5f * (sourceHeight / (tanHalfFovY * 2.0f)));
            m_curMat.SetFloat("_NegInvRadius2", -1 / (RayMarchingRadius * RayMarchingRadius));
            m_curMat.SetTexture("_NoiseTex", noiseTex);
            m_curMat.SetVector("_TargetScale", new Vector4((sourceWidth + 0.5f) / sourceWidth, (sourceHeight + 0.5f) / sourceHeight, 1f, 1f));
            m_curMat.SetVector("_Offset_X", new Vector2(1, 0) * BlurRadius);
            m_curMat.SetVector("_Offset_Y", new Vector2(0, 1) * BlurRadius * 0.5f);

            int rtWidth = sourceWidth >> DownSample;
            int rtHeight = sourceHeight >> DownSample;

            int aoRT = Shader.PropertyToID("HBAO RT");
            int blurRT = Shader.PropertyToID("HBAO BLUR RT");

            m_commandBuffer.GetTemporaryRT(aoRT, rtWidth, rtHeight, 0, FilterMode.Bilinear);
            m_commandBuffer.GetTemporaryRT(blurRT, rtWidth >> 1, rtHeight >> 1, 0, FilterMode.Bilinear);

            m_commandBuffer.Blit(para.ColorTex, aoRT, m_curMat, 0);

            m_commandBuffer.Blit(aoRT, blurRT, m_curMat, 4);

            m_commandBuffer.Blit(blurRT, aoRT, m_curMat, 5);

            m_commandBuffer.SetGlobalTexture("_AOTex", aoRT);
            m_commandBuffer.Blit(null, para.ColorTex, m_curMat, 1);

            m_commandBuffer.ReleaseTemporaryRT(aoRT);
            m_commandBuffer.ReleaseTemporaryRT(blurRT);

            mainCamera.AddCommandBuffer(CameraEvent.AfterImageEffectsOpaque, m_commandBuffer);

            lastDownSample = DownSample;
        }

        void DisposeCommandBuffer()
        {
            if (m_commandBuffer != null)
            {
                mainCamera.RemoveCommandBuffer(CameraEvent.AfterImageEffectsOpaque, m_commandBuffer);
                m_commandBuffer.Dispose();
                m_commandBuffer = null;
            }
        }

        void UpdateMaterial()
        {
            int sourceWidth = para.PostResolution.width;
            int sourceHeight = para.PostResolution.height;

            m_curMat.SetFloat("_Intensity", Mathf.Max(Strength, 0.001f));
            m_curMat.SetFloat("_MaxPixelRadius", MaxPixelRadius);
            m_curMat.SetInt("_RayMarchingStepCount", RayMarchingStepCount);
            m_curMat.SetInt("_RayMarchingDirectionCount", RayMarchingDirectionCount);
            m_curMat.SetFloat("_AngleBiasValue", AngleBiasValue);
            m_curMat.SetFloat("_AOmultiplier", 2.0f * (1.0f / (1.0f - AngleBiasValue)));
            m_curMat.SetFloat("_MaxDistance", Mathf.Max(MaxDistance, 0.01f));
            m_curMat.SetFloat("_DistanceFalloff", Mathf.Max(DistanceFalloff, 0.01f));

            float tanHalfFovY = Mathf.Tan(0.5f * mainCamera.fieldOfView * Mathf.Deg2Rad);
            float invFocalLenX = 1.0f / (1.0f / tanHalfFovY * (sourceHeight / (float)sourceWidth));
            float invFocalLenY = 1.0f / (1.0f / tanHalfFovY);
            m_curMat.SetVector("_UVToView", new Vector4(2.0f * invFocalLenX, -2.0f * invFocalLenY, -1.0f * invFocalLenX, 1.0f * invFocalLenY));

            m_curMat.SetFloat("_Radius", RayMarchingRadius * 0.5f * (sourceHeight / (tanHalfFovY * 2.0f)));
            m_curMat.SetFloat("_NegInvRadius2", -1 / (RayMarchingRadius * RayMarchingRadius));
            m_curMat.SetTexture("_NoiseTex", noiseTex);
            m_curMat.SetVector("_TargetScale", new Vector4((sourceWidth + 0.5f) / sourceWidth, (sourceHeight + 0.5f) / sourceHeight, 1f, 1f));
            m_curMat.SetVector("_Offset_X", new Vector2(1, 0) * BlurRadius);
            m_curMat.SetVector("_Offset_Y", new Vector2(0, 1) * BlurRadius * 0.5f);
        }

        public override void OnRenderHandle(ref RenderTexture source, ref RenderTexture destination, ref RenderTexture depth, ref int count)
        {

        }

        private Texture2D CreateRandomTexture()
        {
            float[] MersenneTwisterNumbers = new float[] {
            0.463937f,0.340042f,0.223035f,0.468465f,0.322224f,0.979269f,0.031798f,0.973392f,0.778313f,0.456168f,0.258593f,0.330083f,0.387332f,0.380117f,0.179842f,0.910755f,
            0.511623f,0.092933f,0.180794f,0.620153f,0.101348f,0.556342f,0.642479f,0.442008f,0.215115f,0.475218f,0.157357f,0.568868f,0.501241f,0.629229f,0.699218f,0.707733f
        };

            int size = 4;
            var noiseTex = new Texture2D(size, size, TextureFormat.RGB24, false, true);
            noiseTex.filterMode = FilterMode.Point;
            noiseTex.wrapMode = TextureWrapMode.Repeat;
            int z = 0;
            for (int x = 0; x < size; ++x)
            {
                for (int y = 0; y < size; ++y)
                {
                    float r1 = MersenneTwisterNumbers[z++];
                    float r2 = MersenneTwisterNumbers[z++];
                    float angle = 2.0f * Mathf.PI * r1 / RayMarchingDirectionCount;
                    Color color = new Color(Mathf.Cos(angle), Mathf.Sin(angle), r2);
                    noiseTex.SetPixel(x, y, color);
                }
            }
            noiseTex.Apply();

            return noiseTex;
        }
    }
}