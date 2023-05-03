Shader "mdtut/004/lambert_frag"
{
    Properties
    {
        _MainCol("DiffuseColor", Color) = (1,1,1,1)
        _MainTex("DiffuseTex", 2D) = "white"{}
        _LightDirWS("LightDir", Vector) = (0,1,0,0)
        _LightCol("LightColor", Color) = (1,1,1,1)
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
                float2 vertUV : TEXCOORD0;
                float3 vertNormalOS : NORMAL;
            };

            struct Varyings     // v2f
            {
                float2 fragUV : TEXCOORD0;
                float4 positionCS : SV_POSITION;
                float3 fragNormalWS : NORMAL;
            };

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            half4 _MainCol;
            float3 _LightDirWS;
            half4 _LightCol;

            Varyings vert(Attributes v)
            {
                Varyings o = (Varyings)0;
                o.positionCS = TransformObjectToHClip(v.positionOS);
                // object·¨Ïß->world
                o.fragNormalWS = TransformObjectToWorldDir(v.vertNormalOS);
                o.fragUV = v.vertUV;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                // lambert
                float nDotL = saturate(dot(normalize(_LightDirWS), normalize(i.fragNormalWS)));
                //return nDotL;

                half4 col = _LightCol * nDotL;
                //return col;

                half4 mainTexCol = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.fragUV);
                col *= mainTexCol * _MainCol;
                return col;

            }
            ENDHLSL
        }
    }
}
