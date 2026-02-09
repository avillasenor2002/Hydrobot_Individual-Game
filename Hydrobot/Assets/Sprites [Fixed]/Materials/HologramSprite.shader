Shader "Custom/2D/HologramSprite"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Hologram Tint", Color) = (0.2, 0.8, 1, 1)

        _Opacity ("Opacity", Range(0,1)) = 0.6

        _OutlineColor ("Outline Color", Color) = (0.4, 1, 1, 0.5)
        _OutlineSize ("Outline Size", Range(0,0.02)) = 0.005

        _ScanlineStrength ("Scanline Strength", Range(0,1)) = 0.25
        _ScanlineSpeed ("Scanline Speed", Float) = 4
        _ScanlineDensity ("Scanline Density", Float) = 300

        _DistortionStrength ("Distortion Strength", Range(0,0.02)) = 0.004
        _DistortionSpeed ("Distortion Speed", Float) = 3

        _FlickerStrength ("Brightness Flicker", Range(0,0.5)) = 0.15
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Sprite"
            "CanUseSpriteAtlas"="True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        Lighting Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float2 texcoord : TEXCOORD0;
                fixed4 color    : COLOR;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                float2 uv       : TEXCOORD0;
                fixed4 color    : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            fixed4 _Color;
            fixed4 _OutlineColor;
            float _OutlineSize;
            float _Opacity;

            float _ScanlineStrength;
            float _ScanlineSpeed;
            float _ScanlineDensity;

            float _DistortionStrength;
            float _DistortionSpeed;

            float _FlickerStrength;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color;
                return o;
            }

            fixed4 SampleOutline(float2 uv)
            {
                float alpha = 0;
                alpha += tex2D(_MainTex, uv + float2(_OutlineSize, 0)).a;
                alpha += tex2D(_MainTex, uv + float2(-_OutlineSize, 0)).a;
                alpha += tex2D(_MainTex, uv + float2(0, _OutlineSize)).a;
                alpha += tex2D(_MainTex, uv + float2(0, -_OutlineSize)).a;
                return fixed4(_OutlineColor.rgb, saturate(alpha));
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Time-based values
                float t = _Time.y;

                // Distortion (horizontal jitter)
                float distortion =
                    sin((i.uv.y + t * _DistortionSpeed) * 40) *
                    _DistortionStrength;

                float2 distortedUV = i.uv + float2(distortion, 0);

                fixed4 tex = tex2D(_MainTex, distortedUV);

                // Outline
                fixed4 outline = SampleOutline(i.uv);

                // Scanlines
                float scanline =
                    sin((i.uv.y + t * _ScanlineSpeed) * _ScanlineDensity) * 0.5 + 0.5;

                scanline = lerp(1, scanline, _ScanlineStrength);

                // Flicker
                float flicker =
                    1 + sin(t * 25 + i.uv.y * 100) * _FlickerStrength;

                // Final color
                fixed3 hologramColor = tex.rgb * _Color.rgb * scanline * flicker;
                float alpha = tex.a * _Opacity;

                // Blend outline behind sprite
                fixed4 finalColor;
                finalColor.rgb = lerp(outline.rgb, hologramColor, tex.a);
                finalColor.a = max(outline.a * 0.5, alpha);

                return finalColor;
            }
            ENDCG
        }
    }
}
