using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CenturyGame.PostProcess
{
    [RequireComponent(typeof(PostProcessVolume))]
    public class PostProcessTween : MonoBehaviour
    {
        public enum PlayTrigger
        {
            None,
            Start,
            OnEnable,
        }
        public PlayTrigger playTrigger = PlayTrigger.Start;
        public float lifeTime;
        public bool loop;
        public AnimationCurve weightCurve = new AnimationCurve();

        PostProcessVolume volume;
        bool isPlaying;
        float time;
        public bool IsPlaying { get => isPlaying; }

        private void Awake()
        {
            volume = GetComponent<PostProcessVolume>();
        }

        private void Start()
        {
            if (playTrigger == PlayTrigger.Start)
            {
                Play();
            }
        }

        private void OnEnable()
        {
            if (playTrigger == PlayTrigger.OnEnable)
            {
                Play();
            }
        }

        public void Play()
        {
            time = 0;
            isPlaying = true;
            UpdateVolume();
        }

        private void Update()
        {
            if (isPlaying)
            {
                time += Time.deltaTime;
                if (time >= lifeTime)
                {
                    if (!loop)
                    {
                        time = lifeTime;
                        isPlaying = false;
                    }
                    else
                    {
                        time = time - lifeTime;
                    }
                }
                UpdateVolume();
            }
        }

        void UpdateVolume()
        {
            float newWeight = weightCurve.Evaluate(lifeTime > 0 ? time / lifeTime : 0);
            volume.weight = newWeight;
        }

        private void OnValidate()
        {
            lifeTime = Mathf.Max(lifeTime, 0);
        }
    }
}
