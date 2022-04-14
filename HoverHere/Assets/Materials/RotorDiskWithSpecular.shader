Shader "Custom/RotorDiskWithSpecular"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _MainTex("Albedo (RGB)", 2D) = "white" {}
        _MainTex2("Albedo (RGB)", 2D) = "white" {}
        _Glossiness("Smoothness", Range(0,1)) = 0.5
        _Metallic("Metallic", Range(0,1)) = 0.0
        _CollectiveSpecular("CollectiveSpecular", Range(-0.5,0.5)) = 0.5
    }
        SubShader
        {
            Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjectors" = "True"}
            LOD 200

            // ZWrite pre-pass
            Pass {
                ZWrite On
                ColorMask 0

                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #include "UnityCG.cginc"

                float4 vert(float4 vertex : POSITION) : SV_POSITION { return UnityObjectToClipPos(vertex); }
                fixed4 frag() : SV_Target { return 0; }
                ENDCG
            }





            // ------------------------------------------------------------------
            // transparent shadows support, taken from unity'S standard.shader
            // https://forum.unity.com/threads/add-semitransparent-shadow-pass-to-a-custom-shader.470681/
            // https://unity3d.com/get-unity/download/archive
            // ------------------------------------------------------------------
            //  Shadow rendering pass
            Pass {
                Name "ShadowCaster"
                Tags { "LightMode" = "ShadowCaster" }

                ZWrite On ZTest LEqual

                CGPROGRAM
                #pragma target 3.0

                    // -------------------------------------


                    #pragma shader_feature_local _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
                    #pragma shader_feature_local _METALLICGLOSSMAP
                    #pragma shader_feature_local _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
                    #pragma shader_feature_local _PARALLAXMAP
                    #pragma multi_compile_shadowcaster
                    #pragma multi_compile_instancing
                    // Uncomment the following line to enable dithering LOD crossfade. Note: there are more in the file to uncomment for other passes.
                    //#pragma multi_compile _ LOD_FADE_CROSSFADE

                    #pragma vertex vertShadowCaster
                    #pragma fragment fragShadowCaster

                    #include "UnityStandardShadow.cginc"

                    ENDCG
                }
                    // ------------------------------------------------------------------





                    CGPROGRAM
                    // Physically based Standard lighting model, and enable shadows on all light types
                    #pragma surface surf Standard fullforwardshadows alpha:fade

                    // Use shader model 3.0 target, to get nicer looking lighting
                    #pragma target 3.0

                    sampler2D _MainTex;
                    sampler2D _MainTex2;

                    struct Input
                    {
                        float2 uv_MainTex : TEXCOORD0;
                        float2 uv2_MainTex2;
                    };

                    half _Glossiness;
                    half _Metallic;
                    fixed4 _Color;
                    half _CollectiveSpecular;

                    float rand(float myVector)
                    {
                        return frac(sin(_Time[0] * myVector * 12.9898 * 43758.5453));
                    }

                    float3 rotationMatrix(float3 axis, float angle)
                    {
                        float s = sin(angle);
                        float c = cos(angle);
                        return float3(c * axis.x - s * axis.y, s * axis.x + c * axis.y, 0);
                    }

                    // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
                    // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
                    // #pragma instancing_options assumeuniformscaling
                    UNITY_INSTANCING_BUFFER_START(Props)
                        // put more per-instance properties here
                    UNITY_INSTANCING_BUFFER_END(Props)

                    void surf(Input IN, inout SurfaceOutputStandard o)
                    {
                        // rotor blades  specular highlights
                        // half factor = 0.5;  // -0.5...0.5 -->  +-collective angle
                        // normals are not normal to disc surface but tilted in rotation direction
                        half3 normalized = normalize(half3((IN.uv2_MainTex2.y - 0.5), -(IN.uv2_MainTex2.x - 0.5), 0));
                        if (length(normalized) != 0)
                        {
                            float _CollectiveSpecular___ = _CollectiveSpecular;

                           float pct = sqrt((IN.uv2_MainTex2.y - 0.5) * (IN.uv2_MainTex2.y - 0.5) + (IN.uv2_MainTex2.x - 0.5) * (IN.uv2_MainTex2.x - 0.5));
                           float range = 0.47;
                            if (pct > range)
                            {
                                float value = ((pct - range) / (0.5 - range)); // 0...1
                                //_CollectiveSpecular___ = _CollectiveSpecular * cos(value * 3.1415 * 1);
                                //_CollectiveSpecular___ = _CollectiveSpecular * (1 - value*0.5 );

                                normalized = rotationMatrix(normalized, value * 1);
                            }
                            o.Normal = normalize(half3(normalized.x * _CollectiveSpecular___, normalized.y * _CollectiveSpecular___, 1.0));
                        }


                        // Albedo comes from a texture tinted by color
                        fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
                        o.Albedo = c.rgb;
                        // Metallic and smoothness come from slider variables
                        o.Metallic = _Metallic;
                        o.Smoothness = _Glossiness;
                        o.Alpha = c.a;

                    }
                    ENDCG
        }
            FallBack "Diffuse"
}
