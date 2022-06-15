using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace CenturyGame.PostProcess
{
    public class IPostProcess : ScriptableObject
    {
        public string Title;
        public bool Enable;
        public bool LastEnable = false;
        public bool Display;
        public uint Weight;
        public string[] Propertys = null;

        public virtual void OnRenderHandle(ref RenderTexture source, ref RenderTexture target, ref RenderTexture depth, ref int count)
        {
            ++count;
        }
        public virtual void Init() { }
        public virtual void OnPreCull() { }
        public virtual void OnPostRender() { }
        public virtual void Update() { }
        public virtual string GUIMessage() { return Title + ":" + Enable; }
        //public virtual void OnEnable() { Enable = true; Debug.Log(Title + "->OnEnable" + Enable); }//如果使用这个名称，系统会自动调用，被坑了

        public virtual void DoEnable(CenturyGame.PostProcess.PostProcessHandle parameter)
        {
            Enable = true;
            //Debug.Log(Title + "->OnEnable" + Enable);
        }

        public virtual void DoDisable()
        {
            Enable = false;
            //Debug.Log(Title + "->OnDisable" + Enable);
        }

        public virtual void ReSize(Resolution size)
        {

        }

        public bool CheckConfig(uint config)
        {
            if ((config & Weight) > 0) return false;
            return true;
        }

        public virtual void Blend(IPostProcess other, float factor)
        {
        }

        public virtual void Clear()
        {
        }
    }

    public delegate void IPostProcessAction(IPostProcess post);
    public delegate T LoadAction<T>(string path) where T : Object;
    public delegate Material GetUITargetHandle(ref RenderTexture target);
}