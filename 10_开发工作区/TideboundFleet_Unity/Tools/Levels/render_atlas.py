#!/usr/bin/env python3
"""Render diagnostic layout sheets from canonical JSON (not Unity/device screenshots). Requires Pillow."""
import argparse
import json
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

parser = argparse.ArgumentParser()
parser.add_argument('count', type=int, choices=[30, 100])
parser.add_argument('output', type=Path)
args = parser.parse_args()
project = Path(__file__).resolve().parents[2]
base = project / 'Assets/Tidebound/Config'
folder = base / 'Levels/Campaign'
manifest = json.loads((folder / f'manifest-{args.count}.json').read_text())
args.output.mkdir(parents=True, exist_ok=True)
colors = {'Up': '#2189ac', 'Down': '#cf6547', 'Left': '#8c63ba', 'Right': '#499169'}
directions = {'Up': (0, 1), 'Down': (0, -1), 'Left': (-1, 0), 'Right': (1, 0)}
font = ImageFont.load_default()
for batch in range(0, args.count, 10):
    canvas = Image.new('RGB', (1460, 820), '#f4f1e9')
    draw = ImageDraw.Draw(canvas)
    for j, row in enumerate(manifest['levels'][batch:batch+10]):
        origin = base / 'LevelPrototypes/Phase5R_TenLevelCandidates' if row['number'] <= 10 else folder
        level = json.loads((origin / row['layoutFile']).read_text())
        x0, y0 = 20 + (j % 5) * 290, 52 + (j // 5) * 410
        draw.text((x0, y0-40), f"{row['number']:03d}  {row['structure']}", fill='#162d38', font=font)
        draw.text((x0, y0-24), row['pace'], fill='#576873', font=font)
        cell = 18
        draw.rectangle((x0, y0, x0+14*cell, y0+18*cell), fill='#e1e9e8')
        for ship in level['ships']:
            x, y = ship['position']['x'], ship['position']['y']
            dx, dy = directions[ship['direction']]
            points = [(x+i*dx, y+i*dy) for i in range(ship['length'])]
            rect = (x0+min(p[0] for p in points)*cell+2, y0+(17-max(p[1] for p in points))*cell+2,
                    x0+(max(p[0] for p in points)+1)*cell-2, y0+(18-min(p[1] for p in points))*cell-2)
            draw.rounded_rectangle(rect, radius=4, fill=colors[ship['direction']], outline='white' if ship['length']==3 else None, width=2)
            hx, hy = points[-1]
            cx, cy = x0+(hx+.5)*cell, y0+(17-hy+.5)*cell
            sx, sy = dx, -dy
            tip = (cx+sx*5, cy+sy*5)
            draw.line([(cx-sx*4, cy-sy*4),tip], fill='white', width=2)
            draw.line([(cx-sx*0+sy*3,cy-sy*0-sx*3),tip,(cx-sy*3,cy+sx*3)], fill='white',width=2)
        features=row['features']
        draw.text((x0,y0+331),f"exits {features['InitialExits']} | key +{features['MaximumInitialUnlock']} | bridges {features['BridgingLongShips']}",fill='#162d38',font=font)
    canvas.save(args.output / f'Layouts_{batch+1:03d}_{min(batch+10,args.count):03d}.png')
