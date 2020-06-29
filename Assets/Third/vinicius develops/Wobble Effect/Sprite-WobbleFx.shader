﻿// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "vdev/FX/Sprite Wobble (Multiply)"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
        _Color("Tint", Color) = (1,1,1,1)
        _Intensity("Intensity", Range(0.0, 360.0)) = 20.0
        _Speed("Speed", Range(0, 3)) = 0.0
        [MaterialToggle] PixelSnap("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha("Enable External Alpha", Float) = 0
    }

        SubShader
        {
            Tags
            {
                "Queue" = "Transparent"
                "IgnoreProjector" = "True"
                "RenderType" = "Transparent"
                "PreviewType" = "Plane"
                "CanUseSpriteAtlas" = "True"
            }

            Cull Off
            Lighting Off
            ZWrite Off
            Blend One OneMinusSrcAlpha

            Pass
            {
                CGPROGRAM
                #pragma vertex SpriteVert
                #pragma fragment WSpriteFrag
                #pragma target 2.0
                #pragma multi_compile_instancing
                #pragma multi_compile _ PIXELSNAP_ON
                #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
                #include "UnitySprites.cginc"

                float _Intensity;
                int _Speed;

                fixed4 WSpriteFrag(v2f IN) : SV_Target
                {
                    int speed = round(_Speed);
                    float sine = sin((IN.texcoord.y + abs(_Time[speed]))* _Intensity);
                    IN.texcoord.x += sine * 0.01;

                    fixed4 c = SampleSpriteTexture(IN.texcoord) * IN.color;
                    c.rgb *= c.a;
                    return c;
                }
                ENDCG
            }
        }
}
