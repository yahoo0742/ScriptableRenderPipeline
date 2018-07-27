// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Custom/ShowLightAndReflectionProbeLinear" {
    SubShader {
        Pass {
            Tags { "LightMode" = "ForwardBase" }

CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma target 3.0
#include "UnityCG.cginc"

struct v2f {
  float4 pos : SV_POSITION;
  half3 worldNormal : TEXCOORD0;
  float3 worldPos : TEXCOORD1;
  half3 sh : TEXCOORD2;
  UNITY_VERTEX_OUTPUT_STEREO
};


v2f vert (appdata_full v) {
  v2f o;
  UNITY_INITIALIZE_OUTPUT(v2f,o);
  UNITY_SETUP_INSTANCE_ID(v);
  UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
  o.pos = UnityObjectToClipPos (v.vertex);
  float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
  fixed3 worldNormal = UnityObjectToWorldNormal(v.normal);
  o.worldPos = worldPos;
  o.worldNormal = worldNormal;
  // calculate illumination from light probes
  o.sh = ShadeSH9 (half4(worldNormal,1.0));
  return o;
}

fixed4 frag (v2f IN) : SV_Target {
  UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
  float3 worldPos = IN.worldPos;
  fixed3 worldViewDir = normalize(UnityWorldSpaceViewDir(worldPos));
  fixed4 c = 0;

  // sample reflection probe mip 0
  half3 reflVec = reflect(-worldViewDir, IN.worldNormal);
  half4 rgbm = UNITY_SAMPLE_TEXCUBE_LOD(unity_SpecCube0, reflVec, 0);

  // add reflection & SH
  c.rgb = rgbm.xyz;
  c.a = 1;
  return c;
}

ENDCG

        }
    }
}
