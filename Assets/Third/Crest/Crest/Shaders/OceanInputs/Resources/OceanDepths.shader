// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// Renders ocean depth - signed distance from sea level to sea floor
Shader "Crest/Inputs/Depth/Ocean Depth From Geometry"
{
	SubShader
	{
		Pass
		{
			BlendOp Min

			CGPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag

			#include "UnityCG.cginc"
			#include "../../OceanLODData.hlsl"

			struct Attributes
			{
				float3 positionOS : POSITION;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float depth : TEXCOORD0;
			};

			Varyings Vert(Attributes input)
			{
				Varyings o;
				o.positionCS = UnityObjectToClipPos(input.positionOS);

				float altitude = mul(unity_ObjectToWorld, float4(input.positionOS, 1.0)).y;

				o.depth = _OceanCenterPosWorld.y - altitude;

				return o;
			}

			float Frag(Varyings input) : SV_Target
			{
				return input.depth;
			}
			ENDCG
		}
	}
}
