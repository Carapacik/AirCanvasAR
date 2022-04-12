// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'
// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

 Shader "Lambert"
 {
     Properties
     {
         _Color ("Color", Color) = (1.0,1.0,1.0,1.0)
     }
     
     CGINCLUDE
     #include "UnityCG.cginc"
     #include "AutoLight.cginc"
     #include "Lighting.cginc"
     ENDCG
 
  SubShader
  {
      LOD 200
      Tags { "RenderType"="Opaque" }
      Pass { 
             Lighting On
             Tags {"LightMode" = "ForwardBase"}
             CGPROGRAM
             #pragma vertex vert
             #pragma fragment frag
             #pragma multi_compile_fwdbase
 
             uniform half4        _Color;
 
             struct vertexInput
             {
                 half4     vertex        :    POSITION;
                 half3     normal        :    NORMAL;
             };
             
             struct vertexOutput
             {
                 half4     pos                :    SV_POSITION;
                 fixed4     lightDirection    :    TEXCOORD1;
                 fixed3     viewDirection    :    TEXCOORD2;
                 fixed3     normalWorld        :    TEXCOORD3;
                 LIGHTING_COORDS(4,6)
             };
 
              vertexOutput vert (vertexInput v)
             {
                 vertexOutput o;
                 
                 half4 posWorld = mul( unity_ObjectToWorld, v.vertex );
                 o.normalWorld = normalize( mul(half4(v.normal, 0.0), unity_WorldToObject).xyz );
                 o.pos = UnityObjectToClipPos(v.vertex);
                 o.viewDirection = normalize(_WorldSpaceCameraPos.xyz - posWorld.xyz);
             
                 TRANSFER_VERTEX_TO_FRAGMENT(o);
                 
                 return o;
             }
             
             half4 frag (vertexOutput i) : COLOR
             {
                 fixed NdotL = dot(i.normalWorld, i.lightDirection);
                 half atten = LIGHT_ATTENUATION(i);
                 fixed3 diffuseReflection = _LightColor0.rgb * atten * _Color.rgb;
                 fixed3 finalColor = UNITY_LIGHTMODEL_AMBIENT.xyz + diffuseReflection;
                 
                 return float4(finalColor, 1.0);
             }
             
              ENDCG
          }
      }
      FallBack "Diffuse"
  }