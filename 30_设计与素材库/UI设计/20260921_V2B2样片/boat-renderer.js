/* Original procedural geometry. WebGL visual study only; no logical Grid reads from mesh bounds. */
(() => {
'use strict';
const C={cream:[1,.94,.78],white:[1,.99,.91],teal:[.055,.34,.43],coral:[.98,.35,.22],gold:[1,.69,.21],glass:[.05,.42,.57],blue:[.20,.53,.84]};
class Mesh {
 constructor(){this.data=[];}
 tri(a,b,c,color){const u=b.map((v,i)=>v-a[i]),v=c.map((x,i)=>x-a[i]);let n=[u[1]*v[2]-u[2]*v[1],u[2]*v[0]-u[0]*v[2],u[0]*v[1]-u[1]*v[0]];const d=Math.hypot(...n)||1;n=n.map(x=>x/d);for(const p of [a,b,c])this.data.push(...p,...n,...color);}
 quad(a,b,c,d,col){this.tri(a,b,c,col);this.tri(a,c,d,col);}
 ring(shape,s,z,t,w,col){shape.forEach((p,i)=>{const q=shape[(i+1)%shape.length];this.quad([p[0]*s,p[1]*s,z],[q[0]*s,q[1]*s,z],[q[0]*t,q[1]*t,w],[p[0]*t,p[1]*t,w],col);});}
 cap(shape,s,z,col){const c=shape.reduce((a,p)=>[a[0]+p[0]*s/shape.length,a[1]+p[1]*s/shape.length],[0,0]);shape.forEach((p,i)=>{const q=shape[(i+1)%shape.length];this.tri([...c,z],[p[0]*s,p[1]*s,z],[q[0]*s,q[1]*s,z],col);});}
 solid(shape,z,h,col){this.ring(shape,1,z,1,h,col);this.cap(shape,1,h,col);}
 cylinder(x,y,z,r,h,col,n=20){const s=Array.from({length:n},(_,i)=>[x+r*Math.cos(i*2*Math.PI/n),y+r*Math.sin(i*2*Math.PI/n)]);this.solid(s,z,z+h,col);}
 torus(x,y,z,r,t,col,axis='x'){const pt=(a,b)=>{const u=(r+t*Math.cos(b))*Math.cos(a),v=(r+t*Math.cos(b))*Math.sin(a),w=t*Math.sin(b);return axis==='x'?[x+w,y+u,z+v]:[x+u,y+w,z+v];};for(let i=0;i<32;i++)for(let j=0;j<8;j++){const a=i*Math.PI/16,b=j*Math.PI/4;this.quad(pt(a,b),pt(a+Math.PI/16,b),pt(a+Math.PI/16,b+Math.PI/4),pt(a,b+Math.PI/4),i%8<4?col:C.white);}}
}
function rect(l,b,r,t,k=.07){return [[l+k,b],[r-k,b],[r,b+k],[r,t-k],[r-k,t],[l+k,t],[l,t-k],[l,b+k]];}
// Port of B1 BuildToy outline, heights, cabin and direction chevron. Vertex palette is a study.
function smallBoat(length=2,blue=false){const m=new Mesh(),h=length*.5-.09;
const s=[[-.23,-h],[.23,-h],[.34,-h+.09],[.40,-h+.25],[.40,h-.73],[.33,h-.30],[.18,h-.10],[0,h],[-.18,h-.10],[-.33,h-.30],[-.40,h-.73],[-.40,-h+.25],[-.34,-h+.09]];
m.ring(s,.90,.05,.96,.32,C.teal);m.ring(s,.96,.32,1,.46,C.cream);m.ring(s,1,.46,.96,.53,C.cream);m.cap(s,.96,.53,C.white);
m.solid(rect(-.25,-h+.30,.25,-h+.89),.535,.79,C.cream);
m.solid(rect(-.28,-h+.26,.28,-h+.94,.09),.79,.86,length===3?[.47,.6,.63]:blue?C.blue:C.coral);
m.solid(rect(-.12,-h+.12,.12,-h+.26,.035),.53,.71,C.teal);
const y=h-.39;m.tri([-.21,y-.16,.54],[0,y+.18,.54],[0,y-.02,.54],C.teal);m.tri([0,y-.02,.54],[0,y+.18,.54],[.21,y-.16,.54],C.teal);
if(length===3)m.solid(rect(-.25,-.34,.25,.35,.055),.54,.66,[.38,.49,.53]);return m;}
function flagship(variant=0){const m=new Mesh(),v=variant,roof=v?C.blue:C.coral;
const s=[[-.62,-1.34],[.62,-1.34],[.85,-1.18],[.98,-.80],[.98,.48],[.88,.90],[.65,1.29],[.34,1.57],[0,1.70],[-.34,1.57],[-.65,1.29],[-.88,.90],[-.98,.48],[-.98,-.80],[-.85,-1.18]];
m.ring(s,.72,.02,.96,.42,C.teal);m.ring(s,.96,.42,1,.61,C.teal);m.ring(s,1,.61,1,.72,C.cream);m.ring(s,1,.72,.97,.78,C.white);m.cap(s,.97,.78,C.cream);
// Raised rounded gunwale follows the waterline, leaving the deck visible.
for(let i=0;i<s.length;i++){const p=s[i],q=s[(i+1)%s.length];m.quad([p[0],p[1],.77],[q[0],q[1],.77],[q[0],q[1],.94],[p[0],p[1],.94],C.white);m.quad([p[0],p[1],.94],[q[0],q[1],.94],[q[0]*.9,q[1]*.9,.94],[p[0]*.9,p[1]*.9,.94],C.white);}
const front=v?.61:.38,back=v?-.91:-.70,top=v?1.46:1.65;
m.solid(rect(-.62,back,.62,front,.15),.79,top,C.cream);
// Separate glass geometry; large readable windows, never baked into texture.
for(const x of [-.30,.30])m.quad([x-.20,front+.002,1.02],[x+.20,front+.002,1.02],[x+.17,front+.002,top-.12],[x-.17,front+.002,top-.12],C.glass);
for(const x of [-.623,.623])for(const y of [back+.29,front-.26])m.quad([x,y-.15,1.01],[x,y+.15,1.01],[x,y+.15,top-.13],[x,y-.15,top-.13],C.glass);
const r=rect(-.73,back-.09,.73,front+.10,.19);m.ring(r,.96,top,1,top+.09,roof);m.ring(r,1,top+.09,.94,top+.18,roof);m.cap(r,.94,top+.18,roof);
if(v){m.solid(rect(-.38,-.65,.38,-.08,.10),top+.19,top+.54,C.cream);m.solid(rect(-.44,-.71,.44,-.02,.12),top+.54,top+.63,roof);m.quad([-.25,-.075,top+.28],[.25,-.075,top+.28],[.25,-.075,top+.48],[-.25,-.075,top+.48],C.glass);}
for(const x of v?[-.32,.32]:[-.28]){m.cylinder(x,-1.00,.81,.14,.73,C.cream);m.cylinder(x,-1.00,1.40,.17,.14,roof);m.cylinder(x,-1.00,1.54,.12,.012,C.teal);}
m.cylinder(.32,-.38,top+.16,.033,.70,C.gold);m.quad([.32,-.38,top+.76],[.93,-.34,top+.65],[.81,-.34,top+.44],[.32,-.38,top+.47],roof);
m.torus(.99,-.49,.65,.24,.064,C.coral);m.torus(-.99,-.49,.65,.24,.064,C.coral);
for(const x of [-.75,.75])m.cylinder(x,.74,.80,.06,.19,C.gold);
m.torus(.84,.77,.64,.12,.037,C.gold);
return m;}
class BoatView {
 constructor(canvas,mode='hero'){this.canvas=canvas;this.mode=mode;this.gl=canvas.getContext('webgl',{alpha:true,antialias:true,preserveDrawingBuffer:true});if(!this.gl)throw Error('WebGL unavailable');this.variant=0;this.draws=0;this.init();}
 init(){const g=this.gl;const vs=`attribute vec3 p,n,c;uniform float yaw,elevation,scale,aspect;uniform vec3 offset;varying vec3 color;void main(){float a=cos(yaw),b=sin(yaw);vec3 q=vec3(a*p.x-b*p.y,b*p.x+a*p.y,p.z)+offset;float e=sin(elevation),f=cos(elevation);gl_Position=vec4(q.x/scale/aspect,(-q.y*e+q.z*f)/scale,(-q.y*f-q.z*abs(e))/100.,1.);vec3 N=vec3(a*n.x-b*n.y,b*n.x+a*n.y,n.z);float light=.69+.31*abs(dot(normalize(N),normalize(vec3(-.4,-.5,1.))));color=c*light;}`;
 const fs=`precision mediump float;varying vec3 color;void main(){gl_FragColor=vec4(color,1.);}`;
 const shader=(type,src)=>{let s=g.createShader(type);g.shaderSource(s,src);g.compileShader(s);if(!g.getShaderParameter(s,g.COMPILE_STATUS))throw Error(g.getShaderInfoLog(s));return s;};
 const p=g.createProgram();g.attachShader(p,shader(g.VERTEX_SHADER,vs));g.attachShader(p,shader(g.FRAGMENT_SHADER,fs));g.linkProgram(p);if(!g.getProgramParameter(p,g.LINK_STATUS))throw Error(g.getProgramInfoLog(p));g.useProgram(p);this.program=p;this.uniforms=Object.fromEntries(['yaw','elevation','scale','aspect','offset'].map(n=>[n,g.getUniformLocation(p,n)]));this.attributes=['p','n','c'].map(n=>g.getAttribLocation(p,n));g.enable(g.DEPTH_TEST);g.clearColor(0,0,0,0);this.buffers=new Map();}
 buffer(key,mesh){if(!this.buffers.has(key)){const g=this.gl,b=g.createBuffer();g.bindBuffer(g.ARRAY_BUFFER,b);g.bufferData(g.ARRAY_BUFFER,new Float32Array(mesh.data),g.STATIC_DRAW);this.buffers.set(key,{b,count:mesh.data.length/9});}return this.buffers.get(key);}
 mesh(key,make,yaw,elev,scale,offset=[0,0,-.70]){const g=this.gl,u=this.uniforms,b=this.buffers.has(key)?this.buffers.get(key):this.buffer(key,make());g.bindBuffer(g.ARRAY_BUFFER,b.b);this.attributes.forEach((a,i)=>{g.enableVertexAttribArray(a);g.vertexAttribPointer(a,3,g.FLOAT,false,36,i*12);});g.uniform1f(u.yaw,yaw);g.uniform1f(u.elevation,elev);g.uniform1f(u.scale,scale);g.uniform1f(u.aspect,this.canvas.width/this.canvas.height);g.uniform3fv(u.offset,offset);g.drawArrays(g.TRIANGLES,0,b.count);}
 render(t=0,moving=false){const g=this.gl,c=this.canvas,dpr=Math.min(devicePixelRatio||1,2),w=Math.round(c.clientWidth*dpr),h=Math.round(c.clientHeight*dpr);if(c.width!==w||c.height!==h){c.width=w;c.height=h;}g.viewport(0,0,w,h);g.clear(g.COLOR_BUFFER_BIT|g.DEPTH_BUFFER_BIT);if(this.mode==='board'){const l=window.STUDY_LEVEL;for(let i=0;i<l.ships.length;i++){const s=l.ships[i],a={Up:0,Right:-Math.PI/2,Down:Math.PI,Left:Math.PI/2}[s.direction],dx={Right:1,Left:-1,Up:0,Down:0}[s.direction],dy={Up:1,Down:-1,Left:0,Right:0}[s.direction];this.mesh('small'+s.length+(i%5===1),()=>smallBoat(s.length,i%5===1),a,-Math.PI/2,10,[s.position.x+dx*(s.length-1)/2-6.5,s.position.y+dy*(s.length-1)/2-8.5,0]);}}else{const phase=t/5000*Math.PI*2;this.mesh('hero'+this.variant,()=>flagship(this.variant),-.55+(moving?Math.sin(phase)*.018:0),.40,this.scale||1.80,[0,0,-.72+(moving?Math.sin(phase)*.012:0)]);}this.draws++;}
}
window.BoatView=BoatView;
})();
