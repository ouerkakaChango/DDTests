using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
using System.Linq;

namespace CenturyGame.PostProcessEditor
{
    public class DefaultPostProcessEffectEditor : PostProcessEffectBaseEditor
    {
        protected List<SerializedProperty> properties = new List<SerializedProperty>();

        public override void OnEnable()
        {
            foreach (var field in effect.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Where(t => t.IsPublic && DefinedAttribute(t, typeof(CenturyGame.PostProcess.EffectPropertyAttribute))))
            {
                properties.Add(SerializedObjectHandle.FindProperty(field.Name));
            }
        }

        public override void OnInspectorGUI()
        {
            SerializedObjectHandle.Update();

            FPEditorGUIUtility.BeginGroup(!effect.Enable);

            foreach (var property in properties)
            {
                EditorGUILayout.PropertyField(property, FPEditorGUIUtility.TempContent(property.name), true);
            }

            FPEditorGUIUtility.EndGroup();

            SerializedObjectHandle.ApplyModifiedProperties();
        }

        private static bool DefinedAttribute(FieldInfo t, Type attributeType)
        {
            return t.GetCustomAttributes(attributeType, false).Length != 0;
        }
    }
}
