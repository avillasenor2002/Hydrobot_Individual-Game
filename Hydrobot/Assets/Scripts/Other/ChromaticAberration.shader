Shader "Unlit/ChromaticAberration"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _Intensity("Aberration Intensity", Range(0, 1)) = 0
    }
        SubShader
        {
            Tags { "Queue" = "Overlay" }
            Pass
            {
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #include "UnityCG.cginc"

                struct appdata_t
                {
                    float4 vertex : POSITION;
                    float2 uv : TEXCOORD0;
                };

                struct v2f
                {
                    float2 uv : TEXCOORD0;
                    float4 vertex : SV_POSITION;
                };

                sampler2D _MainTex;
                float _Intensity;

                v2f vert(appdata_t v)
                {
                    v2f o;
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.uv = v.uv;
                    return o;
                }

                fixed4 frag(v2f i) : SV_Target
                {
                    float2 offset = float2(_Intensity * 0.01, 0);

                    float4 col;
                    col.r = tex2D(_MainTex, i.uv + offset).r;
                    col.g = tex2D(_MainTex, i.uv).g;
                    col.b = tex2D(_MainTex, i.uv - offset).b;
                    col.a = tex2D(_MainTex, i.uv).a;

                    return col;
                }
                ENDCG
            }
        }
}
