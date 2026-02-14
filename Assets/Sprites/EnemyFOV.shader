Shader "Custom/EnemyFOV"
{
    Properties
    {
        _Color ("FOV Color", Color) = (1,0,0,0.35)
        _EdgeSoftness ("Edge Softness", Range(0.5,4)) = 2
        _DistanceFade ("Distance Fade", Range(0.5,4)) = 1.5
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
        }

        ZWrite Off
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            float _EdgeSoftness;
            float _DistanceFade;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Soft sides of cone
                float side = abs(i.uv.x - 0.5) * 2.0;
                float edgeFade = saturate(1.0 - pow(side, _EdgeSoftness));

                // Fade with distance
                float distFade = saturate(1.0 - pow(i.uv.y, _DistanceFade));

                float alpha = edgeFade * distFade * _Color.a;

                return fixed4(_Color.rgb, alpha);
            }
            ENDCG
        }
    }
}
