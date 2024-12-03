Shader "JJ/Environment/WarFog/Clip_HighLigh_ColorTint"
{
    Properties
    {
        [NoScaleOffset] _MainTex("Texture", 2D) = "white" {}
        // 迷雾滚动速度 xy:迷雾1层, zw:迷雾2层
        _Speed("Speed", Vector) = (0.0, 0.0, 0.0, 0.0)
        // 迷雾平铺 xy:迷雾1层, zw:迷雾2层
        _CloudTiling("CloudTiling", Vector) = (1.0, 1.0, 1.0, 1.0)
        // 深度调整参数
        _DepthTweak("DepthTweak", float) = 1
        // 深度淡出参数
        _FadeParameter("FadeParameter", float) = 1
        // 迷雾色
        _SpriteColor("SpriteColor", Color) = (1.0, 1.0, 1.0, 1.0)

        _AlphaClip("AlphaClip", Range(0, 1)) = 0.1

        // todo 测试结束后，关闭_FogMaskTex property
        [NoScaleOffset]_FogMaskTex("迷雾遮罩（自动创建不用管）", 2D) = "white" {}
        [NoScaleOffset]_ClipMask("裁切遮罩（自动创建不用管）", 2D) = "white" {}

        
        // 高亮颜色
        _HighlightCol("高亮选择色", Color) = (1.0, 1.0, 1.0, 1.0)
        _HighlightFreq("高亮闪动频率", float) = 1

        // 变色(用于显示未解锁区域等等)
        _SecondCol("未解锁区域颜色", Color) = (1.0, 1.0, 1.0, 1.0)

        [Toggle(ENABLE_DEPTH_TRANSPARENCY)] _enableDepthTransparency("使用(基于深度的)半透效果", float) = 0

        // 沿相机Z轴微调距离
        _ViewZOffset("微调视距(正：推远，负：拉近))", float) = 0

    }   
        SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent+100" "PreviewType" = "Plane"}
        // LOD 100

        Pass
        {
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha , One One

            Stencil
            {
                Ref 8   // 1000
                Comp Always
                Pass Replace
                writeMask 15 // 1111
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile __ ENABLE_DEPTH_TRANSPARENCY
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Speed;
                float4 _CloudTiling;
                float _DepthTweak;
                float _FadeParameter;
                half4 _SpriteColor;
                half _AlphaClip;
                half4 _HighlightCol;
                half _HighlightFreq;
                half4 _SecondCol;
                float _ViewZOffset;
            CBUFFER_END

            struct appdata
            {
                float4 vertex : POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 fog_uv : TEXCOORD1; // xy:迷雾1层, zw:迷雾2层
#if defined(ENABLE_DEPTH_TRANSPARENCY)
                float4 screenpos : TEXCOORD2;
#endif
                half4 color : COLOR;
                float4 vertex : SV_POSITION;
            };


            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D_X_FLOAT(_CameraDepthTexture);
            SAMPLER(sampler_CameraDepthTexture);
            TEXTURE2D(_FogMaskTex);
            SAMPLER(sampler_FogMaskTex);
            TEXTURE2D(_ClipMask);
            SAMPLER(sampler_ClipMask);


            v2f vert(appdata v)
            {
                v2f o;
                float4 worldPos = mul(unity_ObjectToWorld, v.vertex);

                o.uv.xy = v.uv.xy;

                // 世界xz坐标用作迷雾(两层)采样uv
                float4 speed = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Speed);
                o.fog_uv = speed * _Time.x + worldPos.xzxz;    // 滚动
                // 平铺缩放
                // 1世界单位对应着1uv
                float4 cloudTiling = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _CloudTiling);
                o.fog_uv *= cloudTiling;

                // 沿view z轴偏移
                float3 viewPos = TransformWorldToView(worldPos.xyz);
                float viewZOffset = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _ViewZOffset);
                viewPos.z -= viewZOffset;

                o.vertex = TransformWViewToHClip(viewPos);
#if defined(ENABLE_DEPTH_TRANSPARENCY)
                // 屏幕坐标 vs阶段  
                o.screenpos = ComputeScreenPos(o.vertex);
#endif

                // 顶点色
                o.color = v.color;

                return o;
            }



            

            half4 frag(v2f i) : SV_Target
            {
                half4 mask = half4(0,0,0,0);

                // #if defined(FOG_MASK)
                half clipMask = SAMPLE_TEXTURE2D(_ClipMask, sampler_ClipMask, i.uv).r;
                mask.r = clipMask;

                half4 fogMask = SAMPLE_TEXTURE2D(_FogMaskTex, sampler_FogMaskTex, i.uv);
                mask.gba = fogMask.gba;
                half maskAlpha = mask.r;
                half alphaClip = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _AlphaClip);
                clip(maskAlpha - alphaClip);
                // #endif

#if defined(ENABLE_DEPTH_TRANSPARENCY)
    // 采样相机深度图
                float depth = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, i.screenpos.xy / i.screenpos.w).x;
                depth = LinearEyeDepth(depth, _ZBufferParams);    // 转换为线性值

                // 沿相机z轴方向，云层到地表的距离
                float depthTweak = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _DepthTweak);
                depth = depth * depthTweak - i.screenpos.w;

                float fadeParameter = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _FadeParameter);
                depth *= fadeParameter;
                depth = saturate(depth);
                // return half4(depth, depth, depth, 1);

                half4 col = 0;
                // col.a = depth * i.color.a;    // 混合顶点alpha，做半透过渡
                col.a = depth;
#else
                half4 col = 1;
#endif    

                // 采样两次迷雾纹理
                half3 fog1 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.fog_uv.xy).xyz;
                half3 fog2 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.fog_uv.zw).xyz;
                col.rgb = fog1 + fog2;  // 累加

                /* 2022-6-17 wusy 本意是尝试让云雾贴图进一步影响迷雾边缘透明度，让边缘看上去不那么楞，实测发现效果不好，有一条隐约可见的白边
                if (col.a < 1)
                {
                    col.a *= col.r * 0.5;
                }
                */

                half4 spriteColor = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _SpriteColor);
                col.rgb *= spriteColor.rgb;    // 混色
                col.rgb *= 0.5;    // 降低亮度到合理范围

                col.a *= maskAlpha * i.color.a;







                // 高亮
                half4 highlightCol = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _HighlightCol);
                half highlightFreq = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _HighlightFreq);
                col.rgb += col.rgb * mask.g * 0.5 * highlightCol * (abs(frac(_Time.y * highlightFreq) - 0.5) * 0.4 + 0.25);

                // 变色
                half4 secondCol = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _SecondCol);
                secondCol = lerp((1,1,1,1), secondCol, mask.b);
                col.rgb *= secondCol.rgb;

                // 额外透明度
                col.a *= mask.a;

                return col;
            }

            ENDHLSL
        }
    }
}
