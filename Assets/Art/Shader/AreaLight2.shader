Shader "Customized /AreaLight2"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_Color("Color", Color) = (1,1,1,1)
	    _Dis("Dis",float)=100
		_Gloss("Gloss",Range(0,1)) = 0.5
		_Metallic("Metallic",Range(0,1)) = 0
		_RippleTex("RippleTex",2D) = "white" {}
		_RippleScale("RippleScale",Range(0.1,10)) = 1
	}
		SubShader
		{
			Tags{
					"RenderType" = "Opaque" /*"Transparent" "TransparentCutout" "Background" "Overlay"*/
					"Queue" = "Transparent-10"
				}
			Cull Off /* Front Back  ZWrite Off ZTest Always  Off LEqual */ ZWrite Off ZTest LEqual
			/* GrabPass {"_Mygrab"} */
			Pass
			{
			//Tags{"LightMode" = "ForwardBase" /* "ForwardAdd" "Vertex" "VertexLit" "ShadowCaster" "ShadowCollector"*/}
			//AlphaTest Off /*Less L/G/Not Equal[0.2] Greater[0.5] Never */
			Blend  SrcAlpha One
			ColorMask RBG
			Offset 1,1 
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			#include "UnityCG.cginc"
			#define PI 3.14159

			struct appdata
			{
				float4 vertex : POSITION;
				float3 normal:NORMAL;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float3 objPos :TEXCOORD1;
				float4 projpos :TEXCOORD2;
				float4 litPos :TEXCOORD3;
				float4 vertex : SV_POSITION;
			};

			float4 litPos;
			float _Dis;

			v2f vert(appdata v)
			{
				v2f o;
				//float3 lightDir = ObjSpaceLightDir(v.vertex);
				o.litPos = float4(litPos.x, litPos.y, litPos.z, litPos.w);
				float factor = dot(normalize(litPos), v.normal);
				float exfactor = step(factor, 0);

				v.vertex.xyz += v.normal*0.03;
				v.vertex.xyz -= litPos * (exfactor * _Dis);
				o.objPos = v.vertex.xyz / v.vertex.w;

				v.vertex.xy *=(distance(o.litPos, o.objPos)+0.2) * (exfactor * 0.05);
				o.vertex = UnityObjectToClipPos(v.vertex);

				o.uv = v.uv;
				return o;
			}

			sampler2D _MainTex;
			sampler2D _RippleTex;

			fixed4 _Color;
			fixed _Gloss;
			fixed _Metallic;
			fixed _RippleScale;

			fixed4 frag(v2f i) : SV_Target
			{
					//float2 uv = i.uv-0.5;
					//uv = uv / _RippleScale;
					//uv += 0.5;
					//fixed4 ripple = tex2D(_RippleTex, uv);
					//ripple.yz = ripple.yz * 2 - 1;
					//float dropFrac = frac(ripple.a + _Time.y);
					//float timeFrac = dropFrac - 1 + ripple.x;
					//float dropFactor = 1 - saturate(dropFrac);
					//float final = dropFactor * sin(clamp(timeFrac * 9, 0, 4) * PI);
					//ripple.yz *= final;
					//fixed3 compute = fixed3(ripple.y, ripple.z, 1);
					//i.uv *= compute /10;
					//fixed4 col = tex2D(_MainTex, i.uv);
					//return col;

				fixed4 col = tex2D(_MainTex, i.uv) * _Color ;
				float tol = distance(i.litPos, i.objPos);
				float dis = tol - 0.2;
				float att = saturate(1.001 - dis / _Dis);
				//col.a = pow(att, 1);
				col.a = att;
				return col;
			}
			ENDCG
		}
	}
			Fallback "VertexLit"
}
