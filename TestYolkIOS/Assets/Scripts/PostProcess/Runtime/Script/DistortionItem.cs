using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CenturyGame.PostProcess
{
    public class DistortionItem : MonoBehaviour
    {
        // Start is called before the first frame update
        private FPDistortion post;
        private Renderer renderer;

        public bool DistortionOnly = true;
        private void OnEnable()
        {
            StartCoroutine(AddToRenderList());
        }
        private IEnumerator AddToRenderList()
        {
            yield return new WaitForEndOfFrame();
            renderer = GetComponent<Renderer>();
            post = PostProcessManager.GetPostProcess<FPDistortion>();
            if (post != null && renderer != null)
            {
                post.AddRenderer(renderer);
                if (DistortionOnly)
                    renderer.gameObject.layer = 2;
            }
            yield return null;
        }
        private void OnDisable()
        {
            if (post != null && renderer != null)
            {
                renderer.gameObject.layer = 0;
                if (DistortionOnly)
                    post.RemoveRenderer(renderer);
            }
        }
    }
}