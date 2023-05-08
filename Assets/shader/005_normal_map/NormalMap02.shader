Shader "mdtut/005/normalmap_02"
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
            _NormalTex("NormalTex", 2D) = "bump"{}
            _NormalScale("NormalScale", Range(-5, 5)) = 1
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
                        float4 vertTangentOS : TANGENT;
                    };

                    struct Varyings     // v2f
                    {
                        float4 fragUV : TEXCOORD0;  // xy:mainTex, zw:normalTex
                        float4 positionCS : SV_POSITION;
                        float4 TToW0 : TEXCOORD2;
                        float4 TToW1 : TEXCOORD3;
                        float4 TToW2 : TEXCOORD4;
                    };

                    TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex); float4 _MainTex_ST;
                    half4 _MainCol;
                    float3 _LightDirWS;
                    half4 _LightCol;
                    half4 _SpecularCol;
                    float _Gloss;
                    //TEXTURE2D(_SpecMap); SAMPLER(sampler_SpecMap);
                    TEXTURE2D(_NormalTex); SAMPLER(sampler_NormalTex); float4 _NormalTex_ST;
                    float _NormalScale;

                    Varyings vert(Attributes v)
                    {
                        Varyings o = (Varyings)0;
                        float3 posWS = TransformObjectToWorld(v.positionOS.xyz);
                        o.positionCS = TransformWorldToHClip(posWS);

                        float3 normalWS = TransformObjectToWorldDir(v.vertNormalOS);
                        float3 tangentWS = TransformObjectToWorldDir(v.vertTangentOS.xyz);
                        float3 binormal = cross(normalWS, tangentWS) * v.vertTangentOS.w;

                        o.TToW0 = float4(tangentWS.x, binormal.x, normalWS.x, posWS.x);
                        o.TToW1 = float4(tangentWS.y, binormal.y, normalWS.y, posWS.y);
                        o.TToW2 = float4(tangentWS.z, binormal.z, normalWS.z, posWS.z);

                        o.fragUV.xy = v.vertUV * _MainTex_ST.xy + _MainTex_ST.zw;
                        o.fragUV.zw = v.vertUV * _NormalTex_ST.xy + _NormalTex_ST.zw;
                        return o;
                    }

                    half4 frag(Varyings i) : SV_Target
                    {
                        // normal map
                        half3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalTex, sampler_NormalTex, i.fragUV.zw), _NormalScale);
                        half3 normalWS = half3(dot(normalTS, i.TToW0.xyz), dot(normalTS, i.TToW1.xyz), dot(normalTS, i.TToW2.xyz));
                        normalWS = normalize(normalWS);

                        float3 posWS = float3(i.TToW0.w, i.TToW1.w, i.TToW2.w);
                        half3 lightDirWS = normalize(_LightDirWS);
                        // lambert
                        float nDotL = saturate(dot(lightDirWS, normalWS));
                        half4 col = _LightCol * nDotL;
                        half4 mainTexCol = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.fragUV.xy);
                        col *= mainTexCol * _MainCol;

                        // blinn specular
                        float3 viewDirWS = normalize(_WorldSpaceCameraPos - posWS);
                        half3 halfVec = normalize(viewDirWS + lightDirWS);
                        float nDotH = saturate(dot(normalWS, halfVec));
                        //return nDotH;
                        float specular = pow(nDotH, _Gloss);
                        //return specular;

                        col += _LightCol * specular * _SpecularCol;
                        return col;

                    }
                    ENDHLSL
                }
            }
}
