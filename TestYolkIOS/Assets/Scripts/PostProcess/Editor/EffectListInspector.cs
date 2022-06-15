using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using System;
using System.Linq;
using CenturyGame.PostProcess;

namespace CenturyGame.PostProcessEditor
{
    public class EffectListEditor
    {
        static List<Type> g_postprocessTypes;
        static Dictionary<Type, Type> g_postProcessEditorTypes;

        PostProcessProfile profile;
        SerializedObject serializedObject;
        List<PostProcessEffectBaseEditor> effectEditors;
        SerializedProperty effectListProperty;

        public EffectListEditor(PostProcessProfile profile, SerializedObject serializedObject)
        {
            EnumAllPostProcessEffect();

            this.profile = profile;
            this.serializedObject = serializedObject;
            effectEditors = new List<PostProcessEffectBaseEditor>();

            if (profile != null)
            {
                effectListProperty = serializedObject.FindProperty("EffectList");

                foreach (var effect in profile.EffectList)
                {
                    CreateEditor(effect);
                }
            }

            Undo.undoRedoPerformed += OnUndoPerformed;
        }

        public void Dispose()
        {
            profile = null;
            serializedObject = null;
            effectListProperty = null;
            effectEditors.Clear();

            Undo.undoRedoPerformed -= OnUndoPerformed;
        }

        void OnUndoPerformed()
        {
            Debug.Log("undo");
        }

        void CreateEditor(IPostProcess effect)
        {
            Type editorType = null;
            if (!g_postProcessEditorTypes.TryGetValue(effect.GetType(), out editorType))
                editorType = typeof(DefaultPostProcessEffectEditor);

            var editor = (PostProcessEffectBaseEditor)Activator.CreateInstance(editorType);
            editor.Init(effect);
            editor.OnEnable();

            effectEditors.Add(editor);
        }

        public void OnInspectorGUI()
        {
            if (serializedObject == null)
                return;

            int removeID = -1;

            for (int i = 0; i < effectEditors.Count; ++i)
            {
                var effectEditor = effectEditors[i];

                bool isClose = false;
                if (FPEditorGUIUtility.ToggleGroupClose(effectEditor.effect.GetType().Name, true, ref effectEditor.effect.Display, effectEditor.enableProperty, ref isClose))
                {
                    effectEditor.OnInspectorGUI();
                }

                if (isClose)
                {
                    removeID = i;
                }
            }

            if (GUILayout.Button("Add Effect"))
            {
                var list = g_postprocessTypes.Except(profile.EffectList.Select(e => e.GetType()));
                var menu = new GenericMenu();

                foreach (var type in list)
                {
                    menu.AddItem(new GUIContent(type.Name), false, () => AddEffect(type));
                }

                menu.ShowAsContext();
            }

            if (removeID != -1)
                RemoveEffect(removeID);
        }

        void AddEffect(Type type)
        {
            serializedObject.Update();

            Undo.RecordObject(profile, "Add Effect");
            var effect = ScriptableObject.CreateInstance(type) as IPostProcess;
            effect.Enable = true;
            effect.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;

            Undo.RegisterCreatedObjectUndo(effect, "Add Effect");

            if (EditorUtility.IsPersistent(profile))
                AssetDatabase.AddObjectToAsset(effect, profile);

            ++effectListProperty.arraySize;
            var effectProperty = effectListProperty.GetArrayElementAtIndex(effectListProperty.arraySize - 1);
            effectProperty.objectReferenceValue = effect;

            CreateEditor(effect);

            serializedObject.ApplyModifiedProperties();

            if (EditorUtility.IsPersistent(profile))
            {
                EditorUtility.SetDirty(profile);
                AssetDatabase.SaveAssets();
            }
        }

        void RemoveEffect(int id)
        {
            //effectEditors[id].();
            effectEditors.RemoveAt(id);

            serializedObject.Update();

            var property = effectListProperty.GetArrayElementAtIndex(id);
            var effect = property.objectReferenceValue;

            property.objectReferenceValue = null;

            effectListProperty.DeleteArrayElementAtIndex(id);

            serializedObject.ApplyModifiedProperties();

            Undo.DestroyObjectImmediate(effect);

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
        }

        void EnumAllPostProcessEffect()
        {
            if (g_postprocessTypes == null || g_postProcessEditorTypes == null)
            {
                var assemblyTypes = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(t =>
                    {
                        // Ugly hack to handle mis-versioned dlls
                        var innerTypes = new Type[0];
                        try
                        {
                            innerTypes = t.GetTypes();
                        }
                        catch { }
                        return innerTypes;
                    });

                g_postprocessTypes = new List<Type>();
                g_postprocessTypes.AddRange(assemblyTypes.Where(t => t.IsSubclassOf(typeof(IPostProcess)) && !t.IsAbstract && t.IsDefined(typeof(RegisteredEffectAttribute), false)));

                g_postProcessEditorTypes = new Dictionary<Type, Type>();
                foreach (var type in assemblyTypes.Where(t => t.IsDefined(typeof(PostProcessEffectEditorAttribute), false) && !t.IsAbstract))
                {
                    var attribute = type.GetCustomAttributes(typeof(PostProcessEffectEditorAttribute), false)[0] as PostProcessEffectEditorAttribute;
                    g_postProcessEditorTypes.Add(attribute.effectType, type);
                }
            }
        }
    }
}