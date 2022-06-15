using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CenturyGame.PostProcess
{
    [RequireComponent(typeof(PostProcessVolume))]
    public class PostProcessTweenWithParticle : MonoBehaviour
    {
        public bool playOnAwake;
        public float lifeTime;
        public bool loop;
        public AnimationCurve weightCurve = new AnimationCurve();

        PostProcessVolume volume;
        ParticleSystem particle;
        bool isPlaying;
        float time;
        public bool IsPlaying { get => isPlaying; }

        private void Awake()
        {
            volume = GetComponent<PostProcessVolume>();

            if (playOnAwake)
                Play();

            particle = gameObject.AddComponent<ParticleSystem>();
            particle.hideFlags = HideFlags.HideAndDontSave | HideFlags.HideInInspector;
            particle.Stop();

            var pMain = particle.main;
            pMain.duration = 0.0001f;
            pMain.loop = false;

            var pEmission = particle.emission;
            pEmission.enabled = false;

            var pRender = GetComponent<ParticleSystemRenderer>();
            pRender.enabled = false;

            volume.weight = 0;
        }

        public void Play()
        {
            time = 0;
            isPlaying = true;
            UpdateVolume();
        }

        private void Update()
        {
            if (particle.isPlaying)
            {
                if (!IsPlaying)
                {
                    Play();
                    return;
                }
            }
            if (IsPlaying)
            {
                time += Time.deltaTime;
                if (time >= lifeTime)
                {
                    if (!loop)
                    {
                        time = lifeTime;
                        isPlaying = false;
                        volume.weight = 0;
                        return;
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