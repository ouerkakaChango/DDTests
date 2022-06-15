using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
public class FPRenderConf : EditorWindow
{
    private static FPRenderConf window;
    [MenuItem("FP/渲染/渲染管理器")]
    static void Init()
    {
        window = (FPRenderConf)EditorWindow.GetWindow(typeof(FPRenderConf));
        window.titleContent = new GUIContent("渲染管理器V1.0");
        window.position = new Rect((Screen.currentResolution.width - 400) / 2, (Screen.currentResolution.height - 300) / 2, 400, 300);
        window.Show();
    }
    bool showEnvironmentLighting = true, showSceneList = true;
    private List<FPRenderScene> sceneList;
    private void OnEnable()
    {
        FPRenderMgr.GetSceneHandle(OnFPRenderMgr);
    }
    void OnGUI()
    {
        EditorGUILayout.LabelField("光照");
        showEnvironmentLighting = EditorGUILayout.BeginToggleGroup("环境", showEnvironmentLighting);
        RenderSettings.ambientLight = EditorGUILayout.ColorField("环境颜色:", RenderSettings.ambientLight);
        RenderSettings.subtractiveShadowColor = EditorGUILayout.ColorField("阴影颜色:", RenderSettings.subtractiveShadowColor);
        EditorGUILayout.EndToggleGroup();
        showSceneList = EditorGUILayout.BeginToggleGroup("实时光", showSceneList);
        if (sceneList != null)
        {
            int count = sceneList.Count;
            for (int i = 0; i < sceneList.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                
                GUILayout.Button(i == count - 1 ? "当前" : "", GUILayout.Width(80));
                EditorGUILayout.LabelField(sceneList[i].SceneName, GUILayout.Width(100));
                EditorGUILayout.ObjectField("", sceneList[i], typeof(FPRenderScene), true);
                EditorGUILayout.EndHorizontal();
            }
        }
    }
    public void OnFPRenderMgr(FPRenderMgr mgr)
    {
        sceneList = mgr.SceneList;
    }
}
