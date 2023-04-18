Shader "mdtut/vert_color"
{

    SubShader
    {
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes  // AppData
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
            };

            struct Varyings     // v2f
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
            };

            Varyings vert (Attributes v)
            {
                Varyings o = (Varyings)0;
                o.positionCS = TransformObjectToHClip(v.positionOS);
                o.color = v.color;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 col = i.color;
                return col;
            }
            ENDHLSL
        }
    }
}
