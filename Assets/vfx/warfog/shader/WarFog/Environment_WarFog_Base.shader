Shader "JJ/Environment/WarFog/Base"
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

        [Toggle(ENABLE_DEPTH_TRANSPARENCY)] _enableDepthTransparency("使用(基于深度的)半透效果", float) = 0
        // todo 测试结束后，关闭_FogMaskTex property
        [NoScaleOffset]_FogMaskTex("迷雾遮罩（自动创建不用管）", 2D) = "white" {}

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
            Blend SrcAlpha OneMinusSrcAlpha

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
            #pragma shader_feature ENABLE_DEPTH_TRANSPARENCY
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Speed;
                float4 _CloudTiling;
                float _DepthTweak;
                float _FadeParameter;
                half4 _SpriteColor;
                half _AlphaClip;
                float _ViewZOffset;
            CBUFFER_END

            // mask.a：迷雾开合
            half getClipMask(half4 mask)
            {
                return mask.a;
            }

            #include "./warfog.hlsl"
            
            half4 frag(v2f i) : SV_Target
            {
                half4 mask = half4(0,0,0,0);
                return MainFrag(i, mask);
            }

            ENDHLSL
        }
    }
}
