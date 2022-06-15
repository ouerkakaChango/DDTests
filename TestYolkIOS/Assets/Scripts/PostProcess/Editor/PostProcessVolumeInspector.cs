using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using CenturyGame.PostProcess;

namespace CenturyGame.PostProcessEditor
{
    [CustomEditor(typeof(PostProcessVolume))]
    public class PostProcessVolumeInspector : Editor
    {
        PostProcessVolume Volume;
        EffectListEditor effectListEditor;

        private void OnEnable()
        {
            Volume = target as PostProcessVolume;

            RefreshEffectList(Volume.profile);
        }

        public override void OnInspectorGUI()
        {
            bool priorityDirty = false;

            serializedObject.Update();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("priority"));
            priorityDirty = EditorGUI.EndChangeCheck();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("weight"));

            EditorGUI.BeginChangeCheck();
            var profileProperty = serializedObject.FindProperty("profile");
            EditorGUILayout.PropertyField(profileProperty);
            if (EditorGUI.EndChangeCheck())
            {
                if (profileProperty.objectReferenceValue == null)
                    effectListEditor.Dispose();
                else
                    RefreshEffectList(profileProperty.objectReferenceValue as PostProcessProfile);
            }

            EditorGUILayout.Space();

            effectListEditor.OnInspectorGUI();

            serializedObject.ApplyModifiedProperties();

            if (priorityDirty)
                CenturyGame.PostProcess.PostProcessVolumeManager.Instance.SetVolumeDirty();
        }

        void RefreshEffectList(PostProcessProfile profile)
        {
            SerializedObject obj = profile != null ? new SerializedObject(profile) : null;
            effectListEditor = new EffectListEditor(profile, obj);
        }
    }
}