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
	half4 _SpecColor;
	half _Shininess;
	half _Parallax;
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
	float fogFactor : TEXCOORD5;
};

Varyings Vert(Attributes input)
{
	Varyings output;
	float3 positionOS = input.positionOS.xyz;

#if defined(URP_VERTEX_DISPLACEMENT)
	float2 displacementUV = TRANSFORM_TEX(input.uv, _ParallaxMap);
	positionOS += input.normalOS * SAMPLE_TEXTURE2D_LOD(_ParallaxMap, sampler_ParallaxMap, displacementUV, 0).a * _Parallax;
#endif

	output.positionWS = TransformObjectToWorld(positionOS);
	output.positionCS = TransformWorldToHClip(output.positionWS);
	output.normalWS = TransformObjectToWorldNormal(input.normalOS);
	output.tangentWS = float4(TransformObjectToWorldDir(input.tangentOS.xyz), input.tangentOS.w);
	output.uvMain = TRANSFORM_TEX(input.uv, _MainTex);
	output.uvBump = TRANSFORM_TEX(input.uv, _BumpMap);
	output.fogFactor = ComputeFogFactor(output.positionCS.z);
	return output;
}

half4 Frag(Varyings input) : SV_Target
{
	half4 texel = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uvMain);
	half3 normalTS = SampleNormal(input.uvBump, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap), 1.0h);
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
	surfaceData.albedo = texel.rgb * _Color.rgb;
	surfaceData.specular = _SpecColor.rgb * _Shininess;
	surfaceData.smoothness = texel.a;
	surfaceData.normalTS = half3(0, 0, 1);
	surfaceData.occlusion = 1;
	surfaceData.alpha = texel.a * _Color.a;

	half4 color = UniversalFragmentPBR(inputData, surfaceData);
	color.rgb = MixFog(color.rgb, inputData.fogCoord);
	return color;
}
