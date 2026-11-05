Shader "Custom/DepthShader"
{
    Properties
    {
        _DepthColor ("Depth Color", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float depth : TEXCOORD0;
            };

            fixed4 _DepthColor;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);

                // Calculate depth value (clip space z divided by w)
                o.depth = o.pos.z / o.pos.w;

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Map depth to a grayscale color
                float depth = i.depth;

                // Convert depth value to visual intensity
                fixed4 color = fixed4(depth, depth, depth, 1.0);

                // Optionally tint with a custom color
                return color * _DepthColor;
            }
            ENDCG
        }
    }
    Fallback "Diffuse"
}
