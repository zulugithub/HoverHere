Shader "Toon/Basic Outline"
{
	Properties
	{
		_Color("Main Color", Color) = (.5,.5,.5,1)
		_OutlineColor("Outline Color", Color) = (0,0,0,1)
		_Outline("Outline width", Range(.002, .03)) = .005
		_MainTex("Base (RGB)", 2D) = "white" {}
		_ToonShade("ToonShader Cubemap(RGB)", Cube) = "" {}
	}

	SubShader
	{
		Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" }
		UsePass "Toon/Basic/BASE"

		Pass
		{
			Name "OUTLINE"
			Tags { "LightMode" = "SRPDefaultUnlit" }
			Cull Front
			ZWrite On
			ColorMask RGB

			HLSLPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag
			#pragma multi_compile_fog

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			CBUFFER_START(UnityPerMaterial)
				half4 _OutlineColor;
				half _Outline;
			CBUFFER_END

			struct Attributes
			{
				float4 positionOS : POSITION;
				float3 normalOS : NORMAL;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float fogFactor : TEXCOORD0;
			};

			Varyings Vert(Attributes input)
			{
				Varyings output;
				output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
				float3 normalVS = TransformWorldToViewDir(TransformObjectToWorldNormal(input.normalOS));
				output.positionCS.xy += normalVS.xy * _Outline * output.positionCS.w;
				output.fogFactor = ComputeFogFactor(output.positionCS.z);
				return output;
			}

			half4 Frag(Varyings input) : SV_Target
			{
				half4 color = _OutlineColor;
				color.rgb = MixFog(color.rgb, input.fogFactor);
				return color;
			}
			ENDHLSL
		}
	}
}
