Shader "Skybox/6 Sided - Arbitrary Rotation and Flipping Horizontaly"
{
	Properties
	{
		_TintColor("Tint Color", Color) = (.5, .5, .5, .5)
		[Gamma] _Exposure("Exposure", Range(0, 8)) = 1.0
		_FlippingHorizontaly("Flipping Horizontaly", Range(0, 1)) = 0
		_Rotation("Rotation", Range(0, 360)) = 0
		_Rotationaxis("Rotation axis", Vector) = (0, 1, 0)
		[NoScaleOffset] _FrontTex("Front [+Z]   (HDR)", 2D) = "grey" {}
		[NoScaleOffset] _BackTex("Back [-Z]   (HDR)", 2D) = "grey" {}
		[NoScaleOffset] _LeftTex("Left [+X]   (HDR)", 2D) = "grey" {}
		[NoScaleOffset] _RightTex("Right [-X]   (HDR)", 2D) = "grey" {}
		[NoScaleOffset] _UpTex("Up [+Y]   (HDR)", 2D) = "grey" {}
		[NoScaleOffset] _DownTex("Down [-Y]   (HDR)", 2D) = "grey" {}
	}

	SubShader
	{
		Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" "RenderPipeline" = "UniversalPipeline" }
		Cull Off
		ZWrite Off

		HLSLINCLUDE
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
		#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"

		CBUFFER_START(UnityPerMaterial)
			half4 _TintColor;
			half _Exposure;
			float _FlippingHorizontaly;
			float _Rotation;
			float3 _Rotationaxis;
		CBUFFER_END

		struct Attributes
		{
			float4 positionOS : POSITION;
			float2 uv : TEXCOORD0;
		};

		struct Varyings
		{
			float4 positionCS : SV_POSITION;
			float2 uv : TEXCOORD0;
		};

		float3 RotateDirection(float3 direction, float3 axis, float angle)
		{
			axis = normalize(axis + 1e-8);
			float sine;
			float cosine;
			sincos(angle, sine, cosine);
			return direction * cosine + cross(axis, direction) * sine + axis * dot(axis, direction) * (1.0 - cosine);
		}

		half3 ApplyTintAndExposure(half3 color)
		{
			// Match unity_ColorSpaceDouble from the original shader in Gamma color space.
			return color * _TintColor.rgb * _Exposure * 2.0h;
		}

		Varyings Vert(Attributes input)
		{
			Varyings output;
			float3 direction = RotateDirection(input.positionOS.xyz, _Rotationaxis, radians(_Rotation));
			if (_FlippingHorizontaly > 0.5)
				direction.x = -direction.x;

			output.positionCS = TransformObjectToHClip(direction);
			output.uv = input.uv;
			return output;
		}
		ENDHLSL

		Pass
		{
			HLSLPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag
			TEXTURE2D(_FrontTex);
			SAMPLER(sampler_FrontTex);
			float4 _FrontTex_HDR;
			half4 Frag(Varyings input) : SV_Target
			{
				half3 color = DecodeHDREnvironment(SAMPLE_TEXTURE2D(_FrontTex, sampler_FrontTex, input.uv), _FrontTex_HDR);
				return half4(ApplyTintAndExposure(color), 1);
			}
			ENDHLSL
		}

		Pass
		{
			HLSLPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag
			TEXTURE2D(_BackTex);
			SAMPLER(sampler_BackTex);
			float4 _BackTex_HDR;
			half4 Frag(Varyings input) : SV_Target
			{
				half3 color = DecodeHDREnvironment(SAMPLE_TEXTURE2D(_BackTex, sampler_BackTex, input.uv), _BackTex_HDR);
				return half4(ApplyTintAndExposure(color), 1);
			}
			ENDHLSL
		}

		Pass
		{
			HLSLPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag
			TEXTURE2D(_LeftTex);
			SAMPLER(sampler_LeftTex);
			float4 _LeftTex_HDR;
			half4 Frag(Varyings input) : SV_Target
			{
				half3 color = DecodeHDREnvironment(SAMPLE_TEXTURE2D(_LeftTex, sampler_LeftTex, input.uv), _LeftTex_HDR);
				return half4(ApplyTintAndExposure(color), 1);
			}
			ENDHLSL
		}

		Pass
		{
			HLSLPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag
			TEXTURE2D(_RightTex);
			SAMPLER(sampler_RightTex);
			float4 _RightTex_HDR;
			half4 Frag(Varyings input) : SV_Target
			{
				half3 color = DecodeHDREnvironment(SAMPLE_TEXTURE2D(_RightTex, sampler_RightTex, input.uv), _RightTex_HDR);
				return half4(ApplyTintAndExposure(color), 1);
			}
			ENDHLSL
		}

		Pass
		{
			HLSLPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag
			TEXTURE2D(_UpTex);
			SAMPLER(sampler_UpTex);
			float4 _UpTex_HDR;
			half4 Frag(Varyings input) : SV_Target
			{
				half3 color = DecodeHDREnvironment(SAMPLE_TEXTURE2D(_UpTex, sampler_UpTex, input.uv), _UpTex_HDR);
				return half4(ApplyTintAndExposure(color), 1);
			}
			ENDHLSL
		}

		Pass
		{
			HLSLPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag
			TEXTURE2D(_DownTex);
			SAMPLER(sampler_DownTex);
			float4 _DownTex_HDR;
			half4 Frag(Varyings input) : SV_Target
			{
				half3 color = DecodeHDREnvironment(SAMPLE_TEXTURE2D(_DownTex, sampler_DownTex, input.uv), _DownTex_HDR);
				return half4(ApplyTintAndExposure(color), 1);
			}
			ENDHLSL
		}
	}
}
