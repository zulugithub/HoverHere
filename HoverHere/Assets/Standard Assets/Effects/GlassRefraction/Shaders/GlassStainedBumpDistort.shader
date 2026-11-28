Shader "FX/Glass/Stained BumpDistort"
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
		UsePass "Custom/GlassStainedBumpDistort/Forward"
	}
}
