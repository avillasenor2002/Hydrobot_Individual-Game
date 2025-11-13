Shader "Custom/Blur"
{
    Properties
    {
        [PerRendererData] _MainTex("Base Texture", 2D) = "white" {}
        _Color("Tint", Color) = (1,1,1,1)
        _BlurSize("Blur Size", Range(0.0, 0.1)) = 0.05
    }

        SubShader
        {
            Tags { "Queue" = "Overlay" "RenderType" = "Transparent" }

            Pass
            {
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #include "UnityCG.cginc"

                struct appdata_t
                {
                    float4 vertex   : POSITION;
                    float4 color    : COLOR;
                    float2 texcoord : TEXCOORD0;
                };

                struct v2f
                {
                    float4 vertex   : SV_POSITION;
                    float2 uv       : TEXCOORD0;
                };

                sampler2D _MainTex;
                float _BlurSize;

                v2f vert(appdata_t IN)
                {
                    v2f OUT;
                    OUT.vertex = UnityObjectToClipPos(IN.vertex);
                    OUT.uv = IN.texcoord;
                    return OUT;
                }

                fixed4 frag(v2f i) : SV_Target
                {
                    // Apply blur effect to the entire screen texture, while preserving the alpha transparency
                    fixed4 sum = fixed4(0.0, 0.0, 0.0, 0.0);
                    sum += tex2D(_MainTex, i.uv + float2(0.0, -4.0 * _BlurSize)) * 0.05;
                    sum += tex2D(_MainTex, i.uv + float2(0.0, -3.0 * _BlurSize)) * 0.09;
                    sum += tex2D(_MainTex, i.uv + float2(0.0, -2.0 * _BlurSize)) * 0.12;
                    sum += tex2D(_MainTex, i.uv + float2(0.0, -1.0 * _BlurSize)) * 0.15;
                    sum += tex2D(_MainTex, i.uv) * 0.16;
                    sum += tex2D(_MainTex, i.uv + float2(0.0, 1.0 * _BlurSize)) * 0.15;
                    sum += tex2D(_MainTex, i.uv + float2(0.0, 2.0 * _BlurSize)) * 0.12;
                    sum += tex2D(_MainTex, i.uv + float2(0.0, 3.0 * _BlurSize)) * 0.09;
                    sum += tex2D(_MainTex, i.uv + float2(0.0, 4.0 * _BlurSize)) * 0.05;

                    // Return the blurred color, keeping the original alpha intact for transparency
                    return fixed4(sum.rgb, tex2D(_MainTex, i.uv).a);
                }
                ENDCG
            }
        }
}