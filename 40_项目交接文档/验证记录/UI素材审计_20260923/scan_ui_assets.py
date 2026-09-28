"""Read-only source UI inventory. Run with Python + Pillow; writes audit JSON beside this script."""
from pathlib import Path
from collections import Counter
import json,re,hashlib
from PIL import Image
OUT=Path(__file__).resolve().parent
ROOT=OUT.parents[2]
BASE=ROOT/'00_远端接收区/已购源码与参考项目'
PROJECTS={'U':'unity--Bus_Mania_100_BugFix','P':'cocos源码-救救小猪','R':'cocos逆向_猪了个猪_2.4.15'}
SCOPES={'U':['Assets/TJ/Texture2D','Assets/TJ/Sprites','Assets/Texture2D'], 'P':['assets/resources/Imager','assets/Game/Imager'], 'R':['assets/game/texture','assets/game2/texture','assets/Texture','assets/resources']}
assets=[];prefabs=[];summaries={};evidence={}
def record(p):
 evidence[str(p.relative_to(ROOT))]=hashlib.sha256(p.read_bytes()).hexdigest()
def walkmeta(d):
 if not isinstance(d,dict):return
 if 'uuid' in d:yield d
 for c in d.get('subMetas',{}).values():yield from walkmeta(c)
def nodepath(data,i):
 if i is None:return ''
 n=data[i];parent=n.get('_parent') or {};p=parent.get('__id__')
 return (nodepath(data,p)+'/' if p is not None else '')+n.get('_name','')
for code,dirname in PROJECTS.items():
 base=BASE/dirname;mapping={};metadata={};allfiles=[p for p in (base/('Assets' if code=='U' else 'assets')).rglob('*') if p.is_file()]
 for m in allfiles:
  if m.suffix!='.meta':continue
  txt=m.read_text(errors='replace');record(m)
  if code=='U':
   mat=re.search(r'^guid: (\w+)',txt,re.M)
   if mat:mapping[mat[1]]=str(m.relative_to(base))[:-5]
  else:
   try:d=json.loads(txt)
   except ValueError:continue
   for v in walkmeta(d):mapping[v['uuid']]=str(m.relative_to(base))[:-5];metadata[v['uuid']]=v
 paths=sorted(set(p for s in SCOPES[code] for p in (base/s).rglob('*') if p.suffix.lower() in ('.png','.jpg','.jpeg')))
 for p in paths:
  record(p);im=Image.open(p);im.load();a=im.getchannel('A') if 'A' in im.getbands() else None
  row={'project':code,'path':str(p.relative_to(base)),'width':im.width,'height':im.height,'mode':im.mode,'bytes':p.stat().st_size,'sha256':evidence[str(p.relative_to(ROOT))],'alpha_extrema':a.getextrema() if a else None,'borders':[],'used_by':[]}
  m=Path(str(p)+'.meta')
  if m.exists():
   txt=m.read_text()
   if code=='U':
    borders=re.findall(r'spriteBorder: \{x: ([^,]+), y: ([^,]+), z: ([^,]+), w: ([^}]+)\}',txt)
    row['borders']=[dict(zip(['left','bottom','right','top'],map(float,b))) for b in borders if any(float(n)>0 for n in b)]
   else:
    for v in walkmeta(json.loads(txt)):
     b={s: v.get('border'+s.capitalize(),0) for s in ['left','bottom','right','top']}
     if any(b.values()):row['borders'].append(b)
  assets.append(row)
 ps=[base/'Assets/TJ/Prefabs/UIManager.prefab'] if code=='U' else sorted(p for p in (base/'assets').rglob('*.prefab') if ('UIPanel' in p.parts if code=='P' else 'uiPrefab' in p.parts or p.name in ['notify.prefab','illustrateAnimalItem.prefab']))
 for p in ps:
  record(p);txt=p.read_text();rows=[];buttons=[];labels=[]
  if code=='U':
   blocks=re.split(r'^--- !u!',txt,flags=re.M);names={}
   for block in blocks:
    h=re.match(r'(\d+) &(\d+)',block);n=re.search(r'^  m_Name: (.*)',block,re.M)
    if h and h[1]=='1' and n:names[h[2]]=n[1]
   for block in blocks:
    g=re.search(r'm_GameObject: \{fileID: (\d+)\}',block);name=names.get(g[1],'?') if g else '?'
    sp=re.search(r'm_Sprite: \{fileID: ([^,}]+)(?:, guid: (\w+))?',block)
    if sp:
     ty=re.search(r'^  m_Type: (\d+)',block,re.M)
     rows.append({'node':name,'uuid':sp[2],'asset':mapping.get(sp[2]),'image_type':int(ty[1]) if ty else None})
    tr=re.search(r'm_Transition: (\d+)',block)
    if tr:buttons.append({'node':name,'transition':int(tr[1]),'has_sprite_swap':bool(re.search(r'm_(?:Highlighted|Pressed|Selected|Disabled)Sprite: \{fileID: [1-9]',block))})
   nodes=len(names)
  else:
   data=json.loads(txt)
   if not all(isinstance(x,dict) for x in data):
    prefabs.append({'project':code,'path':str(p.relative_to(base)),'format':'compact-not-expanded','nodes':None,'sprites':[],'buttons':[],'labels':[]});continue
   nodes=sum(x.get('__type__')=='cc.Node' for x in data)
   for x in data:
    t=x.get('__type__');idx=(x.get('node') or {}).get('__id__');name=nodepath(data,idx)
    if t=='cc.Sprite':
     uid=(x.get('_spriteFrame') or {}).get('__uuid__')
     rows.append({'node':name,'uuid':uid,'asset':mapping.get(uid),'image_type':x.get('_type',0),'size':data[idx].get('_contentSize') if idx is not None else None})
    elif t=='cc.Button':buttons.append({'node':name,'transition':x.get('_N$transition',x.get('transition',0)),'duration':x.get('duration','engine-default'),'zoom':x.get('zoomScale','engine-default')})
    elif t=='cc.Label':labels.append({'node':name,'fontSize':x.get('_fontSize'),'text':x.get('_string','')[:100]})
  item={'project':code,'path':str(p.relative_to(base)),'nodes':nodes,'sprites':rows,'buttons':buttons,'labels':labels}
  prefabs.append(item)
  for r in rows:
   for a in assets:
    if a['project']==code and a['path']==r['asset']:a['used_by'].append({'prefab':item['path'],'node':r['node'],'image_type':r['image_type']})
 aa=[a for a in assets if a['project']==code];pp=[p for p in prefabs if p['project']==code]
 summaries[code]={'project':dirname,'image_scope':SCOPES[code],'scoped_images':len(aa),'with_nonzero_border':sum(bool(a['borders']) for a in aa),'referenced_images_in_scanned_prefabs':sum(bool(a['used_by']) for a in aa),'scanned_prefabs':len(pp),'sprite_components':sum(len(p['sprites']) for p in pp),'unresolved_sprite_uuid_occurrences':sum(bool(r['uuid']) and not r['asset'] for p in pp for r in p['sprites']),'font_files':[str(p.relative_to(base)) for p in allfiles if p.suffix.lower() in ['.ttf','.otf','.fnt']],'layered_ui_sources':[str(p.relative_to(base)) for p in allfiles if p.suffix.lower() in ['.psd','.ai','.fig','.sketch']]}
for name,data in [('assets.json',assets),('prefabs.json',prefabs),('summary.json',summaries),('source_hashes.json',evidence)]:
 (OUT/name).write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n')
print(json.dumps(summaries,ensure_ascii=False,indent=2))
