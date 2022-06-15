using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using CenturyGame.PostProcess;

namespace CenturyGame.PostProcessEditor
{
    [CustomEditor(typeof(PostProcessProfile))]
    public class PostProcessProfileInspector : Editor
    {
        PostProcessProfile profile;
        EffectListEditor effectListEditor;

        private void OnEnable()
        {
            profile = target as PostProcessProfile;
            effectListEditor = new EffectListEditor(profile, serializedObject);
        }

        private void OnDisable()
        {
            effectListEditor.Dispose();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            effectListEditor.OnInspectorGUI();

            serializedObject.ApplyModifiedProperties();
        }

    }
}
