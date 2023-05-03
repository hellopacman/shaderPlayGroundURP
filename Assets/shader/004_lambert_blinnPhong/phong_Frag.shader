Shader "mdtut/004/phong_frag"
{
    Properties
    {
        _MainCol("Diffuse", Color) = (1,1,1,1)
        _MainTex("DiffuseMap", 2D) = "white"{}
        _LightDirWS("(World)LightDir", Vector) = (0,1,0,0)
        _LightCol("LightCol", Color) = (1,1,1,1)
        _SpecularCol("SpecularCol", Color) = (1,1,1,1)
        //_SpecMap("SpecMap", 2D) = "white"{}
        _Gloss("Gloss", float) = 10
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
                float3 posWS : TEXCOORD3;
            };

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            half4 _MainCol;
            float3 _LightDirWS;
            half4 _LightCol;
            half4 _SpecularCol;
            float _Gloss;
            //TEXTURE2D(_SpecMap); SAMPLER(sampler_SpecMap);
            

            Varyings vert(Attributes v)
            {
                Varyings o = (Varyings)0;
                o.posWS = TransformObjectToWorld(v.positionOS);
                o.positionCS = TransformWorldToHClip(o.posWS);
                // object·¨Ïß->world
                o.fragNormalWS = TransformObjectToWorldDir(v.vertNormalOS);
                o.fragUV = v.vertUV;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half3 lightDirWS = normalize(_LightDirWS);
                half3 normalWS = normalize(i.fragNormalWS);
                // lambert
                float nDotL = saturate(dot(lightDirWS, normalWS));
                half4 col = _LightCol * nDotL;
                half4 mainTexCol = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.fragUV);
                col *= mainTexCol * _MainCol;

                // phong specular
                float3 viewDirWS = normalize(_WorldSpaceCameraPos - i.posWS);
                half3 refl = reflect(-lightDirWS, normalWS);
                float vDotR = saturate(dot(viewDirWS, refl));
                //return vDotR;
                float specular = pow(vDotR, _Gloss);
                //return specular;

                col += _LightCol * specular * _SpecularCol;
                return col;

            }
            ENDHLSL
        }
    }
}
