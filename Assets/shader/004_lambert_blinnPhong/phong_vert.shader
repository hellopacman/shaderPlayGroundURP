Shader "mdtut/004/phong_vert"
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
                //float3 fragNormalWS : NORMAL;
                //float3 posWS : TEXCOORD3;
                half4 diffuse : TEXCOORD4;
                half4 specular : TEXCOORD5;
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
                float3 posWS = TransformObjectToWorld(v.positionOS);
                o.positionCS = TransformWorldToHClip(posWS);

                half3 normalWS = TransformObjectToWorldDir(v.vertNormalOS);
                half3 lightDirWS = normalize(_LightDirWS);

                // lambert
                float nDotL = saturate(dot(normalize(_LightDirWS), normalWS));
                o.diffuse = _LightCol * nDotL * _MainCol;
                
                // phong specular
                half3 viewDirWS = normalize(_WorldSpaceCameraPos - posWS);
                half3 refl = reflect(-lightDirWS, normalWS);
                float vDotR = saturate(dot(viewDirWS, refl));
                float specular = pow(vDotR, _Gloss);
                o.specular = _LightCol * specular * _SpecularCol;

                // uv
                o.fragUV = v.vertUV;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half col = i.diffuse;

                // vert lambert
                half4 mainTexCol = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.fragUV);
                col *= mainTexCol;
                //return col;

                // vert phong
                col += i.specular;
                return col;
            }
            ENDHLSL
        }
    }
}
