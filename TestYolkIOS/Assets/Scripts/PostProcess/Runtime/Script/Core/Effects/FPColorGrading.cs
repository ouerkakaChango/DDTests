using System;
using UnityEngine;

namespace CenturyGame.PostProcess
{
    [RegisteredEffect]
    public class FPColorGrading : IPostProcess
    {
        private static AnimationCurve defaultMasterCurve = new AnimationCurve(new Keyframe(0f, 0f, 1f, 1f), new Keyframe(1f, 1f, 1f, 1f));
        private static AnimationCurve defaultredCurve = new AnimationCurve(new Keyframe(0f, 0f, 1f, 1f), new Keyframe(1f, 1f, 1f, 1f));
        private static AnimationCurve defaultgreenCurve = new AnimationCurve(new Keyframe(0f, 0f, 1f, 1f), new Keyframe(1f, 1f, 1f, 1f));
        private static AnimationCurve defaultblueCurve = new AnimationCurve(new Keyframe(0f, 0f, 1f, 1f), new Keyframe(1f, 1f, 1f, 1f));
        private static AnimationCurve defaulthueVsHueCurve = new AnimationCurve();
        private static AnimationCurve defaulthueVsSatCurve = new AnimationCurve();
        private static AnimationCurve defaultsatVsSatCurve = new AnimationCurve();
        private static AnimationCurve defaultlumVsSatCurve = new AnimationCurve();

        public override void Clear()
        {
            Enable = false;
            gradingMode = GradingMode.LDR;
            tonemapper = Tonemapper.None;
            ldrLut = null;
            ldrLutContribution = 1;
            toneCurveToeStrength = 0;
            toneCurveToeLength = 0.5f;
            toneCurveShoulderStrength = 0;
            toneCurveShoulderLength = 0.5f;
            toneCurveShoulderAngle = 0;
            toneCurveGamma = 1;
            temprature = 0;
            tint = 0;
            colorFilter = Color.white;
            hue = 0;
            saturation = 0;
            brightness = 0;
            contrast = 0.0f;
            mixerRedOutRedIn = 100;
            mixerRedOutGreenIn = 0;
            mixerRedOutBlueIn = 0;
            mixerGreenOutRedIn = 0;
            mixerGreenOutGreenIn = 100;
            mixerGreenOutBlueIn = 0;
            mixerBlueOutRedIn = 0;
            mixerBlueOutGreenIn = 0;
            mixerBlueOutBlueIn = 100;
            lift = new Vector4(1, 1, 1, 0);
            gamma = new Vector4(1, 1, 1, 0);
            gain = new Vector4(1, 1, 1, 0);
            masterCurve = defaultMasterCurve;
            redCurve = defaultredCurve;
            greenCurve = defaultgreenCurve;
            blueCurve = defaultblueCurve;
            hueVsHueCurve = defaulthueVsHueCurve;
            hueVsSatCurve = defaulthueVsSatCurve;
            satVsSatCurve = defaultsatVsSatCurve;
            lumVsSatCurve = defaultlumVsSatCurve;
        }

