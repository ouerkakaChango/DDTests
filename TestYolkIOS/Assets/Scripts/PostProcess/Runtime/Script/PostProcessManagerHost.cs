using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CenturyGame.PostProcess
{
    [ExecuteInEditMode]
    public class PostProcessManagerHost : MonoBehaviour
    {
        void Awake()
        {
            host = this;
            initMat();
            useLinear = !UseLinear;
        }

        void OnEnable()
        {
            UIInstance = this;
            setCommand();
            initRT();
        }

        void OnDisable()
        {
            deleteRT();
        }

        void Update()
        {
            setCommand();
        }

        void OnDestroy()
        {
            deleteMat();
        }
        public static float ResolutionScale = 1.0f;
        private static PostProcessManagerHost UIInstance = null;
        private static bool init = true;
        private static int sWidth = Screen.width, sHeight = Screen.height;

        public static void SetResolutionScale(float scale)
        {
            if (init)
            {
                init = false;
                sWidth = Screen.width;
                sHeight = Screen.height;
            }

            if (ResolutionScale != scale)
            {
                ResolutionScale = scale;
                Screen.SetResolution((int)(sWidth * ResolutionScale), (int)(sHeight * ResolutionScale), true);
                if (UIInstance != null)
                {
                    UIInstance.ResetRT();
                }
            }
        }

        public void ResetRT()
        {
            initRT();
        }

        void OnPostRender()
        {
            if (UseLinear)
            {
#if UNITY_EDITOR
                Graphics.Blit(uiRT, null as RenderTexture, mixMaterial, 1);
#else

#if UNITY_IOS
            Graphics.Blit(uiRT, null as RenderTexture, mixMaterial, 1);
#else
            Graphics.Blit(uiRT, null as RenderTexture);
#endif
#endif
            }
            else
            {
                Graphics.Blit(uiRT, null as RenderTexture);
            }

            FPFinalRT.instance.finalRT = uiRT;
        }

        #region ForPostProcess
        void setCommand()
        {
            if (useLinear != UseLinear)
            {
                useLinear = UseLinear;
                if (UseLinear)
                {
                    Shader.EnableKeyword("USE_LINEAR");
                }
                else
                {
                    Shader.DisableKeyword("USE_LINEAR");
                }
            }
        }

        public PostProcessManager Instance;
        public bool UseLinear = true;
        private bool useLinear = false;
        private Camera uiCamera;
        private void initRT()
        {
            deleteRT();
            if (uiRT == null)
            {
                uiRT = new RenderTexture((int)(sWidth * ResolutionScale), (int)(sHeight * ResolutionScale), 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
                uiRT.name = "ui_rt";
                UICamera.SetTargetBuffers(uiRT.colorBuffer, uiRT.depthBuffer);
            }
        }
        private void deleteRT()
        {
            if (uiRT != null)
            {
                UICamera.targetTexture = null;
                GameObject.DestroyImmediate(uiRT);
                uiRT = null;
            }
        }

        private void initMat()
        {
            if (mixMaterial == null)
            {
                Shader m_curShader = PostProcessHandle.LoadShader("Shaders/Post/FPFinal");
                if (!SystemInfo.supportsImageEffects || m_curShader == null || !m_curShader.isSupported)
                {
                    return;
                }
                mixMaterial = new Material(m_curShader);
                mixMaterial.hideFlags = HideFlags.HideAndDontSave;
            }
        }
        void deleteMat()
        {
            if (mixMaterial != null)
            {
                GameObject.DestroyImmediate(mixMaterial);
            }
        }
        private Material mixMaterial = null;
        private RenderTexture uiRT = null;
        public Material GetTarget(ref RenderTexture target)
        {
            if (UICamera != null)
            {
                target = uiRT;
            }
            return mixMaterial;
        }

        public Camera UICamera
        {
            get
            {
                if (uiCamera == null)
                {
                    uiCamera = GetComponent<Camera>();
                }
                return uiCamera;
            }
        }

        public void SetCurrent(PostProcessManager instance)
        {
            if (instance != null)
                Instance = instance;
        }
        public static PostProcessManagerHost host = null;
        public static GetUITargetHandle SetMgr(PostProcessManager instance, bool enable)
        {
            if (host == null)
                return null;
            if (enable)
            {
                host.SetCurrent(instance);
                host.Instance.DoEnable();
            }
            else
            {
                if (host.Instance != null)
                {
                    host.Instance.DoDisable();
                }
                host.Instance = null;
            }
            return host.GetTarget;
        }

        #endregion
    }
}