
Shader "Cus/Line"{
	Properties{
		_MainTex("MainTex", 2D) = "white" {}
	}

	SubShader
	{
	    Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }

	    Pass
	    {
	        ZWrite Off
	        Blend SrcAlpha OneMinusSrcAlpha

	        CGPROGRAM
	        #pragma vertex vert
	        #pragma fragment frag
	        #include "UnityCG.cginc"

			
			#define SMOOTH(r,R) (.01-smoothstep(R-.001,R+.001, r))
			#define RANGE(a,b,x) ( step(a,x)*(1.0-step(b,x)) )
			#define RS(a,b,x) ( smoothstep(a-.001,a+.001,x)*(1-smoothstep(b-.001,b+.001,x)) )
			#define M_PI 3.1415926535897932384626433832795

			#define blue1 fixed3(0.74,0.95,1.00)
			#define blue2 fixed3(0.87,0.98,1.00)
			#define blue3 fixed3(0.35,0.76,0.83)
			#define blue4 fixed3(0.953,0.969,0.89)

			#define green1 fixed3(0,0.35,0)
			#define green2 fixed3(0.5,0.5,0)
			#define green3 fixed3(0,0.45,0)
			#define green4 fixed3(0,0.5,0)

			#define red   fixed3(1.00,0.38,0.227)

			#define MOV(a,b,c,d,t) (fixed2(a*cos(t)+b*cos(0.1*(t)), c*sin(t)+d*cos(0.1*(t))))

            struct v2f {
		        fixed4 pos : SV_POSITION;
		        fixed2 uv : TEXCOORD0;
		        fixed2 uv_depth : TEXCOORD1;
		        fixed4 interpolatedRay : TEXCOORD2;
	        };

		    fixed4x4 _FrustumCornersRay;
	        fixed4 _MainTex_TexelSize;
	        sampler2D _CameraDepthTexture;
	        //Variables
			sampler2D _MainTex;


	        v2f vert(appdata_img v) {
		        v2f o;
		        o.pos = UnityObjectToClipPos(v.vertex);

		        o.uv = v.texcoord;
		        o.uv_depth = v.texcoord;

        #if UNITY_UV_STARTS_AT_TOP
		        if (_MainTex_TexelSize.y < 0)
			        o.uv_depth.y = 1 - o.uv_depth.y;
        #endif

		        int index = 0;
		        if (v.texcoord.x < 0.5 && v.texcoord.y < 0.5) {
			        index = 0;
		        }
		        else if (v.texcoord.x > 0.5 && v.texcoord.y < 0.5) {
			        index = 1;
		        }
		        else if (v.texcoord.x > 0.5 && v.texcoord.y > 0.5) {
			        index = 2;
		        }
		        else {
			        index = 3;
		        }

        #if UNITY_UV_STARTS_AT_TOP
		        if (_MainTex_TexelSize.y < 0)
			        index = 3 - index;
        #endif
		        o.interpolatedRay = _FrustumCornersRay[index];
                //VertexFactory
		        return o;
	        }//end fixedt

            fixed4 ProcessFrag(v2f i);


	        fixed4 frag(v2f i) : SV_Target
	        {
                fixed linearDepth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv_depth));
		        fixed3 worldPos = _WorldSpaceCameraPos + linearDepth * i.interpolatedRay.xyz;
		        //get Unity world pos
		        fixed4 finalColor = tex2D(_MainTex, i.uv);

		        fixed4 processCol = ProcessFrag(i);
		        if(processCol.w < linearDepth){
			        finalColor = processCol;
			        finalColor.w =1.0;
		        }

		        return processCol;
	        }

