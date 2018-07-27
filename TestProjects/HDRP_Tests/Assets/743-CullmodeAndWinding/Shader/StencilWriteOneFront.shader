Shader "Custom/StencilWriteOneFront" {
    Properties
    {
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        Cull Front
        Stencil
        {
            Ref 1
            Comp always
            Pass replace
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog
        #pragma target 3.0

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

            fixed4 frag (v2f i,float facing : VFACE) : SV_Target
            {
                // sample the texture
            fixed4 c = (facing > 0.0f) ? float4(0.0, 0.2, 0.0, 1.0) : float4(0.2, 0.0, 0.0, 1.0);
                // apply fog
                return c;
            }
            ENDCG
        }
    }
}
