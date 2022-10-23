Shader "Custom/Instancing Shader"
{
    Properties
    {
        _MainColor ("Color", Color) = (1, 0, 0, 1)
    }
    SubShader
    {
        Cull Off
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

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
            uniform float4x4 Rotation;

            v2f vert (VertexData v, uint svInstanceID : SV_InstanceID)
            {
                InitIndirectDrawArgs(0);
                v2f o;
                uint cmdID = GetCommandID(0);
                uint instanceID = GetIndirectInstanceID(svInstanceID);
                o.vertex = UnityObjectToClipPos(v.vertex + PositionsBuffer[instanceID]);
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