//-----------------------------------------------------
	        //Sci-fi radar based on the work of gmunk for Oblivion
			//http://work.gmunk.com/OBLIVION-GFX


			fixed movingLine(fixed2 uv, fixed2 center, fixed radius)
			{
				//angle of the line
				fixed theta0 = 90.0 * _Time.y;
				fixed2 d = uv - center;
				fixed r = sqrt( dot( d, d ) );
				if(r<radius)
				{
					//compute the distance to the line theta=theta0
					fixed2 p = radius*fixed2(cos(theta0*M_PI/180.0),
										-sin(theta0*M_PI/180.0));
					fixed l = length( d - p*clamp( dot(d,p)/dot(p,p), 0.0, 1.0) );
    				d = normalize(d);
					//compute gradient based on angle difference to theta0
   	 				fixed theta = fmod(180.0*atan2(d.x,d.y)/M_PI+theta0,360.0);
					fixed gradient = clamp(1.0-theta/90.0,0.0,1.0);
					return SMOOTH(l,1.0)+0.5*gradient;
				}
				else return 0.0;
			}

			fixed circle(fixed2 uv, fixed2 center, fixed radius, fixed width)
			{
				fixed r = length(uv - center);
				return SMOOTH(r-width/2.0,radius)-SMOOTH(r+width/2.0,radius);
			}

			fixed circle2(fixed2 uv, fixed2 center, fixed radius, fixed width, fixed opening)
			{
				fixed2 d = uv - center;
				fixed r = sqrt( dot( d, d ) );
				d = normalize(d);
				if( abs(d.y) > opening )
					return SMOOTH(r-width/2.0,radius)-SMOOTH(r+width/2.0,radius);
				else
					return 0.0;
			}
			fixed circle3(fixed2 uv, fixed2 center, fixed radius, fixed width)
			{
				fixed2 d = uv - center;
				fixed r = sqrt( dot( d, d ) );
				d = normalize(d);
				fixed theta = 180.0*(atan2(d.x,d.y)/M_PI);
				return smoothstep(5, 5.1, abs(fmod(theta+2.0,45.0)-2.0)) *
					lerp( 0.5, 1.0, step(45.0, abs(fmod(theta, 180.0)-90.0)) ) *
					(SMOOTH(r-width/2.0,radius)-SMOOTH(r+width/2.0,radius));
			}

			fixed triangles(fixed2 uv, fixed2 center, fixed radius)
			{
				fixed2 d = uv - center;
				return RS(-.08, .0, d.x-radius) * (1.0-smoothstep( .07+d.x-radius,.09+d.x-radius, abs(d.y)))
					 + RS( .0, .08, d.x+radius) * (1.0-smoothstep( .07-d.x-radius,.09-d.x-radius, abs(d.y)))
					 + RS(-.08, .0, d.y-radius) * (1.0-smoothstep( .07+d.y-radius,.09+d.y-radius, abs(d.x)))
					 + RS( .0, .08, d.y+radius) * (1.0-smoothstep( .07-d.y-radius,.09-d.y-radius, abs(d.x)));
			}

			fixed _cross(fixed2 uv, fixed2 center, fixed radius)
			{
				fixed2 d = uv - center;
				int x = int(d.x);
				int y = int(d.y);
				fixed r = sqrt( dot( d, d ) );
				if( (r<radius) && ( (x==y) || (x==-y) ) )
					return 1.0;
				else return 0.0;
			}
			fixed dots(fixed2 uv, fixed2 center, fixed radius)
			{
				fixed2 d = uv - center;
				fixed r = sqrt( dot( d, d ) );
				if( r <= 2.5 )
					return 1.0;
				if( ( r<= radius) && ( (abs(d.y+0.5)<=1.0) && ( fmod(d.x+1.0, 50.0) < 2.0 ) ) )
					return 1.0;
				else if ( (abs(d.y+0.5)<=1.0) && ( r >= 50.0 ) && ( r < 115.0 ) )
					return 0.5;
				else
					return 0.0;
			}
			fixed bip1(fixed2 uv, fixed2 center)
			{
				return SMOOTH(length(uv - center),0.5);
			}
			fixed bip2(fixed2 uv, fixed2 center)
			{
				fixed r = length(uv - center);
				fixed R = .08+fmod(.36*_Time.y, .08);
				return (0.5-0.5*cos(.3*_Time.y)) * SMOOTH(r,.005)
					+ SMOOTH(.06,r)-SMOOTH(.08,r)
					+ smoothstep(max(.08,R-.02),R,r)-SMOOTH(R,r);
			}
			fixed bip3(fixed2 uv, fixed2 center)
			{
				fixed r = length(uv - center);
				fixed R = .016 + fmod(.306*_Time.x, .016);
				return /*(0.05 - 0.05* 1) **/ SMOOTH(r, .0005)
					+ SMOOTH(.006, r) - SMOOTH(.008, r)
					+ smoothstep(max(.008, R - .002), R, r) - SMOOTH(R, r);
			}
			fixed4 _Targets[10];
			fixed4 ProcessFrag(v2f i)  {
                 
				fixed3 finalColor;
				fixed2 uv = i.uv;
				//center of the image
				fixed2 c = 1/2.0;
				//fixed cro =0.3*_cross(uv, c, 1.);
				finalColor = fixed3(0,0,0  );
				finalColor += ( circle(uv, c, 0.49, .002)
							  + circle(uv, c, 0.485, .002 ))* green1;
				finalColor += (circle(uv, c, 0.4, .002) )* green2;//+ dots(uv,c,240.0)) * blue4;
				finalColor += circle3(uv, c, 0.32, 0.002) * green1;
				//finalColor += triangles(uv, c, 0.3+ sin(_Time.y)) * blue2;
				finalColor += movingLine(uv, c, .5) * green3;
				finalColor += circle(uv, c, .15,.002) * green3;
				finalColor += 0.7 * circle2(uv, c, 0.24, .002, 0.5+0.2*cos(_Time.z)) * green3;
				//if( length(uv-c) < 0.25 )
				//{
				//	//animate some bips with random movements
    //				fixed2 p = .5*MOV(1.3,1.0,1.0,1.4,3.0+0.1*_Time.y);
   	//				//finalColor += bip1(uv, c+p) * fixed3(1,1,1);
				//	p = .5*MOV(0.9,-1.1,1.7,0.8,-2.0+sin(0.1*_Time.y)+0.15*_Time.y);
				//	//finalColor += bip1(uv, c+p) * fixed3(1,1,1);
				//	p = .5*MOV(1.54,1.7,1.37,1.8,sin(0.1*_Time.y+7.0)+0.2*_Time.y);
				//	finalColor += bip2(uv,c+p) * red;
				//}
				//MOV(a, b, c, d, t) (fixed2(a*cos(t) + b * cos(0.1*(t)), c*sin(t) + d * cos(0.1*(t))))

				for (int i = 0; i < 10; i++)
				{
					finalColor += bip3(uv, fixed2(_Targets[i].x, _Targets[i].y))* red;
				}
				return fixed4(finalColor, max(finalColor.x, finalColor.y));
			}
		ENDCG
		}
	}//end SubShader
}//end Shader