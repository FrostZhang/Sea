Shader "Cus/ProjectorDecal" {
	Properties{
		_Color("Main Color", Color) = (1,1,1,1)
		_ShadowTex("Cookie", 2D) = "" {}
		_FalloffTex("FallOff", 2D) = "" {}

		_H("H min", Range(0, 2)) = 1
		_L("L", Range(0, 1)) = 1
		_V("V",Range(0, 1)) = 1
		_Step("Step",Range(0, 8)) = 1

		_Thickness("Thickness",Range(0, 5)) = 0.1
	}

		Subshader{
			Tags {"Queue" = "Transparent-100"}
			Pass {
				ZWrite Off
				ColorMask RGB
			//Blend OneMinusDstColor One
			BlendOp Max
			Offset -1, -1

			CGPROGRAM
			#pragma target 5.0
			#pragma vertex vert
			#pragma fragment frag
			#pragma geometry geom
			#pragma multi_compile_fog
			#include "UnityCG.cginc"

			struct v2f {
				float4 uvShadow : TEXCOORD0;
				//float4 uvFalloff : TEXCOORD1;
				UNITY_FOG_COORDS(2)
				float4 pos : SV_POSITION;
				float3  dist	: TEXCOORD2;	// distance to each edge of the triangle
			};

			float4x4 unity_Projector;
			float4x4 unity_ProjectorClip;

			v2f vert(float4 vertex : POSITION)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(vertex);
				o.uvShadow = mul(unity_Projector, vertex);
				//o.uvFalloff = mul(unity_ProjectorClip, vertex);
				UNITY_TRANSFER_FOG(o,o.pos);
				return o;
			}

			float3 HSVConvertToRGB(float3 hsv)
			{
				float R, G, B;
				//float3 rgb;
				if (hsv.y == 0)
				{
					R = G = B = hsv.z;
				}
				else
				{
					hsv.x = hsv.x / 60.0;
					int i = abs((int)hsv.x);
					float f = hsv.x - (float)i;
					float a = hsv.z * (1 - hsv.y);
					float b = hsv.z * (1 - hsv.y * f);
					float c = hsv.z * (1 - hsv.y * (1 - f));
					switch (i)
					{
					case 0: R = hsv.z; G = c; B = a;
						break;
					case 1: R = b; G = hsv.z; B = a;
						break;
					case 2: R = a; G = hsv.z; B = c;
						break;
					case 3: R = a; G = b; B = hsv.z;
						break;
					case 4: R = c; G = a; B = hsv.z;
						break;
					default: R = hsv.z; G = a; B = b;
						break;
					}
				}
				return float3(R, G, B);
			}

			// Geometry Shader
			[maxvertexcount(3)]
			void UCLAGL_geom(triangle v2f p[3], inout TriangleStream<v2f> triStream)
			{
				//points in screen space
				float2 p0 = _ScreenParams.xy * p[0].pos.xy / p[0].pos.w;
				float2 p1 = _ScreenParams.xy * p[1].pos.xy / p[1].pos.w;
				float2 p2 = _ScreenParams.xy * p[2].pos.xy / p[2].pos.w;

				//edge vectors
				float2 v0 = p2 - p1;
				float2 v1 = p2 - p0;
				float2 v2 = p1 - p0;

				//area of the triangle
				float area = abs(v1.x*v2.y - v1.y * v2.x);

				//values based on distance to the edges
				float dist0 = area / length(v0);
				float dist1 = area / length(v1);
				float dist2 = area / length(v2);

				v2f pIn;

				//add the first point
				pIn.pos = p[0].pos;
				pIn.uvShadow = p[0].uvShadow;
				pIn.dist = float3(dist0, 0, 0);
				triStream.Append(pIn);

				//add the second point
				pIn.pos = p[1].pos;
				pIn.uvShadow = p[1].uvShadow;
				pIn.dist = float3(0, dist1, 0);
				triStream.Append(pIn);

				//add the third point
				pIn.pos = p[2].pos;
				pIn.uvShadow = p[2].uvShadow;
				pIn.dist = float3(0, 0, dist2);
				triStream.Append(pIn);
			}

			float _Thickness;
			// Fragment Shader
			float4 UCLAGL_frag(v2f input) : COLOR
			{
				//find the smallest distance
				float val = min(input.dist.x, min(input.dist.y, input.dist.z));
				//calculate power to 2 to thin the line
				val = exp2(-1 / _Thickness * val * val);

				//blend between the lines and the negative space to give illusion of anti aliasing
				float4 targetColor = fixed4(1, 0, 0, 1);// *tex2D(_MainTex, input.uv);
				float4 transCol = fixed4(1, 0, 0, 1);// * tex2D( _MainTex, input.uv);
				transCol.a = 0;
				return val * targetColor + (1 - val) * transCol;
			}

			// Geometry Shader
			[maxvertexcount(3)]
			void geom(triangle v2f p[3], inout TriangleStream<v2f> triStream)
			{
				UCLAGL_geom(p, triStream);
			}

			//// Fragment Shader
			//float4 frag(UCLAGL_g2f input) : COLOR
			//{
			//	return UCLAGL_frag(input)*fixed4(1,0,0,1);
			//}


			fixed4 _Color;
			sampler2D _ShadowTex;
			sampler2D _FalloffTex;
			float _L;
			float _H;
			float _V;
			float _Step;
			fixed4 frag(v2f i) : SV_Target
			{
				float4 uv = i.uvShadow;
				fixed x = uv.x / uv.w;
				fixed y = uv.y / uv.w;
				fixed4 texS = fixed4(0,0,0,0);
				clip(x - 0.001);
				clip(0.9999 - x);
				clip(y - 0.001);
				clip(0.9999 - y);

					texS = tex2Dproj(_ShadowTex, UNITY_PROJ_COORD(i.uvShadow));
					//texS *= _Color;
					//texS.a *= (uv.y / uv.w);
					//float h = 0;
					float hm = uv.w - uv.z;
					float h = step(hm, _H);
					h =	h*(pow(hm / _H, _Step) * 240 + 115) + (1 - h)*355.99;
					//115 到 355.99
					texS.rbg = HSVConvertToRGB(float3(h,_L, _V));

					//fixed4 texF = tex2Dproj (_FalloffTex, UNITY_PROJ_COORD(i.uvFalloff));
					//fixed4 res = texS * texF.a;
					//texS.rbg += UCLAGL_frag(i);

					//UNITY_APPLY_FOG_COLOR(i.fogCoord, res, fixed4(1,1,1,1));
					return texS;
				}
				ENDCG
			}
		}

			FallBack "DIFFUSE"
}
