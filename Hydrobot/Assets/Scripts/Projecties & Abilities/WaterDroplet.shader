Shader "Custom/WaterDroplet_Edge"
{
    Properties
    {
        _MainTex("Droplet Sprite", 2D) = "white" {}
        _Tint("Tint", Color) = (1,1,1,1)
        _DistortStrength("Distortion Strength", Range(0,0.05)) = 0.01
        _DistortSpeed("Distortion Speed", Range(0,5)) = 1.0
        _Transparency("Transparency", Range(0,1)) = 0.5
        _EdgeColor("Edge Color", Color) = (1,1,1,1)
        _EdgeThickness("Edge Thickness", Range(0,0.05)) = 0.01
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
            float4 _Tint;
            float _DistortStrength;
            float _DistortSpeed;
            float _Transparency;
            float4 _EdgeColor;
            float _EdgeThickness;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 grabPos : TEXCOORD1;
            };

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

                // --- EDGE DETECTION ---
                float2 offset = float2(_EdgeThickness, 0);
                float alphaC = droplet.a;
                float alphaL = tex2D(_MainTex, i.uv - offset).a;
                float alphaR = tex2D(_MainTex, i.uv + offset).a;
                float alphaU = tex2D(_MainTex, i.uv + float2(0,_EdgeThickness)).a;
                float alphaD = tex2D(_MainTex, i.uv - float2(0,_EdgeThickness)).a;

                float edge = smoothstep(0.0, 1.0, max(max(alphaL, alphaR), max(alphaU, alphaD)) - alphaC);
                fixed4 edgeCol = _EdgeColor * edge;

                // --- SUBTLE WAVE DISTORTION ---
                float2 grabUV = i.grabPos.xy / i.grabPos.w;

                // Use Unity _Time.y directly inside the function
                float waveX = sin(i.uv.y * 20 + _Time.y * _DistortSpeed) * _DistortStrength;
                float waveY = cos(i.uv.x * 20 + _Time.y * _DistortSpeed) * _DistortStrength;
                grabUV += float2(waveX, waveY);

                fixed4 behind = tex2D(_GrabTex, grabUV);

                // Combine droplet with background and edge
                fixed4 finalCol = lerp(behind, droplet, droplet.a * _Transparency);
                finalCol = lerp(finalCol, edgeCol, edgeCol.a);

                return finalCol;
            }
            ENDCG
        }
    }
}
