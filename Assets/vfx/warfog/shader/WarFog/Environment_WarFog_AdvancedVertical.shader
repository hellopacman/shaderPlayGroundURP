Shader "JJ/Environment/WarFog/AdvancedVertical"
{
	Properties
	{	
		// 主纹理
		[Header(MAIN)]
		[Header(Texture and Color)]
		[Space]
		_MainTex ("Main Texture", 2D) = "white" {}
		_Color("Color", Color) = (1, 1, 1, 0.5)
		[PowerSlider(1.0)]_IntersectionThresholdMax("Fog Intensity", Range(0, 2)) = 0.5
		[Space]
		_Alpha("Alpha", Range(-1.0, 1)) = 1
		[Toggle]_Invert("Invert Color", Float) = 0
		[Space]

		// Cookie 纹理
		[Header(Cookie)]
		[Toggle]_UseCookie("Enable Cookie", Float) = 0
		// 确认，_Cookie貌似没有应用自己的Tile/Offset，那就改成[NoScaleOffset]
		[NoScaleOffset]_Cookie("Cookie", 2D) = "white" {}
		_CookieStrength("Cookie Alpha", Range(0, 1)) = 1
		[Space]
		
		// 主纹理旋转
		[Header(Movement)]
		[Toggle]_Rotation("Rotate", Float) = 0
		_RotationSpeed("Rotation Speed", Range(-1, 1)) = 0
		_OriginX("Origin X", Range(-2, 2)) = 0.0									
		_OriginY("Origin Y", Range(-2, 2)) = 0.0
		[Space(30)]
		
		
		[Header(DISTORTION)]
		[Header(Distortion Texture)]
		_DistortTex("Texture", 2D) = "white" {}		
		[Space]
		
		[Toggle]_UseMainDistort("Override Texture - Use Main Texture", Float) = 0
		_MainDistortAmount("Main Texture Distortion Amount", Range(0, 1)) = 1
		[Space]
		
		[Toggle]_DistortCookie("Override Texture - Use Cookie Texture", Float) = 0
		_DistortCookieAmount("Cookie Distortion Amount", Range(0, 1)) = 1
		
		[Space]
		[Toggle]_MainDistort("Distort Main Texture", Float) = 0
		[Header(Distortion Values)]
		
		// 8字舞控制项
		_Magnitude("Magnitude", Range(0, 10)) = 0
		[PowerSlider(2.0)]_DistortSpeed("Speed", Range(0, 2)) = 0									
		_Period("Period", Range(-3, 3)) = 1
		_Offset("Period Offset", Range(0, 15)) = 0		
		[Space]
		
												
		[Header(Distortion Movement)]
		// 旋转uv2
		[Toggle]_DistortionRotation("Rotate", Float) = 0
		_DistortRotationSpeed("Rotation Speed", Range(-2, 2)) = 0
		
		[Space]
		// 是否产生平移纹理动画
		[Toggle]_Translate("Move", Float) = 0								
		_SpeedX("X Speed", Range(-0.5, 0.5)) = 0
		_SpeedY("Y Speed", Range(-0.5, 0.5)) = 0
		
		[Header(Debug)]
		[Toggle]_TestDistortion("Show Distortion Texture", Float) = 0

		
		[Space]		
		[NoScaleOffset]_FogMaskTex("迷雾遮罩（自动创建不用管）", 2D) = "white"{}
		
		
	}
	SubShader
	{
		Tags { "Queue"="Transparent" "RenderType"="Transparent" "PreviewType"="Plane"}
		LOD 100

		Pass
		{
			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite Off
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
 			
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;				
				float2 uv2 : TEXCOORD1;													
			};										

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float2 uv2 : TEXCOORD1;
				float2 uv3 : TEXCOORD3;	// 先临时用uv3做遮罩，以后可以放到uv.zw中
				float4 scrPos : TEXCOORD2;			
				float4 vertex : SV_POSITION;
			};

			
			float2 sinusoid(float2 x, float2 m, float2 M, float2 p)		
			{
				float2 e = M - m;
				float2 c = 3.1415 * 2.0 / p;
				return e / 2.0 * (1.0 + sin(x * c)) + m;
				// 1.0+sin(x*c) 值域 [0, 2] 
				// e / 2.0 * (1.0 + sin(x * c)) 值域 e*[0,1]
				// e / 2.0 * (1.0 + sin(x * c)) + m 值域 e*[0,1] + m， 代入e = M - m， 得
				// (M - m)*[0,1] + m， 即用 (1.0 + sin(x * c)) / 2.0 对[m, M]做插值
				// 进一步整理，即用(1.0 + sin(x/p * 2PI)) / 2.0 对[m, M]做插值
			}

			CBUFFER_START(UnityPerMaterial)
			float4 _Color;
			float _IntersectionThresholdMax;
			float _Magnitude;
			float _Period;
			float _Offset;
			float _DistortRotationSpeed;
			float _Translate;
			float _TestDistortion;
			float _MainDistort;
			float _DistortSpeed;
			float _OriginX;
			float _OriginY;
			float _Invert;
			float _Alpha;
			float _CookieStrength;
			float _UseCookie;
			float _Rotation;
			float _DistortionRotation;
			float _RotationSpeed;
			float _MainDistortAmount;
			float _DistortCookie;
			float _DistortCookieAmount;
			float _SpeedX;
			float _SpeedY;
			float _UseMainDistort;
			float4 _MainTex_ST;
			float4 _DistortTex_ST;
			CBUFFER_END

			TEXTURE2D_X_FLOAT(_CameraDepthTexture);
            SAMPLER(sampler_CameraDepthTexture);
			
			TEXTURE2D(_FogMaskTex); 
            SAMPLER(sampler_FogMaskTex);
			TEXTURE2D(_Cookie); 
            SAMPLER(sampler_Cookie);
			TEXTURE2D(_MainTex); 
            SAMPLER(sampler_MainTex);
			TEXTURE2D(_DistortTex); 
            SAMPLER(sampler_DistortTex);
			
		
			v2f vert (appdata v)
			{
				
				v2f o;
				
				o.vertex = TransformObjectToHClip(v.vertex.xyz);
				// 暂时没想明白自定义uv2有什么特别的好处，为了测试，uv2暂取uv的值
				o.uv2 = v.uv;
				// uv, uv2都可能会旋转/平移，迷雾遮罩需要自己单独的uv3
				o.uv3 = v.uv;
				
				// _Translate:  纹理offset平移动画
				if(_Translate == 1.0)						
				{												
					float2 scrollCoords = float2(_SpeedX, _SpeedY) * _Time[1];

					// 决定对哪个纹理做_Translate。_MainTex？还是_DistortTex_ST？
					if(_UseMainDistort == 1.0)
					{
						_MainTex_ST.zw = scrollCoords;
					}
					else
					{
						// _DistortTex平移动画，改变_DistortTex的offset
						_DistortTex_ST.zw = scrollCoords;				
					}
					
				}

				// 主纹理uv旋转
				if(_Rotation == 1.0)					
				{
					float SinX = sin(_RotationSpeed * _Time[1]);		
					float CosX = cos(_RotationSpeed * _Time[1]);

					float2x2 rotationMatrix = float2x2( CosX, -SinX, SinX, CosX);	

					v.uv.x += (_OriginX + 0.5) * -1.0;			
					v.uv.y += (_OriginY + 0.5) * -1.0;			

					v.uv.xy = mul ( v.uv.xy, rotationMatrix );	
				}
				
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);

				// 是否对对uv2进行旋转处理
				if(_DistortionRotation == 1.0)				
				{
					// uv坐标系旋转矩阵
					float sinX = sin(_DistortRotationSpeed * _Time[1]);		
					float cosX = cos(_DistortRotationSpeed * _Time[1]);
					float2x2 rotationMatrix = float2x2( cosX, -sinX, sinX, cosX);	

					// 平移uv坐标系原点
					// 效果：_OriginX，_OriginY为0时，uv坐标系原点在(0.5, 0.5)即内置Plane的中心点
					v.uv2.x += (_OriginX + 0.5) * -1.0;
					v.uv2.y += (_OriginY + 0.5) * -1.0;

					// 行向量 * 行矩阵
					// uv坐标系顺时针旋转，画面逆时针旋转
					v.uv2.xy = mul ( v.uv2.xy, rotationMatrix );	
				}

				// uv2 做 TRANSFORM_TEX时取哪个纹理的Tile/Offset。
				// _MainTex?_DistortTex?
				if(_UseMainDistort == 1.0)
				{
					o.uv2 = TRANSFORM_TEX(v.uv2, _MainTex);		
				}
				else
				{
					o.uv2 = TRANSFORM_TEX(v.uv2, _DistortTex);
				}

				// 屏幕坐标-vs阶段  
				o.scrPos = ComputeScreenPos(o.vertex);
				
				return o;
			}
			
			half4 frag (v2f i) : SV_Target
			{
				// 如果要对uv进行旋转
				if(_Rotation == 1.0)
				{
					// 把uv坐标原点复位
					i.uv.x += 0.5;		
					i.uv.y += 0.5;		
				}

				// 如果要对uv2进行旋转
				if (_DistortionRotation == 1.0)
				{
					i.uv2.x += 0.5;
					i.uv2.y += 0.5;
				}

				half4 distort;

				// 同理，distort值从哪个纹理采样获取
				if (_UseMainDistort == 1.0)
				{
					distort = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex, i.uv2);
				}
				else
				{
					distort = SAMPLE_TEXTURE2D(_DistortTex,sampler_DistortTex, i.uv2);
				}

				// distort是否额外应用_Cookie纹理
				if(_DistortCookie == 1.0)
				{
					half4 DistortCookie = SAMPLE_TEXTURE2D(_Cookie,sampler_Cookie, i.uv2);
					distort *= saturate(DistortCookie + (1.0 - _DistortCookieAmount));
				}
				
				half4 MainTex;			
											
				float time1 = sin(_Time[1] * 5.0 * _DistortSpeed) + _Offset;	// [-1, 1]+_Offset
				float time2 = cos(_Time[1] * 5.0 * _DistortSpeed) + _Offset;	
				float MainX;
				float MainY;

				// 是否额外用_MainTex进一步扰动 distort
				if(_MainDistort == 1.0)		
				{
					MainTex = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex, i.uv2);
					MainX = MainTex.x * _MainDistortAmount;
					MainY = MainTex.y * _MainDistortAmount;
				}
				else
				{
					MainX = 0.0;
					MainY = 0.0;
				}

				// 用(1.0 + sin(x/p * 2PI)) / 2.0 正弦函数对[m, M]做插值
				// p越小，扰动图案越密
				float2 Displacement = sinusoid
				(
					float2(time1 * (distort.x + MainX), time2 * (distort.y + MainY)),	
					float2(-_Magnitude * 0.001, -_Magnitude * 0.001),					
					float2(_Magnitude * 0.001, _Magnitude * 0.001),						
					float2(_Period, _Period)
				);
				
				
				i.uv.xy += Displacement;					
				MainTex = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex, i.uv);				

				
				if(_Invert == 1.0)								
					{
						MainTex.r = 1.0 - MainTex.r;
						MainTex.g = 1.0 - MainTex.g;
						MainTex.b = 1.0 - MainTex.b;
					}

				// Cookie纹理
				if(_UseCookie == 1.0)
				{
					// 用主uv采样
					half4 Cookie = SAMPLE_TEXTURE2D(_Cookie,sampler_Cookie, i.uv);
					MainTex.rgb *= saturate(Cookie + (1.0 - _CookieStrength));
				}
				
				
				float depth = LinearEyeDepth(SAMPLE_TEXTURE2D_X(_CameraDepthTexture,sampler_CameraDepthTexture, i.scrPos.xy/i.scrPos.w).x,_ZBufferParams);	
				float diff = saturate(_IntersectionThresholdMax * (depth - i.scrPos.w));					
																											

				half4 col = lerp(half4(_Color.rgb, 0.0), _Color, diff * diff * diff * diff * (diff * (6 * diff - 15) + 10));	

				if(_TestDistortion == 1)				
				{
					col.rgb *= distort.rgb;
				}
				else
				{										
										
					
					float lum = dot(MainTex.rgb,half3(0.22, 0.707, 0.071));
					
					col.rgb *= MainTex.rgb;					

					col.a *= saturate(lum + _Alpha);

				}


                // #if defined(FOG_MASK)
                half mask = SAMPLE_TEXTURE2D(_FogMaskTex,sampler_FogMaskTex, i.uv3).a;
                col.a *= mask;
                // #endif
				
				return col;
			}
			
			ENDHLSL
		}
	}
}
