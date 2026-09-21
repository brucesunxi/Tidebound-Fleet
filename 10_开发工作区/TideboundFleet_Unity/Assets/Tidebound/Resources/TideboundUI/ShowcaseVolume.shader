Shader "Tidebound/UI/ShowcaseVolume"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION;float3 normal:NORMAL;float4 color:COLOR; };
            struct v2f { float4 pos:SV_POSITION;float3 normal:TEXCOORD0;float3 view:TEXCOORD1;float4 color:COLOR; };
            v2f vert(appdata v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.normal=UnityObjectToWorldNormal(v.normal);o.view=WorldSpaceViewDir(v.vertex);o.color=v.color;return o;}
            half4 frag(v2f i):SV_Target
            {
                float3 n=normalize(i.normal),l=normalize(float3(-.65,.85,-1)),v=normalize(i.view);
                float diffuse=saturate(dot(n,l));float spec=pow(saturate(dot(n,normalize(l+v))),42)*.35;
                float rim=pow(1-saturate(dot(n,v)),3)*.09;
                return half4(i.color.rgb*(.64+.40*diffuse)+spec+rim,1);
            }
            ENDHLSL
        }
    }
}
