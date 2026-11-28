Shader "Custom/SurfaceShader_VC_Smoke"
{
	Properties
	{
		_Color("Color", Color) = (1,1,1,1)
		_MainTex("Albedo (RGB)", 2D) = "white" {}
		_Normal("Normal Map", 2D) = "bump" {}
	}

	SubShader
	{
		Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" "RenderType" = "Transparent" }
		UsePass "Custom/SurfaceShader_VC/ForwardLit"
	}
}