        public override void Blend(IPostProcess other, float factor)
        {
            var o = other as FPColorGrading;
            if (!o) return;
            Enable |= o.Enable;
            gradingMode = factor <= 0 ? gradingMode : o.gradingMode;
            tonemapper = factor < 0 ? tonemapper : o.tonemapper;
            ldrLut = factor <= 0 ? ldrLut : o.ldrLut;
            ldrLutContribution = Mathf.Lerp(ldrLutContribution, o.ldrLutContribution, factor);

            toneCurveToeStrength = Mathf.Lerp(toneCurveToeStrength, o.toneCurveToeStrength, factor);
            toneCurveToeLength = Mathf.Lerp(toneCurveToeLength, o.toneCurveToeLength, factor);
            toneCurveShoulderStrength = Mathf.Lerp(toneCurveShoulderStrength, o.toneCurveShoulderStrength, factor);
            toneCurveShoulderLength = Mathf.Lerp(toneCurveShoulderLength, o.toneCurveShoulderLength, factor);
            toneCurveShoulderAngle = Mathf.Lerp(toneCurveShoulderAngle, o.toneCurveShoulderAngle, factor);
            toneCurveGamma = Mathf.Lerp(toneCurveGamma, o.toneCurveGamma, factor);

            temprature = Mathf.Lerp(temprature, o.temprature, factor);
            tint = Mathf.Lerp(tint, o.tint, factor);
            colorFilter = Color.Lerp(colorFilter, o.colorFilter, factor);
            hue = Mathf.Lerp(hue, o.hue, factor);
            saturation = Mathf.Lerp(saturation, o.saturation, factor);
            brightness = Mathf.Lerp(brightness, o.brightness, factor);
            contrast = Mathf.Lerp(contrast, o.contrast, factor);
            mixerRedOutRedIn = Mathf.Lerp(mixerRedOutRedIn, o.mixerRedOutRedIn, factor);
            mixerRedOutGreenIn = Mathf.Lerp(mixerRedOutGreenIn, o.mixerRedOutGreenIn, factor);
            mixerRedOutBlueIn = Mathf.Lerp(mixerRedOutBlueIn, o.mixerRedOutBlueIn, factor);
            mixerGreenOutRedIn = Mathf.Lerp(mixerGreenOutRedIn, o.mixerGreenOutRedIn, factor);
            mixerGreenOutGreenIn = Mathf.Lerp(mixerGreenOutGreenIn, o.mixerGreenOutGreenIn, factor);
            mixerGreenOutBlueIn = Mathf.Lerp(mixerGreenOutBlueIn, o.mixerGreenOutBlueIn, factor);
            mixerBlueOutRedIn = Mathf.Lerp(mixerBlueOutRedIn, o.mixerBlueOutRedIn, factor);
            mixerBlueOutGreenIn = Mathf.Lerp(mixerBlueOutGreenIn, o.mixerBlueOutGreenIn, factor);
            mixerBlueOutBlueIn = Mathf.Lerp(mixerBlueOutBlueIn, o.mixerBlueOutBlueIn, factor);
            lift = Vector4.Lerp(lift, o.lift, factor);
            gamma = Vector4.Lerp(gamma, o.gamma, factor);
            gain = Vector4.Lerp(gain, o.gain, factor);
            masterCurve = factor <= 0 ? masterCurve : o.masterCurve;
            redCurve = factor <= 0 ? redCurve : o.redCurve;
            greenCurve = factor <= 0 ? greenCurve : o.greenCurve;
            blueCurve = factor <= 0 ? blueCurve : o.blueCurve;
            hueVsHueCurve = factor <= 0 ? hueVsHueCurve : o.hueVsHueCurve;
            hueVsSatCurve = factor <= 0 ? hueVsSatCurve : o.hueVsSatCurve;
            satVsSatCurve = factor <= 0 ? satVsSatCurve : o.satVsSatCurve;
            lumVsSatCurve = factor <= 0 ? lumVsSatCurve : o.lumVsSatCurve;
        }

        const int k_Lut2DSize = 32;
        const int k_CurvePrecision = 128;
        const float k_CurveStep = 1f / k_CurvePrecision;

        static class ShaderIDs
        {
            internal static readonly int MainTex = Shader.PropertyToID("_MainTex");
            internal static readonly int Lut2D = Shader.PropertyToID("_Lut2D");
            internal static readonly int Lut2D_Params = Shader.PropertyToID("_Lut2D_Params");
            internal static readonly int UserLut2D_Params = Shader.PropertyToID("_UserLut2D_Params");
            internal static readonly int PostExposure = Shader.PropertyToID("_PostExposure");
            internal static readonly int ColorBalance = Shader.PropertyToID("_ColorBalance");
            internal static readonly int ColorFilter = Shader.PropertyToID("_ColorFilter");
            internal static readonly int HueSatCon = Shader.PropertyToID("_HueSatCon");
            internal static readonly int Brightness = Shader.PropertyToID("_Brightness");
            internal static readonly int ChannelMixerRed = Shader.PropertyToID("_ChannelMixerRed");
            internal static readonly int ChannelMixerGreen = Shader.PropertyToID("_ChannelMixerGreen");
            internal static readonly int ChannelMixerBlue = Shader.PropertyToID("_ChannelMixerBlue");
            internal static readonly int Lift = Shader.PropertyToID("_Lift");
            internal static readonly int InvGamma = Shader.PropertyToID("_InvGamma");
            internal static readonly int Gain = Shader.PropertyToID("_Gain");
            internal static readonly int Curves = Shader.PropertyToID("_Curves");
        }

