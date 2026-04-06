Shader "VR_URP/CheapMetal"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (0.6, 0.65, 0.7, 1)
        _SpecColor("Specular Color", Color) = (1, 1, 1, 1)
        _Shininess("Shininess", Range(10, 200)) = 100
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing // Crucial for VR

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

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
                half4 _BaseColor;
                half4 _SpecColor;
                half _Shininess;
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

                Light mainLight = GetMainLight();
                float3 normalWS = normalize(input.normalWS);
                
                // Diffuse
                half NdotL = saturate(dot(normalWS, normalize(mainLight.direction)));
                half3 diffuse = mainLight.color * NdotL;
                
                // Specular
                float3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                float3 halfVector = normalize(mainLight.direction + viewDirWS);
                half NdotH = saturate(dot(normalWS, halfVector));
                half specularTerm = pow(NdotH, _Shininess);
                half3 specular = _SpecColor.rgb * specularTerm;
                
                // Ambient (Spherical Harmonics) so shadows aren't pitch black
                half3 ambient = SampleSH(normalWS);
                
                half3 finalColor = _BaseColor.rgb * (diffuse + ambient) + specular;
                return half4(finalColor, _BaseColor.a);
            }
            ENDHLSL
        }
    }
}