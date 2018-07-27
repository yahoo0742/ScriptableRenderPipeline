﻿Shader "Unlit/ShowVFace"
{
	Properties
	{
		_ColorFront ("Front Color", Color) = (1, 0, 0, 1)
        _ColorBack ("Back Color", Color) = (0, 1, 0, 1)
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

            fixed4 _ColorFront;
            fixed4 _ColorBack;

            float4 vert (float4 vertex : POSITION) : SV_POSITION
            {
                return UnityObjectToClipPos(vertex);
            }

			float4 frag (fixed facing : VFACE) : SV_Target
			{
				return facing > 0 ? _ColorFront : _ColorBack;
			}
			ENDCG
		}
	}
}
