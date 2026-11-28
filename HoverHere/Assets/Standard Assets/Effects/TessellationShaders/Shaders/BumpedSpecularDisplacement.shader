Shader "Tessellation/Bumped Specular (displacement)"
{
	Properties
	{
		_Color("Main Color", Color) = (1,1,1,1)
		_SpecColor("Specular Color", Color) = (0.5, 0.5, 0.5, 1)
		_Shininess("Shininess", Range(0.03, 1)) = 0.078125
		_Parallax("Height", Range(0.0, 1.0)) = 0.5
		_MainTex("Base (RGB) Gloss (A)", 2D) = "white" {}
		_BumpMap("Normalmap", 2D) = "bump" {}
		_ParallaxMap("Heightmap (A)", 2D) = "black" {}
		_EdgeLength("Edge length", Range(3,50)) = 10
	}

	SubShader
	{
		Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" }

		Pass
		{
			Name "ForwardLit"
			Tags { "LightMode" = "UniversalForward" }
			HLSLPROGRAM
			#pragma target 3.0
			#pragma vertex Vert
			#pragma fragment Frag
			#pragma multi_compile_fog
			#pragma shader_feature_local _NORMALMAP
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
			#pragma multi_compile_fragment _ _SHADOWS_SOFT
			#define URP_VERTEX_DISPLACEMENT 1
			#include "BumpedSpecularURP.hlsl"
			ENDHLSL
		}
	}
}
