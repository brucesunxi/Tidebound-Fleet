"""Build an offline review gallery; embeds source image bytes without modifying them."""
from pathlib import Path
import json, base64, html
OUT = Path(__file__).resolve().parent
ROOT = OUT.parents[2]
BASE = ROOT / '00_远端接收区/已购源码与参考项目'
PROJECTS = {'U':'unity--Bus_Mania_100_BugFix','P':'cocos源码-救救小猪','R':'cocos逆向_猪了个猪_2.4.15'}
NAMES = {'U':'Bus Mania · Unity','P':'救救小猪 · Cocos','R':'猪了个猪 · Cocos'}
assets = json.loads((OUT/'assets.json').read_text())
selection = []
def add(code, directory, names, category, verdict, note):
    for name in names.split('|'):
        path = directory+'/'+name+'.png'
        a = next(a for a in assets if a['project']==code and a['path']==path)
        selection.append(dict(a, category=category, verdict=verdict, note=note))
add('U','Assets/TJ/Texture2D','BlueButton|GreenButton|GrayButton|PurpleButton|RedButton','按钮','换肤候选','同形状彩色底图，文字可独立；颜色变体不能自动等同于按下/禁用状态。')
add('U','Assets/TJ/Texture2D','BlueButton_Small|BoosterButtonBase','按钮','换肤候选','圆角、侧壁、阴影分层清晰；检查实际 Image 类型，有 Border 不代表组件已启用 Sliced。')
add('U','Assets/TJ/Texture2D','PanelSettingsInside|PanelInsideWhite','面板','优先候选','留白干净，浅色较接近潮汐舰队；需核对描边厚度、缩放和现有金边风格。')
add('U','Assets/TJ/Texture2D','Panel|Panel2|PanelRectangleRound|PanelBehindButton2|ShopPanel','面板','换肤候选','可扩展的面板族；蓝色和立体程度需统一，不能混用全部变体。')
add('U','Assets/TJ/Texture2D','TopUI_Panel_B|TopUI_Panel_F|BoosterTopBanner_Middle','资源条/标题','换肤候选','前后底板、标题板拆件设计；数字和标签保留为独立文本。')
add('U','Assets/TJ/Texture2D','Close_Button|Settings_Icon|Plus','图标/开关','换肤候选','可复用语义图标；统一圆形底板、线条粗细与触摸热区。')
add('U','Assets/TJ/Texture2D','VolumeOn_icon|VolumeOff_icon|VibrationsOn_icon|VibrationsOff_icon','图标/开关','优先候选','已有明确的开关语义成对图标；是状态素材，不是完整 Toggle 组件。')
add('P','assets/resources/Imager/Common','M_FloorBase9|whitesample','面板','优先候选','干净底板且有九宫格；PopUpMain 使用同一底板组合面板与确认/取消按钮。')
add('P','assets/resources/Imager/Common','skillBG|a1','面板','换肤候选','适合道具槽/小卡片；边框和高光较强，先验证缩放后的视觉厚度。')
add('P','assets/resources/Imager/Common','btn4','按钮','换肤候选','已有完整黄色按钮底图，但元数据没有切片边界；复用时需要新建自己的 Border。')
add('P','assets/resources/Imager/Common','close|set|ad','图标/开关','换肤候选','功能小图标可作为候选；广告图标不意味着需要现在接入广告功能。')
add('P','assets/resources/Imager/Common','bg1|logo|cgsb','主题装饰','仅参考','整幅背景、品牌和烘焙标题不适合作通用组件；只学习层级与信息安排。')
add('R','assets/game/texture/common','img_ty_btn_huang|img_ty_btn_lv|img_ty_btn_zhi','按钮','换肤候选','小尺寸底图通过九宫格变成宽按钮；图标与文案单独作为子节点。')
add('R','assets/game/texture/common','img_ty_dk','面板','仅参考','九宫格外壳含装订环和动物爪印，主题已烘焙；学习分区，不直接当舰队通用弹窗。')
add('R','assets/game/texture/common','img_ty_btn_cha','图标/开关','换肤候选','独立关闭键，可拆离原弹窗；需统一位置和点击区域。')
add('R','assets/game2/texture/props','img_dj_dk','面板','优先候选','89×73 的奶油色底板，中心仅5×7；四角保持形状，中部拉伸，复用价值高。')
add('R','assets/game2/texture/set','img_sz_guan|img_sz_kai|img_sz_dian','图标/开关','优先候选','关闭/开启轨道与滑块分离，是真正可组合的开关素材；尺寸仍需统一。')
add('R','assets/game2/texture/rank','img_ph_btnhuang|img_ph_btnlan','标签/列表','换肤候选','成组选中感/普通感标签底图；不要仅以颜色承载选中信息。')
add('R','assets/game2/texture/rank','img_ph_dkwo|img_ph_dk4','标签/列表','换肤候选','列表高亮与容器底板；这里只研究图形和布局，不提前实施排名后台。')
add('R','assets/game2/texture/result','img_gxg_btn_huang|img_gxg_btn_lan','按钮','换肤候选','结算按钮的成组变体；适合学习主次操作区分。')
add('R','assets/game2/texture/result','img_gxg_bg|img_gxg_zi','主题装饰','仅参考','奖励光芒与中文标题；光效宜克制，烘焙文字不利于英文适配。')
(OUT/'candidates.json').write_text(json.dumps(selection,ensure_ascii=False,indent=2)+'\n')
def uri(a):
    return 'data:image/png;base64,'+base64.b64encode((BASE/PROJECTS[a['project']]/a['path']).read_bytes()).decode()
