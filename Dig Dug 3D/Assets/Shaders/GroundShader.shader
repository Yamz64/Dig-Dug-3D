Shader "Unlit/GroundShader"
{
    Properties
    {
        _OrganicColor("Organic Color", Color) = (222.0, 222.0, 0.0)
        _TopsoilColor("Topsoil Color", Color) = (255.0, 183.0, 0.0)
        _EluviationColor("Eluviation Color", Color) = (222.0, 103.0, 0.0)
        _SubsoilColor("Subsoil Color", Color) = (183.0, 33.0, 0.0)
        _ParentRockColor("Parentrock Color", Color) = (151.0, 0.0, 0.0)

        _SoilWavePeriod("Soil Wave Period", Float) = 1.0
        _SoilWaveAmplitude("Soil Wave Amplitude", Float) = 1.0
        _SoilTop("Soil Top Offset", Float) = -0.3
        _SoilBottom("Soil Bottom", Float) = -10.0
        _SoilThickness("Soil Layer Thickness", Float) = 10.0
        _PixelDensity("Pixel Density", Float) = 8.0
        _OffColorFrequency("Off-Color Frequency", Range(0.0, 1.0)) = 0.1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

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
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                half3 normal : TEXCOORD1;
                float4 world_pos : TEXCOORD2;
            };

            //Colors of the layers of the soil
            half4 _OrganicColor;
            half4 _TopsoilColor;
            half4 _EluviationColor;
            half4 _SubsoilColor;
            half4 _ParentRockColor;

            float _SoilWavePeriod;
            float _SoilWaveAmplitude;
            float _SoilTop;
            float _SoilBottom;
            float _SoilThickness;
            float _PixelDensity;
            float _OffColorFrequency;

            //random functions for aiding in determining which soil to use
            float Random(float3 world_position) {
                return frac(sin(dot(world_position, float3(12.9898, 78.233, 12.9898))) * 43758.5453123);
            }

            float PixelRand(float3 world_position) {
                float rand = Random(floor(world_position * _PixelDensity));
                if (rand > 1.0 - _OffColorFrequency)
                    return 0.0;

                return 1.0;
            }
            
            //function for snapping values to nearest increment
            float Snap(float value, float step) {
                return round(value / step) * step;
            }

            //function determines which soil color to use
            half4 SoilColor(float3 world_position) {
                //divvy the soil up into 5 major layers
                float relative_y = world_position.y + Snap(_SoilWaveAmplitude * sin(_SoilWavePeriod * (world_position.x + world_position.z) / 2.0), 0.2) - _SoilBottom;
                float soil_layer = relative_y % _SoilThickness;
                soil_layer = 1.0 * (soil_layer / _SoilThickness);
                for (int i = 0; i < 4; i++) {
                    if (soil_layer >= i * .25 && soil_layer < (i + 1) * .25) {
                        soil_layer = (i + 1) * .25;
                        break;
                    }
                }

                //Choose a color based off from the layer
                if (soil_layer == 0.25f) {
                    //primary color
                    if (PixelRand(world_position) == 0)
                        return _ParentRockColor;
                    //secondary color
                    return _SubsoilColor;
                }
                else if (soil_layer == 0.5f) {

                    //primary color
                    if (PixelRand(world_position) == 0)
                        return _SubsoilColor;
                    //secondary color
                    return _EluviationColor;
                }
                else if (soil_layer == 0.75f) {

                    //primary color
                    if (PixelRand(world_position) == 0)
                        return _EluviationColor;
                    //secondary color
                    return _TopsoilColor;
                }
                else if (soil_layer == 1.0f) {

                    //primary color
                    if (PixelRand(world_position) == 0)
                        return _TopsoilColor;
                    //secondary color
                    return _OrganicColor;
                }
                //primary color
                if (PixelRand(world_position) == 0)
                    return _ParentRockColor;
                //secondary color
                return _SubsoilColor;
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.normal = v.normal;
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
                if (color_modifier != 1.0 || i.world_pos.y < -_SoilBottom + _SoilTop)
                    col *= SoilColor(i.world_pos);
                else
                    col *= _OrganicColor

                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
