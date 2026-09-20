Shader "Tidebound/Prototype/ContactShadow"
{
    Properties { _Color ("Shadow",Color)=(0,0,0,.5) _Reveal ("Reveal",Range(0,1))=1 _Hint ("Hint",Range(0,1))=0 }
    SubShader
    {
        Tags { "Queue"="Transparent-10" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID};
            struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID};
            float4 _Color;
            UNITY_INSTANCING_BUFFER_START(PerShip)
                UNITY_DEFINE_INSTANCED_PROP(float,_Reveal)
                UNITY_DEFINE_INSTANCED_PROP(float,_Hint)
            UNITY_INSTANCING_BUFFER_END(PerShip)
            v2f vert(appdata v){v2f o;UNITY_SETUP_INSTANCE_ID(v);UNITY_TRANSFER_INSTANCE_ID(v,o);o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;return o;}
            half4 frag(v2f i):SV_Target
            {UNITY_SETUP_INSTANCE_ID(i);float2 p=i.uv*2-1;float a=1-smoothstep(.40,1,dot(p,p));return half4(_Color.rgb,_Color.a*a*UNITY_ACCESS_INSTANCED_PROP(PerShip,_Reveal));}
            ENDHLSL
        }
    }
}
