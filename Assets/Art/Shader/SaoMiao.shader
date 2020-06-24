Shader "Hidden/SaoMiao"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
		_Color("Color",COLOR) = (0,0,0,1)
		_DepthColor("Depth Color",COLOR) = (1,0,0,1)
		_Threshold("Threshold",float) = 0.5
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

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
				float4 projpos :TEXCOORD2;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;

				o.projpos = ComputeScreenPos(o.vertex);	//projectedPosition 为了frag获得该物体在深度图的位置
				COMPUTE_EYEDEPTH(o.projpos.z);	//获得此物体的深度

                return o;
            }
			UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
            sampler2D _MainTex;
			fixed4 _DepthColor;
			fixed4 _Color;
			float  _Threshold;
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) *_Color;

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
