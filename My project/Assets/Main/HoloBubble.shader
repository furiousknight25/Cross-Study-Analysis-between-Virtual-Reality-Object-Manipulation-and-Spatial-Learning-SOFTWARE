Shader "VR_URP/HoloBubble"
{
    Properties
    {
        [HDR] _Color("Bubble Color & Glow", Color) = (0.0, 0.8, 1.0, 0.5)
        _FresnelPower("Rim Sharpness", Range(0.1, 10.0)) = 3.0
        _AlphaScale("Overall Transparency", Range(0.0, 1.0)) = 0.8
    }
    SubShader
    {
        // Set up for transparent rendering
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        LOD 100

        // Standard alpha blending, don't write to depth buffer (prevents sorting glitches)
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back // Change to 'Off' if you want to see the inside of the bubble when the hand gets close

        Pass
        {
            Name "Unlit"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing // Crucial for VR rendering

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _FresnelPower;
                half _AlphaScale;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                // Normalize vectors for accurate math
                float3 N = normalize(input.normalWS);
                float3 V = normalize(GetCameraPositionWS() - input.positionWS);

                // Calculate Fresnel (Dot product of Normal and View Direction)
                // This makes the edges value 1, and the center value 0.
                half fresnelTerm = pow(1.0 - saturate(dot(N, V)), _FresnelPower);

                // Combine the base alpha with the glowing rim
                half finalAlpha = saturate(fresnelTerm + (_Color.a * 0.2)) * _AlphaScale; 

                // Return the color and the newly calculated transparent rim
                return half4(_Color.rgb, finalAlpha);
            }
            ENDHLSL
        }
    }
}