Shader "Unlit/ColorSwapWithLayeredShadow_Tint_MultiKey_Mirrored"
{
    Properties
    {
        _MainTex("Sprite Texture", 2D) = "white" {}
        _Color("Tint Color", Color) = (1,1,1,1)
        _KeyColor("Color to Remove 1", Color) = (0,1,0,1)
        _KeyColor2("Color to Remove 2", Color) = (1,0,0,1)
        _Threshold("Color Threshold", Range(0,1)) = 0.1
        _ShadowOffset("Shadow Offset (Pixels)", Vector) = (1, -1, 0, 0)
        _ShadowColor("Shadow Color", Color) = (0,0,0,0.5)
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        Lighting Off
        ZWrite Off

        // -------------------
        // Shadow Pass (pixel-perfect)
        // -------------------
        Pass
        {
            Name "Shadow"
            Tags { "Queue"="Transparent-1" "LightMode"="Always" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _ShadowOffset;
            fixed4 _KeyColor;
            fixed4 _KeyColor2;
            float _Threshold;
            fixed4 _ShadowColor;
            fixed4 _Color;

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;

                // Apply offset IN OBJECT SPACE (pixel perfect)
                float4 offsetVertex = v.vertex;
                offsetVertex.xy += _ShadowOffset.xy;

                o.vertex = UnityObjectToClipPos(offsetVertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);

                if (distance(col.rgb, _KeyColor.rgb) < _Threshold ||
                    distance(col.rgb, _KeyColor2.rgb) < _Threshold)
                    discard;

                return _ShadowColor * _Color;
            }
            ENDCG
        }

        // -------------------
        // Main Sprite Pass
        // -------------------
        Pass
        {
            Name "MainSprite"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _KeyColor;
            fixed4 _KeyColor2;
            float _Threshold;
            fixed4 _Color;

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);

                if (distance(col.rgb, _KeyColor.rgb) < _Threshold ||
                    distance(col.rgb, _KeyColor2.rgb) < _Threshold)
                    col.a = 0;

                return col * _Color;
            }
            ENDCG
        }
    }
}
