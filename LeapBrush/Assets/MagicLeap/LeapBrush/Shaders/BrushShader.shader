Shader "MagicLeap/LeapBrush/Unlit/BrushShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        Cull Off

        Pass {
            Lighting Off
            ZWrite On
            SetTexture[_] {
                constantColor [_Color]
                Combine constant
            }
        }
    }
}
