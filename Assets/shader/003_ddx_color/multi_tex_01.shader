Shader "mdtut/003/sample_tex_02"
{
    Properties
    {
        _MainCol("MainColor", Color) = (1,1,1,1)
        [NoScaleOffset]_Tex01 ("Texture01", 2D) = "white" {}
        [NoScaleOffset]_Tex02("Texture02", 2D) = "white" {}
        [NoScaleOffset]_Tex03("Texture03", 2D) = "white" {}
        [NoScaleOffset]_Tex04("Texture04", 2D) = "white" {}
        [NoScaleOffset]_BlendMask("BlendMask", 2D) = "black" {}
    }

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
                float2 uv : TEXCOORD0;
            };

            struct Varyings     // v2f
            {
                float2 uv : TEXCOORD0;
                float4 positionCS : SV_POSITION;
            };

            TEXTURE2D(_Tex01); SAMPLER(sampler_Tex01);
            TEXTURE2D(_Tex02); SAMPLER(sampler_Tex02);
            TEXTURE2D(_Tex03); SAMPLER(sampler_Tex03);
            TEXTURE2D(_Tex04); SAMPLER(sampler_Tex04);
            TEXTURE2D(_BlendMask); SAMPLER(sampler_BlendMask);
            half4 _MainCol;

            Varyings vert (Attributes v)
            {
                Varyings o = (Varyings)0;
                o.positionCS = TransformObjectToHClip(v.positionOS);
                o.uv = v.uv;
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                half4 tex01 = SAMPLE_TEXTURE2D(_Tex01, sampler_Tex01, i.uv);
                half4 tex02 = SAMPLE_TEXTURE2D(_Tex02, sampler_Tex02, i.uv);
                half4 tex03 = SAMPLE_TEXTURE2D(_Tex03, sampler_Tex03, i.uv);
                half4 tex04 = SAMPLE_TEXTURE2D(_Tex04, sampler_Tex04, i.uv);
                half4 mask = SAMPLE_TEXTURE2D(_BlendMask, sampler_BlendMask, i.uv);

                half4 col = tex01 * mask.r + tex02 * mask.g + tex03 * mask.b + tex04 * mask.a;
                return col * _MainCol;
            }
            ENDHLSL
        }
    }
}
