// Pass 7 — animation/treatment shader for the SUPPLIED hero flame artwork.
// The PNG is authored art; this shader only ADAPTS it: a directional reveal sweep (fire
// emitted from the attacker end toward the target end), a burning hot front at the reveal
// edge, a burn-away erode from the tail, a heat-shimmer / impact warp, and a restrained
// additive bloom. It never reshapes the flame — reveal/burn are along the sweep axis only.
Shader "MulliganCombatFeel/HeavyFireHero"
{
    Properties
    {
        _MainTex     ("Hero art (straight alpha)", 2D) = "black" {}
        _Noise       ("Noise", 2D) = "gray" {}
        _Tint        ("Tint", Color) = (1,1,1,1)
        _Intensity   ("Intensity", Range(0,3)) = 1
        _AlphaMul    ("Alpha Multiply", Range(0,2)) = 1

        [Header(Sweep  0 origin to 1 far end)]
        [Toggle] _Radial ("Radial reveal (impact burst) vs directional (slash)", Float) = 0
        _RadialCore  ("Radial core UV", Vector) = (0.28,0.53,0,0)
        _RadialMax   ("Radial max dist", Float) = 0.9
        _Reveal      ("Reveal", Range(-0.2,1.5)) = 1.5
        _RevealSoft  ("Reveal softness", Range(0.01,0.4)) = 0.14
        _RevealNoise ("Reveal edge ragged", Range(0,0.5)) = 0.22
        _FrontHeat   ("Front hot-edge", Range(0,3)) = 1.4
        _Burn        ("Burn-away from origin", Range(-0.2,1.5)) = -0.2
        _BurnNoise   ("Burn edge ragged", Range(0,0.6)) = 0.32

        [Header(Motion)]
        _Shimmer     ("Heat shimmer", Range(0,0.03)) = 0.005
        _Warp        ("Impact warp (leading edge)", Range(0,0.12)) = 0
        _FlowSpeed   ("Noise flow speed", Float) = 1.3

        [Header(Look)]
        _Bloom       ("Hot bloom", Range(0,3)) = 0.9
        _YtoO        ("Yellow toward orange", Range(0,1)) = 0.25
        _TimeOffset  ("Time Offset", Float) = 0
    }

    SubShader
    {
        Tags { "Queue"="Transparent+12" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" }
        Blend One OneMinusSrcAlpha
        Cull Off ZWrite Off ZTest LEqual Lighting Off Fog { Mode Off }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex; float4 _MainTex_ST;
            sampler2D _Noise;
            float4 _Tint, _RadialCore;
            float _Radial, _RadialMax;
            float _Intensity,_AlphaMul,_Reveal,_RevealSoft,_RevealNoise,_FrontHeat,_Burn,_BurnNoise;
            float _Shimmer,_Warp,_FlowSpeed,_Bloom,_YtoO,_TimeOffset;

            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            v2f vert(appdata v){ v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.uv=TRANSFORM_TEX(v.uv,_MainTex); o.color=v.color; return o; }

            fixed4 frag(v2f i):SV_Target
            {
                float t = _Time.y + _TimeOffset;

                // sweep coordinate:
                //  directional (slash) — 0 at the tail (attacker) end, 1 at the curl (target) end
                //  radial (impact)     — 0 at the contact core, 1 at the outer streak tips
                float rc = _Radial > 0.5
                    ? saturate(distance(i.uv, _RadialCore.xy) / max(0.05, _RadialMax))
                    : saturate(i.uv.x * 0.95 + (1.0 - i.uv.y) * 0.06);

                // scrolling noise for shimmer / burn raggedness
                float2 nuv = i.uv * float2(2.3, 3.1) + float2(-t * _FlowSpeed, t * 0.4);
                float nz = tex2D(_Noise, nuv).r;
                float nz2 = tex2D(_Noise, i.uv * float2(4.7, 5.3) + float2(t * 0.7, -t * 0.5)).r;

                // heat shimmer + impact warp (warp ramps toward the leading edge)
                float warpAmt = _Shimmer + _Warp * smoothstep(0.45, 1.0, rc);
                float2 uv = i.uv + (float2(nz, nz2) - 0.5) * warpAmt;

                fixed4 tex = tex2D(_MainTex, uv);
                float a = tex.a;
                if (a < 0.003) return 0;

                // directional reveal — show where rc < _Reveal (ragged edge)
                float rcR = rc + (nz - 0.5) * _RevealNoise;
                float revealMask = smoothstep(_Reveal + _RevealSoft, _Reveal - _RevealSoft, rcR);

                // burn-away — erode from the tail forward
                float rcB = rc + (nz2 - 0.5) * _BurnNoise;
                float burnMask = smoothstep(_Burn - 0.09, _Burn + 0.05, rcB);

                a *= revealMask * burnMask * _AlphaMul;
                if (a < 0.003) return 0;

                float3 col = tex.rgb * _Tint.rgb * i.color.rgb;

                // push broad yellows a touch toward orange (art-direction lever)
                float yellowness = saturate((col.r + col.g) * 0.5 - col.b - 0.15);
                col = lerp(col, col * float3(1.0, 0.80, 0.52), _YtoO * yellowness);

                // burning hot front right at the reveal edge — the fire igniting along the sweep.
                // narrow band, hot-ORANGE (not white) so a frozen frame doesn't read as a splotch.
                float front = smoothstep(_Reveal - 0.13, _Reveal - 0.03, rcR)
                            * smoothstep(_Reveal + 0.03, _Reveal - 0.03, rcR);
                col = lerp(col, float3(1.0, 0.74, 0.34), saturate(front * _FrontHeat) * 0.7);
                float3 frontBloom = front * _FrontHeat * float3(0.9, 0.42, 0.12);

                // restrained additive bloom on the brightest pixels
                float lum = dot(col, float3(0.3,0.59,0.11));
                float3 bloom = pow(saturate(lum - 0.62), 1.7) * _Bloom * col;

                // lift intensity, but roll off above 1 so a +brightness pass does NOT blow out the
                // authored yellow/white core (preserves the red/orange separation lower down).
                float3 lit = col * _Intensity;
                lit = lerp(lit, 1.0 - exp(-lit), 0.45);   // partial filmic knee on the highlights only

                float3 outp = (lit + bloom + frontBloom) * a;   // premultiplied
                return fixed4(outp, a);
            }
            ENDCG
        }
    }
    Fallback Off
}
