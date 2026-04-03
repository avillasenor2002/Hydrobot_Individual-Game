Shader "Custom/WaterDroplet_Edge"
{
    Properties
    {
        _MainTex("Droplet Sprite", 2D) = "white" {}
        _Tint("Tint", Color) = (1,1,1,1)
        _DistortStrength("Distortion Strength", Range(0,0.05)) = 0.01
        _DistortSpeed("Distortion Speed", Range(0,5)) = 1.0
        _Transparency("Transparency", Range(0,1)) = 0.5
        _EdgeColor("Edge Color", Color) = (1,1,1,0.6)
        _EdgeThickness("Edge Thickness", Range(0.001,0.05)) = 0.012
        _EdgeSoftness("Edge Softness", Range(0.001,0.1)) = 0.02
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
            float4 _MainTex_TexelSize;   // auto-filled: .xy = 1/width, 1/height
            float4 _MainTex_ST;
            float4 _Tint;
            float _DistortStrength;
            float _DistortSpeed;
            float _Transparency;
            float4 _EdgeColor;
            float _EdgeThickness;
            float _EdgeSoftness;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f
            {
                float4 pos      : SV_POSITION;
                float2 uv       : TEXCOORD0;
                float4 grabPos  : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos     = UnityObjectToClipPos(v.vertex);
                o.uv      = TRANSFORM_TEX(v.uv, _MainTex);
                o.grabPos = ComputeGrabScreenPos(o.pos);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // ── Sample centre ────────────────────────────────────────
                fixed4 droplet = tex2D(_MainTex, i.uv) * _Tint;
                if (droplet.a < 0.01) discard;

                // ── Edge detection ───────────────────────────────────────
                // Strategy: for each fragment INSIDE the sprite, sample
                // outward in 8 directions by _EdgeThickness. Any direction
                // that reaches alpha < a threshold means we are near the
                // boundary. The minimum of those outer samples, subtracted
                // from the centre alpha, tells us how close to the edge we are.
                //
                // Using _MainTex_TexelSize keeps the offset in UV space
                // proportional to the actual texture resolution.

                float2 t = _EdgeThickness * _MainTex_TexelSize.xy
                           * float2(_MainTex_TexelSize.z, _MainTex_TexelSize.w);
                // _TexelSize.zw = texture width/height in pixels, so
                // t is now exactly _EdgeThickness texels wide/tall.
                // If you'd rather drive it in UV space directly, just use:
                //   float2 t = float2(_EdgeThickness, _EdgeThickness);

                // 8-tap ring
                float minOuter =  1.0;
                minOuter = min(minOuter, tex2D(_MainTex, i.uv + float2( t.x,  0  )).a);
                minOuter = min(minOuter, tex2D(_MainTex, i.uv + float2(-t.x,  0  )).a);
                minOuter = min(minOuter, tex2D(_MainTex, i.uv + float2( 0,    t.y)).a);
                minOuter = min(minOuter, tex2D(_MainTex, i.uv + float2( 0,   -t.y)).a);
                minOuter = min(minOuter, tex2D(_MainTex, i.uv + float2( t.x,  t.y)).a);
                minOuter = min(minOuter, tex2D(_MainTex, i.uv + float2(-t.x,  t.y)).a);
                minOuter = min(minOuter, tex2D(_MainTex, i.uv + float2( t.x, -t.y)).a);
                minOuter = min(minOuter, tex2D(_MainTex, i.uv + float2(-t.x, -t.y)).a);

                // edgeFactor → 1 when we are right at the boundary, 0 deep inside.
                // smoothstep over _EdgeSoftness controls how sharp/soft the line is.
                float edgeFactor = smoothstep(0.3, 0.3 + _EdgeSoftness, droplet.a - minOuter);

                // ── Background distortion ─────────────────────────────────
                float2 grabUV = i.grabPos.xy / i.grabPos.w;
                float  waveX  = sin(i.uv.y * 20.0 + _Time.y * _DistortSpeed) * _DistortStrength;
                float  waveY  = cos(i.uv.x * 20.0 + _Time.y * _DistortSpeed) * _DistortStrength;
                grabUV += float2(waveX, waveY);
                fixed4 behind = tex2D(_GrabTex, grabUV);

                // ── Composite ─────────────────────────────────────────────
                // 1. Blend droplet body over background
                fixed4 col = lerp(behind, droplet, droplet.a * _Transparency);

                // 2. Layer the edge line on top.
                //    _EdgeColor.a is the maximum opacity of the line.
                //    edgeFactor drives it so only the boundary lights up.
                float  edgeAlpha = _EdgeColor.a * edgeFactor;
                col.rgb = lerp(col.rgb, _EdgeColor.rgb, edgeAlpha);
                col.a   = max(col.a, edgeAlpha);          // keep the pixel visible

                return col;
            }
            ENDCG
        }
    }
}
