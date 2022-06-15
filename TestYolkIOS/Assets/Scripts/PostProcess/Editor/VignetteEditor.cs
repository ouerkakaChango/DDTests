using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using CenturyGame.PostProcess;

namespace CenturyGame.PostProcessEditor
{
    [PostProcessEffectEditor(typeof(FPVignette))]
    public class VigenetteEditor : PostProcessEffectBaseEditor
    {
        SerializedProperty mode;
        SerializedProperty color;
        SerializedProperty center;
        SerializedProperty intensity;
        SerializedProperty smoothness;
        SerializedProperty roundness;
        SerializedProperty rounded;
        SerializedProperty mask;
        SerializedProperty opacity;

        public override void OnEnable()
        {
            mode = SerializedObjectHandle.FindProperty("mode");
            color = SerializedObjectHandle.FindProperty("color");
            center = SerializedObjectHandle.FindProperty("center");
            intensity = SerializedObjectHandle.FindProperty("intensity");
            smoothness = SerializedObjectHandle.FindProperty("smoothness");
            roundness = SerializedObjectHandle.FindProperty("roundness");
            rounded = SerializedObjectHandle.FindProperty("rounded");
            mask = SerializedObjectHandle.FindProperty("mask");
            opacity = SerializedObjectHandle.FindProperty("opacity");
        }

        public override void OnInspectorGUI()
        {
            SerializedObjectHandle.Update();

            EditorGUILayout.PropertyField(mode);
            EditorGUILayout.PropertyField(color);

            if (mode.enumValueIndex == (int)VignetteMode.Classic)
            {
                EditorGUILayout.PropertyField(center);
                EditorGUILayout.PropertyField(intensity);
                EditorGUILayout.PropertyField(smoothness);
                EditorGUILayout.PropertyField(roundness);
                EditorGUILayout.PropertyField(rounded);
            }
            else
            {
                EditorGUILayout.PropertyField(mask);
                EditorGUILayout.PropertyField(opacity);
            }

            SerializedObjectHandle.ApplyModifiedProperties();
        }
    }
}