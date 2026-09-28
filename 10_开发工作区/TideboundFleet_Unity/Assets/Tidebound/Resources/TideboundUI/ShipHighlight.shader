Shader "Tidebound/UI/ShipHighlight"
{
    Properties
    {
        [PerRendererData] _MainTex("Ship silhouette",2D)="white"{}
        _StencilComp("Stencil Comparison",Float)=8
        _Stencil("Stencil ID",Float)=0
        _StencilOp("Stencil Operation",Float)=0
        _StencilWriteMask("Stencil Write Mask",Float)=255
        _StencilReadMask("Stencil Read Mask",Float)=255
        _ColorMask("Color Mask",Float)=15
    }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True"}
        Stencil {Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask]}
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct Input {float4 vertex:POSITION;fixed4 color:COLOR;float2 uv:TEXCOORD0;};
            struct Varying {float4 pos:SV_POSITION;fixed4 color:COLOR;float2 uv:TEXCOORD0;float4 local:TEXCOORD1;};
            sampler2D _MainTex;float4 _ClipRect;
            Varying vert(Input v)
            {Varying o;o.local=v.vertex;o.pos=UnityObjectToClipPos(v.vertex);o.color=v.color;o.uv=v.uv;return o;}
            fixed4 frag(Varying i):SV_Target
            {
                // RGB comes from the highlight, so dark hull paint cannot dim the red edge.
                fixed4 c=i.color;c.a*=tex2D(_MainTex,i.uv).a;
                #ifdef UNITY_UI_CLIP_RECT
                c.a*=UnityGet2DClipping(i.local.xy,_ClipRect);
                #endif
                return c;
            }
            ENDCG
        }
    }
}
