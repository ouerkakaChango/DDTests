using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
namespace CenturyGame.PostProcess
{
    public sealed class FPRenderTextureManager : IDisposable
    {
        private static FPRenderTextureManager instance;
        public static FPRenderTextureManager Instance
        {
            get
            {
                return instance ?? (instance = new FPRenderTextureManager());
            }
        }
        HashSet<RenderTexture> m_TemporaryRTs;
        HashSet<RenderTexture> m_UITemporaryRTs;

        public FPRenderTextureManager()
        {
            m_TemporaryRTs = new HashSet<RenderTexture>();
            m_UITemporaryRTs = new HashSet<RenderTexture>();
        }

        public RenderTexture Get(RenderTexture baseRenderTexture)
        {
            return Get(baseRenderTexture.width
            , baseRenderTexture.height
            , baseRenderTexture.depth
            , baseRenderTexture.format
            , baseRenderTexture.sRGB ? RenderTextureReadWrite.sRGB : RenderTextureReadWrite.Linear
            , baseRenderTexture.filterMode
            , baseRenderTexture.wrapMode);
        }
        public RenderTexture Get(int width, int height, int depthBuffer = 0
        , RenderTextureFormat format = RenderTextureFormat.ARGBHalf
        , RenderTextureReadWrite rw = RenderTextureReadWrite.Linear
        , FilterMode filterMode = FilterMode.Bilinear
        , TextureWrapMode wrapMode = TextureWrapMode.Clamp

        , string name = "FactoryTempTexture"
        , bool uiMode = false)
        {
            var rt = RenderTexture.GetTemporary(width, height, depthBuffer, format, rw); // add forgotten param rw
            rt.filterMode = filterMode;
            rt.wrapMode = wrapMode;
            rt.name = name;
            if (uiMode)
            {
                m_UITemporaryRTs.Add(rt);
            }
            else
            {
                m_TemporaryRTs.Add(rt);
            }
            return rt;
        }

        public void Release(RenderTexture rt)
        {
            if (rt == null)
            {
                return;
            }

            if (m_TemporaryRTs.Contains(rt))
            {
                m_TemporaryRTs.Remove(rt);
                RenderTexture.ReleaseTemporary(rt);//直接执行这一句编辑器会闪退
            }

            if (m_UITemporaryRTs.Contains(rt))
            {
                m_UITemporaryRTs.Remove(rt);
                RenderTexture.ReleaseTemporary(rt);
            }
        }

        public void ReleaseAll()
        {
            var enumerator = m_TemporaryRTs.GetEnumerator();

            while (enumerator.MoveNext())
            {
                RenderTexture.ReleaseTemporary(enumerator.Current);
            }
            m_TemporaryRTs.Clear();
        }

        public void Dispose()
        {
            ReleaseAll();
        }
    }
}