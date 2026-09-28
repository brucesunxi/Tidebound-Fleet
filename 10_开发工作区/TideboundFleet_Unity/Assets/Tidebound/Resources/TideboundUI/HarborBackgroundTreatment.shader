Shader "Tidebound/UI/HarborBackgroundTreatment"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Saturation ("Saturation", Range(0,1)) = .82
        _BlurPixels ("Blur pixels", Range(0,5)) = 2.1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            ZWrite Off Cull Off
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex; float4 _MainTex_TexelSize; float _Saturation; float _BlurPixels;
            struct appdata { float4 vertex:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR; };
            struct v2f { float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR; };
            v2f vert(appdata v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;return o;}
            half4 frag(v2f i):SV_Target
            {
                float2 centered=(i.uv-.5)*float2(.92,1.08);
                float focus=saturate(1-length(centered)*1.38);
                focus=focus*focus*(3-2*focus);
                float blur=lerp(_BlurPixels*1.55,_BlurPixels*.28,focus);
                float2 d=_MainTex_TexelSize.xy*blur;
                half4 c=tex2D(_MainTex,i.uv)*.28;
                c+=(tex2D(_MainTex,i.uv+float2(d.x,0))+tex2D(_MainTex,i.uv-float2(d.x,0)))*.15;
                c+=(tex2D(_MainTex,i.uv+float2(0,d.y))+tex2D(_MainTex,i.uv-float2(0,d.y)))*.15;
                c+=(tex2D(_MainTex,i.uv+d)+tex2D(_MainTex,i.uv-d))* .06;
                float luma=dot(c.rgb,float3(.2126,.7152,.0722));
                c.rgb=lerp(luma.xxx,c.rgb,lerp(_Saturation*.78,_Saturation,focus));
                c.rgb=lerp(c.rgb,float3(.82,.95,1),.045+focus*.035);
                c.rgb*=lerp(.72,1.02,focus);
                return c*i.color;
            }
            ENDHLSL
        }
    }
}
