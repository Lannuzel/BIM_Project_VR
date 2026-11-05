Shader "Custom/Wall_URP_Stencil_XR"
{
    Properties
    {
        _BaseMap   ("Base Map", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _StencilRef ("Stencil Ref", Int) = 1
    }
    SubShader
    {
        // Keep opaque and broadly compatible
        Tags { "Queue"="Geometry" "RenderType"="Opaque" }

        // ─────────────────────────────────────────
        // COLOR PASS (unlit) — draws only where stencil != Ref
        // ─────────────────────────────────────────
        Pass
        {
            Name "SRPDefaultUnlit"
            Tags { "LightMode"="SRPDefaultUnlit" }

            Cull Back
            ZWrite On
            ZTest LEqual

            Stencil { Ref [_StencilRef] Comp NotEqual Pass Keep Fail Keep ZFail Keep }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            float4 _BaseMap_ST;
            float4 _BaseColor;

            Varyings Vert (Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv * _BaseMap_ST.xy + _BaseMap_ST.zw;
                return o;
            }

            half4 Frag (Varyings i) : SV_Target
            {
                return SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor;
            }
            ENDHLSL
        }

        // ─────────────────────────────────────────
        // DEPTH-ONLY PASS — same stencil rule
        // prevents wall depth in the hole during depth prepass
        // ─────────────────────────────────────────
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }

            Cull Back
            ZWrite On
            ZTest LEqual
            ColorMask 0

            Stencil { Ref [_StencilRef] Comp NotEqual Pass Keep Fail Keep ZFail Keep }

            HLSLPROGRAM
            #pragma vertex VertDepth
            #pragma fragment FragDepth
            #pragma target 2.0
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct ADepth { float4 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct VDepth { float4 positionHCS : SV_POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID UNITY_VERTEX_OUTPUT_STEREO };

            VDepth VertDepth (ADepth v)
            {
                VDepth o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                return o;
            }
            half4 FragDepth (VDepth i) : SV_Target { return 0; }
            ENDHLSL
        }

        // ─────────────────────────────────────────
        // DEPTH-NORMALS-ONLY PASS — same stencil rule
        // covers pipelines that use DepthNormals (e.g., SSAO)
        // ─────────────────────────────────────────
        Pass
        {
            Name "DepthNormalsOnly"
            Tags { "LightMode"="DepthNormalsOnly" }

            Cull Back
            ZWrite On
            ZTest LEqual
            ColorMask 0

            Stencil { Ref [_StencilRef] Comp NotEqual Pass Keep Fail Keep ZFail Keep }

            HLSLPROGRAM
            #pragma vertex VertDN
            #pragma fragment FragDN
            #pragma target 2.0
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct ADN { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct VDN { float4 positionHCS : SV_POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID UNITY_VERTEX_OUTPUT_STEREO };

            VDN VertDN (ADN v)
            {
                VDN o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                return o;
            }
            half4 FragDN (VDN i) : SV_Target { return 0; }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
