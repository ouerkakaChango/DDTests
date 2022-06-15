using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FPRenderScene : MonoBehaviour
{
    public LayerMask LightMask;
    [NonSerialized]
    public List<Light> PointLight = new List<Light>();
    public int ObjId = 0;
    public string SceneName;
    public bool Destroy = false;
    private void Start()
    {
        SceneName = gameObject.scene.name;
        getLights(transform, PointLight);
        if(PointLight.Count > 0)
        {
            ObjId = gameObject.GetInstanceID();
            FPRenderMgr.AddSceneLights(this);
        }
        Debug.Log("OnEnable:" + transform.root.name + ":" + PointLight.Count);
    }
    private void OnDestroy()
    {
        Destroy = true;
        if (ObjId != 0)
        {
            FPRenderMgr.DeleteSceneLights(this);
        }
        Debug.Log("OnDisable:" + transform.root.name + ":" + PointLight.Count);
    }
    public void SetEnable(bool enable)
    {
        if(!Destroy && gameObject.active != enable)
        {
            gameObject.SetActive(enable);
        }
    }
    private void getLights(Transform t, List<Light> lights)
    {
        Light l = t.GetComponent<Light>();
        if(l != null 
            && l.type == LightType.Point
#if UNITY_EDITOR
            && l.lightmapBakeType != LightmapBakeType.Baked
#endif
            && ((l.cullingMask & ((int)LightMask)) != 0))
        {
            lights.Add(l);
        }
        foreach (Transform ch in t)
        {
            getLights(ch, lights);
        }
    }
}
