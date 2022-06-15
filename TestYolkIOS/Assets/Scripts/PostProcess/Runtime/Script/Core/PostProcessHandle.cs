using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif
namespace CenturyGame.PostProcess
{
    public partial class PostProcessHandle : MonoBehaviour
    {
        public enum AntiAliasing
        {
            None,
            FXAA,
        }
        public static LoadAction<Shader> LoadShader
        {
            get
            {
                if (loadShaderHandle == null)
                {
                    loadShaderHandle = Resources.Load<Shader>;
                }
                return loadShaderHandle;
            }
            set
            {
                loadShaderHandle = value;
            }
        }

        private static LoadAction<Shader> loadShaderHandle;
        public GetUITargetHandle GetUITarget;
        public static float ResolutionScale = 1.0f;
        PostProcessShaders m_shaders;
        PostProcessMaterials m_materials;
        CommandBuffer m_cmdBeforeEverything;
        CommandBuffer m_cmdPostProcessOpaque;
        CommandBuffer m_cmdPostProcess;
        CommandBuffer m_cmdFinal;
        Dictionary<Type, IPostProcess> m_settings = new Dictionary<Type, IPostProcess>();
        public AntiAliasing antiAliasing;
        public bool copyDepth;

        void Awake()
        {
            if (QualitySettings.activeColorSpace == ColorSpace.Linear)
                Shader.EnableKeyword("USE_LINEAR");

            PostProcessHandle.LoadShader = new LoadAction<Shader>(Resources.Load<Shader>);
        }

        void InitSettings()
        {
            m_settings.Clear();

            foreach (var type in PostProcessHub.g_postprocessTypes)
            {
                var setting = (IPostProcess)ScriptableObject.CreateInstance(type);
                m_settings.Add(type, setting);
            }
        }

        void SetupSettings()
        {
            m_hbao = GetSetting<FPHBAO>();
            m_dof = GetSetting<FPDepthOfField>();
            m_bloom = GetSetting<FPBloom>();
            m_colorGrading = GetSetting<FPColorGrading>();
            m_gaussianBlur = GetSetting<FPGaussianBlur>();
            m_radialBlur = GetSetting<FPRadialBlur>();
            m_ssss = GetSetting<FPSSSS>();
            m_vignette = GetSetting<FPVignette>();

            m_hbaoAvailable = PostProcessHub.GetEffectAvailable<FPHBAO>();
            m_dofAvailable = PostProcessHub.GetEffectAvailable<FPDepthOfField>();
            m_bloomAvailable = PostProcessHub.GetEffectAvailable<FPBloom>();
            m_colorGradingAvailable = PostProcessHub.GetEffectAvailable<FPColorGrading>();
            m_gaussianBlurAvailable = PostProcessHub.GetEffectAvailable<FPGaussianBlur>();
            m_radialBlurAvailable = PostProcessHub.GetEffectAvailable<FPRadialBlur>();
            m_ssssAvailable = PostProcessHub.GetEffectAvailable<FPSSSS>();
            m_vignetteAvailable = PostProcessHub.GetEffectAvailable<FPVignette>();
        }

        void InitCache()
        {
            for (int i = 0; i < maxBloomPyramidSize; ++i)
            {
                bloomUp[i] = Shader.PropertyToID("_PP_BloomMipUp" + i);
                bloomDown[i] = Shader.PropertyToID("_PP_BloomMipDown" + i);
            }

            for (int i = 0; i < FPGaussianBlur.MaxIterationCount; ++i)
            {
                m_blurBuffers[i] = Shader.PropertyToID("_PP_BlurMip" + i);
            }

            CreateLUT();

            InitSSSS(Color.red, Color.red);
        }

