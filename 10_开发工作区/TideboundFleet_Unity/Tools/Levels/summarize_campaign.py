#!/usr/bin/env python3
"""Export a reviewable per-level catalogue from the certified assets; Python standard library only."""
import argparse
import json
import statistics
from pathlib import Path

p = argparse.ArgumentParser()
p.add_argument('count', type=int, choices=[30, 100])
p.add_argument('output', type=Path)
a = p.parse_args()
base = Path(__file__).resolve().parents[2] / 'Assets/Tidebound/Config'
folder = base / 'Levels/Campaign'
manifest = json.loads((folder / f'manifest-{a.count}.json').read_text())
families = dict(Legacy='原十关', Flow='连续释放', Layers='分层释放', Crossed='多链交错', KeyUnlock='关键船解锁', LinkedRegions='双区联动', LongBridge='长船桥接')
paces = dict(Relief='舒缓', Regular='常规', Challenge='挑战')
lines = [f'# 前{a.count}关实际内容目录', '', '由保存的JSON与证明提取。第1～10关保持原样；节奏是设计标签，未代表真人难度已验收。', '',
         '| 关 | 稳定ID | 结构重点 | 节奏 | 船／长船 | 初始直出 | 完整链深 | 三路径平均选择数¹ | 实际通关外观ID |',
         '|---:|---|---|---|---:|---:|---:|---:|---|']
groups = {}
for row in manifest['levels']:
    origin = base / 'LevelPrototypes/Phase5R_TenLevelCandidates' if row['number'] <= 10 else folder
    proof = json.loads((origin / row['proofFile']).read_text())
    metrics = proof['metrics']
    means = [trace['first60PercentMeanExits'] for trace in proof['releaseTraces']]
    mean = statistics.mean(means)
    if row['number'] > 10:
        groups.setdefault(row['pace'], []).extend(means)
    lines.append(f"| {row['number']} | {row['levelId']} | {families[row['structure']]} | {paces[row['pace']]} | {metrics['shipCount']}／{metrics['longShipCount']} | {metrics['initialExitCount']} | {metrics['completeDependencyDepthNodes']} | {mean:.2f} | {', '.join(row['rewards']) or '—'} |")
lines += ['', '¹ Solver升序、构造降序、固定种子出口顺序，分别取前60%步数的可直接驶出数量，再求三者均值。不是全体玩家路径分布。', '',
          '新增关卡按节奏分组：' + '；'.join(f'{paces[k]} {statistics.mean(v):.2f}' for k,v in groups.items()) + '。', '',
          '奖励节点从当前生产目录读取；金币、道具和首蓝继续遵守既有服务，没有新增数值规则。']
a.output.parent.mkdir(parents=True, exist_ok=True)
a.output.write_text('\n'.join(lines) + '\n')
