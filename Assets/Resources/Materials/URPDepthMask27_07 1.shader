Shader "Kotikov/URPDepthMask_Mukesh_27_07"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry-10" }

        // ✅ Key settings
        ZWrite On
        ZTest Always
        ColorMask 0
        Offset 1, 1

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            void frag(Varyings input) {} // No color output
            ENDHLSL
        }
    }

    Fallback Off
}

