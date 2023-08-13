Shader "Unlit/GroundParticle"
{
    Properties
    {

    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                half3 normal : NORMAL;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                half3 normal : TEXCOORD1;
                float4 world_pos : TEXCOORD2;
                fixed4 color : COLOR;
            };

            //Colors of the layers of the soil

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.normal = v.normal;
                o.color = v.color;
                o.world_pos = mul(unity_ObjectToWorld, v.vertex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // calculate which component of the normal vector is largest, and base the "shading" off from that
                //x
                float color_modifier = 1.0;
                if (abs(i.normal.x) > abs(i.normal.y) && abs(i.normal.x) > abs(i.normal.z)) {
                    color_modifier = .6f;
                }
                //y
                if (abs(i.normal.y) > abs(i.normal.x) && abs(i.normal.y) > abs(i.normal.z)) {
                    if (i.normal.y < 0)
                        color_modifier = .3;
                }
                //z
                if (abs(i.normal.z) > abs(i.normal.x) && abs(i.normal.z) > abs(i.normal.y)) {
                    color_modifier = .8f;
                }


                fixed4 col = fixed4(color_modifier, color_modifier, color_modifier, 1.0);
                col *= i.color;
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
