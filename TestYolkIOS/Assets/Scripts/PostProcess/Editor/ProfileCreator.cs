using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.ProjectWindowCallback;
using UnityEditor;
using System.IO;
using CenturyGame.PostProcess;

namespace CenturyGame.PostProcessEditor
{
    public static class ProfileCreator
    {
        [MenuItem("Assets/Create/DD-PostProcess Profile")]
        static void CreateProfile()
        {
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, ScriptableObject.CreateInstance<DoCreatePostProcessProfile>(), "New PostProcess Profile.asset", null, null);
        }

        public static PostProcessProfile CreatePostProcessProfileAtPath(string path)
        {
            var profile = ScriptableObject.CreateInstance<PostProcessProfile>();
            profile.name = Path.GetFileName(path);
            profile.PostList.Add(CreateFinalItem());
            AssetDatabase.CreateAsset(profile, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return profile;
        }

        static PostProcessItem CreateFinalItem()
        {
            FPFinal final = new FPFinal();
            final.Init();
            PostProcessItem ppi = new PostProcessItem();

            ppi.Name = final.Title;
            ppi.Enable = true;

            var plist = new PostProcessItemKV[final.Propertys.Length];
            for (int j = 0; j < final.Propertys.Length; j++)
            {
                PostProcessItemKV ppiv = new PostProcessItemKV();
                plist[j] = ppiv;
                ppiv.Key = final.Propertys[j];
                ppiv.GetValue(final);
            }
            ppi.PList = plist;
            return ppi;
        }
    }

    class DoCreatePostProcessProfile : EndNameEditAction
    {
        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            var profile = ProfileCreator.CreatePostProcessProfileAtPath(pathName);
            ProjectWindowUtil.ShowCreatedAsset(profile);
        }
    }
}