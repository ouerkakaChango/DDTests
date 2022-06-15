using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

namespace CenturyGame.PostProcess
{
    internal class Available
    {
        public bool Value { get; set; }
        public Available(bool available)
        {
            Value = available;
        }

        public static implicit operator bool(Available available)
        {
            return available.Value;
        }
    }

    public static class PostProcessHub
    {
        public static readonly List<Type> g_postprocessTypes;
        internal static readonly Dictionary<Type, Available> g_effectAvailables;
        public static bool NeedRefreshDepth { get; set; } = false;
        public static bool AntiAliasingAvailable { get; set; } = true;

        static PostProcessHub()
        {
            g_postprocessTypes = new List<Type>();
            g_effectAvailables = new Dictionary<Type, Available>();
            ReloadPostProcessEffects();
            InitDisableEffectList();
        }

#if UNITY_EDITOR
        // Called every time Unity recompile scripts in the editor. We need this to keep track of
        // any new custom effect the user might add to the project
        [UnityEditor.Callbacks.DidReloadScripts]
        static void OnEditorReload()
        {
            ReloadPostProcessEffects();
            InitDisableEffectList();
        }
#endif

        static void ReloadPostProcessEffects()
        {
            g_postprocessTypes?.Clear();

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

            g_postprocessTypes.AddRange(assemblyTypes.Where(t => t.IsSubclassOf(typeof(IPostProcess)) && !t.IsAbstract));
        }

        static void InitDisableEffectList()
        {
            g_effectAvailables.Clear();

            foreach (var type in CenturyGame.PostProcess.PostProcessHub.g_postprocessTypes)
            {
                g_effectAvailables.Add(type, new Available(true));
            }
        }

        internal static Available GetEffectAvailable<T>() where T : IPostProcess
        {
            g_effectAvailables.TryGetValue(typeof(T), out var available);
            return available;
        }

        public static bool IsEffectAvailable(Type type)
        {
            if (PostProcessHub.g_effectAvailables.TryGetValue(type, out var available))
            {
                return available.Value;
            }
            return true;
        }

        public static void DisableEffect(Type type)
        {
            SetEffectAvailable(type, true);
        }

        public static void EnableEffect(Type type)
        {
            SetEffectAvailable(type, false);
        }

        public static void SetEffectAvailable(Type type, bool available)
        {
            if (PostProcessHub.g_effectAvailables.ContainsKey(type))
            {
                PostProcessHub.g_effectAvailables[type].Value = available;
            }
        }
    }
}