        void InitFormats()
        {
            if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.R8))
                R8Format = RenderTextureFormat.R8;
            else if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RHalf))
                R8Format = RenderTextureFormat.RHalf;
            else if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RGB565))
                R8Format = RenderTextureFormat.RGB565;
            else
                R8Format = RenderTextureFormat.Default;

            if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf))
                ARGBHalfFormat = RenderTextureFormat.ARGBHalf;
            else
                ARGBHalfFormat = RenderTextureFormat.ARGB32;
        }

        void InitFullScreenQuad()
        {
            if (m_mesh != null)
                return;

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
            m_mesh.UploadMeshData(true);
        }

        private T GetSetting<T>() where T : IPostProcess
        {
            m_settings.TryGetValue(typeof(T), out var setting);
            return setting as T;
        }

        void OnDestroy()
        {
            m_materials?.Dispose();

            m_shaders = null;
            m_materials = null;
            if (m_mesh != null)
            {
                DestroyImmediate(m_mesh);
                m_mesh = null;
            }
        }

        public enum RenderView
        {
            RunTimeView,
            CheckView
        }
        [System.NonSerialized]
        public bool Run = false;
        public bool showMessage = false;
        private Camera mainCamera;
        public Camera MainCamera
        {
            get
            {
                if (mainCamera == null)
                {
                    mainCamera = GetComponent<Camera>();
                }
                return mainCamera;
            }
        }
        [NonSerialized]
        public RenderTexture ColorTex, DesTex, DepthTex, DepthBuffer;
        public static int DepthTexID = 0;

        private readonly string colorTexIDStr = "_ColorTex",
            desTexIDStr = "_DesTex",
            depthTexIDStr = "_DepthTexDOF";
        readonly string depthTexName = "_DepthTex";
        public ScreenChange OnScreenChange = null;
        int imageCount = 0;
        public Resolution PostResolution
        {
            get
            {
                Resolution getResolution = new Resolution();
                getResolution.width = (int)(m_postResolution.width * ResolutionScale);
                getResolution.height = (int)(m_postResolution.height * ResolutionScale);
                return getResolution;
            }
            set
            {
                if (value.width != m_postResolution.width || value.height != m_postResolution.height)
                {
                    m_postResolution = value;
                    ReSetBufferSize();
                }
            }
        }
        private Resolution m_postResolution;

        //For Debugging
        private Material CheckMaterial = null;
        public RenderView RenderViewType = RenderView.RunTimeView;
        public RenderTextureFormat HDRFormat = RenderTextureFormat.DefaultHDR;
        public RenderTextureFormat ARGBHalfFormat = RenderTextureFormat.ARGBHalf;
        public RenderTextureFormat R8Format = RenderTextureFormat.R8;
        Mesh m_mesh;

        public static string[] StaticEffectList = new string[]
        {
        "FPHBAO",
        "FPSSSS",
        "FPBloom",
        "FPGodRay",
        "FPDistortion",
        "FPRadialBlur",
        "FPDepthOfField",
        "FPGaussianBlur",
        "FPToneMapping",
        "FPColorGrading",
        "FPFXAA3",
        "FPFinal"
        };
        public void DoEnable()
        {
            ReSetBufferSize();
            postProcessAction(onEnableAction);
            Run = true;
        }
        public void _DoEnable()
        {
            m_shaders = new PostProcessShaders();
            m_materials = new PostProcessMaterials(m_shaders);

            InitSettings();
            SetupSettings();
            InitCache();
            InitFormats();
            InitFullScreenQuad();

            m_cmdBeforeEverything = new CommandBuffer() { name = "BeforeEverything" };
            m_cmdPostProcessOpaque = new CommandBuffer() { name = "Post Process Opaque" };
            m_cmdPostProcess = new CommandBuffer() { name = "Post Process" };
            //m_cmdFinal = new CommandBuffer() { name = "Final" };

            MainCamera.AddCommandBuffer(CameraEvent.AfterImageEffectsOpaque, m_cmdPostProcessOpaque);
            MainCamera.AddCommandBuffer(CameraEvent.AfterEverything, m_cmdPostProcess);
            //MainCamera.AddCommandBuffer(CameraEvent.AfterEverything, m_cmdFinal);

            //m_cmdFinal.Blit(ColorTex, null as RenderTexture);
        }
        /// <summary>
        /// 设置后处理RT的尺寸
        /// </summary>
        /// <param name="_width"></param>
        /// <param name="_height"></param>
        public void ReSetBufferSize()
        {

            RtInit(true);

            RtInit(false);

            MainCamera.depthTextureMode = DepthTextureMode.None;
#if UNITY_EDITOR
            SetEditorEnvironment();
#endif

            MainCamera.SetTargetBuffers(ColorTex.colorBuffer, DepthTex.depthBuffer);
            DepthTexID = Shader.PropertyToID(depthTexIDStr);
            Shader.SetGlobalTexture(DepthTexID, DepthTex);
            postProcessAction(resizeAction);
        }
        void SetEditorEnvironment()
        {
            MainCamera.depthTextureMode = DepthTextureMode.Depth;
            Shader.EnableKeyword("DD_SHADER_EDITOR");
        }

        void RtInit(bool destory)
        {
            if (destory)
            {
                if (ColorTex != null)
                {
                    MainCamera.targetTexture = null;
                    GameObject.DestroyImmediate(ColorTex);
                    GameObject.DestroyImmediate(DesTex);
                    GameObject.DestroyImmediate(DepthTex);
                    ColorTex = null;
                }
            }
            else
            {
                if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RGB111110Float))
                {
                    HDRFormat = RenderTextureFormat.RGB111110Float;
                }

                ColorTex = new RenderTexture(PostResolution.width
                , PostResolution.height
                , 0
                , HDRFormat
                , RenderTextureReadWrite.Linear);
                ColorTex.name = colorTexIDStr;

                DesTex = new RenderTexture(PostResolution.width
                , PostResolution.height
                , 0
                , HDRFormat
                , RenderTextureReadWrite.Linear);
                DesTex.name = desTexIDStr;

                DepthTex = new RenderTexture(PostResolution.width
                , PostResolution.height
                , 24
                , RenderTextureFormat.Depth
                , RenderTextureReadWrite.Linear);
                DepthTex.filterMode = FilterMode.Point;
                DepthTex.name = depthTexIDStr;

                DepthBuffer = new RenderTexture(PostResolution.width
                , PostResolution.height
                , 0
                , RenderTextureFormat.RFloat
                , RenderTextureReadWrite.Linear);
                DepthBuffer.filterMode = FilterMode.Point;
                DepthBuffer.name = depthTexName;
            }
        }

        public void DoDisable()
        {
            postProcessAction(onDisableAction);
            RtInit(true);
            //FPRenderTextureManager.Instance.Dispose();
            if (MainCamera != null)
            {
                MainCamera.targetTexture = null;
            }
            Run = false;
        }
        public void _DoDisable()
        {
            m_cmdBeforeEverything.Release();
            m_cmdBeforeEverything = null;

            ReleaseCommandBuffer(CameraEvent.AfterImageEffectsOpaque, ref m_cmdPostProcessOpaque);
            ReleaseCommandBuffer(CameraEvent.AfterEverything, ref m_cmdPostProcess);
            //ReleaseCommandBuffer(CameraEvent.AfterEverything, ref m_cmdFinal);

            if (m_internalLDRLut != null)
            {
                DestroyImmediate(m_internalLDRLut);
                m_internalLDRLut = null;
            }

            if (hbaoNoiseTex != null)
            {
                DestroyImmediate(hbaoNoiseTex);
                hbaoNoiseTex = null;
            }

            lastHBAONoiseTexParam = -1;
        }

        void ReleaseCommandBuffer(CameraEvent evt, ref CommandBuffer buffer)
        {
            if (buffer != null)
                MainCamera.RemoveCommandBuffer(evt, buffer);

            buffer.Release();
            buffer = null;
        }

        public virtual void Update()
        {
            if (!Run) return;
            postProcessAction(updatePostProcessAction);

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Vector2 windowSize = GetMainGameViewSize();
                if (m_postResolution.width != windowSize.x || m_postResolution.height != windowSize.y)
                {
                    m_postResolution.width = (int)windowSize.x;
                    m_postResolution.height = (int)windowSize.y;
                    ReSetBufferSize();
                }
            }

            if (Input.GetMouseButtonDown(1))
            {
                showMessage = !showMessage;
            }
