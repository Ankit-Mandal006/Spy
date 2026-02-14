Shader "Custom/EnemyFOV_WithSprite"
{
    Properties
    {
        _MainTex ("FOV Texture", 2D) = "white" {}
        _Color ("Tint Color", Color) = (1,1,0,0.35)
        _EdgeSoftness ("Edge Softness", Range(0.1,5)) = 2
        _DistanceFade ("Distance Fade", Range(0.1,5)) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
        }

        LOD 100
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;

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
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Sample texture
                fixed4 tex = tex2D(_MainTex, i.uv);

                // Cone edge fade (left-right)
                float x = abs(i.uv.x - 0.5) * 2.0;
                float edgeFade = saturate(1.0 - pow(x, _EdgeSoftness));

                // Distance fade (near-far)
                float distFade = saturate(1.0 - pow(i.uv.y, _DistanceFade));

                float alpha = tex.a * edgeFade * distFade * _Color.a;

                return fixed4(tex.rgb * _Color.rgb, alpha);
            }
            ENDCG
        }
    }
}