        public override void Init()
        {
            Title = "FPColorGrading";
            Propertys = new string[] { "ldrLut", "ldrLutContribution",
            "temprature", "tint",
            "colorFilter", "hue", "saturation", "brightness", "contrast",
            "mixerRedOutRedIn", "mixerRedOutGreenIn", "mixerRedOutBlueIn", "mixerGreenOutRedIn", "mixerGreenOutGreenIn", "mixerGreenOutBlueIn", "mixerBlueOutRedIn", "mixerBlueOutGreenIn", "mixerBlueOutBlueIn",
            "lift", "gamma", "gain",
            "masterCurve", "redCurve", "greenCurve", "blueCurve", "hueVsHueCurve", "hueVsSatCurve", "satVsSatCurve", "lumVsSatCurve" };
            checkSupport();
        }

        public override void DoEnable(CenturyGame.PostProcess.PostProcessHandle cam)
        {
            checkSupport();

            if (Enable)
            {
                CreateLUT();
                RenderLDR2DLUT();
            }
        }

        void CreateLUT()
        {
            var format = GetLutFormat();
            m_internalLDRLut = new RenderTexture(k_Lut2DSize * k_Lut2DSize, k_Lut2DSize, 0, format, RenderTextureReadWrite.Linear)
            {
                name = "FP Color Grading Lut",
                hideFlags = HideFlags.DontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                anisoLevel = 0,
                autoGenerateMips = false,
                useMipMap = false,
            };
            m_internalLDRLut.Create();
        }

        static RenderTextureFormat GetLutFormat()
        {
            // Use ARGBHalf if possible, fallback on ARGB2101010 and ARGB32 otherwise
            var format = RenderTextureFormat.ARGBHalf;

            if (!SystemInfo.SupportsRenderTextureFormat(format))
            {
                format = RenderTextureFormat.ARGB2101010;

                // Note that using a log lut in ARGB32 is a *very* bad idea but we need it for
                // compatibility reasons (else if a platform doesn't support one of the previous
                // format it'll output a black screen, or worse will segfault on the user).
                if (!SystemInfo.SupportsRenderTextureFormat(format))
                    format = RenderTextureFormat.ARGB32;
            }

            return format;
        }

        public override void DoDisable()
        {
            if (m_curMat != null)
            {
                GameObject.DestroyImmediate(m_curMat);
            }
            if (converted3DLut != null)
            {
                DestroyImmediate(converted3DLut);
            }
            converted3DLut = null;

            if (m_internalLDRLut != null)
            {
                DestroyImmediate(m_internalLDRLut);
                m_internalLDRLut = null;
            }

            if (m_gradingCurve != null)
            {
                DestroyImmediate(m_gradingCurve);
                m_gradingCurve = null;
            }
        }

