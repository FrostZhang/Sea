Shader "Hidden/HaiDi"
{
    Properties
    {
		_MainTex("Texture", 2D) = "white" {}
		_Color ("Color", color) = (1,1,1,1)
		_Thickness("Thickness", Float) = 1
    }
    SubShader
    {
		Tags { "RenderType" = "Transparent" "Queue" = "Geometry" }

        Pass
        {
			//Tags { "Queue" = "Geometry" "IgnoreProjector" = "False" "RenderType" = "Opaque" "PreviewType" = "Plane" }
			Cull Off Lighting Off ZWrite On
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
			fixed4 _Color;
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv)*_Color;
                return col;
            }
            ENDCG
        }

		//Pass{
		//		
		//		Tags { "Queue" = "Geometry" "IgnoreProjector" = "False" "RenderType" = "Opaque" "PreviewType" = "Plane" }
		//		Cull Back ZWrite off ZTest Greater
		//		Blend SrcAlpha OneMinusSrcAlpha
		//		LOD 200

		//		CGPROGRAM
		//			#pragma target 5.0
		//			#include "UnityCG.cginc"
		//			#include "UCLA GameLab Wireframe Functions.cginc"
		//			#pragma vertex vert
		//			#pragma fragment frag
		//			#pragma geometry geom

		//			// Vertex Shader
		//			UCLAGL_v2g vert(appdata_base v)
		//			{
		//				return UCLAGL_vert(v);
		//			}

		//		// Geometry Shader
		//		[maxvertexcount(3)]
		//		void geom(triangle UCLAGL_v2g p[3], inout TriangleStream<UCLAGL_g2f> triStream)
		//		{
		//			UCLAGL_geom(p, triStream);
		//		}

		//		// Fragment Shader
		//		float4 frag(UCLAGL_g2f input) : COLOR
		//		{
		//			return UCLAGL_frag(input)*fixed4(1,0,0,1);
		//		}

		//	ENDCG
		//	}
    }

	Fallback "VertexLit"
}
