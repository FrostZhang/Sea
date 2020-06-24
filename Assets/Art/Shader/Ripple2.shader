Shader "Hidden/Ripple"
{
	Properties
	{
		_Color("Color",Color) = (1,1,1,1)
		_MainTex("Texture", 2D) = "white" {}
		_Gloss("Gloss",Range(0,1)) = 0.5
		_Metallic("Metallic",Range(0,1)) = 0
		_RippleTex("RippleTex",2D) = "white" {}
		_RippleScale("RippleScale",Range(0.1,10)) = 1
		_DepthColor("Depth Color",COLOR) = (1,0,0,1)
		_Threshold("Threshold",float) = 0.5
		_Speed("Speed",float) = 1
	}
		SubShader
		{
		Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" "PreviewType" = "Plane" }
		Cull Off Lighting Off ZWrite Off
		Blend  SrcAlpha OneMinusSrcAlpha

			Pass
			{
				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag

				#include "UnityCG.cginc"
				#define PI 3.14159
				struct appdata
				{
					float4 vertex : POSITION;
					float2 uv : TEXCOORD0;
				};

				struct v2f
				{
					float2 uv : TEXCOORD0;
					float4 projpos : TEXCOORD2;
					float4 vertex : SV_POSITION;
				};

				sampler2D _RippleTex;
				sampler2D _MainTex;
				float4 _MainTex_ST;

				v2f vert(appdata v)
				{
					v2f o;
					o.vertex = UnityObjectToClipPos(v.vertex);
					o.uv = TRANSFORM_TEX(v.uv,_MainTex);
					o.projpos = ComputeScreenPos(o.vertex);	//projectedPosition 为了frag获得该物体在深度图的位置
					COMPUTE_EYEDEPTH(o.projpos.z);	//获得此物体的深度
					return o;
				}

				fixed4 _Color;
				fixed _Gloss;
				fixed _Metallic;
				fixed _RippleScale;

				UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
				fixed4 _DepthColor;
				float  _Threshold;
				float _Speed;
				fixed4 frag(v2f i) : SV_Target
				{
					float2 uv = i.uv - 0.5;
					uv = uv / _RippleScale;
					uv += 0.5;
					fixed4 ripple = tex2D(_RippleTex, uv);
					ripple.yz = ripple.yz * 2 - 1;
					float dropFrac = frac(ripple.a + _Time.y*_Speed);
					float timeFrac = dropFrac - 1 + ripple.x;
					float dropFactor = 1 - saturate(dropFrac);
					float final = dropFactor *sin(clamp(timeFrac * 9, 0, 4) * PI);
					ripple.yz *= final;
					fixed3 compute = fixed3(ripple.y, ripple.z, 1);
					i.uv *= compute*3;
					fixed4 col = tex2D(_MainTex, i.uv)*_Color;

					float sz = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.projpos)));
					float fade = saturate(_Threshold*(sz - i.projpos.z));
					float intersect = (1 - fade);
					col = lerp(col, _DepthColor, intersect);

					return col;
				}
				ENDCG
			}
		}
}
