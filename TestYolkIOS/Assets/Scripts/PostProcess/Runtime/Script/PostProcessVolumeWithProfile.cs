using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CenturyGame.PostProcess
{
    [ExecuteAlways]
    public class VolumeWithProfile : MonoBehaviour
    {
        GameObject volumeObj;
        PostProcessVolume volume;
        protected PostProcessProfile profile;
        public int priority;
        [Range(0, 1)]
        public float weight = 1;

        public int Priority { get => priority; set { priority = value; UpdateVolume(); } }
        public float Weight { get => weight; set { weight = value; UpdateVolume(); } }

        // Start is called before the first frame update
        protected virtual void Awake()
        {
            volumeObj = new GameObject();
            volumeObj.hideFlags = HideFlags.HideAndDontSave;
            volume = volumeObj.AddComponent<PostProcessVolume>();

            profile = ScriptableObject.CreateInstance<PostProcessProfile>();
            profile.hideFlags = HideFlags.HideAndDontSave;

            volume.profile = profile;

            UpdateVolume();
        }

        protected virtual void OnValidate()
        {
            UpdateVolume();
        }

        protected virtual void OnEnable()
        {
            volumeObj.SetActive(true);
        }

        protected virtual void OnDisable()
        {
            volumeObj.SetActive(false);
        }

        protected void UpdateVolume()
        {
            if (volume != null)
            {
                volume.priority = priority;
                volume.weight = weight;
            }
        }

        // Update is called once per frame
        void OnDestroy()
        {
            if (volumeObj)
                DestroyImmediate(volumeObj);
            if (profile)
                DestroyImmediate(profile);
        }
    }
}