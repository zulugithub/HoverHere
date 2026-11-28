Shader "Custom/ParallaxOcclusion3"
{
	Properties
	{
		_Color("Color", Color) = (1,1,1,1)
		_MainTex("Albedo (RGB)", 2D) = "white" {}
		_BumpMap("Normal map (RGB)", 2D) = "bump" {}
		_BumpScale("Bump scale", Range(0,1)) = 1
		_ParallaxMap("Height map (R)", 2D) = "white" {}
		_Parallax("Height scale", Range(0,1)) = 0.05
		_Glossiness("Smoothness", Range(0,1)) = 0.5
		_Metallic("Metallic", Range(0,1)) = 0.0
		_ParallaxMinSamples("Parallax min samples", Range(2,10000)) = 4
		_ParallaxMaxSamples("Parallax max samples", Range(2,10000)) = 20
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
			#pragma shader_feature_local _NORMALMAP
			#pragma multi_compile_fog
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
			#pragma multi_compile_fragment _ _SHADOWS_SOFT
			#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
			#pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

			TEXTURE2D(_MainTex);
			SAMPLER(sampler_MainTex);
			TEXTURE2D(_ParallaxMap);
			SAMPLER(sampler_ParallaxMap);

			CBUFFER_START(UnityPerMaterial)
				float4 _MainTex_ST;
				float4 _BumpMap_ST;
				float4 _ParallaxMap_ST;
				half4 _Color;
				half _BumpScale;
				half _Parallax;
				half _Glossiness;
				half _Metallic;
				half _ParallaxMinSamples;
				half _ParallaxMaxSamples;
			CBUFFER_END

			struct Attributes
			{
				float4 positionOS : POSITION;
				float3 normalOS : NORMAL;
				float4 tangentOS : TANGENT;
				float2 uv : TEXCOORD0;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float3 positionWS : TEXCOORD0;
				float3 normalWS : TEXCOORD1;
				float4 tangentWS : TEXCOORD2;
				float2 uvMain : TEXCOORD3;
				float2 uvBump : TEXCOORD4;
				float2 uvParallax : TEXCOORD5;
				float3 eyeTS : TEXCOORD6;
				float sampleRatio : TEXCOORD7;
				float fogFactor : TEXCOORD8;
			};

			Varyings Vert(Attributes input)
			{
				Varyings output;
				output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
				output.positionCS = TransformWorldToHClip(output.positionWS);
				output.normalWS = TransformObjectToWorldNormal(input.normalOS);
				output.tangentWS = float4(TransformObjectToWorldDir(input.tangentOS.xyz), input.tangentOS.w);

				float3 normalWS = normalize(output.normalWS);
				float3 tangentWS = normalize(output.tangentWS.xyz);
				float3 bitangentWS = cross(normalWS, tangentWS) * output.tangentWS.w;
				float3 eyeWS = -GetWorldSpaceNormalizeViewDir(output.positionWS);
				output.eyeTS = float3(dot(eyeWS, tangentWS), dot(eyeWS, bitangentWS), dot(eyeWS, normalWS));
				output.sampleRatio = saturate(1.0 - dot(normalize(eyeWS), -normalWS));
				output.uvMain = TRANSFORM_TEX(input.uv, _MainTex);
				output.uvBump = TRANSFORM_TEX(input.uv, _BumpMap);
				output.uvParallax = TRANSFORM_TEX(input.uv, _ParallaxMap);
				output.fogFactor = ComputeFogFactor(output.positionCS.z);
				return output;
			}

			float2 ParallaxOffset(float2 uv, float3 eyeTS, float sampleRatio)
			{
				float parallaxLimit = -length(eyeTS.xy) / max(abs(eyeTS.z), 1e-4);
				parallaxLimit *= _Parallax;
				float2 maxOffset = normalize(eyeTS.xy + 1e-6) * parallaxLimit;
				int sampleCount = (int)clamp(lerp(_ParallaxMinSamples, _ParallaxMaxSamples, sampleRatio), 2.0, 256.0);
				float stepSize = rcp((float)sampleCount);
				float2 dx = ddx(uv);
				float2 dy = ddy(uv);
				float rayHeight = 1.0;
				float2 currentOffset = 0;
				float2 previousOffset = 0;
				float previousSample = 1;

				[loop]
				for (int sampleIndex = 0; sampleIndex < sampleCount; sampleIndex++)
				{
					float sampledHeight = SAMPLE_TEXTURE2D_GRAD(_ParallaxMap, sampler_ParallaxMap, uv + currentOffset, dx, dy).r;
					if (sampledHeight > rayHeight)
					{
						float delta1 = sampledHeight - rayHeight;
						float delta2 = rayHeight + stepSize - previousSample;
						float denominator = delta1 + delta2;
						float ratio = abs(denominator) > 1e-5 ? delta1 / denominator : 0.0;
						currentOffset = lerp(currentOffset, previousOffset, ratio);
						break;
					}

					rayHeight -= stepSize;
					previousOffset = currentOffset;
					currentOffset += stepSize * maxOffset;
					previousSample = sampledHeight;
				}

				return currentOffset;
			}

			half4 Frag(Varyings input) : SV_Target
			{
				float2 parallaxOffset = ParallaxOffset(input.uvParallax, input.eyeTS, input.sampleRatio);
				float2 uvMain = input.uvMain + parallaxOffset;
				float2 uvBump = input.uvBump + parallaxOffset;
				half4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvMain) * _Color;
				half3 normalTS = SampleNormal(uvBump, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap), _BumpScale);
				half3 bitangentWS = cross(input.normalWS, input.tangentWS.xyz) * input.tangentWS.w;
				half3 normalWS = NormalizeNormalPerPixel(TransformTangentToWorld(
					normalTS, half3x3(normalize(input.tangentWS.xyz), normalize(bitangentWS), normalize(input.normalWS))));

				InputData inputData = (InputData)0;
				inputData.positionWS = input.positionWS;
				inputData.positionCS = input.positionCS;
				inputData.normalWS = normalWS;
				inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
				inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
				inputData.fogCoord = input.fogFactor;
				inputData.bakedGI = SampleSH(normalWS);
				inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
				inputData.shadowMask = half4(1, 1, 1, 1);

				SurfaceData surfaceData = (SurfaceData)0;
				surfaceData.albedo = albedo.rgb;
				surfaceData.metallic = _Metallic;
				surfaceData.smoothness = _Glossiness;
				surfaceData.normalTS = half3(0, 0, 1);
				surfaceData.occlusion = 1;
				surfaceData.alpha = albedo.a;

				half4 color = UniversalFragmentPBR(inputData, surfaceData);
				color.rgb = MixFog(color.rgb, input.fogFactor);
				return color;
			}
			ENDHLSL
		}
	}
}
