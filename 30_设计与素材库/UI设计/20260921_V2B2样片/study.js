'use strict';
const paths={
 settings:'<path d="M25 5l3 6 6 1 5-3 6 8-4 5 1 6 5 3-4 9-6-1-5 3-1 6H20l-1-6-5-3-6 1-4-9 5-3 1-6-4-5 6-8 5 3 6-1z" fill="#49b8c6"/><circle cx="25" cy="26" r="8" fill="#fff4d8"/>',
 coin:'<circle cx="25" cy="25" r="21" fill="#ffcc4b"/><circle cx="25" cy="25" r="16" fill="#ffdc6c"/><path d="M25 14v22m-9-9q0 10 9 10t9-10m-15-6h12" fill="none"/><circle cx="25" cy="13" r="3" fill="#fff0b0"/>',
 anchor:'<path d="M25 14v26M14 27q0 13 11 13t11-13M17 23h16M10 28l4-4 4 4m14 0 4-4 4 4" fill="none"/><circle cx="25" cy="10" r="5" fill="#ffe196"/>',
 ticket:'<path d="M5 13h40v9q-7 3 0 7v9H5v-9q7-4 0-7z" fill="#ac91de"/><path d="M25 17l3 6 7 1-5 5 1 7-6-4-6 4 1-7-5-5 7-1z" fill="#ffe399"/>',
 book:'<path d="M5 10q12-5 20 1 8-6 20-1v31q-12-4-20 1-8-5-20-1z" fill="#fff8df"/><path d="M25 11v31M10 18h9m-9 7h9m-9 7h9" fill="none"/><path d="M29 29h13l-4 6h-6z" fill="#39b0c4"/><path d="M35 16v12h8z" fill="#f38b67"/>',
 chest:'<rect x="5" y="16" width="40" height="27" rx="5" fill="#c99155"/><path d="M5 23h40M12 16v27m26-27v27" fill="none"/><path d="M6 18V9h38v9" fill="#e6b978"/><rect x="21" y="22" width="8" height="10" rx="2" fill="#ffe197"/>',
 calendar:'<rect x="7" y="10" width="36" height="34" rx="7" fill="#fff9e9"/><path d="M7 21h36V11H7z" fill="#ec8065"/><path d="M16 6v10m18-10v10m-19 15 7 6 13-14" fill="none"/>',
 gift:'<rect x="7" y="20" width="36" height="24" rx="4" fill="#ffd470"/><path d="M5 17h40v8H5z" fill="#ffd470"/><path d="M21 18h8v26h-8z" fill="#ee8265"/><path d="M25 18q-22-1-17-11 8-9 17 11 10-20 18-11 4 10-18 11z" fill="#ee8265"/>',
 trophy:'<path d="M14 7h22v16q-1 10-11 10T14 23z" fill="#ffd252"/><path d="M14 11H6v9q0 10 11 10m19-19h8v9q0 10-11 10M25 33v9m-9 2h18" fill="none"/><path d="M25 12l3 5 5 1-4 4 1 5-5-3-5 3 1-5-4-4 5-1z" fill="#fff3b4"/>',
 trail:'<path d="M7 14q8-8 16 0t20 0M7 25q8-8 16 0t20 0M7 36q8-8 16 0t20 0" stroke="#36b7cb" stroke-width="5" fill="none"/>',
 ship:'<path d="M5 30h40l-8 13H14z" fill="#257d95"/><path d="M13 29V14h23v15" fill="#fff3d6"/><path d="M11 12h27v6H11z" fill="#ef8264"/><path d="M19 22h4m6 0h4" fill="none"/><path d="M25 12V4h13l-4 6H25" fill="#ef8264"/>',
 scene:'<rect x="5" y="5" width="40" height="40" rx="8" fill="#b1e8e5"/><circle cx="16" cy="15" r="5" fill="#ffe18b"/><path d="M7 36l14-15 9 9 5-7 9 13v8H7z" fill="#82b89b"/>',
 home:'<path d="M5 23L25 6l20 17M11 21v23h11V30h8v14h10V21" fill="#b0e5df"/>',
 share:'<path d="M21 12H8v32h32V29M21 29q2-13 16-13v8l10-13L37 3v7Q17 10 21 29z" fill="#8bd7b4"/>',
 rescue:'<path d="M10 34h30l-6 10H16z" fill="#56b7c4"/><path d="M25 34V9m-7 7 7-7 7 7" fill="none"/><circle cx="25" cy="8" r="5" fill="#ffe391"/>',
 shuffle:'<path d="M7 13h8c13 0 9 24 23 24h6M7 37h8c13 0 9-24 23-24h6m-8-7 8 7-8 7m0 10 8 7-8 7" fill="none" stroke="#2e97ab" stroke-width="4"/>',
 flip:'<path d="M9 19a17 17 0 0 1 30-6l4 8m-1-13 1 13-13-1M41 31A17 17 0 0 1 11 37l-4-8m1 13L7 29l13 1" fill="none" stroke="#de9274" stroke-width="4"/>'
};
function icon(name){return `<svg viewBox="0 0 50 50" aria-hidden="true" stroke="#265265" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">${paths[name]||paths.ship}</svg>`;}
function icons(root=document){root.querySelectorAll('[data-icon]').forEach(e=>e.innerHTML=icon(e.dataset.icon));}
const names=[['Sea Breeze','海风旗舰'],['Coast Explorer','巡岸探险号']];
const categories=[['Skins','船只皮肤','Draw to collect','抽奖获取','ship'],['Trails','拖尾','Clear levels to unlock','通关获取','trail'],['Showcase','展示船','Clear levels to unlock','通关获取','ship'],['Scenes','背景','Clear levels to unlock','通关获取','scene']];
const itemNames=[['Classic','Blue Tide','Coral','Sailor','Pearl','Sunrise'],['Foam','First Light','Starwake','Aqua','Breeze','Moonlight'],['Sea Breeze','Coast Explorer','Harbor Pilot','Voyager','Cloud Sail','Evening Star'],['Bright Harbor','Sandy Cove','Quiet Bay','Azure Coast','Sunset','Starlight']];
const selectedItems=[0,0,0,0];
const itemZh=[['经典船','蓝潮','珊瑚','航海家','珍珠','朝阳'],['基础水纹','晨光水纹','星光尾迹','碧浪','海风','月光'],['海风旗舰','巡岸探险号','港湾领航','远航号','云帆','晚星'],['晴日港湾','沙滩海湾','静谧海湾','蔚蓝海岸','落日','星夜']];
let languagePreference='auto';
try {languagePreference=localStorage.getItem('tidebound.b2.language')||'auto';} catch (_) {}
if(!['auto','en','zh'].includes(languagePreference))languagePreference='auto';
// Visual-study region provider. Real device region resolution belongs to Unity C2.
let detectedRegion='unknown';
function resolvedLanguage(){return languagePreference==='auto'?(detectedRegion==='china'?'zh':'en'):languagePreference;}
function L(en,zh){return resolvedLanguage()==='zh'?zh:en;}
function collectionCards(tab=0,count=6,thumbnails=[]){return `<div class="cards">${Array.from({length:count},(_,i)=>{const unlocked=i===0||(tab===0&&i===1);return `<button class="item-card ${i===selectedItems[tab]?'equipped':unlocked?'':'locked'}" data-item="${i}" aria-label="${L(itemNames[tab][i],itemZh[tab][i])} ${unlocked?L('owned','已拥有'):L('locked','未解锁')}">${tab===2&&i<2&&thumbnails[i]?`<img alt="${itemNames[tab][i]}" src="${thumbnails[i]}">`:`<i data-icon="${categories[tab][4]}"></i>`}<b>${L(itemNames[tab][i],itemZh[tab][i])}</b><small>${i===selectedItems[tab]?L('IN USE','使用中'):unlocked?L('OWNED','已拥有'):tab===0?L('DRAW','抽奖获取'):L('CLEAR LEVELS','通关获取')}</small></button>`;}).join('')}</div>`;}
if(document.body.classList.contains('component-page')){
 languagePreference='en';
 document.getElementById('component-grid').innerHTML=categories.map((c,t)=>`<section class="component-group"><h2>${c[0]}</h2><p class="source-note">${c[2]}</p>${collectionCards(t,4)}<p class="source-note">${t===0?'Five combat slots':'Independent cosmetic selection'}</p></section>`).join('');icons();
}else{
 let screen='home',tab=0,variant=0,resultLevel=3,toastTimer=0,previousFocus=null,modalKind='';
 const q=s=>document.querySelector(s),modal=q('#modal');
 const homeView=new BoatView(q('#home-boat')),boardView=new BoatView(q('#board'),'board'),resultView=new BoatView(q('#result-boat'));
 homeView.scale=2.30;
 const media=matchMedia('(prefers-reduced-motion: reduce)');
 const thumbs=[];const thumbCanvas=document.createElement('canvas');Object.assign(thumbCanvas.style,{width:'220px',height:'180px',position:'fixed',left:'-1000px'});document.body.append(thumbCanvas);const tv=new BoatView(thumbCanvas);for(let i=0;i<2;i++){tv.variant=i;tv.render();thumbs.push(thumbCanvas.toDataURL());}thumbCanvas.remove();tv.gl.getExtension('WEBGL_lose_context')?.loseContext();
 const originalLevel=window.STUDY_LEVEL;const deadlockLevel={width:14,height:18,ships:[{x:5,y:6,d:'Up'},{x:5,y:8,d:'Right'},{x:7,y:7,d:'Down'},{x:6,y:6,d:'Left'},{x:3,y:6,d:'Right'},{x:6,y:4,d:'Up'}].map((s,i)=>({id:'D'+i,length:2,position:{x:s.x,y:s.y},direction:s.d}))};
 function applyLanguage(){
 document.documentElement.lang=resolvedLanguage()==='zh'?'zh-CN':'en';
 const labels=[['.left .feature:nth-child(1) b','Collection','舰队图鉴'],['.left .feature:nth-child(2) b','Skins','皮肤抽奖'],['.left .feature:nth-child(3) b','Supplies','道具补给'],['.right .feature:nth-child(1) b','Daily Gift','每日奖励'],['.right .feature:nth-child(2) b','Invite','邀请有礼'],['.right .feature:nth-child(3) b','Rankings','排行榜'],['.launch strong','Continue','继续挑战'],['.victory h1','VICTORY','挑战成功'],['.target-copy strong','First Light','晨光水纹'],['.result-actions [data-action="nextlevel"] strong','Next Level','下一关'],['.ship-selector div>small','SHOWCASE SHIP','主页展示船'],['.level-label','LEVEL 3','第3关'],['.boss b','DEEP SEA GUARDIAN','深海守卫'],['.fleet-row span','FLEET ASSEMBLY','舰队集结区'],['.victory p','A little closer to your next reward','距离下一份奖励更近一步'],['.reward-coins>span','First clear 100 · Battle 80','首通100 · 战斗80'],['.target-copy>small','YOUR NEXT REWARD','下一份通关奖励'],['.result-actions [data-action="home"] span','Home','主页'],['.result-actions [data-action="share"] span','Share','分享'],['.deadlock','All boats are blocked. Try a tool!','船只完全堵住了，试试道具吧']];
 labels.forEach(([selector,en,zh])=>q(selector).textContent=L(en,zh));
 q('.launch>span').textContent=L('LEVEL 3','第3关');q('.tools>small').innerHTML=L('TOOLS THIS RUN','本次道具额度')+' <b>5 / 5</b>';
 ['Rescue','Shuffle','Flip'].forEach((en,i)=>q('.tools>div').children[i].querySelector('b').textContent=L(en,['救援','洗牌','反转'][i]));
 q('.victory>small').textContent=L('LEVEL '+resultLevel,'第'+resultLevel+'关');
 q('.milestones').innerHTML=resolvedLanguage()==='en'?'<b>✓ START</b><b>5 · TRAIL</b><span>8 · SHOWCASE</span>':'<b>✓ 起航</b><b>5关 · 拖尾</b><span>8关 · 展示船</span>';
 document.querySelectorAll('.future>small').forEach(e=>e.textContent=L('SOON','后续开放'));
 q('#ship-name').textContent=L(...names[variant]);q('#reward-distance').textContent=resultLevel===3?L('2 more levels to unlock','再过2关获得'):L('Reward unlocked','奖励已解锁');
 const aria=[['#home','Home','主页'],['#game','Gameplay','局内'],['#result','Results','结算'],['#home-boat','Changeable 3D showcase ship','可更换的三维展示船'],['#board','Level 3: 80 boats','第3关真实80船布局'],['#result-boat','Victory showcase ship','胜利展示船'],['[data-action="settings"]','Settings','设置'],['[data-action="pause"]','Pause','暂停'],['[data-action="close"]','Close','关闭'],['[data-action="previous"]','Previous showcase ship','上一款展示船'],['[data-action="nextship"]','Next showcase ship','下一款展示船']];
 aria.forEach(([selector,en,zh])=>q(selector).setAttribute('aria-label',L(en,zh)));
 q('#toast').hidden=true;
 }
 function toast(en,zh=''){const t=q('#toast');t.textContent=L(en,zh);t.hidden=false;clearTimeout(toastTimer);toastTimer=setTimeout(()=>t.hidden=true,4200);}
 function setScreen(s){screen=s;['home','game','result'].forEach(x=>q('#'+x).hidden=x!==s);modal.hidden=true;modalKind='';document.body.dataset.screen=s;requestAnimationFrame(()=>{if(s==='game')boardView.render();else if(s==='result'){resultView.variant=variant;resultView.render();}else homeView.render();});}
 function ship(v){variant=(v+2)%2;homeView.variant=variant;q('#ship-name').textContent=L(...names[variant]);homeView.render();}
 function showPanel(title,body,kind='info'){if(modal.hidden)previousFocus=document.activeElement;modalKind=kind;modal.hidden=false;q('#modal-title').innerHTML=title;q('#modal-content').innerHTML=body;icons(modal);q('[data-action="close"]').focus();}
 function close(){modal.hidden=true;modalKind='';previousFocus?.focus();}
 function collection(){showPanel(L('Collection','舰队图鉴'),`<div class="tabs" role="tablist">${categories.map((c,i)=>`<button role="tab" aria-selected="${i===tab}" class="${i===tab?'selected':''}" data-tab="${i}">${L(c[0],c[1])}</button>`).join('')}</div><div class="collection-info"><b>${L(categories[tab][2],categories[tab][3])}</b><span>${tab===0?'2':'1'} / 6</span></div>${collectionCards(tab,6,thumbs)}<p class="source-note">${tab===0?L('Ship skins come from draws. Default and first-blue gifts remain available.','船只皮肤通过抽奖获取；保留默认与首蓝赠送。'):L('Clear levels to unlock. Select an owned item to use it.','通关获取；点击已拥有外观即可更换。')}</p>${tab===0?`<p class="slots-label">${L('COMBAT LOADOUT','出战5槽')}</p><div class="slots"><span>1</span><span>2</span><span>3</span><span>4</span><span>5</span></div>`:`<p class="source-note">${L('Separate from combat slots','不占用出战5槽')}</p>`}`,'collection');}
 function settings(){showPanel(L('Settings','设置'),`<label class="language-label" for="language">${L('Language','语言')}</label><select id="language"><option value="auto" ${languagePreference==='auto'?'selected':''}>${L('Automatic','自动')}</option><option value="en" ${languagePreference==='en'?'selected':''}>${L('English','英文')}</option><option value="zh" ${languagePreference==='zh'?'selected':''}>${L('Chinese','中文')}</option></select><p>${L('China region: Chinese. Other or unknown region: English. Your manual choice takes priority.','中国地区使用中文，其他地区或未识别时使用英文。手动选择优先并保存。')}</p><button class="plain" data-action="close">${L('Done','完成')}</button>`,'settings');}
 function result(level){resultLevel=level;q('.reward-progress>span').style.width=level===3?'60%':'100%';applyLanguage();setScreen('result');}
 document.addEventListener('change',e=>{if(e.target.id==='language'){languagePreference=e.target.value;try{localStorage.setItem('tidebound.b2.language',languagePreference);}catch(_){}applyLanguage();settings();q('#language').focus();}else if(e.target.id==='region'){detectedRegion=e.target.value;applyLanguage();if(modalKind==='settings')settings();else if(modalKind==='collection')collection();}});
 document.addEventListener('click',e=>{const b=e.target.closest('button');if(!b)return;if(b.dataset.review){q('.deadlock').hidden=true;q('#shuffle').classList.remove('pointed');window.STUDY_LEVEL=originalLevel;switch(b.dataset.review){case'collection':setScreen('home');collection();break;case'ten':result(10);break;case'result':result(3);break;case'deadlock':window.STUDY_LEVEL=deadlockLevel;setScreen('game');q('.deadlock').hidden=false;q('#shuffle').classList.add('pointed');break;default:setScreen(b.dataset.review);}return;}
 if(b.dataset.tab!==undefined){tab=Number(b.dataset.tab);collection();return;}if(b.dataset.item!==undefined){const i=Number(b.dataset.item);if(i===0||(tab===0&&i===1)){selectedItems[tab]=i;collection();toast('Selected for this preview','已在样片中选择');}else toast(tab===0?'Get this skin from draws':'Unlock by clearing levels',tab===0?'皮肤通过抽奖获取':'通过通关获取');return;}
 switch(b.dataset.action){case'previous':ship(variant-1);break;case'nextship':ship(variant+1);break;case'home':setScreen('home');break;case'play':window.STUDY_LEVEL=originalLevel;q('.deadlock').hidden=true;setScreen('game');break;case'close':close();break;case'collection':tab=0;collection();break;case'future':toast('More adventures ahead','后续开放');break;
 case'draw':showPanel(L('Skin Draw','皮肤抽奖'),`<p>${L('Collect ship skins and fill your five combat slots.','收集船只皮肤，搭配出战5槽。')}</p><p>${L('This visual study does not spend currency. Draws, pity and vouchers use the existing game rules.','本样片不扣币。抽取、保底与收藏券沿用现有工程规则。')}</p><button class="plain" data-action="collection">${L('View Collection','查看图鉴')}</button>`);break;
 case'supply':showPanel(L('Supplies','道具补给'),`<p>${L('Rescue · Shuffle · Flip','救援 · 洗牌 · 反转')}</p><p>${L('Supply prices and inventory follow the existing game. No purchases in this study.','补给使用既有金币商店与库存，本样片不执行购买。')}</p>`);break;
 case'pause':showPanel(L('Paused','已暂停'),`<div class="stack"><button class="primary" data-action="close"><strong>${L('Resume','继续')}</strong></button><button class="plain" data-action="home">${L('Home','返回主页')}</button></div><p>${L('Your current run will be kept.','保留当前挑战与道具额度。')}</p>`);break;
 case'settings':settings();break;
 case'nextlevel':if(resultLevel>=10&&!q('#next-available').checked){toast('More levels are on the way. Your rewards are saved.','后续关卡暂未开放，奖励已保存。');}else{toast('Next-level route available in this study.','目录衔接示意：下一关入口可用。');}break;
 case'share':showPanel(L('Share the Journey','分享航程'),`<div class="share-card"><b>${L('TIDEBOUND FLEET','潮汐舰队')}</b><p>${L('LEVEL '+resultLevel+' CLEARED','第'+resultLevel+'关挑战成功')}</p></div><p>${L('Share card preview. Platform sharing is not connected. No coin reward.','分享卡布局预览，未接平台，不发放金币。')}</p><button class="plain" data-action="close">${L('Back to Results','返回结算')}</button>`);break;
 case'tool':toast('Tool feedback sample','道具反馈样式预览');break;
 }
 });
 modal.addEventListener('keydown',e=>{if(e.key==='Escape'){close();return;}if(e.key==='Tab'){const list=[...modal.querySelectorAll('button,a,input,select')],first=list[0],last=list[list.length-1];if(e.shiftKey&&document.activeElement===first){e.preventDefault();last.focus();}else if(!e.shiftKey&&document.activeElement===last){e.preventDefault();first.focus();}}});
 q('#waves').addEventListener('change',e=>q('.ripples').hidden=!e.target.checked);
 let lastActive=false;function tick(t){const active=screen==='home'&&modal.hidden&&!document.hidden&&q('#motion').checked&&!media.matches;if(active)homeView.render(t,true);else if(lastActive)homeView.render(0,false);lastActive=active;requestAnimationFrame(tick);}icons();applyLanguage();ship(0);setScreen('home');requestAnimationFrame(tick);window.addEventListener('resize',()=>{if(screen==='game')boardView.render();else if(screen==='result')resultView.render();else homeView.render();});
 window.studyQA={homeView,boardView,resultView,get screen(){return screen;},get variant(){return variant;},get motionActive(){return lastActive;},get tab(){return tab;},get language(){return resolvedLanguage();},get preference(){return languagePreference;},get region(){return detectedRegion;},originalLevel,thumbs};
}
