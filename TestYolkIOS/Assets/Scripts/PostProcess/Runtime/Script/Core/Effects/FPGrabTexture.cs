using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;
using System.Collections.Generic;
namespace CenturyGame.PostProcess
{
    public class FPGrabTexture : IPostProcess
    {
        private CommandBuffer opaqueCmdBuffer = null;
        private CommandBuffer transparentCmdBuffer = null;
        private int opaqueCopyID = 0;
        private int transparentCopyID = 0;
        private CenturyGame.PostProcess.PostProcessHandle para;
        public override void Init()
        {
            Title = "FPGrabTexture";
            //这里是需要暴露到编辑器面板的属性名称
            Propertys = new string[] { };
            checkSupport();
        }

        public override void DoEnable(CenturyGame.PostProcess.PostProcessHandle cam)
        {
            para = cam;
            AddOpaqueCmd();
            //AddTransparentCmd();
        }

        public override void DoDisable()
        {
            RemoveOpaqueCmd();
            //RemoveTransparentCmd();
        }

        public override void OnRenderHandle(ref RenderTexture source, ref RenderTexture destination, ref RenderTexture depth, ref int count)
        {
        }

        void checkSupport()
        {
        }

        void AddOpaqueCmd()
        {
            //GrabPass CommandBuffer
            opaqueCmdBuffer = new CommandBuffer();
            opaqueCmdBuffer.name = "FP_Grab_Opaque_CMD";
            opaqueCopyID = Shader.PropertyToID("_FPGrabOpaque");

            RenderTextureFormat rtformat = RenderTextureFormat.Default;
            if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf))
            {
                rtformat = RenderTextureFormat.ARGBHalf;
            }
            else if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RGB111110Float))
            {
                rtformat = RenderTextureFormat.RGB111110Float;
            }

            opaqueCmdBuffer.GetTemporaryRT(opaqueCopyID, -1, -1, 0, FilterMode.Bilinear, rtformat);
            opaqueCmdBuffer.Blit(BuiltinRenderTextureType.CurrentActive, opaqueCopyID);
            opaqueCmdBuffer.SetGlobalTexture("_FPGrabOpaque", opaqueCopyID);
            para.MainCamera.AddCommandBuffer(CameraEvent.AfterSkybox, opaqueCmdBuffer);
        }

        void RemoveOpaqueCmd()
        {
            if (null != opaqueCmdBuffer)
            {
                opaqueCmdBuffer.ReleaseTemporaryRT(opaqueCopyID);
                para.MainCamera.RemoveCommandBuffer(CameraEvent.AfterSkybox, opaqueCmdBuffer);
                opaqueCmdBuffer.Clear();
                opaqueCmdBuffer.Release();
            }
        }

        void AddTransparentCmd()
        {
            //GrabPass CommandBuffer
            transparentCmdBuffer = new CommandBuffer();
            transparentCmdBuffer.name = "FP_Grab_Transparent_CMD";
            transparentCopyID = Shader.PropertyToID("_FPGrabTransparent");
            transparentCmdBuffer.GetTemporaryRT(transparentCopyID, -1, -1, 0, FilterMode.Bilinear, RenderTextureFormat.RGB111110Float);
            transparentCmdBuffer.Blit(BuiltinRenderTextureType.CurrentActive, transparentCopyID);
            transparentCmdBuffer.SetGlobalTexture("_FPGrabTransparent", transparentCopyID);
            para.MainCamera.AddCommandBuffer(CameraEvent.AfterForwardAlpha, transparentCmdBuffer);
        }

        void RemoveTransparentCmd()
        {
            if (null != transparentCmdBuffer)
            {
                transparentCmdBuffer.ReleaseTemporaryRT(transparentCopyID);
                para.MainCamera.RemoveCommandBuffer(CameraEvent.AfterForwardAlpha, transparentCmdBuffer);
                transparentCmdBuffer.Clear();
                transparentCmdBuffer.Release();
            }
        }
    }
}