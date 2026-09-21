Shader "Tidebound/UI/ShowcaseWater"
{
    Properties { _Phase ("Motion phase",Float)=0 }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Cull Off ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float _Phase;
            struct v2f { float4 pos:SV_POSITION;float2 uv:TEXCOORD0; };
            v2f vert(appdata_base v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.texcoord.xy;return o;}
            half4 frag(v2f i):SV_Target
            {
                float2 p=(i.uv-.5)*float2(2.16,2.8);
                float taper=1-saturate(p.y)*.28;
                float2 q=p/float2(.53*taper,.86);
                float radius=length(q),angle=atan2(q.y,q.x);
                float wave=sin(angle*9+_Phase*.55)*.013+sin(angle*23-_Phase*.3)*.008;
                float contact=exp(-pow((radius-1.01-wave)/.025,2));
                float breaks=smoothstep(-.6,.5,sin(angle*13+sin(angle*4)));
                float outer=exp(-pow((radius-1.13-wave)/.012,2))*smoothstep(.2,.8,sin(angle*7+1.6));
                float innerShadow=exp(-pow(radius/.88,4))*.22;
                float foam=(contact*(.10+breaks*.85)+outer*.16)*.78;
                float wash=exp(-pow((radius-1.09)/.16,2))*.08;
                float alpha=saturate(innerShadow+foam+wash);
                float3 col=(float3(.015,.24,.30)*innerShadow+float3(.78,.99,1)*foam+float3(.17,.71,.81)*wash)/max(.001,alpha);
                return half4(col,alpha);
            }
            ENDHLSL
        }
    }
}
