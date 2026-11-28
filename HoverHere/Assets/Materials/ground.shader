Shader "Custom/ground"
{
	Properties
	{
		_TintColor("Tint Color", Color) = (.5, .5, .5, .5)
		[Gamma] _Exposure("Exposure", Range(0.0, 2)) = 1.0
		_SkyCube("Cubemap", Cube) = "" {}
		_ShadowStrength("Shadow Strength", Range(0, 1)) = 1
		_CameraHeight("Camera Height", Range(0, 5)) = 1
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
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
			#pragma multi_compile_fragment _ _SHADOWS_SOFT

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

			TEXTURECUBE(_SkyCube);
			SAMPLER(sampler_SkyCube);

			CBUFFER_START(UnityPerMaterial)
				half4 _TintColor;
				half _Exposure;
				half _ShadowStrength;
				half _CameraHeight;
			CBUFFER_END

			struct Attributes
			{
				float4 positionOS : POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float3 skyDirection : TEXCOORD0;
				float4 shadowCoord : TEXCOORD1;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			Varyings Vert(Attributes input)
			{
				Varyings output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
				output.positionCS = positionInputs.positionCS;
				output.shadowCoord = GetShadowCoord(positionInputs);
				output.skyDirection = float3(-input.positionOS.x, input.positionOS.y - _CameraHeight, input.positionOS.z);
				return output;
			}

			half4 Frag(Varyings input) : SV_Target
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

				half4 color = SAMPLE_TEXTURECUBE(_SkyCube, sampler_SkyCube, normalize(input.skyDirection));
				color *= _TintColor * _Exposure * 2.0h;

				Light mainLight = GetMainLight(input.shadowCoord);
				color.rgb *= lerp(1.0h, mainLight.shadowAttenuation, _ShadowStrength);
				return color;
			}
			ENDHLSL
		}

		UsePass "Universal Render Pipeline/Lit/ShadowCaster"
	}
}
