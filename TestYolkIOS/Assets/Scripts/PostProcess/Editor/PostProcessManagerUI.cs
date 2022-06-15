using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using System.Reflection;
using System.Linq;
using CenturyGame.PostProcess;

namespace CenturyGame.PostProcessEditor
{
[CanEditMultipleObjects]
[CustomEditor(typeof(PostProcessManager))]
public class PostProcessManagerUI : Editor
{
    private PostProcessManager targetHandle;

    SerializedProperty antiAliasing;
    SerializedProperty contrastThreshold;
    SerializedProperty relativeThreshold;
    SerializedProperty pointScale;
    SerializedProperty sharpness;

    void OnEnable()
    {
        targetHandle = target as PostProcessManager;

        logo = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Standard Assets/PostProcess/Editor/logo.png");

        antiAliasing = serializedObject.FindProperty("antiAliasing");
        contrastThreshold = serializedObject.FindProperty("contrastThreshold");
        relativeThreshold = serializedObject.FindProperty("relativeThreshold");
        pointScale = serializedObject.FindProperty("pointScale");
        sharpness = serializedObject.FindProperty("sharpness");
    }
    private void OnDestroy()
    {
        logo = null;
    }
    private Texture2D logo = null;
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(antiAliasing);
        if (antiAliasing.enumValueIndex == (int)PostProcessHandle.AntiAliasing.FXAA)
        {
            DoFXAAGUI();
        }

        serializedObject.ApplyModifiedProperties();
    }

    void DoFXAAGUI()
    {
        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(contrastThreshold);
        EditorGUILayout.PropertyField(relativeThreshold);
        EditorGUILayout.PropertyField(pointScale);
        EditorGUILayout.PropertyField(sharpness);
    }
}
}
