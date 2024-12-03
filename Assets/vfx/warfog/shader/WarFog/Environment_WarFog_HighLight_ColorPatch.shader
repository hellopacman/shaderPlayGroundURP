Shader "JJ/Environment/WarFog/HighLight_ColorPatch"
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

            // 2023-2-27 wusy _fogMask格式从原Alpha8升为RGB24。R 迷雾开合, G 迷雾选择, B 预留
            half getClipMask(half4 mask)
            {
                return mask.r;
            }

            #include "./warfog.hlsl"
            
            half4 frag(v2f i) : SV_Target
            {
                half4 mask = half4(0,0,0,0);
                half4 col = MainFrag(i, mask);

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
