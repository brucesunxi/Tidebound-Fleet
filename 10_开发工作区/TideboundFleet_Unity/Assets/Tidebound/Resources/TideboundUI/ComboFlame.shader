Shader "Tidebound/UI/ComboFlame"
{
 Properties { [PerRendererData] _MainTex("Texture",2D)="white"{} _FlameTime("Clock",Float)=0 _Strength("Tier",Float)=1 _Mode("Hull",Float)=0 _Opacity("Opacity",Float)=1 }
 SubShader
 {
  Tags {"Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True"}
  Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode] Blend One OneMinusSrcAlpha
  Pass
  {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   struct input {float4 vertex:POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
   struct varying {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
   float _FlameTime,_Strength,_Mode,_Opacity;
   varying vert(input v){varying o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;return o;}
   float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
   float noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);}
   float fbm(float2 p){float n=0,a=.57;[unroll]for(int i=0;i<4;i++){n+=a*noise(p);p=p*2.03+float2(7.1,3.7);a*=.47;}return n;}
   float tongue(float2 p,float seed){
    float2 flow=float2(p.x*3.3+seed,p.y*2.65-_FlameTime*1.55);
    float broad=fbm(flow*.63+float2(_FlameTime*.08,seed));
    float2 warp=float2(broad-.5,noise(flow*.8+11)-.5);
    float n=fbm(flow+warp*2.1),curl=noise(flow*1.8+float2(-_FlameTime*.2,2));
    float x=p.x+(broad-.5)*(.18+p.y*.7);
    return 1.03-p.y*.86-abs(x)*(.63+.7*max(0,p.y))-(1-n)*.79-max(0,p.y-.25)*(.14*(1-curl));
   }
   fixed4 frag(varying i):SV_Target {
    float2 uv=i.uv;float d=-1,y=(uv.y-.085)/(.60+_Strength*.065);
    if(_Mode<.5){
     [unroll]for(int j=0;j<6;j++){
      float k=j,center=.1+k*.157,sway=.018*sin(_FlameTime*.7+k*3.1);
      float ht=.74+.18*sin(k*4.3)+.06*sin(_FlameTime*.8+k);
      d=max(d,tongue(float2((uv.x-center-sway)/(.14+.014*_Strength),y/ht),k*9.31));
     }
    }else{
     y=(uv.y-.08)/(.52+_Strength*.065);
     [unroll]for(int j=0;j<5;j++){
      float k=j,cx=.22+k*.14,localY=(uv.y-.08-abs(k-2)*.02)/((.52+_Strength*.065)*(.74+.13*sin(k*5.1)));
      d=max(d,tongue(float2((uv.x-cx)/(.07+_Strength*.008),localY),23+k*7.31));
     }
     [unroll]for(int j=0;j<2;j++){
      float cx=j==0?.17:.83,sh=(uv.y-.29)/(.24+_Strength*.1);
      d=max(d,tongue(float2((uv.x-cx)/(.055+_Strength*.009),sh),61+j*19)-max(0,-sh)*9);
     }
    }
    float mask=smoothstep(.015,.095,uv.y)*smoothstep(0,.06,uv.x)*(1-smoothstep(.94,1,uv.x))*(1-smoothstep(.95,1,uv.y));
    float edge=smoothstep(-.035,.08,d);
    float3 col=lerp(float3(.97,.16,.018),float3(1,.57,.035),smoothstep(-.02,.16,d));
    col=lerp(col,float3(1,.89,.32),smoothstep(.15,.34,d));col=lerp(col,float3(1,.985,.79),smoothstep(.33,.55,d));
    if(_Mode>.5&&_Strength>2.5)col=lerp(col,float3(.6,1,.95),smoothstep(.46,.70,d)*.72);
    float alpha=edge*mask*saturate(_Strength);
    float glow=exp(-pow((uv.y-.17)*5,2))*.12*mask*(1-alpha);
    return float4(col*alpha+float3(1,.39,.055)*glow,alpha+glow)*_Opacity*i.color.a;
   }
   ENDCG
  }
 }
}