        public override void OnRenderHandle(ref RenderTexture source, ref RenderTexture destination, ref RenderTexture depth, ref int count)
        {
            if (converted3DLut == null || m_curMat == null)
            {
                Graphics.Blit(source, null as RenderTexture);
            }
            else
            {
                var lut2d_params = new Vector4(1f / m_internalLDRLut.width, 1f / m_internalLDRLut.height, m_internalLDRLut.height - 1f);
                m_curMat.SetVector(ShaderIDs.Lut2D_Params, lut2d_params);
                m_curMat.SetTexture(ShaderIDs.Lut2D, m_internalLDRLut);
                Graphics.Blit(source, destination, m_curMat, QualitySettings.activeColorSpace == ColorSpace.Linear ? 1 : 0);
            }
            base.OnRenderHandle(ref source, ref destination, ref depth, ref count);
        }
        public override void Update()
        {
            if (null == m_curMat)
            {
                return;
            }

#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isPlaying)
            {
                if (lookupTexture != m_oldTexture || null == converted3DLut)
                {
                    Convert(lookupTexture);
                    m_curMat.SetTexture("_ClutTex", converted3DLut);
                    m_oldTexture = lookupTexture;
                }
            }
            else
            {
                Convert(lookupTexture);
                m_curMat.SetTexture("_ClutTex", converted3DLut);
                m_oldTexture = lookupTexture;
            }
#else
        if (lookupTexture != m_oldTexture || null == converted3DLut)
        {
            Convert(lookupTexture);
            m_curMat.SetTexture("_ClutTex", converted3DLut);
            m_oldTexture = lookupTexture;
        }
#endif

            if (amount != m_amount)
            {
                m_curMat.SetFloat("_Amount", amount);
                m_amount = amount;
            }
            if (tintColor != m_tintColor)
            {
                m_curMat.SetColor("_TintColor", tintColor);
                m_tintColor = tintColor;
            }
            if (hue != m_hue)
            {
                m_curMat.SetFloat("_Hue", hue);
                m_hue = hue;
            }
            if (saturation != m_saturation)
            {
                m_curMat.SetFloat("_Saturation", saturation + 1.0f);
                m_saturation = saturation;
            }
            if (brightness != m_brightness)
            {
                m_curMat.SetFloat("_Brightness", brightness + 1.0f);
                m_brightness = brightness;
            }
            if (contrast != m_contrast)
            {
                m_curMat.SetFloat("_Contrast", contrast + 1.0f);
                m_contrast = contrast;
            }
            if (lutSize != m_lutSize)
            {
                m_curMat.SetFloat("_Scale", (lutSize - 1) / (1.0f * lutSize));
                m_curMat.SetFloat("_Offset", 1.0f / (2.0f * lutSize));
                m_lutSize = lutSize;
            }

#if UNITY_EDITOR
            if (null != m_bakerMat)
                RenderLDR2DLUT();
#endif
        }

        private Shader m_curShader;
        private Material m_curMat;

        public Texture2D lookupTexture;
        private Texture2D m_oldTexture;
        private RenderTexture m_internalLDRLut = null;
        private Texture2D m_gradingCurve = null;
        readonly Color[] m_Pixels = new Color[k_CurvePrecision * 2];

        private Shader m_bakerShader;
        private Material m_bakerMat;

        private struct Spline
        {
            bool loop;
            float zeroValue;
            float range;
            public float[] cachedData;
            AnimationCurve internalCurve;

            public Spline(float zeroValue, bool loop, Vector2 bounds)
            {
                this.zeroValue = zeroValue;
                this.loop = loop;
                this.range = bounds.magnitude;
                cachedData = new float[k_CurvePrecision];
                internalCurve = new AnimationCurve();
            }

            public void SetCurve(AnimationCurve curve)
            {
                var length = curve.length;

                if (loop && length > 1)
                {
                    if (internalCurve == null)
                        internalCurve = new AnimationCurve();

                    var prev = curve[length - 1];
                    prev.time -= range;
                    var next = curve[0];
                    next.time += range;
                    internalCurve.keys = curve.keys;
                    internalCurve.AddKey(prev);
                    internalCurve.AddKey(next);
                }

                for (int i = 0; i < k_CurvePrecision; i++)
                    cachedData[i] = Evaluate(curve, (float)i * k_CurveStep, length);
            }

            public float Evaluate(AnimationCurve curve, float t, int length)
            {
                if (length == 0)
                    return zeroValue;

                if (!loop || length == 1)
                    return curve.Evaluate(t);

                return internalCurve.Evaluate(t);
            }
        }

        public enum GradingMode
        {
            LDR,
            HDR,
        }

        public enum Tonemapper
        {
            None,
            Neutral,
            ACES,
            Custom,
        }

        public GradingMode gradingMode = GradingMode.LDR;
        public Tonemapper tonemapper = Tonemapper.None;

