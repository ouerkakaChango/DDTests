using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CenturyGame.PostProcess
{
    [ExecuteAlways]
    public class PostProcessVolume : MonoBehaviour
    {
        public PostProcessProfile profile;
        public int priority;
        [Range(0, 1)]
        public float weight = 1;

        private void OnEnable()
        {
            PostProcessVolumeManager.Instance.Register(this);
        }

        private void OnDisable()
        {
            PostProcessVolumeManager.Instance.Unregister(this);
        }
    }
}