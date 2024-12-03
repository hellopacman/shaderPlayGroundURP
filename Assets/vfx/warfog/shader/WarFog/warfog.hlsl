#ifndef WARFOG_INCLUDED
#define WARFOG_INCLUDED

// todo
// #pragma multi_compile _ FOG_MASK

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


half4 MainFrag(v2f i, out half4 mask)
{
    // #if defined(FOG_MASK)
    mask = SAMPLE_TEXTURE2D(_FogMaskTex,sampler_FogMaskTex, i.uv);
    half maskAlpha = getClipMask(mask);
    half alphaClip = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _AlphaClip);
    clip(maskAlpha - alphaClip);
    // #endif

#if defined(ENABLE_DEPTH_TRANSPARENCY)
    // 采样相机深度图
    float depth = SAMPLE_TEXTURE2D_X(_CameraDepthTexture,sampler_CameraDepthTexture,i.screenpos.xy / i.screenpos.w).x;
    depth = LinearEyeDepth(depth,_ZBufferParams);    // 转换为线性值

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
    half3 fog1 = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex, i.fog_uv.xy).xyz;
    half3 fog2 = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex, i.fog_uv.zw).xyz;
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

    return col;

}


#endif