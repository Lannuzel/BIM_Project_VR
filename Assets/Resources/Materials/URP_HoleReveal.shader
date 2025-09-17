Shader "Custom/URP_HoleReveal_BuildSafe"
{
   
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _HolePosition ("Hole World Position", Vector) = (0,0,0,0)
        _HoleRadius ("Hole Radius", Float) = 0.5
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        ZWrite On
        ZTest LEqual

        Pass
        {
            Name "HoleRevealOpaque"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _HolePosition;
                float _HoleRadius;
            CBUFFER_END

            sampler2D _MainTex;
            float4 _MainTex_ST;

            Varyings vert (Attributes v)
            {
                Varyings o;
                float3 worldPos = TransformObjectToWorld(v.positionOS.xyz);
                o.worldPos = worldPos;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.positionHCS = TransformWorldToHClip(worldPos);
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                float dist = distance(i.worldPos, _HolePosition.xyz);
                if (dist < _HoleRadius)
                    discard;

                return tex2D(_MainTex, i.uv);
            }
            ENDHLSL
        }
    }

}
