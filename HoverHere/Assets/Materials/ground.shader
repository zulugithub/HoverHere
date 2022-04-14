// source:
// https://www.ronja-tutorials.com/2019/01/20/screenspace-texture.html
// https://forum.unity.com/threads/transparent-shader-that-recieves-shadow-kinda-works-already.398674/?_ga=2.69737753.803013357.1578766070-2133810121.1564613509#post-2606783
// https://forum.unity.com/threads/projection-of-skymap-to-objects-in-vr-single-pass-stereo.931332/add-reply

Shader "Custom/ground"
{
	//show values to edit in inspector
	Properties{
		_TintColor("Tint Color", Color) = (.5, .5, .5, 0.5)
		[Gamma] _Exposure("Exposure", Range(0.0, 2)) = 1.0
		_SkyCube("Cubemap", CUBE) = "" {}
	    _ShadowStrength("Shadow Strength", Range(0, 1)) = 1
		_CameraHeight("Camera Height", Range(0, 5)) = 1
	}

	SubShader
	{
		Tags{ "LightMode" = "ForwardBase" }
		//Tags{ "Queue" = "Background" } //  instructs Unity to render this pass before other objects are rendered. 

		Pass
		{
			CGPROGRAM

			//include useful shader functions
			#include "UnityCG.cginc"
			#include "AutoLight.cginc"

			//define vertex and fragment shader
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fwdbase
			#pragma multi_compile_instancing 

			//texture and transforms of the texture
			samplerCUBE _SkyCube;

			//tint of the texture
			half4 _TintColor;
			half _Exposure;
			fixed _ShadowStrength;
			fixed _CameraHeight;

			/*
			//the object data that's put into the vertex shader
			struct appdata
			{
				float4 vertex : POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};*/

			//the data that's used to generate fragments and can be read by the fragment shader
			struct v2f
			{
				float4 pos : SV_POSITION;  // This is the (x, y) position of the pixel in normalized coordinates in the range (-1, -1) to (1, 1). The z is the depth position (used for the depth buffer) in the normalized range 0 to 1.
				float3 camRelativeWorldPos : TEXCOORD0;

				UNITY_VERTEX_INPUT_INSTANCE_ID // necessary only if you want to access instanced properties in fragment Shader.
				UNITY_VERTEX_OUTPUT_STEREO
				SHADOW_COORDS(1) // put shadows data into TEXCOORD1
			};

			v2f vert(appdata_full v)
			{
				v2f o;

				UNITY_SETUP_INSTANCE_ID(v); 
				UNITY_INITIALIZE_OUTPUT(v2f, o);
				UNITY_TRANSFER_INSTANCE_ID(v, o); // necessary only if you want to access instanced properties in the fragment Shader.
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				o.pos = UnityObjectToClipPos(v.vertex);
/*
				float3 worldPos = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1.0)).xyz;
				//o.camRelativeWorldPos = worldPos - _WorldSpaceCameraPos;
				o.camRelativeWorldPos = worldPos - float3(0.0, _CameraHeight, 0.0 ).xyz;*/ // <== matrix multiplication not not needed, because ground object is stationary and at zero position and orienatation
				o.camRelativeWorldPos = float3(-v.vertex.x,  v.vertex.y - _CameraHeight, v.vertex.z).xyz;

				TRANSFER_SHADOW(o); // o._ShadowCoord = mul(unity_World2Shadow[0], mul(_Object2World, v.vertex));

				return o; 
			}

			half4 frag(v2f i) : SV_Target 
			{		
			
				half4 col = texCUBE(_SkyCube, i.camRelativeWorldPos);
				col *= _TintColor * _Exposure *2.0;

				// compute shadow attenuation (1.0 = fully lit, 0.0 = fully shadowed)
				UNITY_SETUP_INSTANCE_ID(i); // necessary only if any instanced properties are going to be accessed in the fragment Shader.
				fixed attenuation = SHADOW_ATTENUATION(i);
				col.rgb *= (1.0f - (1.0 - attenuation) * _ShadowStrength);   
				return col;
			}

			ENDCG
		}

		// shadow casting support
		UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"

	}
	//FallBack "Standard"
}
