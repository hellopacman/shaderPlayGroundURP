Shader "mdtut/004/lambert_vert"
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
                //float3 fragNormalWS : NORMAL;
                half4 interpolatedVertCol : COLOR;
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
                half3 normalWS = TransformObjectToWorldDir(v.vertNormalOS);
                float nDotL = saturate(dot(normalize(_LightDirWS), normalWS));
                o.interpolatedVertCol = _LightCol * nDotL * _MainCol;
                o.fragUV = v.vertUV;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                return i.interpolatedVertCol;
                half4 mainTexCol = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.fragUV);
                half4 col = mainTexCol * i.interpolatedVertCol;
                return col;

            }
            ENDHLSL
        }
    }
}
