using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
public class FPRenderMgr : MonoBehaviour
{
    private int POINT_LIGHT_COUNT_NAME_ID,
                POINT_LIGHT_POSITION_NAME_ID,
                POINT_LIGHT_COLOR_NAME_ID,
                POINT_COUNT_MAX = 3;
    private readonly string POINT_LIGHT_COUNT_NAME = "_POINT_LIGHT_COUNT",
                            POINT_LIGHT_POSITION_NAME = "_POINT_LIGHT_POSITION",
                            POINT_LIGHT_COLOR_NAME = "_POINT_LIGHT_COLOR";
    private static FPRenderMgr handle = null;
    private void OnEnable()
    {
        POINT_LIGHT_COUNT_NAME_ID = Shader.PropertyToID(POINT_LIGHT_COUNT_NAME);
        POINT_LIGHT_POSITION_NAME_ID = Shader.PropertyToID(POINT_LIGHT_POSITION_NAME);
        POINT_LIGHT_COLOR_NAME_ID = Shader.PropertyToID(POINT_LIGHT_COLOR_NAME);
        positionList = new Vector4[POINT_COUNT_MAX];
        colorList = new Vector4[POINT_COUNT_MAX];
        for (int i = 0; i < POINT_COUNT_MAX; i++)
        {
            positionList[i] = new Vector4();
            colorList[i] = new Vector4();
        }
        DontDestroyOnLoad(gameObject);
    }
    private void OnDisable()
    {
        SceneList.Clear();
        ChangeSceneAction();
    }
#if UNITY_EDITOR
    private void Update()
    {
        lightUpdate();
    }
#endif
    private void lightUpdate()
    {
        int sCount = SceneList.Count;
        setLight(sCount > 0 ? SceneList[sCount - 1] : null);
    }
    public List<FPRenderScene> SceneList = new List<FPRenderScene>();
    public void AddLights(FPRenderScene scene)
    {
        SceneList.Add(scene);
        ChangeSceneAction();
    }
    public void DeleteLights(FPRenderScene scene)
    {
        SceneList.Remove(scene);
        ChangeSceneAction();
    }
    public Action<FPRenderMgr> OnSceneChange;
    public void ChangeSceneAction()
    {
        if (OnSceneChange != null)
            OnSceneChange(this);
        lightUpdate();
    }
    private FPRenderScene lastScene = null;
    private Vector4[] positionList, colorList;
    private void setLight(FPRenderScene scene)
    {
        if(scene == null)
        {
            Shader.SetGlobalInt(POINT_LIGHT_COUNT_NAME_ID, 0);
            if (lastScene != null)
            {
                lastScene.SetEnable(true);
                lastScene = null;
            }
            return;
        }
        if (lastScene == null || lastScene.ObjId != scene.ObjId)
        {
            if(lastScene != null)
                lastScene.SetEnable(false);
            scene.SetEnable(true);
            lastScene = scene;
        }
        if (Graphics.activeTier == UnityEngine.Rendering.GraphicsTier.Tier3)
        {
            int count = Mathf.Min(scene.PointLight.Count, POINT_COUNT_MAX);
            Shader.SetGlobalInt(POINT_LIGHT_COUNT_NAME_ID, count);
            for (int i = 0; i < count; i++)
            {
                Light light = scene.PointLight[i];
                Vector4 pos = light.transform.position;
                Vector4 color = light.color * light.intensity;
                color = gamma2linear(color);
                pos.w = light.range;
                //color.w = Mathf.Pow(light.intensity, 2.2f);
                positionList[i] = pos;
                colorList[i] = color;
            }
            Shader.SetGlobalVectorArray(POINT_LIGHT_POSITION_NAME_ID, positionList);
            Shader.SetGlobalVectorArray(POINT_LIGHT_COLOR_NAME_ID, colorList);
        }
    }
    private Vector4 gamma2linear(Vector4 f)
    {
        f.x = Mathf.Pow(f.x, 2.2f);
        f.y = Mathf.Pow(f.y, 2.2f);
        f.z = Mathf.Pow(f.z, 2.2f);
        return f;
    }
    public static void GetSceneHandle(Action<FPRenderMgr> mgrHandle)
    {
        if(handle != null)
        {
            handle.OnSceneChange = mgrHandle;
            handle.ChangeSceneAction();
        }
    }
    public static void AddSceneLights(FPRenderScene scene)
    {
        checkHandle();
        handle.AddLights(scene);
    }
    public static void DeleteSceneLights(FPRenderScene scene)
    {
        checkHandle();
        handle.DeleteLights(scene);
    }
    private static void checkHandle()
    {
        if(handle == null)
        {
            GameObject handleObj = new GameObject("FPRenderMgr");
            handle = handleObj.AddComponent<FPRenderMgr>();
        }
    }
}
