Shader "Projector/Multiply"
{
	Properties
	{
		_ShadowTex("Cookie", 2D) = "gray" {}
		_FalloffTex("FallOff", 2D) = "white" {}
	}

	SubShader
	{
		Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" }
		ZWrite Off
		ColorMask RGB
		Blend DstColor Zero
		Offset -1, -1

		Pass
		{
			Name "ProjectorMultiply"
			Tags { "LightMode" = "UniversalForward" }

			HLSLPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag
			#pragma multi_compile_fog

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			TEXTURE2D(_ShadowTex);
			SAMPLER(sampler_ShadowTex);
			TEXTURE2D(_FalloffTex);
			SAMPLER(sampler_FalloffTex);

			float4x4 unity_Projector;
			float4x4 unity_ProjectorClip;

			struct Attributes
			{
				float4 positionOS : POSITION;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float4 uvShadow : TEXCOORD0;
				float4 uvFalloff : TEXCOORD1;
				float fogFactor : TEXCOORD2;
			};

			Varyings Vert(Attributes input)
			{
				Varyings output;
				output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
				output.uvShadow = mul(unity_Projector, input.positionOS);
				output.uvFalloff = mul(unity_ProjectorClip, input.positionOS);
				output.fogFactor = ComputeFogFactor(output.positionCS.z);
				return output;
			}

			half4 Frag(Varyings input) : SV_Target
			{
				float2 uvShadow = input.uvShadow.xy / max(input.uvShadow.w, 1e-5);
				float2 uvFalloff = input.uvFalloff.xy / max(input.uvFalloff.w, 1e-5);
				half4 shadow = SAMPLE_TEXTURE2D(_ShadowTex, sampler_ShadowTex, uvShadow);
				shadow.a = 1.0h - shadow.a;
				half falloff = SAMPLE_TEXTURE2D(_FalloffTex, sampler_FalloffTex, uvFalloff).a;
				half4 color = lerp(half4(1,1,1,0), shadow, falloff);
				color.rgb = MixFogColor(color.rgb, input.fogFactor, 1);
				return color;
			}
			ENDHLSL
		}
	}
}
