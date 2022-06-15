using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CenturyGame.PostProcess
{
    public class GaussianBlurController : VolumeWithProfile
    {
        FPGaussianBlur gaussianBlur;

        [Range(0, FPGaussianBlur.MaxIterationCount)]
        public int iterationCount = 2;
        public float sampleScale = 2;

        public int IterationCount { get => iterationCount; set { iterationCount = value; UpdateParams(); } }
        public float SampleScale { get => sampleScale; set { sampleScale = value; UpdateParams(); } }

        protected override void Awake()
        {
            base.Awake();

            gaussianBlur = ScriptableObject.CreateInstance<FPGaussianBlur>();
            gaussianBlur.Enable = true;
            profile.EffectList.Add(gaussianBlur);

            UpdateParams();
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            UpdateParams();
        }

        void UpdateParams()
        {
            if (gaussianBlur != null)
            {
                gaussianBlur.iterationCount = iterationCount;
                gaussianBlur.sampleScale = sampleScale;
            }
        }
    }
}
