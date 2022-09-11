Shader "Unlit/Grass"
{
    Properties
    {
        _Albedo1("Albedo 1", Color) = (1, 1, 1)
        _Albedo2("Albedo 2", Color) = (1, 1, 1)
        _AOColor("Ambient Occlusion", Color) = (1, 1, 1)
        _TipColor("Tip Color", Color) = (1, 1, 1)
        _Scale("Scale", Range(0.0, 2.0)) = 0.0
        _Droop("Droop", Range(0.0, 10.0)) = 0.0
        _FogColor("Fog Color", Color) = (1, 1, 1)
        _FogDensity("Fog Density", Range(0.0, 1.0)) = 0.0
        _FogOffset("Fog Offset", Range(0.0, 10.0)) = 0.0
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
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDHLSL
        }
    }
}
