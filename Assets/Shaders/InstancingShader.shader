Shader "Custom/Instancing Shader"
{
    Properties
    {
        _MainColor ("Color", Color) = (1, 0, 0, 1)
    }
    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #define UNITY_INDIRECT_DRAW_ARGS IndirectDrawIndexedArgs
            #include "UnityIndirect.cginc"

            struct VertexData
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR0;
            };

            float4 _MainColor;
            uniform StructuredBuffer<float4> PositionsBuffer;

            v2f vert (VertexData v, uint svInstanceID : SV_InstanceID)
            {
                InitIndirectDrawArgs(0);
                v2f o;
                uint cmdID = GetCommandID(0);
                uint instanceID = GetIndirectInstanceID(svInstanceID);
                float4 pos = v.vertex;
                o.vertex = UnityObjectToClipPos(pos + PositionsBuffer[instanceID]);
                o.color = _MainColor;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return i.color;
            }
            ENDCG
        }
    }
}
