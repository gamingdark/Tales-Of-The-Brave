Shader "TalesOfTheBrave/Rounded Sprite"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Aspect ("Panel Aspect", Float) = 1
        _Radius ("Corner Radius", Range(0, 0.25)) = 0.018
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "CanUseSpriteAtlas" = "True"
        }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

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
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float _Aspect;
            float _Radius;

            v2f vert(appdata input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                output.color = input.color * _Color;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float2 roundedPoint = (input.uv - 0.5) * float2(_Aspect, 1.0);
                float2 halfSize = float2(_Aspect * 0.5, 0.5);
                float2 distanceToCorner = abs(roundedPoint) - (halfSize - _Radius);
                float roundedDistance =
                    length(max(distanceToCorner, 0.0)) +
                    min(max(distanceToCorner.x, distanceToCorner.y), 0.0) -
                    _Radius;
                clip(-roundedDistance);

                fixed4 color = tex2D(_MainTex, input.uv) * input.color;
                color.rgb *= color.a;
                return color;
            }
            ENDCG
        }
    }
}
