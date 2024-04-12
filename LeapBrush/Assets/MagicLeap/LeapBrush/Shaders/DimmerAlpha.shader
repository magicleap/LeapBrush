Shader "MagicLeap/Dimmer Alpha"
{
    Properties
    {
        _Alpha("Alpha", Float) = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
        }
        LOD 100
        Cull Off

        Pass
        {
            Lighting Off
            Name "DimmerAlpha"
            ColorMask A
            Blend One Zero

            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag

            #include "UnityCG.cginc"

            uniform float _Alpha;

            fixed4 frag(v2f_img i) : SV_Target {
                return fixed4(0, 0, 0, _Alpha);
            }
            ENDCG
        }
    }
}
