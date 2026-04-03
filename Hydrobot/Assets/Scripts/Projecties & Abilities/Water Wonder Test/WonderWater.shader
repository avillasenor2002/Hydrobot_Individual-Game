Shader "Custom/WonderWater2D"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint Color", Color) = (0.2, 0.6, 1.0, 0.75)
        
        // Distortion
        _DistortionTex ("Distortion Noise Texture", 2D) = "bump" {}
        _DistortionStrength ("Distortion Strength", Range(0, 0.1)) = 0.02
        _DistortionSpeed ("Distortion Speed", Vector) = (0.3, 0.4, 0, 0)
        
        // Surface wave
        _SurfaceHeight ("Surface Band Height (UV)", Range(0, 0.2)) = 0.06
        _SurfaceFoamColor ("Surface Foam Color", Color) = (1,1,1,1)
        _SurfaceWaveSpeed ("Surface Wave Speed", Float) = 2.0
        _SurfaceWaveFreq ("Surface Wave Frequency", Float) = 8.0
        _SurfaceWaveAmp ("Surface Wave Amplitude (UV)", Range(0, 0.05)) = 0.015
        
        // Edge glow
        _EdgeWidth ("Edge Glow Width (UV)", Range(0, 0.08)) = 0.025
        _EdgeColor ("Edge Glow Color", Color) = (1,1,1,0.9)
        _EdgeFalloff ("Edge Falloff", Range(1, 8)) = 3.0
        
        // Caustics / shimmer
        _CausticsSpeed ("Caustics Speed", Float) = 1.2
        _CausticsScale ("Caustics Scale", Float) = 4.0
        _CausticsStrength ("Caustics Brightness", Range(0, 0.5)) = 0.15
        
        // Depth fade (sprite alpha)
        _DepthFadeStart ("Depth Fade Start (UV from top)", Range(0,1)) = 0.1
        _DepthFadeEnd   ("Depth Fade End (UV from top)",   Range(0,1)) = 0.35
        
        // Sorting
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [HideInInspector] _AlphaTex ("External Alpha", 2D) = "white" {}
        [HideInInspector] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"           = "Transparent"
            "RenderType"      = "Transparent"
            "RenderPipeline"  = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "WonderWater2D"
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // ── Textures ──────────────────────────────────────────────
            TEXTURE2D(_MainTex);       SAMPLER(sampler_MainTex);
            TEXTURE2D(_DistortionTex); SAMPLER(sampler_DistortionTex);

            // ── CBs ───────────────────────────────────────────────────
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _DistortionTex_ST;
                float4 _Color;
                float4 _SurfaceFoamColor;
                float4 _EdgeColor;
                float2 _DistortionSpeed;
                float  _DistortionStrength;
                float  _SurfaceHeight;
                float  _SurfaceWaveSpeed;
                float  _SurfaceWaveFreq;
                float  _SurfaceWaveAmp;
                float  _EdgeWidth;
                float  _EdgeFalloff;
                float  _CausticsSpeed;
                float  _CausticsScale;
                float  _CausticsStrength;
                float  _DepthFadeStart;
                float  _DepthFadeEnd;
                float4 _RendererColor;
            CBUFFER_END

            // ── Vertex ────────────────────────────────────────────────
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float2 uvRaw      : TEXCOORD1;   // un-transformed UVs
                float4 color      : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes IN)
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                Varyings OUT;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv         = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.uvRaw      = IN.uv;          // [0,1] space, top=0 for sprites
                OUT.color      = IN.color * _RendererColor;
                return OUT;
            }

            // ── Helpers ───────────────────────────────────────────────

            // Cheap 2-octave value noise used for caustics
            float hash21(float2 p)
            {
                p = frac(p * float2(234.34, 435.345));
                p += dot(p, p + 34.23);
                return frac(p.x * p.y);
            }

            float valueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(
                    lerp(hash21(i),             hash21(i + float2(1,0)), u.x),
                    lerp(hash21(i + float2(0,1)),hash21(i + float2(1,1)), u.x),
                    u.y);
            }

            float caustics(float2 uv, float t)
            {
                float2 p = uv * _CausticsScale;
                float n  = valueNoise(p + float2(t * 0.7, t * 0.4));
                      n += valueNoise(p * 1.7 + float2(-t * 0.5, t * 0.8)) * 0.5;
                return saturate(n * 1.5 - 0.4);
            }

            // ── Fragment ──────────────────────────────────────────────
            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv    = IN.uv;
                float2 uvRaw = IN.uvRaw;   // Y: 0 = top sprite edge, 1 = bottom
                float  t     = _Time.y;

                // ── 1. Scrolling distortion ──────────────────────────
                float2 distUV  = uv * _DistortionTex_ST.xy + _DistortionSpeed * t;
                float2 distUV2 = uv * _DistortionTex_ST.xy * 0.7 + _DistortionSpeed * float2(-0.6, 0.5) * t;

                float2 d1 = SAMPLE_TEXTURE2D(_DistortionTex, sampler_DistortionTex, distUV ).rg;
                float2 d2 = SAMPLE_TEXTURE2D(_DistortionTex, sampler_DistortionTex, distUV2).rg;
                float2 distOffset = (d1 + d2 - 1.0) * _DistortionStrength;

                // Suppress distortion near the top surface so foam stays crisp
                float suppressSurface = smoothstep(0.0, _SurfaceHeight * 1.5, uvRaw.y);
                distOffset *= suppressSurface;

                float2 sampledUV = uv + distOffset;

                // ── 2. Sample sprite ─────────────────────────────────
                half4 sprite = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, sampledUV);
                sprite *= IN.color;

                // ── 3. Water tint ────────────────────────────────────
                // Multiply sprite color by water color, keep alpha
                half3 waterCol = sprite.rgb * _Color.rgb * 2.0;  // *2 to stay bright
                half  waterAlp = sprite.a   * _Color.a;

                // Depth alpha fade: more transparent near top, more opaque below
                float depthFade = smoothstep(_DepthFadeStart, _DepthFadeEnd, uvRaw.y);
                waterAlp *= lerp(0.55, 1.0, depthFade);

                // ── 4. Caustics shimmer (below surface band) ─────────
                float caustMask = smoothstep(_SurfaceHeight, _SurfaceHeight + 0.05, uvRaw.y);
                float caust     = caustics(sampledUV, t * _CausticsSpeed);
                waterCol += caust * _CausticsStrength * caustMask;

                // ── 5. Surface foam band ─────────────────────────────
                // Animated sine wave defines the surface line in UV space
                float waveY = _SurfaceHeight
                            + sin(uvRaw.x * _SurfaceWaveFreq + t * _SurfaceWaveSpeed) * _SurfaceWaveAmp
                            + sin(uvRaw.x * _SurfaceWaveFreq * 2.3 + t * _SurfaceWaveSpeed * 0.7) * _SurfaceWaveAmp * 0.4;

                // Distance from the current pixel to the wave line
                float distToWave = uvRaw.y - waveY;   // negative = above water surface

                // Foam: a soft band just at + slightly above the wave
                float foamBand = smoothstep(0.0, _SurfaceHeight * 0.5, distToWave)
                               * (1.0 - smoothstep(0.0, _SurfaceHeight * 0.8, distToWave));
                // Second inner sparkle layer
                float foamBand2 = smoothstep(0.0, _SurfaceHeight * 0.2, distToWave)
                                * (1.0 - smoothstep(0.0, _SurfaceHeight * 0.35, distToWave));

                float foamAlpha = saturate(foamBand + foamBand2 * 0.5);

                // Clip pixels fully above the wave surface (outside water body)
                float insideWater = step(0.0, distToWave);   // 1 if below wave

                // ── 6. X/Y Edge glow ────────────────────────────────
                // Left / right edges
                float edgeX = min(uvRaw.x, 1.0 - uvRaw.x);
                float glowX = pow(saturate(1.0 - edgeX / _EdgeWidth), _EdgeFalloff);

                // Bottom edge only (top is handled by the surface foam)
                float edgeY    = 1.0 - uvRaw.y;  // 1 at bottom
                float glowYBot = pow(saturate(edgeY / _EdgeWidth), _EdgeFalloff);
                // Also a faint glow on the top surface edge (just below foam band)
                float glowYTop = pow(saturate(1.0 - abs(distToWave) / (_EdgeWidth * 2.0)), _EdgeFalloff * 0.5);

                float edgeGlow = saturate(glowX + glowYBot + glowYTop * 0.5);

                // ── 7. Composite ─────────────────────────────────────
                half3 col = waterCol;

                // Blend foam over water color
                col = lerp(col, _SurfaceFoamColor.rgb, foamAlpha * _SurfaceFoamColor.a);

                // Add edge glow
                col = lerp(col, _EdgeColor.rgb, edgeGlow * _EdgeColor.a);

                // Final alpha
                half alpha = waterAlp * insideWater;
                // Foam and edges slightly boost alpha
                alpha = saturate(alpha + foamAlpha * 0.4 * insideWater + edgeGlow * _EdgeColor.a * 0.3 * insideWater);

                return half4(col, alpha);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/2D/Sprite-Unlit-Default"
}
