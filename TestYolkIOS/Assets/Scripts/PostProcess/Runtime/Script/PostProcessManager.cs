using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
#if UNITY_EDITOR
using UnityEditor;
#endif
namespace CenturyGame.PostProcess
{
[ExecuteAlways]
public class PostProcessManager : PostProcessHandle
{
    private void OnEnable()
    {
        if (Instance != null && Instance != this)
        {
            Instance.MainCamera.gameObject.SetActive(false);
        }
        base.Init();

        this.GetUITarget = PostProcessManagerHost.SetMgr(this, true);
        bool flag3 = this.GetUITarget == null;
        if (flag3)
        {
            base.DoEnable();
        }
        _DoEnable();
    }

    void OnDisable()
    {
        GetUITarget = PostProcessManagerHost.SetMgr(this, false);
        if (GetUITarget == null)
            DoDisable();
        _DoDisable();
    }

    /// <summary>
    /// 获取子对应后渲染
    /// </summary>
    public static T GetPostProcess<T>() where T : IPostProcess
    {
        return null;
    }

    /// <summary>
    /// LUA调用接口，获取后处理
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>

    /// <summary>
    /// 返回后渲染RT的大小
    /// </summary>
    /// <param name="resolutionScale"></param>
    public static Resolution Resolution()
    {
        if (Instance != null)
        {
            return new Resolution();
        }
        return Instance.PostResolution;
    }

    public static void SetResolutionScale(float scale)
    {
        ResolutionScale = scale;
        if (Instance != null)
        {
            Instance.DoDisable();
            Instance.DoEnable();
        }
    }

    /// <summary>
    /// 增加屏幕尺寸发生改变时的事件
    /// </summary>
    /// <param name="onChange"></param>
    /// <param name="add"></param>

    public static void ScreenChange(ScreenChange onChange, bool add, bool doIt)
    {
        if (Instance != null)
        {
            onChange(false, Screen.width, Screen.height);
            return;
        }
        if (add)
        {
            Instance.OnScreenChange += onChange;
        }
        else
        {
            Instance.OnScreenChange -= onChange;
        }
        if (doIt)
        {
            //Debug.Log("Instance.ScreenWidth: " + Instance.ScreenWidth + "Instance.ScreenHeight: " + Instance.ScreenHeight);
            onChange(true, Instance.PostResolution.width, Instance.PostResolution.height);
        }
    }
    }
}
