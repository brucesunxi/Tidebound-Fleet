Shader "Tidebound/Prototype/ShipVolume"
{
    Properties
    {
        _Color ("Palette", Color) = (1,1,1,1)
        _Reveal ("Entry reveal", Range(0,1)) = 1
        _Hint ("Hint", Range(0,1)) = 0
        _Unlit ("Unlit surface", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite On
            Cull Back
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; float4 color:COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 pos:SV_POSITION; float3 normal:TEXCOORD0; float4 color:COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            float4 _Color;
            float _Unlit;
            UNITY_INSTANCING_BUFFER_START(PerShip)
                UNITY_DEFINE_INSTANCED_PROP(float, _Reveal)
                UNITY_DEFINE_INSTANCED_PROP(float, _Hint)
            UNITY_INSTANCING_BUFFER_END(PerShip)
            v2f vert(appdata v)
            {
                v2f o; UNITY_SETUP_INSTANCE_ID(v); UNITY_TRANSFER_INSTANCE_ID(v,o);
                o.pos=UnityObjectToClipPos(v.vertex);o.normal=UnityObjectToWorldNormal(v.normal);o.color=v.color;return o;
            }
            half4 frag(v2f i):SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float reveal=UNITY_ACCESS_INSTANCED_PROP(PerShip,_Reveal);clip(reveal-.001);
                float hint=UNITY_ACCESS_INSTANCED_PROP(PerShip,_Hint);
                half3 baseColor=lerp(i.color.rgb,i.color.rgb*_Color.rgb,i.color.a);
                half lighting=lerp(.56+.44*saturate(dot(normalize(i.normal),normalize(float3(-.45,.60,-.80)))),1,_Unlit);
                return half4(lerp(baseColor*lighting,half3(1,.82,.25),hint*.75),reveal);
            }
            ENDHLSL
        }
    }
}
