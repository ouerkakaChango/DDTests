Shader "Yolk/UGUI/DianDianUIBlur1"
{
    Properties {
        [HideInInspector]
        _MainTex("MainTex", 2D) = "white" {}
        _KFactor ("Intensity", Range(1, 2)) = 1
        _MFactor ("BlurStep", Range(1, 2000)) = 3
		_BlurMaskTex("Blur Mask Texture", 2D) = "white" {}

        [HideInInspector]
		_StencilComp ("Stencil Comparison", Float) = 8
		[HideInInspector]
		_Stencil ("Stencil ID", Float) = 0
		[HideInInspector]
		_StencilOp ("Stencil Operation", Float) = 0
		[HideInInspector]
		_StencilWriteMask ("Stencil Write Mask", Float) = 255
		[HideInInspector]
		_StencilReadMask ("Stencil Read Mask", Float) = 255

    }
 
        Category{


            SubShader {
                LOD 300
                Tags {
                "Queue" = "Transparent"
                "IgnoreProjector" = "True"
                "RenderType" = "Opaque"
            }

            Cull Off
            ZWrite Off
            ZTest Off
            Stencil {
                Ref[_Stencil]
                Comp[_StencilComp]
                Pass[_StencilOp]
                ReadMask[_StencilReadMask]
                WriteMask[_StencilWriteMask]
            }
                GrabPass {
                }

                // Vertical
                Pass {
                    Name "VERTICAL"
                    Tags { "LightMode" = "Always" }

                    CGPROGRAM
                    #pragma vertex vert
                    #pragma fragment fragY
                    #pragma fragmentoption ARB_precision_hint_fastest
                    #include "UnityCG.cginc"
                    #include "DianDianBlurCG1.cginc"
                    ENDCG
                }

                GrabPass {
                }

                // Horizontal
                Pass {
                    Name "HORIZONTAL"

                    Tags { "LightMode" = "Always" }

                    CGPROGRAM
                    #pragma vertex vert
                    #pragma fragment fragX
                    #pragma fragmentoption ARB_precision_hint_fastest
                    #include "UnityCG.cginc"
                    #include "DianDianBlurCG1.cginc"
                    ENDCG
                }

            }

            SubShader {
                LOD 200
                Tags {
                    "Queue" = "Transparent"
                    "IgnoreProjector" = "True"
                    "RenderType" = "Opaque"
                }

                Cull Off
                ZWrite Off
                ZTest Off
                Stencil {
                    Ref[_Stencil]
                    Comp[_StencilComp]
                    Pass[_StencilOp]
                    ReadMask[_StencilReadMask]
                    WriteMask[_StencilWriteMask]
                }
                GrabPass {
                }

                // gaussian 3x3
                Pass {
                    Name "G3"
                    Tags { "LightMode" = "Always" }

                    CGPROGRAM
                    #pragma vertex vert
                    #pragma fragment fragG3
                    #pragma fragmentoption ARB_precision_hint_fastest
                    #include "UnityCG.cginc"
                    #include "DianDianBlurCG1.cginc"
                    ENDCG
                }
            }
        }
}
