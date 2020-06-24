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
	}
		SubShader
		{
			// No culling or depth
			Cull Off ZWrite Off ZTest Always

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
					float4 vertex : SV_POSITION;
				};

				sampler2D _RippleTex;

				v2f vert(appdata v)
				{
					v2f o;
					o.vertex = UnityObjectToClipPos(v.vertex);
					o.uv = v.uv;

					return o;
				}

				sampler2D _MainTex;
				fixed4 _Color;
				fixed _Gloss;
				fixed _Metallic;
				fixed _RippleScale;

				fixed4 frag(v2f i) : SV_Target
				{
					float2 uv = i.uv - 0.5;
					uv = uv / _RippleScale;
					uv += 0.5;
					fixed4 ripple = tex2D(_RippleTex, uv);
					ripple.yz = ripple.yz * 2 - 1;
					float dropFrac = frac(ripple.a + _Time.y);
					float timeFrac = dropFrac - 1 + ripple.x;
					float dropFactor = 1 - saturate(dropFrac);
					float final = dropFactor * sin(clamp(timeFrac * 9, 0, 4) * PI);
					ripple.yz *= final;
					fixed3 compute = fixed3(ripple.y, ripple.z, 1);
					i.uv *= compute;
					fixed4 col = tex2D(_MainTex, i.uv);
					return col;
				}
				ENDCG
			}
		}
}
