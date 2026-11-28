Shader "Custom/GlassStainedBumpDistort"
{
	Properties
	{
		_BumpAmt("Distortion", Range(0, 128)) = 10
		_MainTex("Tint Color (RGB)", 2D) = "white" {}
		_BumpMap("Normal Map", 2D) = "bump" {}
	}

	SubShader
	{
		Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" "RenderType" = "Transparent" }
		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			Name "Forward"
			Tags { "LightMode" = "UniversalForward" }

			HLSLPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag
			#pragma multi_compile_instancing

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

			TEXTURE2D(_MainTex);
			SAMPLER(sampler_MainTex);
			TEXTURE2D(_BumpMap);
			SAMPLER(sampler_BumpMap);

			CBUFFER_START(UnityPerMaterial)
				float4 _MainTex_ST;
				float4 _BumpMap_ST;
				half _BumpAmt;
			CBUFFER_END

			struct Attributes
			{
				float4 positionOS : POSITION;
				float2 uv : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float2 uvMain : TEXCOORD0;
				float2 uvBump : TEXCOORD1;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			Varyings Vert(Attributes input)
			{
				Varyings output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
				output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
				output.uvMain = TRANSFORM_TEX(input.uv, _MainTex);
				output.uvBump = TRANSFORM_TEX(input.uv, _BumpMap);
				return output;
			}

			half4 Frag(Varyings input) : SV_Target
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

				half4 packedNormal = SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uvBump);
				half2 normalXY = packedNormal.ag * 2.0h - 1.0h;
				float2 screenUV = GetNormalizedScreenSpaceUV(input.positionCS);
				screenUV += normalXY * _BumpAmt * _CameraOpaqueTexture_TexelSize.xy;

				half4 color = half4(SampleSceneColor(screenUV), 1.0h);
				half4 tint = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uvMain);
				return color * tint;
			}
			ENDHLSL
		}
	}
}
