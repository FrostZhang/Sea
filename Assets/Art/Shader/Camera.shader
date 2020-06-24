Shader "Hidden/Camera"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
		_FogHigh("FogHigh",float) = 0.01
		_FogColor("FogColor",color) = (1,1,1,1)
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

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 uv : TEXCOORD0;
				float3 frustumDir  : TEXCOORD1;
                float4 vertex : SV_POSITION;
            };
			sampler2D _MainTex;
			float4 _MainTex_ST;
			float4 _MainTex_TexelSize;
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv.xy = TRANSFORM_TEX(v.uv, _MainTex);
				o.uv.zw = v.uv;

#if UNITY_UV_STARTS_AT_TOP
				if (_MainTex_TexelSize.y < 0)
					o.uv.w = 1 - o.uv.w;
#endif
				int ix = (int)o.uv.z;
				int iy = (int)o.uv.w;
				o.frustumDir = ix + 2 * iy;

				UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

			UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
			fixed4 _FogColor;
			float _FogHigh;
            fixed4 frag (v2f i) : SV_Target
            {
				fixed4 col = tex2D(_MainTex, i.uv);
				fixed dep = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture , i.uv);
				dep = Linear01Depth(dep);

				float linearEyeDepth = LinearEyeDepth(dep);
				float3 worldPos = _WorldSpaceCameraPos.xyz;// +i.frustumDir * linearEyeDepth;
				float fogDensity = (worldPos.y - 0) / (_FogHigh - 0);
				fogDensity = saturate(fogDensity * 0.1);
				col = lerp(_FogColor, col, fogDensity);
				UNITY_APPLY_FOG(i.fogCoord, finalColor);

                return col;
            }
            ENDCG
        }
    }

}
