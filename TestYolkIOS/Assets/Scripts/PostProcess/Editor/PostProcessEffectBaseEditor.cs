using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using CenturyGame.PostProcess;

namespace CenturyGame.PostProcessEditor
{
    public abstract class PostProcessEffectBaseEditor
    {
        public IPostProcess effect;
        protected SerializedObject SerializedObjectHandle;
        public SerializedProperty enableProperty;

        public void Init(IPostProcess effect)
        {
            this.effect = effect;
            SerializedObjectHandle = new SerializedObject(effect);
            enableProperty = SerializedObjectHandle.FindProperty("Enable");
        }

        public virtual void OnEnable()
        {
        }

        public abstract void OnInspectorGUI();
    }
}