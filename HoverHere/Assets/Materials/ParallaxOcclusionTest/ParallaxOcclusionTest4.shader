Shader "Custom/ParallaxOcclusion4"
{
	Properties
	{
		_MainTex("Diffuse map (RGB)", 2D) = "white" {}
		_HeightMap("Height map (R)", 2D) = "white" {}
		_Parallax("Height scale", Range(0.005, 0.1)) = 0.08
		_ParallaxSamples("Parallax samples", Range(10, 100)) = 40
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
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
			#pragma multi_compile_fragment _ _SHADOWS_SOFT

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

			TEXTURE2D(_MainTex);
			SAMPLER(sampler_MainTex);
			TEXTURE2D(_HeightMap);
			SAMPLER(sampler_HeightMap);

			CBUFFER_START(UnityPerMaterial)
				float4 _MainTex_ST;
				float4 _HeightMap_ST;
				half _Parallax;
				half _ParallaxSamples;
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
				float2 uvHeight : TEXCOORD4;
				float3 viewDirTS : TEXCOORD5;
				float fogFactor : TEXCOORD6;
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
				float3 viewDirWS = GetWorldSpaceNormalizeViewDir(output.positionWS);
				output.viewDirTS = float3(dot(viewDirWS, tangentWS), dot(viewDirWS, bitangentWS), dot(viewDirWS, normalWS));
				output.uvMain = TRANSFORM_TEX(input.uv, _MainTex);
				output.uvHeight = TRANSFORM_TEX(input.uv, _HeightMap);
				output.fogFactor = ComputeFogFactor(output.positionCS.z);
				return output;
			}

			half4 Frag(Varyings input) : SV_Target
			{
				float3 viewDirTS = normalize(input.viewDirTS);
				float2 viewOffset = normalize(viewDirTS.xy + 1e-6) *
					(length(viewDirTS.xy) / max(abs(viewDirTS.z), 1e-4)) * _Parallax;
				int sampleCount = (int)clamp(lerp(6.0, min(_ParallaxSamples, 100.0),
					saturate(1.0 - dot(normalize(GetWorldSpaceNormalizeViewDir(input.positionWS)), normalize(input.normalWS)))), 6.0, 100.0);
				float stepSize = rcp((float)sampleCount);
				float2 dx = ddx(input.uvHeight);
				float2 dy = ddy(input.uvHeight);
				float currentBound = 1.0;
				float previousHeight = 1.0;
				float currentHeight = 0.0;
				float2 uv = input.uvHeight;
				float2 crossingUpper = 0;
				float2 crossingLower = 0;
				bool crossed = false;

				[loop]
				for (int sampleIndex = 0; sampleIndex < sampleCount; sampleIndex++)
				{
					uv -= viewOffset * stepSize;
					currentHeight = SAMPLE_TEXTURE2D_GRAD(_HeightMap, sampler_HeightMap, uv, dx, dy).r;
					currentBound -= stepSize;
					if (currentHeight > currentBound)
					{
						crossingUpper = float2(currentBound, currentHeight);
						crossingLower = float2(currentBound + stepSize, previousHeight);
						crossed = true;
						break;
					}
					previousHeight = currentHeight;
				}

				float parallaxAmount = 0.0;
				if (crossed)
				{
					float delta2 = crossingLower.x - crossingLower.y;
					float delta1 = crossingUpper.x - crossingUpper.y;
					float denominator = delta2 - delta1;
					if (abs(denominator) > 1e-5)
						parallaxAmount = (crossingUpper.x * delta2 - crossingLower.x * delta1) / denominator;
				}

				float2 parallaxOffset = -viewOffset * (1.0 - parallaxAmount);
				float2 uvMain = input.uvMain + parallaxOffset;
				half3 normalWS = NormalizeNormalPerPixel(input.normalWS);
				Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
				half3 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvMain).rgb;
				half3 color = albedo * (mainLight.color * saturate(dot(normalWS, mainLight.direction)) * mainLight.shadowAttenuation + SampleSH(normalWS));
				return half4(MixFog(color, input.fogFactor), 1);
			}
			ENDHLSL
		}
	}
}
