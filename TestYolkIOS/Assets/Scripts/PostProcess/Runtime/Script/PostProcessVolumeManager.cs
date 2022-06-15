using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CenturyGame.PostProcess
{
    public class PostProcessVolumeManager
    {
        static PostProcessVolumeManager s_Instance;

        public static PostProcessVolumeManager Instance
        {
            get
            {
                if (s_Instance == null)
                    s_Instance = new PostProcessVolumeManager();
                return s_Instance;
            }
        }

        bool dirty = true;
        List<PostProcessVolume> volumes;

        PostProcessVolumeManager()
        {
            volumes = new List<PostProcessVolume>();
        }

        public void Register(PostProcessVolume volume)
        {
            dirty = true;
            volumes.Add(volume);
        }

        public void Unregister(PostProcessVolume volume)
        {
            dirty = true;
            volumes.Remove(volume);
        }

        public void SetVolumeDirty()
        {
            dirty = true;
        }

        public PostProcessVolume GetVolume()
        {
            if (dirty)
            {
                SortVolumes(volumes);
                dirty = false;
            }

            PostProcessVolume volume = null;
            for (int i = volumes.Count - 1; i >= 0; --i)
            {
                var v = volumes[i];
                if (!v.isActiveAndEnabled || v.profile == null)
                    continue;

                volume = v;
                break;
            }

            return volume;
        }

        static void SortVolumes(List<PostProcessVolume> volumes)
        {
            for (int i = 1; i < volumes.Count; ++i)
            {
                var temp = volumes[i];
                int j = i - 1;

                while (j >= 0 && volumes[j].priority > temp.priority)
                {
                    volumes[j + 1] = volumes[j];
                    --j;
                }

                volumes[j + 1] = temp;
            }
        }

        void ClearSettings(Dictionary<System.Type, IPostProcess> settings)
        {
            foreach (var kv in settings)
            {
                kv.Value.Clear();
            }
        }

        void OverrideSetting(Dictionary<System.Type, IPostProcess> settings, PostProcessProfile profile, float factor)
        {
            foreach (var item in profile.EffectList)
            {
                if (!item.Enable)
                    continue;

                if (settings.TryGetValue(item.GetType(), out var setting))
                {
                    setting.Blend(item, factor);
                }
            }
        }

        public void UpdateSettings(Dictionary<System.Type, IPostProcess> settings)
        {
            ClearSettings(settings);

            if (dirty)
            {
                SortVolumes(volumes);
                dirty = false;
            }

            foreach (var v in volumes)
            {
                if (!v)
                    continue;

                if (!v.isActiveAndEnabled || v.profile == null || v.weight <= 0)
                    continue;

                OverrideSetting(settings, v.profile, Mathf.Clamp01(v.weight));
            }
        }
    }
}