        public Texture2D ldrLut = null;

        [Range(0, 1)]
        public float ldrLutContribution = 1;

        [Range(0, 1)]
        public float toneCurveToeStrength = 0;

        [Range(0, 1)]
        public float toneCurveToeLength = 0.5f;

        [Range(0, 1)]
        public float toneCurveShoulderStrength = 0;

        [Min(0)]
        public float toneCurveShoulderLength = 0.5f;

        [Range(0, 1)]
        public float toneCurveShoulderAngle = 0;

        [Min(0.001f)]
        public float toneCurveGamma = 1;

        [Range(-100, 100)]
        public float temprature = 0;

        [Range(-100, 100)]
        public float tint = 0;

        public Color colorFilter = Color.white;

        [Range(-180, 180)]
        public float hue = 0;
        private float m_hue = 0.0f;

        [Range(-1, 1)]
        public float saturation = 0;
        private float m_saturation = 0.0f;

        [Range(-1, 1)]
        public float brightness = 0;
        private float m_brightness = 0.0f;

        [Range(-1, 1)]
        public float contrast = 0.0f;
        private float m_contrast = 0.0f;

        [Range(-200, 200)]
        public float mixerRedOutRedIn = 100;

        [Range(-200, 200)]
        public float mixerRedOutGreenIn = 0;

        [Range(-200, 200)]
        public float mixerRedOutBlueIn = 0;

        [Range(-200, 200)]
        public float mixerGreenOutRedIn = 0;

        [Range(-200, 200)]
        public float mixerGreenOutGreenIn = 100;

        [Range(-200, 200)]
        public float mixerGreenOutBlueIn = 0;

        [Range(-200, 200)]
        public float mixerBlueOutRedIn = 0;

        [Range(-200, 200)]
        public float mixerBlueOutGreenIn = 0;

        [Range(-200, 200)]
        public float mixerBlueOutBlueIn = 100;

        public Vector4 lift = new Vector4(1, 1, 1, 0);
        public Vector4 gamma = new Vector4(1, 1, 1, 0);
        public Vector4 gain = new Vector4(1, 1, 1, 0);

        public AnimationCurve masterCurve = new AnimationCurve(new Keyframe(0f, 0f, 1f, 1f), new Keyframe(1f, 1f, 1f, 1f));
        public AnimationCurve redCurve = new AnimationCurve(new Keyframe(0f, 0f, 1f, 1f), new Keyframe(1f, 1f, 1f, 1f));
        public AnimationCurve greenCurve = new AnimationCurve(new Keyframe(0f, 0f, 1f, 1f), new Keyframe(1f, 1f, 1f, 1f));
        public AnimationCurve blueCurve = new AnimationCurve(new Keyframe(0f, 0f, 1f, 1f), new Keyframe(1f, 1f, 1f, 1f));

        public AnimationCurve hueVsHueCurve = new AnimationCurve();
        public AnimationCurve hueVsSatCurve = new AnimationCurve();
        public AnimationCurve satVsSatCurve = new AnimationCurve();
        public AnimationCurve lumVsSatCurve = new AnimationCurve();

        Spline masterSpline = new Spline(0f, false, new Vector2(0f, 1f));
        Spline redSpline = new Spline(0f, false, new Vector2(0f, 1f));
        Spline greenSpline = new Spline(0f, false, new Vector2(0f, 1f));
        Spline blueSpline = new Spline(0f, false, new Vector2(0f, 1f));

        Spline hueVsHueSpline = new Spline(0.5f, true, new Vector2(0f, 1f));
        Spline hueVsSatSpline = new Spline(0.5f, true, new Vector2(0f, 1f));
        Spline satVsSatSpline = new Spline(0.5f, false, new Vector2(0f, 1f));
        Spline lumVsSatSpline = new Spline(0.5f, false, new Vector2(0f, 1f));

        public Color tintColor = Color.white;
        private Color m_tintColor = Color.black;

        private int lutSize = 0;
        private int m_lutSize = 0;

        private Texture3D converted3DLut = null;

