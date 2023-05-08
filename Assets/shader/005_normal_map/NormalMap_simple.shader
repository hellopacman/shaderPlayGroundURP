Shader "StepByStep/Lit/NormalMap_simple"
{
    Properties
    {
        _Color("Main Color", Color) = (1,1,1,1)
        _MainTex("Texture", 2D) = "white" {}
        _MyWorldSpaceLightDir("模拟平行光位置", vector) = (0, 1, 0, 0)
        _MyLightColor("平行光颜色", Color) = (1,1,1,1)

        _Specular("高光色", Color) = (1,1,1,1)
        _Gloss("光泽度", float) = 20

        _NormalTex("法线贴图", 2D) = "bump" {}
        //_BumpScale("法线强度", float) = 1
    }
    
    SubShader
    {
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;   // 顶点的对象(模型)空间坐标  OS-> ObjectSpace
                float2 uv : TEXCOORD0;      // 要求unity提供 1u
                float3 normalOS : NORMAL;   // 要求unity提供对象(模型)空间法线 
                float4 tangentOS : TANGENT;     // 要求unity提供对象(模型)空间切线
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;    // 片元裁切空间坐标
                float4 transformedUV : TEXCOORD0;   // xy _MainTex uv; zw _NormalTex uv
                float3 normalWS : TEXCOORD1;    // 片元世界空间法线
                float4 tangentWS : TEXCOORD2;   // 片元世界空间切线
                float3 positionWS : TEXCOORD3;  // 片元世界坐标
            };

            half4 _Color;
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;
            half3 _MyWorldSpaceLightDir;
            half4 _MyLightColor;
            half3 _Specular;
            float _Gloss;
            TEXTURE2D(_NormalTex);
            SAMPLER(sampler_NormalTex);
            float4 _NormalTex_ST;
            //half _BumpScale;


            Varyings vert(Attributes v)
            {
                Varyings o = (Varyings)0;
                o.positionCS = TransformObjectToHClip(v.positionOS);

                // 对模型uv1应用_MainTex的Tiling/Offset计算
                o.transformedUV.xy = v.uv * _MainTex_ST.xy + _MainTex_ST.zw;
                // 对模型uv1应用_MainTex的Tiling/Offset计算
                o.transformedUV.zw = v.uv * _NormalTex_ST.xy + _NormalTex_ST.zw;

                o.normalWS = TransformObjectToWorldNormal(v.normalOS); // 把顶点法线从模型空间转化到世界空间
                o.tangentWS.xyz = TransformObjectToWorldDir(v.tangentOS);  // 把顶点切线从模型空间转化到世界空间
                o.tangentWS.w = v.tangentOS.w; // 特别记录一下顶点切线.w的值，它将决定后边求副法线结果的方向
                o.positionWS = TransformObjectToWorld(v.positionOS);   // 把顶点坐标从模型空间转换到世界空间
                return o;
            }

            // fragment shader函数必须加 : SV_Target 结尾
            half4 frag(Varyings i) : SV_Target 
            {
                // --- 法线贴图部分
                half4 normalTex = SAMPLE_TEXTURE2D(_NormalTex, sampler_NormalTex, i.transformedUV.zw);  // 采样法线纹理
                half3 n = UnpackNormal(normalTex);  // 把法线纹理颜色值（根据纹理格式等）转换成有效的法线矢量

                float3 wsBinormal = cross(i.normalWS, i.tangentWS); // 法线点乘切线得到两者的垂线
                wsBinormal *= i.tangentWS.w;  // 垂线需要乘以v.tangent.w才能得到正确的方向

                // 把切线空间下的法线值转换到世界空间
                half3 normalWS = normalize(
                    n.x * i.tangentWS +
                    n.y * wsBinormal +
                    n.z * i.normalWS
                );

                // --- 漫反射部分
                half3 wsLightDir = normalize(_MyWorldSpaceLightDir);
                float lightIntensity = saturate(dot(wsLightDir, normalWS));   // 计算漫反射光照强度
                half4 diffuseLightCol = _MyLightColor * lightIntensity;

                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.transformedUV.xy); // 采样主纹理
                col *= _Color * diffuseLightCol;

                // --- phongBlinn高光
                // 计算片元到相机的方向矢量
                float3 viewDir = normalize(_WorldSpaceCameraPos.xyz - i.positionWS.xyz);
                // 计算half向量
                half3 harfDir = normalize(wsLightDir + viewDir);
                // 计算half向量与视线的重合度(点乘)
                float nDotH = saturate(dot(normalWS, harfDir));
                // 计算phong高光
                float phong = pow(nDotH, _Gloss);  // 用幂函数调整高光强度
                half3 specCol = _MyLightColor * _Specular * phong;  // 结合灯光，材质高光色，计算最终高光颜色

                // 高光叠加到总颜色上
                col.rgb += specCol;

                return col;
            }
            ENDHLSL
        }
    }
}
