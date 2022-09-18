Shader "Unlit/Grass"
{
    Properties
    {
        _MainColor("Main Color", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Cull Off
        Zwrite On

        Tags {
            "RenderType"="Transparent"
            "Queue"="Transparent"
        }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #pragma target 4.5

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

            float4 _MainColor;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = 0.0;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = _MainColor;
                return col;
            }
            ENDHLSL
        }
    }
}
