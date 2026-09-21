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
            struct appdata { float4 vertex:POSITION;float3 normal:NORMAL;float4 color:COLOR;float2 finish:TEXCOORD0; };
            struct v2f { float4 pos:SV_POSITION;float3 normal:TEXCOORD0;float3 view:TEXCOORD1;float4 color:COLOR;float2 finish:TEXCOORD2;float3 local:TEXCOORD3; };
            v2f vert(appdata v)
            {
                v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.normal=UnityObjectToWorldNormal(v.normal);
                // Orthographic showcase: constant viewing direction avoids differences between isolated rig positions.
                o.view=mul((float3x3)UNITY_MATRIX_I_V,float3(0,0,1));o.color=v.color;o.finish=v.finish;o.local=v.vertex.xyz;return o;
            }
            half4 frag(v2f i):SV_Target
            {
                float3 n=normalize(i.normal),l=normalize(float3(-.65,.85,-1)),v=normalize(i.view);
                float metal=i.finish.x,rough=i.finish.y;
                float nl=saturate(dot(n,l)),nv=saturate(dot(n,v));
                float3 sky=lerp(float3(.13,.35,.45),float3(.94,.98,1.05),saturate(n.y*.5+.5));
                float3 ambient=lerp(float3(.25,.36,.43),float3(.50,.59,.62),saturate(-n.z*.5+.5));
                float cavity=1;
                // Local contact darkening beneath roof and on the lower hull; no world-space lights or shadow maps.
                float underRoof=(1-smoothstep(-1.30,-1.15,i.local.z))*step(-1.315,i.local.z);
                cavity-=underRoof*.24;
                cavity*=lerp(.70,1,saturate((-i.local.z-.04)/.42));
                float3 base=i.color.rgb*(ambient+nl*float3(.68,.59,.45))*cavity;
                if(rough<.16)
                {
                    float top=saturate((-i.local.z-.88)/.37);
                    base=lerp(float3(.012,.16,.32),float3(.13,.65,.86),top)*(.70+nl*.35);
                    float bandDistance=abs(frac((i.local.x+i.local.y)*2.5+i.local.z*.75)-.45);
                    float reflectionBand=(1-smoothstep(.045,.15,bandDistance))*.25;
                    base=lerp(base,float3(.76,.94,1),reflectionBand);
                }
                float power=lerp(150,22,rough);
                float spec=pow(saturate(dot(n,normalize(l+v))),power);
                float broad=pow(saturate(dot(n,normalize(float3(.6,.45,-1)+v))),14)*.22;
                float3 reflection=reflect(-v,n);
                float stripe=pow(saturate(dot(reflection,normalize(float3(-.25,.6,-.5)))),24);
                float3 f0=lerp(float3(.055,.055,.055),i.color.rgb,metal);
                float fresnel=pow(1-nv,5);
                float3 sheen=(f0+(1-f0)*fresnel)*(spec*3.4+broad+stripe*.75);
                base=lerp(base,base*.28+sky*i.color.rgb*.72,metal);
                return half4(base+sheen+sky*fresnel*.10,1);
            }
            ENDHLSL
        }
    }
}
