Shader "Tidebound/UI/ShipReflection"
{
    Properties { _MainTex("Ship",2D)="white"{} _Phase("Ripple phase",Float)=0 }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            ZWrite Off Cull Off Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            float4 _ClipRect;
            sampler2D _MainTex;float _Phase;
            struct appdata { float4 vertex:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR; };
            struct v2f { float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;float4 local:TEXCOORD1; };
            v2f vert(appdata v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;o.local=v.vertex;return o;}
            half4 frag(v2f i):SV_Target
            {
                float ripple=sin(i.uv.y*42+i.uv.x*15+_Phase*1.7)*.012*(1-i.uv.y);
                half4 c=tex2D(_MainTex,float2(i.uv.x+ripple,1-i.uv.y));
                float fade=smoothstep(.08,.88,i.uv.y);
                float bands=.76+.24*sin(i.uv.y*58+i.uv.x*7+_Phase*1.4);
                c.rgb=lerp(c.rgb,float3(.03,.50,.72),.62);
                c.a*=fade*bands*i.color.a;
                #ifdef UNITY_UI_CLIP_RECT
                c.a*=UnityGet2DClipping(i.local.xy,_ClipRect);
                #endif
                return c;
            }
            ENDHLSL
        }
    }
}
