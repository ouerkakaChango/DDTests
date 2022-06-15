using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CenturyGame.PostProcess
{
    [ExecuteInEditMode]
    public class FxHdrItem : MonoBehaviour
    {
        // Start is called before the first frame update
        private FPBloom post;
        private Renderer[] renderer;
        private bool _init = true;
        void OnEnable()
        {
            init();
        }
        void Start()
        {
            init();
        }
        private void init()
        {
            if (_init)
            {
                post = PostProcessManager.GetPostProcess<FPBloom>();
                if (post != null)
                {
                    List<Renderer> list = new List<Renderer>();
                    GetRender(transform, list);
                    renderer = list.ToArray();
                    for (int i = 0; i < renderer.Length; i++)
                    {
                        post.AddRenderer(renderer[i]);
                    }
                    _init = false;
                }
            }

        }
        void OnDisable()
        {
            _init = true;
            if (post != null)
            {
                if (renderer != null)
                {
                    for (int i = 0; i < renderer.Length; i++)
                    {
                        post.RemoveRenderer(renderer[i]);
                    }
                }
                post = null;
                renderer = null; ;
            }
        }

        void GetRender(Transform t, List<Renderer> list)
        {
            Renderer r = t.GetComponent<Renderer>();
            if (r != null && t.gameObject.activeSelf)
            {
                list.Add(r);
            }
            foreach (Transform i in t)
            {
                GetRender(i, list);
            }
        }
    }
}