cards=[]
for i,a in enumerate(selection):
    border=a['borders'][0] if a['borders'] else None
    b=' / '.join(f'{border[k]:g}' for k in ['left','bottom','right','top']) if border else '无切片边界'
    refs=''.join('<li>'+html.escape(r['prefab']+' → '+r['node']+' · type='+str(r['image_type']))+'</li>' for r in a['used_by'])
    cards.append(f'''<article data-project="{a['project']}" data-verdict="{a['verdict']}" data-search="{html.escape(a['path']+' '+a['category']+' '+a['note'])}">
<div class="picture"><img loading="lazy" src="{uri(a)}" alt="{html.escape(Path(a['path']).name)}"></div>
<div class="info"><div class="tags"><span>{NAMES[a['project']]}</span><b class="v{a['verdict']}">{a['verdict']}</b></div>
<h3>{html.escape(Path(a['path']).name)}</h3><p>{a['category']} · {a['width']} × {a['height']} px · {len(a['used_by'])} 处引用</p>
<p class="note">{a['note']}</p><details><summary>路径、切片与引用</summary><code>{html.escape(PROJECTS[a['project']]+'/'+a['path'])}</code><p>Border 左 / 下 / 右 / 上：{b}</p><ul>{refs or '<li>本次扫描的预制体中未引用；不代表全项目未使用。</li>'}</ul></details></div></article>''')
demo=next(a for a in selection if Path(a['path']).name=='img_dj_dk.png')
page='''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>三套源码 · UI 素材审计</title>
<style>
*{box-sizing:border-box}body{margin:0;background:#edf2f5;color:#183849;font:15px/1.6 system-ui,-apple-system,sans-serif}header,main{max-width:1440px;margin:auto;padding:28px}header{padding-bottom:12px}h1{font-size:34px;margin:6px 0}h2{font-size:23px}h3{font-size:16px;margin:10px 0;overflow-wrap:anywhere}p{margin:8px 0}.eyebrow{color:#647e8d;letter-spacing:2px;font-size:12px}.intro{max-width:950px}.stats{display:flex;gap:12px;flex-wrap:wrap;margin:20px 0}.stat{background:#fff;border-radius:12px;padding:14px 22px}.stat b{font-size:26px;margin-right:8px}.controls{display:flex;gap:12px;flex-wrap:wrap;position:sticky;top:0;z-index:5;background:#edf2f5ee;padding:14px 0}input,select{font:inherit;border:1px solid #b8ccd8;padding:10px;border-radius:8px;max-width:100%}input[type=search]{min-width:270px}#count{padding:10px}.grid{display:grid;grid-template-columns:repeat(auto-fill,minmax(285px,1fr));gap:18px}article{background:white;border-radius:14px;overflow:hidden;border:1px solid #d5e0e6}article[hidden]{display:none}.picture{height:188px;padding:20px;display:flex;align-items:center;justify-content:center;background:repeating-conic-gradient(#dbe5eb 0% 25%,#edf3f6 0% 50%) 50%/22px 22px}.picture img{max-width:100%;max-height:100%;object-fit:contain}.info{padding:17px}.tags{display:flex;justify-content:space-between;gap:4px;font-size:11px;color:#537181}.tags b{border-radius:5px;padding:2px 7px;background:#f3dfb4;color:#6c4b17}.tags b.v优先候选{background:#d5ede0;color:#205f47}.tags b.v仅参考{background:#e5e6eb;color:#606675}.note{min-height:72px}details{font-size:12px;border-top:1px solid #e4ebef;margin-top:14px;padding-top:10px}summary{cursor:pointer;color:#37677c}code,li{overflow-wrap:anywhere}ul{padding-left:16px}section.demo{background:#fff;border-radius:14px;padding:24px;margin-bottom:24px}.compare{display:flex;gap:32px;flex-wrap:wrap;margin-top:20px}.sample{height:110px;width:240px;display:flex;align-items:center;justify-content:center;color:#6b573f}.sliced{border:solid transparent;border-width:33px 42px;border-image-slice:33 42 33 42 fill;border-image-repeat:stretch}.stretch{background-size:100% 100%}.demo small{display:block;color:#647e8d}.range{display:flex;align-items:center;gap:15px;margin-top:16px}.caption{font-size:12px;color:#647e8d}footer{padding:40px 0;color:#647e8d}@media(max-width:600px){header,main{padding:16px}h1{font-size:27px}.controls{position:static}.note{min-height:0}}
</style><header><div class="eyebrow">TIDEBOUND FLEET / SOURCE STUDY / 2026.09.23</div><h1>三套源码，哪些 UI 真正能复用？</h1>
<p class="intro">这里展示源文件原图与真实元数据。优先候选＝技术上易拆、视觉较中性；换肤候选＝结构可用但需统一风格；仅参考＝主题、文字或背景已烘焙。它们均未导入正式工程，候选评级不代表已确认素材授权范围。</p>
<div class="stats"><div class="stat"><b>3</b>套源码</div><div class="stat"><b>848</b>范围内图片</div><div class="stat"><b>47</b>带切片边界图片</div><div class="stat"><b>COUNT</b>图册候选</div></div>
<p class="caption">848包含游戏画面、重复图片、字体图集及编辑器皮肤，不能当作848个UI控件。引用数仅来自指定23个预制体，其中1个压缩格式未展开。</p></header><main>
<section class="demo"><h2>先看一个成熟做法：一张小底图，适配不同宽度</h2><p>原始素材：猪了个猪 / img_dj_dk.png / 89×73 px。拖动宽度，观察普通拉伸的圆角变形，以及九宫格如何保留边缘。</p>
<div class="range"><label for="size">宽度</label><input id="size" type="range" min="160" max="360" value="240"><output id="width">240 px</output></div>
<div class="compare"><div><small>普通整图拉伸</small><div class="sample stretch">内容区</div></div><div><small>九宫格（CSS演示）</small><div class="sample sliced">内容区</div></div></div><p class="caption">只是用原图演示伸缩原理，并非原游戏截图或Unity实机验收。四边参数来自原 .meta；显示高度固定110px。</p></section>
<div class="controls"><input id="search" type="search" aria-label="搜索素材" placeholder="搜索：按钮 / 面板 / 文件名…"><select id="project" aria-label="项目"><option value="">全部项目</option><option value="U">Bus Mania · Unity</option><option value="P">救救小猪 · Cocos</option><option value="R">猪了个猪 · Cocos</option></select><select id="verdict" aria-label="候选等级"><option value="">全部等级</option><option>优先候选</option><option>换肤候选</option><option>仅参考</option></select><span id="count"></span></div>
<div class="grid">CARDS</div><footer>审计范围、限制与设计学习结论见 UI_ASSET_REUSE_STUDY_20260923.md。图册离线可用，无外部字体、脚本或网络依赖。</footer></main>
<script>
const cards=[...document.querySelectorAll('article')];function filter(){const q=document.querySelector('#search').value.trim().toLowerCase(),p=document.querySelector('#project').value,v=document.querySelector('#verdict').value;let n=0;cards.forEach(c=>{c.hidden=!!((p&&c.dataset.project!==p)||(v&&c.dataset.verdict!==v)||(q&&!c.dataset.search.toLowerCase().includes(q)));if(!c.hidden)n++});document.querySelector('#count').textContent=n+' / '+cards.length+' 件';}['search','project','verdict'].forEach(id=>document.getElementById(id).addEventListener('input',filter));filter();const range=document.querySelector('#size');range.addEventListener('input',()=>{document.querySelectorAll('.sample').forEach(n=>n.style.width=range.value+'px');document.querySelector('#width').value=range.value+' px'});
</script></html>'''
page=page.replace('COUNT',str(len(selection))).replace('CARDS','\n'.join(cards))
page=page.replace('</style>','.sliced{border-image-source:url("'+uri(demo)+'")}.stretch{background-image:url("'+uri(demo)+'")}</style>')
(OUT/'UI素材图册.html').write_text(page)
print(f'{len(selection)} candidates; gallery {len(page.encode()):,} bytes')
