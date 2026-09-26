Shader "Custom/DashedLineURP"
{
    Properties
    {
        [MainTexture] _BaseMap("Texture", 2D) = "white" {}
        [MainColor] _BaseColor("Color Tint", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent" 
            "RenderPipeline" = "UniversalPipeline" 
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
                float4 color        : COLOR;        // 關鍵：接收 LineRenderer 烘焙的頂點漸層色
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float4 color        : COLOR;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST; // 吃 Tiling (xy) 與 C# 傳進來的 Offset (zw)
                float4 _BaseColor;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                
                // URP 頂點座標轉換
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);

                // 計算 UV 平移 (Tiling & Offset)
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);

                // 傳遞 LineRenderer 的頂點漸層色與 Alpha
                output.color = input.color;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 採樣虛線貼圖
                half4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);

                // 貼圖透明度 x 材質底色 x LineRenderer 漸層色
                return texColor * _BaseColor * input.color;
            }
            ENDHLSL
        }
    }
}