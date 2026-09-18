# Tidebound Fleet Phase 5 实施规格

版本：1.0

日期：2026-09-18

状态：自动化验收完成；真机触控、视觉可读性与最低设备性能待后续灰盒场景验收

## 1. 目标

Phase 5 先验证高密度方向疏通是否成立，不实现 Boss 战斗、皮肤经济或正式 UI。完成后，工程必须能加载、校验、显示和操作 12×18、80 艘船的灰盒关卡，并能用专用编辑器直接维护唯一 JSON。

## 2. 不变量

- 保留 Phase 1–4 已验证的 BoardModel、移动事务、出界语义、ExitSequence 和航道 FIFO。
- Grid 是移动与点击归属的唯一逻辑来源；Transform、Renderer、Collider 和模型尺寸不能决定占格。
- 每艘船仍沿自身固定方向移动；受阻停在阻挡前，完全离界后进入航道。
- Boss HP 只在开局按初始船数×10计算一次。
- 购买源码和旧停车逻辑不参与正式实现。

## 3. 数据迁移

### 3.1 JSON schema v2

关卡根字段保持：`schemaVersion`、`levelId`、`width`、`height`、`bossId`、`ships`。

每艘船字段：

```json
{
  "id": "S001",
  "typeId": "TF_BASE_SHIP",
  "length": 2,
  "position": { "x": 0, "y": 0 },
  "direction": "Right"
}
```

- `typeId` 在 MVP 固定为 `TF_BASE_SHIP`。
- `length` 是实例数据，只允许 2／3。
- JSON 不保存随机皮肤；运行时长度2使用 `TF_SKIN_DEFAULT`，长度3使用 `TF_LONG_DEFAULT`。
- `damage`、`state`、Boss HP 和视觉尺寸不得写进 JSON。

### 3.2 ScriptableObject

- `ShipConfigSO` 只保存 `typeId`、固定伤害和默认视觉引用，不再保存 length。
- `LevelConfigSO` 继续只引用 JSON、一个基础船配置和 Boss 配置。
- 旧四船型资源保留但不再被正式关卡引用，避免破坏历史资产。

## 4. 校验规则

- schemaVersion＝2。
- 棋盘最大12×18。
- 逻辑船型目录必须且只能包含一个 `TF_BASE_SHIP`，伤害固定10。
- 每艘 length 只能为2或3，完整占格必须在棋盘内，不得重叠。
- 总船数≤82；长度3船≤8且不超过总船数10%。
- 总占格≤172。
- 12×18正式棋盘至少保留44个空格。
- 第1关4×6、7艘和第2关12×18、80艘均须加载成功。

## 5. 显示与点击

- 使用安全区和屏幕纵横比计算 Boss、棋盘航道、道具三段比例。
- 正式棋盘单格由安全区净宽／12和净高／18两者较小值决定。
- 集中点击路由把世界坐标转换为 GridCell，再从当前 BoardModel 查询唯一 shipId。
- PointerDown 锁定船，PointerUp 仍命中同一 shipId 才提交；空格不吸附到邻船。
- 原 ShipMovementView 点击接口保留为兼容入口，但正式密集棋盘优先使用集中路由。

## 6. Level Studio V0

- 创建、打开、保存 schema v2 JSON。
- 编辑4×6至12×18网格。
- 放置／删除长度2和3的船，支持四方向笔刷。
- 实时显示船数、长船数、占用格、空格和空白率。
- 保存前调用运行时 BoardValidator；非法关卡禁止保存。
- 不包含通用求解器、Undo／Redo、批量关卡、自动生成和正式难度评分。

## 7. 测试与退出条件

- 所有既有 EditMode／PlayMode 测试迁移后通过。
- 新增 schema v2、单基础船、长度实例、容量上限、JSON往返和数据复制测试。
- 新增安全区布局和 Grid 世界坐标命中测试。
- `TestLevel_001`：4×6、7艘、Boss HP70。
- `TestLevel_002`：12×18、80艘、160占格、56空格、Boss HP800。
- Unity 无编译错误；110/110 EditMode 和 5/5 PlayMode 通过；文档和 Git 检查无错误。

真人点击命中率、真机帧率与最终视觉可读性需要后续灰盒场景和设备测试，本阶段自动测试不能替代。

## 8. 实施顺序

1. 数据与schema迁移。
2. BoardValidator容量规则。
3. 教学关和80船关夹具。
4. SafeArea布局计算与Grid点击路由。
5. Level Studio V0。
6. 自动测试、Unity导入与回归。
