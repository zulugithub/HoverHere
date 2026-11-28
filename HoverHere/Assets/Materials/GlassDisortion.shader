Shader "Custom/GlassDisortion"
{
	Properties
	{
		_BumpAmt("Distortion", Range(0, 128)) = 10
		_MainTex("Tint Color (RGB)", 2D) = "white" {}
		_BumpMap("Normal Map", 2D) = "bump" {}
		_grabTexture("Unused legacy grab texture", 2D) = "white" {}
	}

	SubShader
	{
		Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" "RenderType" = "Transparent" }
		UsePass "Custom/GlassStainedBumpDistort/Forward"
	}
}
