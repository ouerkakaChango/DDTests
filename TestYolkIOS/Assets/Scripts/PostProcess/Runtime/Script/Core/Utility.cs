using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CenturyGame.PostProcess
{
    public static class Utility
    {
        static Mesh s_FullscreenTriangle;

        public static Mesh FullscreenTriangle
        {
            get
            {
                if (s_FullscreenTriangle != null)
                    return s_FullscreenTriangle;

                s_FullscreenTriangle = new Mesh { name = "Fullscreen Triangle" };

                // Because we have to support older platforms (GLES2/3, DX9 etc) we can't do all of
                // this directly in the vertex shader using vertex ids :(
                s_FullscreenTriangle.SetVertices(new List<Vector3>
                {
                    new Vector3(-1f, -1f, 0f),
                    new Vector3(-1f,  3f, 0f),
                    new Vector3( 3f, -1f, 0f)
                });
                s_FullscreenTriangle.SetIndices(new[] { 0, 1, 2 }, MeshTopology.Triangles, 0, false);
                s_FullscreenTriangle.UploadMeshData(true);

                return s_FullscreenTriangle;
            }
        }

        public static void DrawFullScreen(CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier destination, Material material, int pass, bool clear = false, Rect? viewport = null)
        {
            cmd.SetGlobalTexture("_MainTex", source);
            cmd.SetRenderTarget(destination);
            if (clear)
                cmd.ClearRenderTarget(true, true, Color.clear);

            cmd.DrawMesh(FullscreenTriangle, Matrix4x4.identity, material, 0, pass);
        }
    }
}