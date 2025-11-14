Shader "Custom/WaterDroplet_Edge"
{
    Properties
    {
        _MainTex("Droplet Sprite", 2D) = "white" {}
        _Tint("Tint", Color) = (1,1,1,1)
        _DistortStrength("Distortion Strength", Range(0,0.1)) = 0.03
        _RefractionStrength("Refraction Strength", Range(0,0.1)) = 0.05
        _Transparency("Transparency", Range(0,1)) = 0.5
        _EdgeColor("Edge Color", Color) = (1,1,1,1)
        _EdgeThickness("Edge Thickness", Range(0,0.5)) = 0.05
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        GrabPass { "_GrabTex" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _GrabTex;
            float4 _MainTex_ST;
            float _DistortStrength;
            float _RefractionStrength;
            float _Transparency;
            float _EdgeThickness;
            float4 _EdgeColor;
            float4 _Tint;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float4 grabPos : TEXCOORD1; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.grabPos = ComputeGrabScreenPos(o.pos);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Sample droplet texture
                fixed4 droplet = tex2D(_MainTex, i.uv) * _Tint;
                if(droplet.a < 0.01) discard;

                // Compute radial distance from center
                float2 center = float2(0.5,0.5);
                float dist = distance(i.uv, center);

                // Edge gradient
                float edge = smoothstep(0.5 - _EdgeThickness, 0.5, dist);
                fixed4 edgeCol = _EdgeColor * edge;

                // Distortion for refraction
                float2 distort = (i.uv - center) * _DistortStrength;
                float2 refractUV = i.grabPos.xy / i.grabPos.w;
                refractUV += distort * _RefractionStrength;

                fixed4 behind = tex2D(_GrabTex, refractUV);

                // Mix background + droplet
                fixed4 finalCol = lerp(behind, droplet, droplet.a * _Transparency);

                // Add edge
                finalCol = lerp(finalCol, edgeCol, edgeCol.a);

                return finalCol;
            }
            ENDCG
        }
    }
}
