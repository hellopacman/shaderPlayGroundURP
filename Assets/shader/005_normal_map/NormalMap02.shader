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
                        float4 tangentWS : TEXCOORD2;
                        float4 binormalWS : TEXCOORD3;
                        float4 normalWS : TEXCOORD4;
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

                        o.tangentWS.xyz = TransformObjectToWorldDir(v.vertTangentOS.xyz);
                        o.normalWS.xyz = TransformObjectToWorldDir(v.vertNormalOS);
                        o.binormalWS.xyz = cross(o.normalWS.xyz, o.tangentWS.xyz) * v.vertTangentOS.w;

                        o.tangentWS.w = posWS.x;
                        o.binormalWS.w = posWS.y;
                        o.normalWS.w = posWS.z;

                        o.fragUV.xy = v.vertUV * _MainTex_ST.xy + _MainTex_ST.zw;
                        o.fragUV.zw = v.vertUV * _NormalTex_ST.xy + _NormalTex_ST.zw;
                        return o;
                    }

                    half4 frag(Varyings i) : SV_Target
                    {
                        // normal map
                        half3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalTex, sampler_NormalTex, i.fragUV.zw), _NormalScale);
                        half3 normalWS = normalTS.x * i.tangentWS.xyz
                            + normalTS.y * i.binormalWS.xyz
                            + normalTS.z * i.normalWS.xyz;
                        normalWS = normalize(normalWS);

                        float3 posWS = float3(i.tangentWS.w, i.binormalWS.w, i.normalWS.w);
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
