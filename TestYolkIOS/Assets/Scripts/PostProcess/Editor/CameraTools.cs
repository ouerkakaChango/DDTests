using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using CenturyGame.PostProcess;
namespace CenturyGame.PostProcessEditor
{
    public class CameraTools : EditorWindow
    {
        [MenuItem("FP/资源管理/后处理相机同步 %m")]

        public static void Off()
        {
            cameraTools = (CameraTools)EditorWindow.GetWindow(typeof(CameraTools));
            cameraTools.titleContent = new GUIContent("CameraTool V1.0");
            cameraTools.position = new Rect((Screen.currentResolution.width - WindowWidth) / 2, (Screen.currentResolution.height - WindowHeight) / 2, WindowWidth, WindowHeight);
            cameraTools.Show();
        }
        public static readonly int WindowWidth = 200;
        public static readonly int WindowHeight = 200;

        void OnDisable()
        {
        }

        void OnDestroy()
        {
        }

        private void OnEnable()
        {
            transform = Camera.main.transform;
            target = SceneView.lastActiveSceneView.camera.transform;
        }
        Transform transform, target;

        void Update()
        {
            if (run)
            {
                transform.SetPositionAndRotation(target.position, target.rotation);
            }
        }
        private static bool run = false;

        void OnGUI()
        {
            if (GUILayout.Button(run ? "停止同步" : "同步相机"))
            {
                run = !run;
            }
            if (GUILayout.Button("添加后处理"))
            {
                PostProcessManager ppm = transform.GetComponent<PostProcessManager>();
                if (ppm == null) ppm = transform.gameObject.AddComponent<PostProcessManager>();
            }
            if (GUILayout.Button("移除后处理"))
            {
                removeImageEffect();
            }
            if (GUILayout.Button("关闭"))
            {
                Close();
            }
            if (GUILayout.Button("关闭&移除后处理"))
            {
                removeImageEffect();
                Close();
            }
        }

        private void removeImageEffect()
        {
            PostProcessManager ppm = transform.GetComponent<PostProcessManager>();
            if (ppm != null)
            {
                GameObject.DestroyImmediate(ppm);
            }
        }
        private static CameraTools cameraTools;
    }
}