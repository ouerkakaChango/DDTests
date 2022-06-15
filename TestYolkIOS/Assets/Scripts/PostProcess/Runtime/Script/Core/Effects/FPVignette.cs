using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CenturyGame.PostProcess
{
    public enum VignetteMode
    {
        /// <summary>
        /// This mode offers parametric controls for the position, shape and intensity of the Vignette.
        /// </summary>
        Classic,

        /// <summary>
        /// This mode multiplies a custom texture mask over the screen to create a Vignette effect.
        /// </summary>
        Masked
    }

    [RegisteredEffect]
    public class FPVignette : IPostProcess
    {
        public override void Clear()
        {
            Enable = false;
            mode = VignetteMode.Classic;
            color = Color.black;
            center = new Vector2(0.5f, 0.5f);
            intensity = 0;
            smoothness = 0.2f;
            roundness = 1;
            rounded = false;
            mask = null;
            opacity = 1;
        }

        public override void Blend(IPostProcess other, float factor)
        {
            var o = other as FPVignette;
            if (!o) return;
            Enable |= o.Enable;
            mode = factor <= 0 ? mode : o.mode;
            color = Color.Lerp(color, o.color, factor);
            center = Vector2.Lerp(center, o.center, factor);
            intensity = Mathf.Lerp(intensity, o.intensity, factor);
            smoothness = Mathf.Lerp(smoothness, o.smoothness, factor);
            roundness = Mathf.Lerp(roundness, o.roundness, factor);
            rounded = factor <= 0 ? rounded : o.rounded;
            mask = factor <= 0 ? mask : o.mask;
            opacity = Mathf.Lerp(opacity, o.opacity, factor);
        }

        [EffectProperty]
        public VignetteMode mode = VignetteMode.Classic;

        [EffectProperty]
        public Color color = Color.black;

        [EffectProperty]
        public Vector2 center = new Vector2(0.5f, 0.5f);

        [EffectProperty]
        [Range(0, 1)]
        public float intensity = 0;

        [EffectProperty]
        [Range(0.01f, 1)]
        public float smoothness = 0.2f;

        [EffectProperty]
        [Range(0, 1)]
        public float roundness = 1;

        [EffectProperty]
        public bool rounded = false;

        [EffectProperty]
        public Texture mask = null;

        [EffectProperty]
        [Range(0, 1)]
        public float opacity = 1;
    }
}