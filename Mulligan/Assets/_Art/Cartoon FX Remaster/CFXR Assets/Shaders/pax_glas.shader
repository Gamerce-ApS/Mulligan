Shader "Custom/BetterGlassShader"
{
    Properties
    {
        _TintColor ("Tint Color", Color) = (1,1,1,0.1)
        _FresnelPower ("Fresnel Power", Range(0, 5)) = 2.0
        _Transparency ("Transparency", Range(0, 1)) = 0.08
        _Smoothness ("Smoothness", Range(0,1)) = 0.95
        _Refraction ("Refraction Strength", Range(0, 1)) = 0.1
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 200
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldNormal : NORMAL;
                float2 uv : TEXCOORD0;
                float3 viewDir : TEXCOORD1;
            };

            float4 _TintColor;
            float _FresnelPower;
            float _Transparency;
            float _Smoothness;
            float _Refraction;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.uv = v.uv;
                o.viewDir = normalize(WorldSpaceViewDir(v.vertex));
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Fresnel effect for smooth glass edges
                float fresnel = pow(1.0 - saturate(dot(i.viewDir, i.worldNormal)), _FresnelPower);

                // Base glass color with slight tint
                float4 glassColor = _TintColor;
                glassColor.a = _Transparency + (fresnel * 0.15); 

                // Soft reflections using fresnel
                float reflectivity = fresnel * _Smoothness;
                glassColor.rgb += reflectivity * 0.5;

                return glassColor;
            }
            ENDCG
        }
    }
}