        [Range(0, 1)]
        public float amount = 0.0f;
        private float m_amount = 0.0f;

        void checkSupport()
        {
            if (m_curShader == null)
            {
                m_curShader = PostProcessHandle.LoadShader("Shaders/Post/FPColorGrading");
            }
            if (m_bakerShader == null)
            {
                m_bakerShader = PostProcessHandle.LoadShader("Shaders/Post/FPColorGradingBaker");
            }
            if (!SystemInfo.supportsImageEffects || m_curShader == null || !m_curShader.isSupported || m_bakerShader == null)
            {
                Enable = false;
                return;
            }
            if (Enable && m_curMat == null)
            {
                m_curMat = new Material(m_curShader);
                //m_curMat.hideFlags = HideFlags.HideAndDontSave;
                InitData();
            }
            if (Enable && m_bakerMat == null)
            {
                m_bakerMat = new Material(m_bakerShader);
            }
        }

        void InitData()
        {
            m_amount = amount - 1;
            m_tintColor = Color.black;
            m_hue = hue - 1;
            m_saturation = saturation - 1;
            m_brightness = brightness - 1;
            m_contrast = contrast - 1;
            m_lutSize = lutSize - 1;
            m_oldTexture = null;
        }

        void SetIdentityLut()
        {
            if (!SystemInfo.supports3DTextures)
            {
                return;
            }

            else if (converted3DLut != null)
            {
                DestroyImmediate(converted3DLut);
            }
            int dim = 16;
            var newC = new Color[dim * dim * dim];
            float oneOverDim = 1.0f / (1.0f * dim - 1.0f);

            for (int i = 0; i < dim; i++)
            {

                for (int j = 0; j < dim; j++)
                {

                    for (int k = 0; k < dim; k++)
                    {
                        newC[i + (j * dim) + (k * dim * dim)] = new Color((i * 1.0f) * oneOverDim, (j * 1.0f) * oneOverDim, (k * 1.0f) * oneOverDim, 1.0f);
                    }
                }
            }
            converted3DLut = new Texture3D(dim, dim, dim, TextureFormat.RGB24, false);
            converted3DLut.SetPixels(newC);
            converted3DLut.name = "IdentityLut";
            converted3DLut.Apply();
            lutSize = converted3DLut.width;
            converted3DLut.wrapMode = TextureWrapMode.Clamp;
        }

        bool ValidDimensions(Texture2D tex2d)
        {
            if (tex2d == null)
            {
                return false;
            }
            int h = tex2d.height;
            if (h != Mathf.FloorToInt(Mathf.Sqrt(tex2d.width)))
            {
                return false;
            }
            return true;
        }

        bool Convert(Texture2D lookupTexture)
        {
            if (!SystemInfo.supports3DTextures)
            {
                Debug.LogError("System does not support 3D textures");
                return false;
            }

            else if (lookupTexture == null)
            {
                SetIdentityLut();
            }
            else
            {
                if (converted3DLut != null)
                {
                    DestroyImmediate(converted3DLut);
                }
                if (lookupTexture.mipmapCount > 1)
                {
                    Debug.LogError("Lookup texture must not have mipmaps");
                    return false;
                }
                try
                {
                    int dim = lookupTexture.width * lookupTexture.height;
                    dim = lookupTexture.height;
                    if (!ValidDimensions(lookupTexture))
                    {
                        Debug.LogError("Lookup texture dimensions must be a power of two. The height must equal the square root of the width.");
                        return false;
                    }
                    var c = lookupTexture.GetPixels();
                    var newC = new Color[c.Length];

                    for (int i = 0; i < dim; i++)
                    {

                        for (int j = 0; j < dim; j++)
                        {

                            for (int k = 0; k < dim; k++)
                            {
                                newC[i + (j * dim) + (k * dim * dim)] = c[k * dim + i + j * dim * dim];
                            }
                        }
                    }
                    converted3DLut = new Texture3D(dim, dim, dim, TextureFormat.ARGB32, false);
                    converted3DLut.SetPixels(newC);
                    converted3DLut.name = "ColorLut";
                    converted3DLut.Apply();
                    lutSize = converted3DLut.width;
                    converted3DLut.wrapMode = TextureWrapMode.Clamp;
                }

                catch (Exception ex)
                {
                    Debug.LogError("Unable to convert texture to LUT texture, make sure it is read/write. Error: " + ex);
                }
            }
            return true;
        }

