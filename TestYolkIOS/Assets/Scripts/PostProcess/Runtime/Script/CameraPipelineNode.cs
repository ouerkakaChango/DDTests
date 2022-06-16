using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using CenturyGame.PostProcess;
namespace CenturyGame.PostProcess
{
    [ExecuteInEditMode]
    public class CameraPipelineNode : CameraPipeline
    {
        Camera subCam;
        CommandBuffer cmd_before;
        CommandBuffer cmd_after;
        public override RenderTexture ColorRT => rt;
        protected RenderTexture rt;
        bool init = false;

        private void OnEnable()
        {
            if(Screen.width<=0)
            {
                return;
            }
            subCam = GetComponent<Camera>();
            rt = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.DefaultHDR, 0);
            subCam.SetTargetBuffers(rt.colorBuffer, rt.depthBuffer);

            cmd_before = new CommandBuffer() { name = "cmd_before" };
            cmd_after = new CommandBuffer() { name = "cmd_after" };

            cmd_after.Blit(rt, null as RenderTexture);
            subCam.AddCommandBuffer(CameraEvent.AfterEverything, cmd_after);

            subCam.AddCommandBuffer(CameraEvent.BeforeForwardOpaque, cmd_before);
        }

        private void OnDisable()
        {
            if (frontPipeline != null)
            {
                frontPipeline.ChangeDoBlit(true);
            }
            init = false;
        }

        private void OnPreRender()
        {
            if(cmd_before==null)
            {
                return;
            }

            cmd_before.Clear();
            cmd_before.Blit(frontPipeline.ColorRT.colorBuffer, rt);
            if (frontPipeline!=null && init == false)
            {
                frontPipeline.ChangeDoBlit(false);
               init = true;
            }
        }

        // Update is called once per frame
        void Update()
        {

        }

        //################################

        public override void ChangeDoBlit(bool doBlit)
        {
            if (cmd_after == null)
            {
                return;
            }
            if (doBlit)
            {
                subCam.AddCommandBuffer(CameraEvent.AfterEverything, cmd_after);
            }
            else
            {
                subCam.RemoveCommandBuffer(CameraEvent.AfterEverything, cmd_after);
            }
        }
    }
}
