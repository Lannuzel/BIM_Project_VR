Shader "Custom/Mask_URP_StencilWrite_DEBUG"
{
    Properties { _StencilRef ("Stencil Ref", Int) = 1 }
    SubShader
    {
        Tags { "Queue"="Geometry-100" "RenderType"="Opaque" }
        Pass
        {
            Name "SRPDefaultUnlit"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Off
            ZWrite Off
            ZTest Always
            ColorMask RGBA
            Stencil { Ref [_StencilRef] Comp Always Pass Replace Fail Keep ZFail Keep }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 2.0
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A { float4 positionOS: POSITION; };
            struct V { float4 positionHCS: SV_POSITION; };
            V Vert(A v){ V o; o.positionHCS = TransformObjectToHClip(v.positionOS.xyz); return o; }
            half4 Frag(V i):SV_Target{ return half4(1,0,1,1); }
            ENDHLSL
        }
    }
}
