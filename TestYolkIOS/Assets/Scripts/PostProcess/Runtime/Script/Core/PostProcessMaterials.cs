using UnityEngine;
using System;

namespace CenturyGame.PostProcess
{
    public class PostProcessShaders
    {
        public readonly Shader hbaoShader;
        public readonly Shader ssssShader;
        public readonly Shader bloomShader;
        public readonly Shader godRayShader;
        public readonly Shader radialBlurShader;
        public readonly Shader depthOfFieldShader;
        public readonly Shader gaussianBlurShader;
        public readonly Shader toneMappingShader;
        public readonly Shader colorGradingShader;
        public readonly Shader colorGradingBakerShader;
        public readonly Shader vignetteShader;
        public readonly Shader fxaaShader;
        public readonly Shader finalShader;

        public PostProcessShaders()
        {
            hbaoShader = PostProcessHandle.LoadShader("Shaders/Post/FPHBAO");
            ssssShader = PostProcessHandle.LoadShader("Shaders/Post/FPSSSS");
            bloomShader = PostProcessHandle.LoadShader("Shaders/Post/FPBloom");
            godRayShader = PostProcessHandle.LoadShader("Shaders/Post/FPGodRay");
            radialBlurShader = PostProcessHandle.LoadShader("Shaders/Post/FPRadialBlur");
            depthOfFieldShader = PostProcessHandle.LoadShader("Shaders/Post/FPDepthOfField");
            gaussianBlurShader = PostProcessHandle.LoadShader("Shaders/Post/FPGaussianBlur");
            toneMappingShader = PostProcessHandle.LoadShader("Shaders/Post/FPTonemapping");
            colorGradingShader = PostProcessHandle.LoadShader("Shaders/Post/FPColorGrading");
            colorGradingBakerShader = PostProcessHandle.LoadShader("Shaders/Post/FPColorGradingBaker");
            vignetteShader = PostProcessHandle.LoadShader("Shaders/Post/FPVignette");
            fxaaShader = PostProcessHandle.LoadShader("Shaders/Post/FPFXAA3");
            finalShader = PostProcessHandle.LoadShader("Shaders/Post/FPFinal");
        }
    }


    public class PostProcessMaterials : IDisposable
    {
        public readonly Material hbaoMat;
        public readonly Material ssssMat;
        public readonly Material bloomMat;
        public readonly Material godRayMat;
        public readonly Material radialBlurMat;
        public readonly Material depthOfFieldMat;
        public readonly Material gaussianBlurMat;
        public readonly Material toneMappingMat;
        public readonly Material colorGradingMat;
        public readonly Material colorGradingBakerMat;
        public readonly Material vignetteMat;
        public readonly Material fxaaMat;
        public readonly Material finalMat;

        public PostProcessMaterials(PostProcessShaders shaders)
        {
            hbaoMat = new Material(shaders.hbaoShader);
            ssssMat = new Material(shaders.ssssShader);
            bloomMat = new Material(shaders.bloomShader);
            godRayMat = new Material(shaders.godRayShader);
            radialBlurMat = new Material(shaders.radialBlurShader);
            depthOfFieldMat = new Material(shaders.depthOfFieldShader);
            gaussianBlurMat = new Material(shaders.gaussianBlurShader);
            toneMappingMat = new Material(shaders.toneMappingShader);
            colorGradingMat = new Material(shaders.colorGradingShader);
            colorGradingBakerMat = new Material(shaders.colorGradingBakerShader);
            vignetteMat = new Material(shaders.vignetteShader);
            fxaaMat = new Material(shaders.fxaaShader);
            finalMat = new Material(shaders.finalShader);
        }

        public void Dispose()
        {
            Destroy(hbaoMat);
            Destroy(ssssMat);
            Destroy(bloomMat);
            Destroy(godRayMat);
            Destroy(radialBlurMat);
            Destroy(depthOfFieldMat);
            Destroy(gaussianBlurMat);
            Destroy(toneMappingMat);
            Destroy(colorGradingMat);
            Destroy(colorGradingBakerMat);
            Destroy(vignetteMat);
            Destroy(fxaaMat);
            Destroy(finalMat);
        }

        void Destroy(Material mat)
        {
            if (mat != null)
            {
#if UNITY_EDITOR
                if (Application.isPlaying)
                    UnityEngine.Object.Destroy(mat);
                else
                    UnityEngine.Object.DestroyImmediate(mat);
#else
                    UnityEngine.Object.Destroy(mat);
#endif
            }
        }
    }
}