        void RenderLDR2DLUT()
        {
            var lut2d_params = new Vector4(k_Lut2DSize, 0.5f / (k_Lut2DSize * k_Lut2DSize), 0.5f / k_Lut2DSize, k_Lut2DSize / (k_Lut2DSize - 1f));
            var colorBalance = ComputeColorBalance(temprature, tint);
            var hsc = new Vector4(hue / 360, saturation + 1, contrast + 1, 0);
            var channelMixerR = new Vector4(mixerRedOutRedIn, mixerRedOutGreenIn, mixerRedOutBlueIn, 0) / 100;
            var channelMixerG = new Vector4(mixerGreenOutRedIn, mixerGreenOutGreenIn, mixerGreenOutBlueIn, 0) / 100;
            var channelMixerB = new Vector4(mixerBlueOutRedIn, mixerBlueOutGreenIn, mixerBlueOutBlueIn, 0) / 100;
            var _lift = ColorToLift(lift);
            var _gain = ColorToGain(gain);
            var invgamma = ColorToInverseGamma(gamma);
            var _brightness = brightness + 1;
            var curveTexture = GetCurveTexture(false);

            m_bakerMat.SetVector(ShaderIDs.Lut2D_Params, lut2d_params);
            m_bakerMat.SetVector(ShaderIDs.ColorBalance, colorBalance);
            m_bakerMat.SetColor(ShaderIDs.ColorFilter, colorFilter);
            m_bakerMat.SetVector(ShaderIDs.HueSatCon, hsc);
            m_bakerMat.SetFloat(ShaderIDs.Brightness, _brightness);
            m_bakerMat.SetVector(ShaderIDs.ChannelMixerRed, channelMixerR);
            m_bakerMat.SetVector(ShaderIDs.ChannelMixerGreen, channelMixerG);
            m_bakerMat.SetVector(ShaderIDs.ChannelMixerBlue, channelMixerB);
            m_bakerMat.SetVector(ShaderIDs.Lift, _lift);
            m_bakerMat.SetVector(ShaderIDs.Gain, _gain);
            m_bakerMat.SetVector(ShaderIDs.InvGamma, invgamma);
            m_bakerMat.SetTexture(ShaderIDs.Curves, curveTexture);

            if (ldrLut == null)
                Graphics.Blit(null, m_internalLDRLut, m_bakerMat, 0);
            else
            {
                var userLut_params = new Vector4(1f / ldrLut.width, 1f / ldrLut.height, ldrLut.height - 1f, ldrLutContribution);
                m_bakerMat.SetVector(ShaderIDs.UserLut2D_Params, userLut_params);
                Graphics.Blit(ldrLut, m_internalLDRLut, m_bakerMat, 1);
            }
        }

        Texture2D GetCurveTexture(bool hdr)
        {
            if (m_gradingCurve == null)
            {
                var format = GetCurveFormat();
                m_gradingCurve = new Texture2D(k_CurvePrecision, 2, format, false, true)
                {
                    name = "Internal Curves Texture",
                    hideFlags = HideFlags.DontSave,
                    anisoLevel = 0,
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear,
                };
            }

            var pixels = m_Pixels;

            masterSpline.SetCurve(masterCurve);
            redSpline.SetCurve(redCurve);
            greenSpline.SetCurve(greenCurve);
            blueSpline.SetCurve(blueCurve);

            hueVsHueSpline.SetCurve(hueVsHueCurve);
            hueVsSatSpline.SetCurve(hueVsSatCurve);
            satVsSatSpline.SetCurve(satVsSatCurve);
            lumVsSatSpline.SetCurve(lumVsSatCurve);


            for (int i = 0; i < k_CurvePrecision; ++i)
            {
                float x = hueVsHueSpline.cachedData[i];
                float y = hueVsSatSpline.cachedData[i];
                float z = satVsSatSpline.cachedData[i];
                float w = lumVsSatSpline.cachedData[i];
                pixels[i] = new Color(x, y, z, w);

                if (!hdr)
                {
                    float m = masterSpline.cachedData[i];
                    float r = redSpline.cachedData[i];
                    float g = greenSpline.cachedData[i];
                    float b = blueSpline.cachedData[i];
                    pixels[i + k_CurvePrecision] = new Color(r, g, b, m);
                }
            }

            m_gradingCurve.SetPixels(pixels);
            m_gradingCurve.Apply(false, false);

            return m_gradingCurve;
        }

