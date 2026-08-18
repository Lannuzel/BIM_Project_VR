Shader "Custom/Mask_URP_StencilWrite_XR_Fresh"
{
    Properties { _StencilRef ("Stencil Ref", Int) = 1 }
    SubShader
    {
        // Draw just before other opaques
        Tags { "Queue"="Geometry-100" "RenderType"="Opaque" }

        Pass
        {
            Name "SRPDefaultUnlit"
            Tags { "LightMode"="SRPDefaultUnlit" }

            Cull Off
            ZWrite Off
            ZTest Always       // stamps regardless of depth
            ColorMask 0        // invisible

            Stencil { Ref [_StencilRef] ReadMask 255 WriteMask 255 Comp Always Pass Replace Fail Keep ZFail Keep }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings  { float4 positionHCS : SV_POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID UNITY_VERTEX_OUTPUT_STEREO };

            Varyings Vert (Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                return o;
            }

            half4 Frag (Varyings i) : SV_Target { return 0; }
            ENDHLSL
        }
    }
}
