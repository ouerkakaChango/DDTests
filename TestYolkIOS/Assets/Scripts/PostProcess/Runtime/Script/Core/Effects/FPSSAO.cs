using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CenturyGame.PostProcess
{
    public class FPSSAO : IPostProcess
    {
        public override void Init()
        {
            Title = "FPHBAO";
            Propertys = new string[] { "MaxDistance", "Strength", "SampleCount", "DepthBias",
            "Radius", "MaxPixelRadius", "RayMarchingRadius", "RayMarchingStepCount", "RayMarchingDirectionCount", "AngleBiasValue",
            "DistanceFalloff"};
            checkSupport();
        }
        private Camera mainCamera;
        public override void DoEnable(CenturyGame.PostProcess.PostProcessHandle cam)
        {
            mainCamera = cam.MainCamera;
            checkSupport();

            if (Enable == false)
                return;

            GenerateAOSampleKernel();
            noiseTex = CreateRandomTexture();
        }
        public float MaxDistance = 150;
        [Range(0, 10)]
        public float Strength = 1;
        public int SampleCount = 8;
        [Range(0, 0.002f)]
        public float DepthBias = 0.002f;

        [Range(0, 2)]
        public float Radius = 0.8f;
        [Range(64, 512)]
        public int MaxPixelRadius = 128;
        [Range(0, 2)]
        public float RayMarchingRadius = 0.8f;
        [Range(2, 6)]
        public int RayMarchingStepCount = 6;
        [Range(3, 8)]
        public int RayMarchingDirectionCount = 8;
        [Range(0, 0.5f)]
        public float AngleBiasValue = 0.05f;
        public float DistanceFalloff = 50;

        Vector4[] sampleKernels;
        const int MAX_SAMPLECOUNT = 32;

        Vector4[] noises;
        Texture2D noiseTex;

        public override void DoDisable()
        {
            if (m_curMat != null)
            {
                GameObject.DestroyImmediate(m_curMat);
            }
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
                m_curShader = CenturyGame.PostProcess.PostProcessHandle.LoadShader("Shaders/Post/FPHBAO");
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
                float tanHalfFOV = Mathf.Tan(0.5f * mainCamera.fieldOfView * Mathf.Deg2Rad);
                float halfHeight = tanHalfFOV * mainCamera.nearClipPlane;
                float halfWidth = halfHeight * mainCamera.aspect;
                Vector3 toTop = mainCamera.transform.up * halfHeight;
                Vector3 toRight = mainCamera.transform.right * halfWidth;
                Vector3 forward = mainCamera.transform.forward * mainCamera.nearClipPlane;
                Vector3 toTopLeft = forward + toTop - toRight;
                Vector3 toBottomLeft = forward - toTop - toRight;
                Vector3 toTopRight = forward + toTop + toRight;
                Vector3 toBottomRight = forward - toTop + toRight;

                toTopLeft /= mainCamera.nearClipPlane;
                toBottomLeft /= mainCamera.nearClipPlane;
                toTopRight /= mainCamera.nearClipPlane;
                toBottomRight /= mainCamera.nearClipPlane;

                Matrix4x4 frustumDir = Matrix4x4.identity;
                frustumDir.SetRow(0, toBottomLeft);
                frustumDir.SetRow(1, toBottomRight);
                frustumDir.SetRow(2, toTopLeft);
                frustumDir.SetRow(3, toTopRight);
                m_curMat.SetMatrix("_InverseProjectionMatrix", mainCamera.projectionMatrix.inverse);
                m_curMat.SetMatrix("_InverseViewMatrix", mainCamera.cameraToWorldMatrix);
                m_curMat.SetMatrix("_FrustumDir", frustumDir);
                m_curMat.SetVector("_Params", new Vector4(FocalDistance01(MaxDistance), Mathf.Max(Strength, 0.001f), 1, 1));
                m_curMat.SetFloat("_AORadius", Mathf.Max(Radius, 0.001f));
                m_curMat.SetInt("_SampleCount", Mathf.Clamp(SampleCount, 1, MAX_SAMPLECOUNT));
                m_curMat.SetVectorArray("_SampleKernelArray", sampleKernels);
                m_curMat.SetFloat("_DepthBiasValue", DepthBias);
                m_curMat.SetVectorArray("_NoiseArray", noises);
                m_curMat.SetFloat("_MaxPixelRadius", MaxPixelRadius);
                m_curMat.SetFloat("_RayMarchingRadius", RayMarchingRadius);
                m_curMat.SetInt("_RayMarchingStepCount", RayMarchingStepCount);
                m_curMat.SetInt("_RayMarchingDirectionCount", RayMarchingDirectionCount);
                m_curMat.SetFloat("_AngleBiasValue", AngleBiasValue);
                m_curMat.SetFloat("_AOmultiplier", 2.0f * (1.0f / (1.0f - AngleBiasValue)));
                m_curMat.SetFloat("_MaxDistance", MaxDistance);
                m_curMat.SetFloat("_DistanceFalloff", DistanceFalloff);

                float tanHalfFovY = Mathf.Tan(0.5f * mainCamera.fieldOfView * Mathf.Deg2Rad);
                float invFocalLenX = 1.0f / (1.0f / tanHalfFovY * (source.height / (float)source.width));
                float invFocalLenY = 1.0f / (1.0f / tanHalfFovY);
                m_curMat.SetVector("_UVToView", new Vector4(2.0f * invFocalLenX, -2.0f * invFocalLenY, -1.0f * invFocalLenX, 1.0f * invFocalLenY));

                m_curMat.SetFloat("_Radius", RayMarchingRadius * 0.5f * (source.height / (tanHalfFovY * 2.0f)));
                m_curMat.SetFloat("_NegInvRadius2", -1 / (RayMarchingRadius * RayMarchingRadius));
                m_curMat.SetTexture("_NoiseTex", noiseTex);

                Graphics.Blit(source, destination, m_curMat);
            }
            base.OnRenderHandle(ref source, ref destination, ref depth, ref count);
        }
        private float FocalDistance01(float distance)
        {
            return distance / (mainCamera.farClipPlane - mainCamera.nearClipPlane);
            //return mainCamera.WorldToViewportPoint((distance - mainCamera.nearClipPlane) * mainCamera.transform.forward + mainCamera.transform.position).z / (mainCamera.farClipPlane - mainCamera.nearClipPlane);
        }

        private void GenerateAOSampleKernel()
        {
            sampleKernels = new Vector4[32];

            List<Vector4> pointList = new List<Vector4>();
            List<Vector4> randomPointList = new List<Vector4>();
            for (int i = 0; i < MAX_SAMPLECOUNT; i++)
            {
                var vec = new Vector3(Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f), Random.Range(0.0f, 1.0f));
                vec.Normalize();

                var scale = (float)i / MAX_SAMPLECOUNT;
                //使分布符合二次方程的曲线
                scale = Mathf.Lerp(0.01f, 1.0f, scale * scale);
                vec *= scale;
                pointList.Add(vec);
            }

            var random = new System.Random();
            foreach (var p in pointList)
            {
                randomPointList.Insert(random.Next(randomPointList.Count + 1), p);
            }

            sampleKernels = randomPointList.ToArray();

            noises = new Vector4[16];
            for (int i = 0; i < 16; ++i)
            {
                var noise = new Vector3(Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f));
                noise.Normalize();
                noises[i] = noise;
            }
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