        static TextureFormat GetCurveFormat()
        {
            // Use RGBAHalf if possible, fallback on ARGB32 otherwise
            var format = TextureFormat.RGBAHalf;

            if (!SystemInfo.SupportsTextureFormat(format))
                format = TextureFormat.ARGB32;

            return format;
        }


        public static Vector3 ComputeColorBalance(float temperature, float tint)
        {
            // Range ~[-1.67;1.67] works best
            float t1 = temperature / 60f;
            float t2 = tint / 60f;

            // Get the CIE xy chromaticity of the reference white point.
            // Note: 0.31271 = x value on the D65 white point
            float x = 0.31271f - t1 * (t1 < 0f ? 0.1f : 0.05f);
            float y = StandardIlluminantY(x) + t2 * 0.05f;

            // Calculate the coefficients in the LMS space.
            var w1 = new Vector3(0.949237f, 1.03542f, 1.08728f); // D65 white point
            var w2 = CIExyToLMS(x, y);
            return new Vector3(w1.x / w2.x, w1.y / w2.y, w1.z / w2.z);
        }

        public static float StandardIlluminantY(float x)
        {
            return 2.87f * x - 3f * x * x - 0.27509507f;
        }

        public static Vector3 CIExyToLMS(float x, float y)
        {
            float Y = 1f;
            float X = Y * x / y;
            float Z = Y * (1f - x - y) / y;

            float L = 0.7328f * X + 0.4296f * Y - 0.1624f * Z;
            float M = -0.7036f * X + 1.6975f * Y + 0.0061f * Z;
            float S = 0.0030f * X + 0.0136f * Y + 0.9834f * Z;

            return new Vector3(L, M, S);
        }

        public static Vector3 ColorToLift(Vector4 color)
        {
            // Shadows
            var S = new Vector3(color.x, color.y, color.z);
            float lumLift = S.x * 0.2126f + S.y * 0.7152f + S.z * 0.0722f;
            S = new Vector3(S.x - lumLift, S.y - lumLift, S.z - lumLift);

            float liftOffset = color.w;
            return new Vector3(S.x + liftOffset, S.y + liftOffset, S.z + liftOffset);
        }

        public static Vector3 ColorToInverseGamma(Vector4 color)
        {
            // Midtones
            var M = new Vector3(color.x, color.y, color.z);
            float lumGamma = M.x * 0.2126f + M.y * 0.7152f + M.z * 0.0722f;
            M = new Vector3(M.x - lumGamma, M.y - lumGamma, M.z - lumGamma);

            float gammaOffset = color.w + 1f;
            return new Vector3(
                1f / Mathf.Max(M.x + gammaOffset, 1e-03f),
                1f / Mathf.Max(M.y + gammaOffset, 1e-03f),
                1f / Mathf.Max(M.z + gammaOffset, 1e-03f)
            );
        }

        public static Vector3 ColorToGain(Vector4 color)
        {
            // Highlights
            var H = new Vector3(color.x, color.y, color.z);
            float lumGain = H.x * 0.2126f + H.y * 0.7152f + H.z * 0.0722f;
            H = new Vector3(H.x - lumGain, H.y - lumGain, H.z - lumGain);

            float gainOffset = color.w + 1f;
            return new Vector3(H.x + gainOffset, H.y + gainOffset, H.z + gainOffset);
        }

    }
}