#else
        if (Input.touchCount > 2)
        {
            showMessage = !showMessage;
        }
#endif
        }

        public virtual void OnPreCull()
        {
            if (!Run) return;
            postProcessAction(onPreCullPostProcessAction);
        }

        private void OnPreRender()
        {
            PostProcessVolumeManager.Instance.UpdateSettings(m_settings);

            BuildCommandBuffers();

            Graphics.ExecuteCommandBuffer(m_cmdBeforeEverything);
        }

        void BuildCommandBuffers()
        {
            m_cmdBeforeEverything.Clear();
            m_cmdPostProcessOpaque.Clear();
            m_cmdPostProcess.Clear();

            BuildBeforeEverythingCommand();
            BuildOpaqueImageEffectCommand();
            BuildPostImageEffectCommand();
        }

        void BuildBeforeEverythingCommand()
        {
            if (m_colorGradingAvailable && m_colorGrading.Enable)
            {
                if (m_colorGrading.gradingMode == FPColorGrading.GradingMode.LDR)
                    BakeColorGradingLDRLUT(m_cmdBeforeEverything);
                else
                    BakeColorGradingHDRLUT(m_cmdBeforeEverything);
            }
        }

        void BuildOpaqueImageEffectCommand()
        {
            var source = ColorTex;

            if (copyDepth)
            {
                CopyDepth(m_cmdPostProcessOpaque);
            }

            bool ssssAvailable = m_ssssAvailable && m_ssss.Enable;
            if (ssssAvailable)
            {
                SSSS(m_cmdPostProcessOpaque, source);
            }

            bool hbaoAvailable = m_hbaoAvailable && m_hbao.Enable;
            if (hbaoAvailable)
            {
                HBAO(m_cmdPostProcessOpaque, source);
            }

            bool hasOtherEffect = copyDepth || ssssAvailable || hbaoAvailable;
            if (PostProcessHub.NeedRefreshDepth && !hasOtherEffect)
            {
                ForceSwitch(m_cmdPostProcessOpaque);
            }
        }

        void BuildPostImageEffectCommand()
        {
            var source = ColorTex;
            var destination = DesTex;

            if (m_dofAvailable && m_dof.Enable)
            {
                DepthOfField(m_cmdPostProcess, source, destination);
                Swap(ref source, ref destination);
            }

            if (m_bloomAvailable && m_bloom.Enable)
            {
                Bloom(m_cmdPostProcess, source, destination);
                Swap(ref source, ref destination);
            }

            if (m_colorGradingAvailable && m_colorGrading.Enable)
            {
                ColorGrading(m_cmdPostProcess, source, destination);
                Swap(ref source, ref destination);
            }

            if (m_radialBlurAvailable && m_radialBlur.Enable)
            {
                RadialBlur(m_cmdPostProcess, source, destination);
                Swap(ref source, ref destination);
            }

            if (m_gaussianBlurAvailable && m_gaussianBlur.Enable && m_gaussianBlur.iterationCount > 0)
            {
                GaussianBlur(m_cmdPostProcess, source, destination);
                Swap(ref source, ref destination);
            }

            if (m_vignetteAvailable && m_vignette.Enable)
            {
                if ((m_vignette.mode == VignetteMode.Classic && m_vignette.intensity > 0) ||
                    (m_vignette.mode == VignetteMode.Masked && m_vignette.opacity > 0 && m_vignette.mask != null))
                {
                    Vignette(m_cmdPostProcess, source, destination);
                    Swap(ref source, ref destination);
                }
            }

            if (PostProcessHub.AntiAliasingAvailable && antiAliasing == AntiAliasing.FXAA)
            {
                FXAA3(m_cmdPostProcess, source, destination);
                Swap(ref source, ref destination);
            }

            FinalBlit(source);
        }

        public virtual void OnPostRender()
        {
            if (!Run) return;

            //Graphics.ExecuteCommandBuffer(m_cmdPostProcess);
        }

        void Swap(ref RenderTexture a, ref RenderTexture b)
        {
            var t = a;
            a = b;
            b = t;
        }

        private bool addLastFind = false;

        void postProcessAction(IPostProcessAction action)
        {

        }

        private void onCheckListAction<T>(IPostProcess post) where T : IPostProcess
        {
            if (post.GetType() == typeof(T))
            {
                addLastFind = true;
            }
        }
        private void onEnableAction(IPostProcess post)
        {
            post.LastEnable = !post.Enable;
        }

        private void onDisableAction(IPostProcess post)
        {
            post.LastEnable = !post.Enable;
            if (post.Enable)
            {
                post.DoDisable();
            }
        }
        private void updateAction(IPostProcess post)
        {
            if (post.Enable != post.LastEnable)
            {
                if (post.Enable)
                {
                    post.DoEnable(this);
                    resizeAction(post);
                }
                else post.DoDisable();
                post.LastEnable = post.Enable;
            }
            if (post.Enable)
            {
                post.Update();
            }
        }

        private void resizeAction(IPostProcess post)
        {
            if (post.Enable)
            {
                post.ReSize(m_postResolution);
            }
        }

        private void onPreCullAction(IPostProcess post)
        {
            if (post.Enable)
            {
                post.OnPreCull();
            }
        }
        public static PostProcessHandle Instance = null;

        /// <summary>
        /// LUA调用接口，获取后处理
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>

        private IPostProcessAction updatePostProcessAction; //update中调用的委托，提前赋值，避免update中赋值产生gc
        private IPostProcessAction onPreCullPostProcessAction;
        protected void Init()
        {
            updateWidth();
            Instance = this;
            updatePostProcessAction = updateAction;
            onPreCullPostProcessAction = onPreCullAction;
        }
        private void updateWidth()
        {
            if (m_postResolution.width == 0)
            {
#if UNITY_EDITOR
                Vector2 screen = GetMainGameViewSize();
                m_postResolution.width = (int)screen.x;
                m_postResolution.height = (int)screen.y;
#else
            m_postResolution.width = Screen.width;
            m_postResolution.height = Screen.height;
#endif
            }
#if !UNITY_EDITOR
        if (Screen.width != m_postResolution.width)
        {
            if (OnScreenChange != null)
            {
                OnScreenChange(true, Screen.width, Screen.height);
            }
        }
#endif
        }
#if UNITY_EDITOR
        public static Vector2 GetMainGameViewSize()
        {
            System.Type T = System.Type.GetType("UnityEditor.GameView,UnityEditor");
            System.Reflection.MethodInfo GetSizeOfMainGameView = T.GetMethod("GetSizeOfMainGameView", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            System.Object Res = GetSizeOfMainGameView.Invoke(null, null);
            return (Vector2)Res;
        }
#endif
    }
    public delegate void ScreenChange(bool ready, int w, int h);
}