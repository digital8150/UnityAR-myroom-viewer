Shader "Custom/URPShadowCatcher" {
    Properties {
        _ShadowColor ("Shadow Color", Color) = (0, 0, 0, 0.5)
    }
    SubShader {
        // URP 투명 렌더링 세팅
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // URP 그림자 매크로 필수!
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes {
                float4 positionOS : POSITION;
            };

            struct Varyings {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            float4 _ShadowColor;

            Varyings vert(Attributes input) {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target {
                // URP 그림자 좌표 가져오기
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                
                // 그림자 감쇠율 (1이면 빛, 0이면 그림자)
                half shadowAttenuation = mainLight.shadowAttenuation;
                
                // 그림자 부분만 _ShadowColor의 알파값 적용
                return half4(_ShadowColor.rgb, (1.0 - shadowAttenuation) * _ShadowColor.a);
            }
            ENDHLSL
        }
    }
}