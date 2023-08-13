Shader "Custom/FygarFire"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        [HDR]_EmissiveColor("Emission", Color) = (1, 1, 1, 1)
    }
        SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent"}
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows alpha:fade

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;

        struct Input
        {
            fixed4 color : COLOR;
        };

        fixed4 _Color;
        fixed4 _EmissiveColor;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 c = _EmissiveColor * IN.color;
            o.Albedo = _Color.rgb;
            // Metallic and smoothness come from slider variables
            o.Metallic = 0.0f;
            o.Smoothness = 0.0f;
            o.Emission = c.rgb;
            o.Alpha = IN.color.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
