Shader "Custom/BuiltinShadowCatcher_V2"
{
    Properties
    {
        _ShadowColor ("Shadow Color", Color) = (0, 0, 0, 0.5)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Geometry+1" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        CGPROGRAM
        #pragma surface surf ShadowOnly alpha:fade
        
        fixed4 _ShadowColor;

        struct Input {
            float2 uv_MainTex;
        };

        inline fixed4 LightingShadowOnly (SurfaceOutput s, fixed3 lightDir, fixed atten)
        {
            fixed4 c;
            c.rgb = _ShadowColor.rgb;
            // atten이 1이면 빛(그림자 없음), 0이면 그림자
            c.a = (1.0 - atten) * _ShadowColor.a; 
            return c;
        }

        void surf (Input IN, inout SurfaceOutput o)
        {
            o.Albedo = 1;
            o.Alpha = 1;
        }
        ENDCG
    }
    Fallback "Transparent/Cutout/VertexLit"
}