"""Optional static figures for the C3 report; requires matplotlib, not used by the model."""
import argparse
import json
from pathlib import Path
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt
from matplotlib import font_manager
import numpy as np


def save_svg(figure, path):
    figure.savefig(path)
    # Matplotlib path data contains trailing spaces; normalize for repository checks.
    path.write_text("\n".join(line.rstrip() for line in path.read_text().splitlines())+"\n")


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('results')
    parser.add_argument('--out', required=True)
    parser.add_argument('--font', help='Optional local font with Chinese glyphs')
    args = parser.parse_args()
    if args.font:
        font_manager.fontManager.addfont(args.font)
        plt.rcParams['font.family'] = font_manager.FontProperties(fname=args.font).get_name()
    plt.rcParams.update({'axes.unicode_minus': False, 'font.size': 12, 'axes.spines.top': False,
                         'axes.spines.right': False, 'figure.facecolor': '#f6f8fa', 'axes.facecolor': '#f6f8fa'})
    data = json.loads(Path(args.results).read_text())
    out = Path(args.out)
    out.mkdir(parents=True, exist_ok=True)

    def get(name, level, metric):
        return data['cases'][name]['checkpoints'][str(level)][metric]

    def series(ax, names, level, metric, color, log=False):
        values = [get(n, level, metric) for n in names]
        mid = [v['p50'] for v in values]
        ax.bar(range(len(names)), mid, color=color, width=.62, zorder=3)
        ax.errorbar(range(len(names)), mid, yerr=([v['p50']-v['p10'] for v in values], [v['p90']-v['p50'] for v in values]),
                    fmt='none', ecolor='#233542', capsize=5, zorder=4)
        if log:
            ax.set_yscale('log')
        ax.grid(axis='y', alpha=.18, zorder=0)
        for i, m in enumerate(mid):
            ax.annotate(f'{m:,.0f}', (i, m), xytext=(14, 6), textcoords='offset points', ha='left', fontweight='bold')

    names = [n+'/balanced_single/0.3' for n in ('live', 'cap_only', 'prior_candidate', 'c3_candidate')]
    labels = ['现行基线', '仅首通封顶', '旧候选', 'C3候选']
    colors = ['#7c8c99', '#709fbc', '#5a83a0', '#087e82']
    fig, axes = plt.subplots(1, 2, figsize=(13.4, 5.8))
    series(axes[0], names, 30, 'skins', colors)
    axes[0].set_title('第30关收藏数量', loc='left', fontweight='bold', pad=16)
    axes[0].set_ylabel('非默认皮肤数（含免费蓝皮和兑换）')
    axes[0].set_ylim(0, 16)
    series(axes[1], names, 300, 'coins', colors, log=True)
    axes[1].set_title('第300关钱包余额', loc='left', fontweight='bold', pad=16)
    axes[1].set_ylabel('金币，对数刻度')
    axes[1].set_ylim(300, 2000000)
    for ax in axes:
        ax.set_xticks(range(4), labels)
    fig.suptitle('I5-C3  /  收入封顶与收藏节奏', x=.055, ha='left', fontsize=21, fontweight='bold')
    fig.text(.055, .02, '每方案1000个模型玩家；轻度需求0.3次/关、保留650金币、单抽。柱为中位数，误差线为P10-P90。\n假定最终完成300关；不代表真实留存或经济平衡已通过。候选参数未启用。', fontsize=10, color='#536473')
    fig.tight_layout(rect=(0, .10, 1, .9))
    fig.savefig(out/'EconomyOverview.png', dpi=170)
    save_svg(fig, out/'EconomyOverview.svg')
    plt.close(fig)

    policies = ['collector_single', 'balanced_single', 'balanced_ten', 'tool_first']
    labels = ['即时单抽\n不留备用金', '单抽\n预留650', '攒十连\n预留650', '道具优先\n预留1500']
    fig, axes = plt.subplots(1, 2, figsize=(13.4, 6.2))
    for need, offset, color, label in ((.3, -.18, '#087e82', '轻度需求 0.3/关'), (1.5, .18, '#c06c43', '高需求 1.5/关')):
        names = ['c3_candidate/'+p+'/'+str(need) for p in policies]
        for ax, metric in zip(axes, ['skins', 'unfunded']):
            values = [get(n, 30, metric) for n in names]
            mid = [v['p50'] for v in values]
            x = np.arange(4)+offset
            ax.bar(x, mid, width=.32, color=color, label=label, zorder=3)
            ax.errorbar(x, mid, yerr=([v['p50']-v['p10'] for v in values], [v['p90']-v['p50'] for v in values]),
                        fmt='none', ecolor='#233542', capsize=4, zorder=4)
            for xx, m in zip(x, mid):
                ax.annotate(f'{m:g}', (xx, m), xytext=(14, 6), textcoords='offset points', ha='left', fontsize=10)
    axes[0].set_title('第30关收藏数量', loc='left', fontweight='bold', pad=16)
    axes[0].set_ylim(0, 12)
    axes[1].set_title('前30关未被资源覆盖的道具请求', loc='left', fontweight='bold', pad=16)
    axes[1].set_ylabel('次数，不能解释为失败关数')
    for ax in axes:
        ax.set_xticks(range(4), labels)
        ax.grid(axis='y', alpha=.18, zorder=0)
        ax.legend(loc='upper right', frameon=False, fontsize=10)
    fig.suptitle('I5-C3  /  同一价格，不同行为', x=.055, ha='left', fontsize=21, fontweight='bold')
    fig.text(.055, .02, '每方案1000人；C3候选，未启用广告或礼包。柱为中位数，误差线为P10-P90。\n高需求且坚持攒十连的人群中，30关前只有21%完成过金币十连；不能以全人群中位数掩盖等待。', fontsize=10, color='#536473')
    fig.tight_layout(rect=(0, .10, 1, .9))
    fig.savefig(out/'BehaviorTradeoffs.png', dpi=170)
    save_svg(fig, out/'BehaviorTradeoffs.svg')
    plt.close(fig)


if __name__ == '__main__':
    main()
