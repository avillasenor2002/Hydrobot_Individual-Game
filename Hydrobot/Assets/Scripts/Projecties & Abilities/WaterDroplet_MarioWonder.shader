Shader "Custom/WaterDroplet_MarioWonder"
{
    Properties
    {
        // ─── Sprite ───────────────────────────────────────────────────────────
        _MainTex            ("Droplet Sprite",          2D)             = "white" {}
        _Tint               ("Tint",                    Color)          = (0.4, 0.75, 1.0, 1.0)

        // ─── Edge Ripple ─────────────────────────────────────────────────────
        _EdgeColor          ("Edge Color",              Color)          = (0.8, 0.95, 1.0, 1.0)
        _EdgeThickness      ("Edge Thickness",          Range(0, 0.06)) = 0.018
        _EdgeGlowPower      ("Edge Glow Power",         Range(0.5, 8))  = 2.5
        _EdgeRippleAmp      ("Edge Ripple Amplitude",   Range(0, 0.04)) = 0.012
        _EdgeRippleFreq     ("Edge Ripple Frequency",   Range(1, 40))   = 14.0
        _EdgeRippleSpeed    ("Edge Ripple Speed",       Range(0, 10))   = 3.5
        _EdgeRippleBands    ("Edge Ripple Bands",       Range(1, 6))    = 3.0

        // ─── Surface Distortion ──────────────────────────────────────────────
        _DistortStrength    ("Distortion Strength",     Range(0, 0.05)) = 0.01
        _DistortSpeed       ("Distortion Speed",        Range(0, 5))    = 1.0

        // ─── Interior Caustics / Shimmer ─────────────────────────────────────
        _CausticColor       ("Caustic / Shimmer Color", Color)          = (0.6, 0.9, 1.0, 0.5)
        _CausticSpeed       ("Caustic Speed",           Range(0, 6))    = 1.4
        _CausticScale       ("Caustic Scale",           Range(1, 40))   = 12.0
        _CausticIntensity   ("Caustic Intensity",       Range(0, 1))    = 0.35
        _CausticSharpness   ("Caustic Sharpness",       Range(0.5, 8))  = 2.5

        // ─── Surface Highlight ───────────────────────────────────────────────
        _HighlightColor     ("Highlight Color",         Color)          = (1.0, 1.0, 1.0, 0.7)
        _HighlightSize      ("Highlight Size",          Range(0.01, 1)) = 0.22
        _HighlightOffset    ("Highlight Offset",        Vector)         = (-0.15, 0.2, 0, 0)
        _HighlightPulseAmp  ("Highlight Pulse Amp",     Range(0, 0.5))  = 0.12
        _HighlightPulseSpeed("Highlight Pulse Speed",   Range(0, 6))    = 1.2

        // ─── Transparency ────────────────────────────────────────────────────
        _Transparency       ("Transparency",            Range(0, 1))    = 0.5

        // ─── Ripple (fragment-space) ──────────────────────────────────────────
        // Moved from vertex shader — now a pure UV deformation in the fragment
        // stage so it works on any sprite mesh density (including quads).
        _RippleAmp          ("Ripple Amplitude",        Range(0, 0.04)) = 0.018
        _RippleFreq         ("Ripple Frequency",        Range(1, 20))   = 6.0
        _RippleSpeed        ("Ripple Speed",            Range(0, 8))    = 2.2
        _RippleEdgeFalloff  ("Ripple Edge Falloff",     Range(0.01, 1)) = 0.15
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        GrabPass { "_GrabTex" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            // ── Textures ──────────────────────────────────────────────────────
            sampler2D _MainTex;
            sampler2D _GrabTex;
            float4    _MainTex_ST;
            float4    _MainTex_TexelSize;

            // ── Uniforms ─────────────────────────────────────────────────────
            fixed4  _Tint;
            fixed4  _EdgeColor;
            float   _EdgeThickness;
            float   _EdgeGlowPower;
            float   _EdgeRippleAmp;
            float   _EdgeRippleFreq;
            float   _EdgeRippleSpeed;
            float   _EdgeRippleBands;
            float   _DistortStrength;
            float   _DistortSpeed;
            fixed4  _CausticColor;
            float   _CausticSpeed;
            float   _CausticScale;
            float   _CausticIntensity;
            float   _CausticSharpness;
            fixed4  _HighlightColor;
            float   _HighlightSize;
            float4  _HighlightOffset;
            float   _HighlightPulseAmp;
            float   _HighlightPulseSpeed;
            float   _Transparency;
            float   _RippleAmp;
            float   _RippleFreq;
            float   _RippleSpeed;
            float   _RippleEdgeFalloff;

            // ── Structs ───────────────────────────────────────────────────────
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                fixed4 color  : COLOR;
            };

            struct v2f
            {
                float4 pos     : SV_POSITION;
                float2 uv      : TEXCOORD0;
                float4 grabPos : TEXCOORD1;
                fixed4 color   : COLOR;
            };

            // ────────────────────────────────────────────────────────────────
            //  Vertex — clean pass-through, no deformation
            // ────────────────────────────────────────────────────────────────
            v2f vert(appdata v)
            {
                v2f o;
                o.pos     = UnityObjectToClipPos(v.vertex);
                o.uv      = TRANSFORM_TEX(v.uv, _MainTex);
                o.grabPos = ComputeGrabScreenPos(o.pos);
                o.color   = v.color;
                return o;
            }

            // ────────────────────────────────────────────────────────────────
            //  Fragment
            // ────────────────────────────────────────────────────────────────
            fixed4 frag(v2f i) : SV_Target
            {
                float  t  = _Time.y;
                float2 uv = i.uv;

                // ════════════════════════════════════════════════════════════
                //  STEP 1 — RIPPLE UV DEFORMATION
                //
                //  The ripple that was previously in the vertex shader is now
                //  computed here in the fragment shader as a UV-space warp.
                //  This produces smooth results on any mesh density (including
                //  a plain quad), because every pixel is processed individually
                //  rather than being interpolated between a sparse set of verts.
                //
                //  Two components combine for the Mario Wonder "jelly" feel:
                //
                //    Primary   — radial sine wave, circular rings that travel
                //                outward from the centre, creating the classic
                //                water-balloon expansion/contraction.
                //
                //    Secondary — tangential (perpendicular-to-radius) wobble,
                //                reproduces the original shader's squash/stretch
                //                axis behaviour without needing vertex layout.
                //
                //  Both components are weighted by edgeWeight so deformation is
                //  strongest near the sprite rim and fades to zero at the centre,
                //  keeping interior details sharp.
                // ════════════════════════════════════════════════════════════

                float2 centredRaw = uv - 0.5;
                float  radial     = length(centredRaw);

                // Normalised radial direction (guard against divide-by-zero at centre)
                float2 radialDir = (radial > 0.001) ? (centredRaw / radial)
                                                    : float2(0.0, 0.0);

                // Tangent direction — 90° CCW rotation of the radial direction
                float2 tangentDir = float2(-radialDir.y, radialDir.x);

                // Edge weight: 0 at centre, 1 at the rim (_RippleEdgeFalloff
                // controls how far inward the ramp reaches)
                float edgeWeight = smoothstep(0.0, _RippleEdgeFalloff, radial);

                // Primary radial ripple — concentric rings travelling outward
                float primaryWave = sin(radial  * _RippleFreq  * 6.2832
                                        - t      * _RippleSpeed)
                                  * _RippleAmp * edgeWeight;

                // Secondary tangential wobble — adds angular variation so the
                // border ripples unevenly (the squash/stretch "jelly" look)
                float secondaryWave = cos(radial * _RippleFreq * 4.0
                                          + t    * _RippleSpeed * 0.8
                                          + atan2(centredRaw.y, centredRaw.x) * 2.0)
                                    * _RippleAmp * 0.5 * edgeWeight;

                // Combine into a UV offset and produce the deformed UV
                float2 rippleOffset = radialDir  * primaryWave
                                    + tangentDir * secondaryWave;
                float2 rippleUV     = uv + rippleOffset;

                // ════════════════════════════════════════════════════════════
                //  STEP 2 — BASE SPRITE SAMPLE
                // ════════════════════════════════════════════════════════════

                fixed4 droplet = tex2D(_MainTex, rippleUV) * _Tint * i.color;
                if (droplet.a < 0.01) discard;

                float2 centred = rippleUV - 0.5;   // re-centre on deformed UV

                // ════════════════════════════════════════════════════════════
                //  STEP 3 — EDGE DETECTION: X AND Y AXES ONLY, NO CORNERS
                //
                //  We sample the sprite alpha in the four cardinal directions
                //  (left, right, up, down) and build SEPARATE masks for the
                //  horizontal (X) and vertical (Y) axes.
                //
                //  Corner exclusion logic
                //  ──────────────────────
                //  A corner/diagonal pixel is one where BOTH the X-axis edge
                //  test AND the Y-axis edge test fire at the same time.  We
                //  detect that with a simple AND (isCorner) and subtract it from
                //  both axis masks, leaving only the flat cardinal faces.
                //
                //  This means:
                //    • Left / right faces of the silhouette  → white border ✓
                //    • Top  / bottom faces of the silhouette → white border ✓
                //    • Diagonal / corner faces               → untouched    ✓
                // ════════════════════════════════════════════════════════════

                float aC = droplet.a;

                // Cardinal axis samples (using the deformed rippleUV)
                float aL = tex2D(_MainTex, rippleUV - float2(_EdgeThickness, 0.0)).a;
                float aR = tex2D(_MainTex, rippleUV + float2(_EdgeThickness, 0.0)).a;
                float aU = tex2D(_MainTex, rippleUV + float2(0.0, _EdgeThickness)).a;
                float aD = tex2D(_MainTex, rippleUV - float2(0.0, _EdgeThickness)).a;

                // Per-axis raw edge masks
                float edgeX = smoothstep(0.0, 1.0, max(aL, aR) - aC);
                float edgeY = smoothstep(0.0, 1.0, max(aU, aD) - aC);

                // Classify corners: both axes fire simultaneously
                float isCorner  = step(0.01, edgeX) * step(0.01, edgeY);

                // Cardinal-only masks: zero out the corner contribution
                float pureEdgeX = edgeX * (1.0 - isCorner);
                float pureEdgeY = edgeY * (1.0 - isCorner);
                float edgeMask  = saturate(pureEdgeX + pureEdgeY);

                // Animated ripple bands travelling along the cardinal border.
                // edgeCoord uses the Chebyshev (L∞) distance so the ripple
                // phase is consistent along both horizontal and vertical runs.
                float edgeCoord = max(abs(centred.x), abs(centred.y));
                float edgeRippleSum = 0.0;
                for (float b = 0.0; b < _EdgeRippleBands; b += 1.0)
                {
                    float phaseOffset = b * (6.2832 / _EdgeRippleBands);
                    edgeRippleSum += sin(edgeCoord * _EdgeRippleFreq
                                         - t       * _EdgeRippleSpeed
                                         + phaseOffset);
                }
                edgeRippleSum /= _EdgeRippleBands;          // –1 .. 1
                float rippleN = edgeRippleSum * 0.5 + 0.5;  //  0 .. 1

                // Hard white line; ripple modulates brightness (never dims below 0.75)
                float  lineBrightness = lerp(0.75, 1.0, rippleN);
                fixed4 edgeCol = fixed4(_EdgeColor.rgb * lineBrightness, edgeMask);

                // ════════════════════════════════════════════════════════════
                //  STEP 4 — BACKGROUND GRAB / REFRACTION
                // ════════════════════════════════════════════════════════════

                float2 grabUV = i.grabPos.xy / i.grabPos.w;
                float  waveX  = sin(uv.y * 20.0 + t * _DistortSpeed) * _DistortStrength;
                float  waveY  = cos(uv.x * 20.0 + t * _DistortSpeed) * _DistortStrength;
                grabUV += float2(waveX, waveY);
                fixed4 behind = tex2D(_GrabTex, grabUV);

                // ════════════════════════════════════════════════════════════
                //  STEP 5 — CAUSTIC / SHIMMER
                // ════════════════════════════════════════════════════════════

                float2 cUV = rippleUV * _CausticScale;
                float c1 = sin(cUV.x + t * _CausticSpeed)
                         * cos(cUV.y - t * _CausticSpeed * 0.7);
                float c2 = sin(cUV.x * 1.3 - cUV.y * 0.9 + t * _CausticSpeed * 1.1);
                float c3 = cos(length(cUV - float2(sin(t * 0.7), cos(t * 0.5))) * 2.0
                               - t * _CausticSpeed * 0.9);

                float caustic = pow(saturate((c1 + c2 + c3) / 3.0 * 0.5 + 0.5),
                                    _CausticSharpness);
                fixed4 causticCol = _CausticColor * caustic * _CausticIntensity * droplet.a;

                // ════════════════════════════════════════════════════════════
                //  STEP 6 — SPECULAR HIGHLIGHT
                // ════════════════════════════════════════════════════════════

                float  hlPulse = 1.0 + sin(t * _HighlightPulseSpeed) * _HighlightPulseAmp;
                float  hlDist  = length(centred - _HighlightOffset.xy);
                float  hlMask  = smoothstep(_HighlightSize * hlPulse,
                                            _HighlightSize * hlPulse * 0.2,
                                            hlDist) * droplet.a;
                fixed4 hlCol   = _HighlightColor * hlMask;

                // ════════════════════════════════════════════════════════════
                //  STEP 7 — COMPOSITE
                // ════════════════════════════════════════════════════════════

                fixed4 bodyCol  = lerp(behind, droplet, droplet.a * _Transparency);
                bodyCol.rgb    += causticCol.rgb * causticCol.a;
                bodyCol         = lerp(bodyCol, hlCol, hlCol.a * _HighlightColor.a);
                bodyCol.rgb     = lerp(bodyCol.rgb, edgeCol.rgb, edgeCol.a);
                bodyCol.a       = droplet.a;

                return bodyCol;
            }
            ENDCG
        }
    }

    FallBack "Sprites/Default"
}
