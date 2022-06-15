using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CenturyGame.PostProcess
{
    public class PostProcessProfile : ScriptableObject
    {
        public List<PostProcessItem> PostList = new List<PostProcessItem>();
        public List<IPostProcess> EffectList = new List<IPostProcess>();

        public T GetEffect<T>() where T : IPostProcess
        {
            foreach (var effect in EffectList)
            {
                if (effect is T)
                    return effect as T;
            }
            return null;
        }
    }
}