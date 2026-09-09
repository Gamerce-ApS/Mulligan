// Experimental VFX Lab — plain additive unlit for glows, impact star, sparks (Built-In RP).
Shader "MulliganCombatFeel/Additive"
{
    Properties
    {
        _MainTex   ("Tex (RGBA)", 2D) = "white" {}
        _Color     ("Color", Color) = (1,1,1,1)
        _Intensity ("Intensity", Range(0,8)) = 1
        _AlphaMul  ("Alpha Multiply", Range(0,4)) = 1
        _TimeOffset("Time Offset", Float) = 0
        _Spin      ("UV Spin (rad/s)", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent+15" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" }
        Blend One One
        Cull Off  ZWrite Off  ZTest LEqual  Lighting Off  Fog { Mode Off }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex; float4 _MainTex_ST; float4 _Color; float _Intensity,_AlphaMul,_TimeOffset,_Spin;
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            struct v2f     { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            v2f vert(appdata v){ v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.uv=TRANSFORM_TEX(v.uv,_MainTex); o.color=v.color; return o; }
            fixed4 frag(v2f i):SV_Target
            {
                float2 uv = i.uv;
                if (abs(_Spin) > 0.0001)
                {
                    float ang = (_Time.y + _TimeOffset) * _Spin;
                    float2 c = uv - 0.5; float s = sin(ang), co = cos(ang);
                    uv = float2(c.x*co - c.y*s, c.x*s + c.y*co) + 0.5;
                }
                fixed4 t = tex2D(_MainTex, uv);
                fixed4 c = t * _Color * i.color;
                float a = saturate(c.a * _AlphaMul);
                return fixed4(c.rgb * a * _Intensity, a);
            }
            ENDCG
        }
    }
    Fallback Off
}
