Shader "mdtut/006/ZTest01"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _MainCol ("MainCol", Color) = (1,1,1,1)
        //[Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("ZTest", float) = 0
        // https://docs.unity3d.com/ScriptReference/Rendering.CompareFunction.html
        //[Toggle] _ZWrite("ZWrite", float) = 1
        // https://docs.unity3d.com/ScriptReference/MaterialPropertyDrawer.html
    }

    SubShader
    {
        Pass
        {
            // ZTest [_ZTest]
            // ZWrite[_ZWrite]
            ZTest LEqual
            ZWrite ON

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes  // AppData
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings     // v2f
            {
                float2 uv : TEXCOORD0;
                float4 positionCS : SV_POSITION;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;
            half4 _MainCol;

            Varyings vert (Attributes v)
            {
                Varyings o = (Varyings)0;
                o.positionCS = TransformObjectToHClip(v.positionOS);
                o.uv = v.uv * _MainTex_ST.xy + _MainTex_ST.zw;
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                return col * _MainCol;
            }
            ENDHLSL
        }
    }
}
