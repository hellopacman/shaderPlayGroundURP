Shader "mdtut/003/sample_tex_vertex_anim"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        height ("height", float) = 0
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
                float2 uv : TEXCOORD0;
            };

            struct Varyings     // v2f
            {
                half4 vertCol : COLOR;
                float4 positionCS : SV_POSITION;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;
            float height;

            Varyings vert (Attributes v)
            {
                Varyings o = (Varyings)0;
                float2 uv = v.uv * _MainTex_ST.xy + _MainTex_ST.zw;
                o.vertCol = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, uv, 0);

                float3 worldPos = TransformObjectToWorld(v.positionOS);
                worldPos.y = worldPos.y + o.vertCol.r * height;
                o.positionCS = TransformWorldToHClip(worldPos);
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                half4 col = i.vertCol;
                return col;
            }
            ENDHLSL
        }
    }
}
