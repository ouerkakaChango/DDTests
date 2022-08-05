// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "PBR_Reflection"
{
	Properties
	{
		_Color("Color", Color) = (1,1,1,1)
		_Albedo("Albedo", 2D) = "white" {}
		_ReflectionTex("ReflectionTex", 2D) = "black" {}
		_MetallicGlossMap("Mask(R:Metallic)(G:Occlusion)(B:Smoothness)(A:Emission)", 2D) = "white" {}
		_Metallic("Metallic Scale", Range( 0 , 1)) = 0
		_GlossMapScale("Smoothness Scale", Range( 0 , 1)) = 1
		_OcclusionStrength("Occlusion Scale", Range( 0 , 1)) = 1
		[HDR]_Emission("Emission", Color) = (0,0,0,0)
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" "IsEmissive" = "true"  }
		Cull Back
		CGPROGRAM
		#pragma target 3.0
		#pragma surface surf Standard keepalpha addshadow fullforwardshadows 
		struct Input
		{
			float2 uv_texcoord;
			float4 screenPos;
		};

		uniform float4 _Color;
		uniform sampler2D _Albedo;
		uniform float4 _Albedo_ST;
		uniform sampler2D _ReflectionTex;
		uniform sampler2D _MetallicGlossMap;
		SamplerState sampler_MetallicGlossMap;
		uniform float4 _MetallicGlossMap_ST;
		uniform float4 _Emission;
		uniform float _Metallic;
		uniform float _GlossMapScale;
		uniform float _OcclusionStrength;

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float2 uv_Albedo = i.uv_texcoord * _Albedo_ST.xy + _Albedo_ST.zw;
			float4 ase_screenPos = float4( i.screenPos.xyz , i.screenPos.w + 0.00000000001 );
			float4 ase_screenPosNorm = ase_screenPos / ase_screenPos.w;
			ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
			o.Albedo = ( ( _Color * tex2D( _Albedo, uv_Albedo ) * _Color.a ) + ( ( 1.0 - _Color.a ) * tex2D( _ReflectionTex, ase_screenPosNorm.xy ) ) ).rgb;
			float2 uv_MetallicGlossMap = i.uv_texcoord * _MetallicGlossMap_ST.xy + _MetallicGlossMap_ST.zw;
			float4 tex2DNode14 = tex2D( _MetallicGlossMap, uv_MetallicGlossMap );
			o.Emission = ( tex2DNode14.a * _Emission ).rgb;
			o.Metallic = ( _Metallic * tex2DNode14.r );
			o.Smoothness = ( _GlossMapScale * tex2DNode14.b );
			o.Occlusion = ( _OcclusionStrength * tex2DNode14.g );
			o.Alpha = 1;
		}

		ENDCG
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=18703
1952;63;1430;951;1838.351;406.0161;1.576253;True;False
Node;AmplifyShaderEditor.TexturePropertyNode;1;-1695.487,-566.9587;Inherit;True;Property;_Albedo;Albedo;1;0;Create;True;0;0;False;0;False;None;None;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.ColorNode;6;-1023.829,-651.5034;Inherit;False;Property;_Color;Color;0;0;Create;True;0;0;False;0;False;1,1,1,1;1,1,1,0.8313726;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ScreenPosInputsNode;5;-1392.878,-131.882;Float;False;0;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TextureCoordinatesNode;3;-1414.085,-418.0605;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;4;-1135.885,-460.9605;Inherit;True;Property;_TextureSample0;Texture Sample 0;2;0;Create;True;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.OneMinusNode;10;-738.1024,-267.5963;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;2;-1116.685,-155.5609;Inherit;True;Property;_ReflectionTex;ReflectionTex;2;0;Create;True;0;0;False;0;False;-1;None;None;True;0;False;black;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;15;-585.3145,617.3452;Inherit;False;Property;_GlossMapScale;Smoothness Scale;5;0;Create;False;0;0;False;0;False;1;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;13;-1028.521,266.598;Inherit;False;Property;_Metallic;Metallic Scale;4;0;Create;False;0;0;False;0;False;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;16;-604.943,847.9128;Inherit;False;Property;_Emission;Emission;7;1;[HDR];Create;True;0;0;False;0;False;0,0,0,0;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;7;-707.7023,-483.5967;Inherit;False;3;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;11;-582.9025,-171.5963;Inherit;False;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;12;-679.0933,421.5836;Inherit;False;Property;_OcclusionStrength;Occlusion Scale;6;0;Create;False;0;0;False;0;False;1;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;14;-1132.609,609.0595;Inherit;True;Property;_MetallicGlossMap;Mask(R:Metallic)(G:Occlusion)(B:Smoothness)(A:Emission);3;0;Create;False;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;20;-731.557,298.4133;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;17;-331.9562,770.403;Inherit;False;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;18;-441.7695,459.4084;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;19;-316.4756,586.8311;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;9;-366.9027,-307.5963;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;99.59128,-17.87625;Float;False;True;-1;2;ASEMaterialInspector;0;0;Standard;PBR_Reflection;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Opaque;0.5;True;True;0;False;Opaque;;Geometry;All;14;all;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;True;0;0;False;-1;0;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;False;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;3;2;1;0
WireConnection;4;0;1;0
WireConnection;4;1;3;0
WireConnection;10;0;6;4
WireConnection;2;1;5;0
WireConnection;7;0;6;0
WireConnection;7;1;4;0
WireConnection;7;2;6;4
WireConnection;11;0;10;0
WireConnection;11;1;2;0
WireConnection;20;0;13;0
WireConnection;20;1;14;1
WireConnection;17;0;14;4
WireConnection;17;1;16;0
WireConnection;18;0;12;0
WireConnection;18;1;14;2
WireConnection;19;0;15;0
WireConnection;19;1;14;3
WireConnection;9;0;7;0
WireConnection;9;1;11;0
WireConnection;0;0;9;0
WireConnection;0;2;17;0
WireConnection;0;3;20;0
WireConnection;0;4;19;0
WireConnection;0;5;18;0
ASEEND*/
//CHKSM=43712D83BCD093F70729D36C212712D12CBB3F0E