Shader "Hidden/NewImageEffectShader"
{
    Properties
    {
		_MainTex("Texture", 2D) = "white" {}
		_Rim ("Rim", float) = 1
    }
    SubShader
    {
		Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" "PreviewType" = "Plane" }
        Cull Off ZWrite On ZTest LEqual
		Blend  SrcAlpha OneMinusSrcAlpha
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
				float3 normal :Normal;
            };

            struct v2f
            {
				float2 uv : TEXCOORD0;
				float3 worldnormal : TEXCOORD1;
				float3 viewDir : TEXCOORD3;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
				o.worldnormal = normalize(UnityObjectToWorldNormal(v.normal));
				float3 worldPos = mul(unity_ObjectToWorld, v.vertex);
				o.viewDir = normalize(_WorldSpaceCameraPos.xyz - worldPos.xyz);
                return o;
            }

            sampler2D _MainTex;
			float _Rim;
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);

				float rim = pow(1-abs(dot(i.viewDir,i.worldnormal)),_Rim);
				col.a = rim;
                return col;
            }
            ENDCG
        }
    }
}
