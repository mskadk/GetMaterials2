# GetMaterials2 项目功能分析与迁移记录

分析日期：2026-05-13  
更新日期：2026-05-14  
项目定位：Unity 本地策划用数据表可视化编辑器，当前核心用途是编辑科技树资源与导出 `Science.xlsx` 数据。

## 1. 项目概览

`GetMaterials2` 是一个基于 Unity 2023.2.15f1c1 的本地桌面编辑工具。它把策划 Excel 数据加载为场景中的科技节点，通过鼠标拖拽、框选、编辑面板、锚点和连线操作来修改科技树结构，最后导出新的科技表。

当前工程包含：

- 主编辑场景：`Assets/Scenes/SampleScene.unity`
- 实验/未完成生产列表场景：`Assets/Scenes/ProductionList.unity`
- 核心脚本：`Assets/Script`
- UI 与节点预制体：`Assets/Prefab`
- 打包产物：`EXEBUILD/test Get Materials 2.exe`
- 外部数据依赖：策划 Excel 表、FreeWorld 图标 `.bin` 与图片资源

工作区当前存在未提交改动和未跟踪文件，迁移前建议先冻结一次版本快照，避免把实验内容和稳定功能混在一起。

## 2. 运行环境与第三方依赖

Unity：

- Unity Editor：`2023.2.15f1c1`
- 使用 Legacy UGUI：`UnityEngine.UI`
- 使用 2D Physics、Tilemap、Grid、LineRenderer、TextMesh

Package 依赖：

- `com.unity.ugui`
- `com.unity.2d.sprite`
- `com.unity.2d.spriteshape`
- `com.unity.2d.tilemap.extras`
- `com.coplaydev.unity-mcp`

插件/DLL：

- `Assets/Plugins/EPPlus.dll`：读取与写入 `.xlsx`
- `Assets/I18N.dll`、`Assets/I18N.West.dll`：配合 Excel/编码环境
- DOTween 插件存在，但当前核心脚本未明显依赖

平台依赖：

- 截图复制到剪贴板只支持 Windows：`ClipboardHelper` 通过 `user32.dll`、`kernel32.dll`、`gdi32.dll` 写入 `CF_DIB`

## 3. 外部路径配置

配置入口是 `Assets/Settings/EditorConfig.asset`，类型为 `EditorConfig`。

当前配置：

- `spritePath`：`D:\work\manager\Assets WorkSpace\FreeWorld\sprite\`
- `excelPath`：`D:\work\manager\策划\项目企划\数据表\`
- `savePath`：`C:\Users\Administrator\Desktop\`

实际图标加载还依赖 `MyStatic.workSpacePath`：

- `D:/work/manager/Assets WorkSpace/FreeWorld/`
- 图标 `.bin` 默认在 `sprite/`
- 图片默认按 `.bin` 中记录路径从 `sprite/` 或 `spriteH/` 下加载

迁移时应把这些硬编码路径改为新框架的配置项，并提供路径校验、缺失资源提示和可视化配置入口。

## 4. 数据表与核心数据模型

### 4.0 新版 ID 类型约束

后续迁移到其他开发环境时，所有数据表中的 `ID` 字段都必须按 `string` 处理，而不是旧工程里的 `int`。

适用范围：

- 所有表的主键 `ID`
- 外键/引用字段，例如科技前置、解锁项引用、路径连接中的前置节点 ID
- 内存字典键，例如 `Dictionary<string, Science>`、`Dictionary<string, TechTreeItem>`
- UI 节点名称、连线名称、锚点选择 Key、命令系统中保存的节点引用 ID

兼容策略：

- 旧表中的纯数字 ID 可读取为字符串，例如 `1001` 读成 `"1001"`。
- 特殊值 `"-1"`、`"-2"` 继续作为字符串哨兵值保留。
- 不应再依赖“负数递减”生成新 ID；迁移后应提供字符串 ID 生成规则，例如临时前缀 `tmp_001` 或由策划输入。
- 排序、过滤、范围筛选如果仍要支持数字语义，应做“可选数字解析”，不能把 ID 类型重新收窄成整数。
- 若继续沿用当前文本字段格式，ID 不应包含 `|`、`:`、`,`、`_`、`->` 这些分隔符；如果业务需要这些字符，必须先设计转义或改用 JSON 等结构化格式。
- `ModuleId`、`SubType`、`IconColor`、坐标、时间等非 ID 字段可继续保持原类型。

当前 Unity 代码仍大量使用 `int`/`int.Parse`/`Dictionary<int, ...>`，这部分是迁移时必须改造的核心差异。

### 4.1 Science.xlsx

`ExcelManager.LoadScience` 从 `Science.xlsx` 的第一个工作表读取数据：

- 第 1 行：字段类型行，保存导出时会原样保留
- 第 2 行：字段名行，保存导出时会原样保留
- 第 3 行起：数据行
- 只加载 `SubType == 1` 的行作为科技树节点

列定义：

| 列 | 字段 | 含义 |
|---|---|---|
| A | ID | 科技 ID，新版必须按 string 读取和保存 |
| B | SubType | 子类型，当前只读取 1 |
| C | ModuleId | 图标帧 ID |
| D | IconScale | 节点图标缩放 |
| E | LineScale | 连线宽度 |
| F | Name | 科技名称 |
| G | Detail | 描述 |
| H | Detail_2 | 附加说明 |
| I | Building_unlock | 解锁建筑项 |
| J | NonBuilding_unlock | 解锁非建筑项 |
| K | HexGridX | 历史字段名，当前存世界坐标 X |
| L | HexGridY | 历史字段名，当前存世界坐标 Y |
| M | Pre_technology | 前置科技 ID 列表，新版每个 ID 都按 string |
| N | PathNode | 连线中间点与端点方向 |
| O | S_Materials | 消耗材料 |
| P | Time | 研究时间 |
| Q | IconColor | 图标颜色索引 |
| R | Trigger_technology | 触发科技 |
| S | Apply | 是否启用，空值或非 `FALSE` 视为 true |

导出逻辑：

- `DataManager.SaveData()` 调用 `ExcelManager.SaveScience`
- 保存到 `EditorConfig.savePath`
- 文件名格式：`Science(yyyy-MM-dd_HH.mm.ss).xlsx`
- 导出会新建工作簿并写入 19 列，不覆盖原表
- 坐标导出保留 3 位小数
- 导出时 `ID` 和所有 ID 引用字段必须写回字符串格式，避免 Excel 自动转数值造成前导零丢失

### 4.2 G_TechTreeItem.xlsx

`ExcelManager.LoadTechTreeitem` 从 `G_TechTreeItem.xlsx` 第一个工作表读取：

- 第 1 列：ID，新版必须按 string 读取和保存
- 第 3 列：Name
- 第 4 列：Desc
- 第 3 行起为数据

该表用于左侧科技解锁项列表和编辑面板内的解锁项预览。工具会统计解锁项被引用次数，重复引用显示红色，正常引用显示绿色。

### 4.3 Science 内存模型

`Science` 是核心领域对象，存放从 Excel 读入的全部科技字段。重要特征：

- 旧代码字典键是 `Science.Id` 的 int；迁移后字典键必须改为 string
- 节点对象持有字典中 `Science` 的引用，不是深拷贝
- 拖动节点、编辑面板输入、命令执行会直接修改内存中的 `Science`
- `After_technology` 是运行时构建的后继科技集合，不直接来自表格列
- `WorldPosition` 是对 `HexGridX/HexGridY` 的便捷封装

迁移时建议把数据模型和 UI 状态拆开：领域模型只存数据，编辑会话维护草稿和撤销历史，避免当前这种多处直接写同一对象带来的隐式副作用。`Science.Id`、`TechTreeItem.Id`、`After_technology`、`Pre_technology` 解析结果都应统一为 string，不要在服务层和 UI 层反复转换。

## 5. 路径与连线格式

### 5.1 前置科技字段

`Pre_technology` 格式：

- 无前置：`-1`
- 多前置：`1001|1002|1003`
- 特殊值 `-2` 被视为不画线的特殊前置

新版约束：分隔符仍可沿用 `|`，但每个 ID 片段按 string 处理。校验重点应从“是否为数字”改为“是否为空、是否包含非法分隔符、是否在当前科技字典中存在、是否和特殊哨兵值冲突”。当前代码中的正则 `Constants.RegexPatterns.PreTechnology` 与实际使用有不一致风险，并且仍带有数字 ID 假设，迁移时应废弃或重写。

### 5.2 PathNode 新格式

`PathNode` 用于描述某个前置节点到当前节点的连线路径。

新格式：

```text
preId,startDir:x,y_x,y:endDir|preId,startDir:x,y:endDir
```

示例：

```text
1001,r:120.0,300.0_160.0,300.0:l|1002,c::t
```

含义：

- `preId`：前置科技 ID，迁移后按 string 处理
- `startDir`：前置节点出线方向
- 中间段：若干世界坐标点，用 `_` 分隔
- `endDir`：当前节点入线方向

方向标记：

- `c`：中心
- `t`：上
- `b`：下
- `l`：左
- `r`：右

兼容旧格式：

```text
preId_x_y_x_y
```

旧格式会被解析为方向均为 `Center` 的连接。

迁移重点：

- 当前 `PathNode` 坐标是世界坐标，不是网格坐标
- `preId` 必须从 int 改为 string，解析器不能再用 `int.TryParse` 判断合法性
- 中间点序列在保存时使用 `F1` 格式，字段注释写“三位小数”但实现是 1 位小数
- 节点坐标本身保存为 3 位小数
- 删除节点时会从后继节点的 `Pre_technology` 和 `PathNode` 中剔除对应 ID

## 6. 启动流程

主流程在 `MainManager.Start()`：

1. `DataManager.Initialize(config)`：读取 Excel，建立科技和科技树项字典
2. 动态给 `MainManager` 添加 `InputManager`，初始化输入状态机
3. `UIManager.Instance.Initialize()`：初始化 UI、解锁项列表、按钮和下拉框
4. `NodeManager.Instance.InitializeNodes()`：把所有科技实例化为节点，并统计解锁项引用
5. 再触发一次 `EventCenter.TriggerDataLoaded()`

当前 `DataLoaded` 会触发两次：一次在 `DataManager.LoadAllData()` 内，另一次在 `MainManager.Start()` 末尾。迁移时可合并为一次明确的加载完成事件。

## 7. 主编辑器功能清单

### 7.1 科技节点显示

每个 `Science` 生成一个 `Node` 预制体实例：

- 节点名称是科技 ID
- 节点位置为 `Science.HexGridX/HexGridY`
- 上方文本显示 ID
- 下方文本显示名称
- 节点颜色由 `IconColor` 映射到 `EditorConfig` 的颜色
- 节点大小由 `IconScale` 控制
- 连线宽度由 `LineScale` 控制
- 图标由 `SpriteManager.Paint(gameObject, "Icon_Technology", 0, ModuleId)` 动态生成

节点图标不是静态 Sprite 引用，而是读取外部 `.bin` 动作组，按 `ModuleId` 作为帧号动态切片。缺资源时节点仍可显示基础形态，但图标缺失。

### 7.2 连接线显示

每个节点根据 `Pre_technology` 绘制从前置节点到当前节点的入边：

- 连线 GameObject 是当前节点的子对象
- 命名格式：`preId->targetId`
- 使用 `LineRenderer`
- 起点颜色取前置节点颜色
- 终点颜色取当前节点颜色
- 中间点来自 `PathNode`
- 首尾点可按方向吸附到节点的 `anc_top/anc_bottom/anc_left/anc_right` 子锚点

线条重建/刷新场景：

- 节点创建、删除、移动
- 编辑前置科技
- 编辑 PathNode
- 修改颜色、大小、图标
- 修改活动连接线端点方向

### 7.3 网格模式

`GridManager` 支持三种网格：

- `Hexagon`：Unity Grid 六边形布局，吸附到格子中心
- `Square`：矩形布局，按 `cellSize * 0.25` 步长吸附
- `Free`：自由模式，不吸附

`GridDrawer` 负责视觉绘制：

- 六边形网格
- 正方形网格
- 自由模式坐标轴和稀疏参考点
- 移动时辅助线
- 原点红色标记

当前节点坐标已经改为世界坐标，切换网格不会把数据重新映射到另一套网格坐标。

### 7.4 新建节点

入口：顶部 UI 按钮调用 `UIManager.BtnNewNode()`。

流程：

1. 实例化 `ghostNodePrefab`
2. `GhostNode` 跟随鼠标移动
3. 根据当前网格模式显示吸附位置
4. 左键确认创建，右键或 ESC 取消
5. 创建走 `CreateNodeCommand`

默认新节点数据：

- 旧代码中 ID 从 `-3` 开始向更小负数寻找空位；迁移后应改为 string 临时 ID 规则，例如 `tmp_001`
- `SubType = 1`
- `ModuleId = 0`
- `IconScale = 0.75`
- `LineScale = 4`
- 名称：`新科技`
- 描述：`描述`
- 备注：`备注`
- 解锁、前置、路径、材料、触发均为 `-1`
- `Time = 0.01`
- 颜色来自新节点颜色滑条

### 7.5 选择与框选

`SelectionManager` 统一管理节点和锚点选择。

支持：

- 左键点击节点单选
- `LeftShift + 左键` 多选/反选节点
- 空白处拖拽框选节点
- 框选时也会选择范围内的锚点
- 对未实例化锚点，框选会通过解析 `PathNode` 的坐标进行数学选中
- 多选时最后选中的节点作为主节点

选择效果：

- 节点显示选择边框
- 当前主节点的活动连接线高亮
- 锚点显示选中色

### 7.6 节点拖拽

拖拽行为在 `StateDrag`：

- 按下后超过 5 像素才算拖拽
- 支持多选节点一起移动
- 移动时根据网格模式吸附
- 直接更新 `Science.HexGridX/HexGridY`
- 同步更新自身入边终点
- 同步更新后继节点入边起点
- 保持未选中锚点的世界坐标不变
- 结束拖拽后生成 `BatchMoveCommand`，支持撤销

编辑面板打开时，拖动当前节点会同步面板坐标字段。

### 7.7 锚点编辑

锚点是连线中间点，对应 `PathNode` 中某条连接的 Waypoints。

功能：

- 编辑模式下双击当前节点附近的连接线可添加锚点
- 选中锚点后拖拽可移动锚点
- `Delete` 可删除选中锚点
- 支持框选多个锚点后批量删除
- 普通模式下按 `Space` 可显示/隐藏选中节点的锚点
- 锚点位置保存到 `PathNode`

锚点命令：

- `AddAnchorCommand`
- `DeleteAnchorCommand`
- `BatchDeleteAnchorCommand`

锚点索引从 1 开始，对应 LineRenderer 中跳过起点后的中间点位置。

### 7.8 活动连接线与方向编辑

对主选中节点，可通过数字键和方向键编辑当前活动连接线：

- `1` 到 `9`：切换主节点的第 N 条入边连接线
- 方向键：修改连接线起始方向
- `RightShift + 方向键`：修改连接线终止方向

修改后会更新 `PathNode` 中对应 `PathConnection` 的 `StartDirection` 或 `EndDirection`。

### 7.9 编辑面板

右键节点打开 `Panel` 编辑面板。面板直接编辑当前节点绑定的 `Science` 对象。

字段：

- ID
- SubType
- Name
- X/Y 世界坐标
- 图标颜色
- 图标与线条尺寸
- ModuleId
- Pre_technology
- PathNode
- Trigger_technology
- S_Materials
- Time
- Building_unlock
- NonBuilding_unlock
- Detail
- Detail_2
- Apply

行为：

- 改 ID 会更新字典键、节点名、连线名、后继节点前置字段、前置节点后继集合
- 改前置科技会更新新旧前置节点的 `After_technology`
- 改 PathNode 会解析校验并刷新线与锚点
- 改颜色会刷新自身和后继线条
- 改大小会刷新自身外观和线宽
- 改解锁项会更新左侧 TTI 引用计数
- 提交按钮当前只是关闭面板，没有额外保存动作
- 关闭面板不会回滚已经直接写入的改动

代码注释里提到希望支持编辑撤销/草稿，但当前没有实现完整面板级回滚。迁移时建议优先补齐“编辑会话草稿 + 提交/取消”的语义。

### 7.10 左侧科技树项列表

`UIManager.InitTTI()` 根据 `G_TechTreeItem.xlsx` 生成列表项。

功能：

- 显示 ID、名称、描述、引用次数
- 引用次数为 0：黑色
- 引用次数为 1：绿色
- 引用次数大于 1：红色，并输出重复引用错误
- 旧代码支持按数字 ID 区间过滤；迁移后应改为字符串搜索/前缀过滤，或仅在 ID 可解析为数字时启用区间过滤
- 支持切换列表显示/隐藏
- 支持“仅显示/隐藏已引用项”的过滤开关

注意：`ToggleTTIFilter()` 当前逻辑只处理 `t_times == "1"` 的项，对重复项或 0 次项的显示策略不完整。

### 7.11 导出与复制

导出：

- UI 按钮调用 `UIManager.SaveScience()`
- 按钮文本临时变为“导出中...”
- 后台线程执行 `DataManager.SaveData()`
- 成功后输出文件路径和耗时

复制数据：

- `DataManager.ScienceToClipBoard()` 可把科技数据以 Tab 分隔文本复制到系统剪贴板
- `InputManager.CopySelectedNodesToClipboard()` 存在但 Ctrl+C 快捷键被注释，当前可能没有 UI 绑定

截图：

- `F2` 截取完整科技树区域并复制到 Windows 剪贴板
- 根据所有 `Node` 计算包围盒，移动场景相机渲染到 `RenderTexture`
- 分辨率倍数默认 2
- 仅节点包围盒参与计算，极端情况下远离节点的锚点/线段可能被裁掉

### 7.12 镜头控制

`CameraEventControll`：

- 鼠标中键拖拽平移场景相机
- 滚轮缩放正交相机
- 缩放范围默认 `200` 到 `8000`

### 7.13 日志提示

`EventCenter` 提供全局事件，`TipText` 订阅日志事件：

- 普通日志：黑色
- 警告：黄色
- 错误：红色
- 显示 2 秒后逐渐淡出到 alpha 0.5

## 8. 快捷键汇总

| 快捷键/操作 | 功能 |
|---|---|
| 左键节点 | 选择节点 |
| LeftShift + 左键节点 | 多选/反选节点 |
| 左键空白拖拽 | 框选节点/锚点 |
| 右键节点 | 打开节点编辑面板 |
| 右键空白 | 清空选择并关闭编辑面板 |
| 左键拖拽节点 | 移动节点 |
| 左键拖拽锚点 | 移动锚点 |
| 编辑模式双击线段 | 添加锚点 |
| Delete | 删除选中锚点，或删除当前编辑节点 |
| Ctrl+Z | 撤销 |
| Ctrl+Y | 重做 |
| Space | 普通模式下显示/隐藏选中节点锚点 |
| KeypadPlus | 放大选中节点 |
| KeypadMinus | 缩小选中节点 |
| 1-9 | 切换主节点活动连接线 |
| 方向键 | 修改活动连接线起始方向 |
| RightShift + 方向键 | 修改活动连接线终止方向 |
| F2 | 截图到剪贴板 |
| `/` | Debug 输出所有科技数据 |
| `.` | Debug 输出鼠标下节点数据 |

## 9. 命令系统与撤销重做

`CommandManager` 使用命令模式：

- `undoStack`：`LinkedList<ICommand>`
- `redoStack`：`Stack<ICommand>`
- 最大撤销栈：50
- 新命令执行后清空 redo
- 每次变化触发 `OnCommandHistoryChanged`

已纳入命令系统的操作：

- 创建节点
- 删除节点
- 移动节点
- 添加锚点
- 删除锚点
- 批量移动
- 批量删除锚点
- 修改节点大小
- 批量修改节点大小

未完整纳入命令系统的操作：

- 编辑面板中改 ID、名称、颜色、图标、前置、PathNode、解锁项、材料、时间、描述、Apply
- 活动连接线方向修改
- TTI 过滤/UI 显示

迁移时建议统一所有数据变更入口，否则用户会遇到“拖拽可撤销，但面板改字段不可撤销”的不一致体验。

## 10. 模块职责清单

### 10.1 管理器

- `MainManager`：启动协调，持有 `EditorConfig`
- `DataManager`：Excel 加载、保存、字典访问、复制数据
- `NodeManager`：节点实例化、销毁、刷新位置
- `UIManager`：主 UI、TTI 列表、过滤、保存、选择框显示
- `UIReferences`：场景对象和 UI 引用集合
- `InputManager`：输入状态机、快捷键、面板打开、截图入口
- `SelectionManager`：节点/锚点选择、活动连接线
- `GridManager`：网格类型与吸附
- `EventCenter`：模块间事件
- `CommandManager`：撤销/重做
- `ScreenshotManager`：全图截图到剪贴板

### 10.2 输入状态

- `StateIdle`：普通等待状态，处理点击、右键、双击线段
- `StateDrag`：统一拖拽节点和锚点
- `StateBoxSelect`：框选状态
- `IInputState`：状态接口

### 10.3 命令

- `CreateNodeCommand`
- `DeleteNodeCommand`
- `MoveNodeCommand`
- `AddAnchorCommand`
- `DeleteAnchorCommand`
- `ChangeNodeScaleCommand`
- `BatchMoveCommand`
- `BatchDeleteAnchorCommand`
- `BatchChangeNodeScaleCommand`

### 10.4 数据与解析

- `Science`：科技数据
- `TechTreeItem`：科技树项数据
- `ExcelManager`：Excel 读写
- `MyExtensions`：ID 列表、PathNode、锚点方向、路径字符串变更工具；迁移后 ID 解析必须从 int 改为 string
- `Constants`：标签、UI 路径、文件名、颜色、缩放、正则
- `EditorConfig`：路径与颜色配置

### 10.5 资源加载

- `SpriteManager`：将 Cat 动作组渲染到世界节点或 UI
- `CatActionGroup`：加载 `.bin` 动作组与纹理
- `CatAction`、`CatFrame`、`CatModule`：解析动作、帧、模块
- `AssetsLoader`：读取外部文件或 StreamingAssets
- `JavaReader`：按 Java 大端格式读写二进制
- `TextureManager`、`TextureNode`、`TextureUtils`：纹理加载、缓存、引用计数
- `ResourceStatic`、`ResourceLoader`：旧资源工具，当前核心科技树路径使用较少

### 10.6 其他/实验

- `RecipeManager`：生产列表实验场景，当前只在 `Start()` 中绘制 `T_Materials` UI 图标
- `BtnReload`：按钮触发场景重载
- `Test`：测试脚本
- `Postext`：简单文本跟随/位置相关脚本，当前不属于核心流程
- `DrawerDraw.cs`：项目根 Assets 下有旧/实验脚本，未纳入主流程分析

## 11. 预制体与场景依赖

### 11.1 SampleScene.unity 核心装配

`Assets/Scenes/SampleScene.unity` 是当前科技树编辑器的核心工程场景。场景不是单纯承载 UI，它同时承担了依赖注入、预制体引用、相机配置、Grid/Tilemap 父节点、按钮事件绑定等装配职责。

根对象：

- `MainManager`：挂载 `MainManager`，引用 `Assets/Settings/EditorConfig.asset`
- `DataManager`：挂载 `DataManager`
- `UIManager`：挂载 `UIReferences`、`UIManager`、`NodeManager`
- `SelectionManager`：挂载 `SelectionManager`
- `Grid`：Unity `Grid`，子对象为 `Tilemap`
- `Canvas`：主 UI 根节点
- `CameraSence`：场景相机，挂载 `GridDrawer` 与 `CameraEventControll`
- `CameraUI`：UI 相机，主要用于选择框 UI 坐标转换
- `EventSystem`：UGUI 输入事件系统
- `Drawer`、`GameObject`、`postext`、`(0.,0)Point`、`科技等级背景条`：实验/辅助对象，迁移时需确认是否保留

`Canvas` 主要层级：

- `Panel/Panel_RightContent`：节点编辑面板实例化父节点
- `Panel/ScrollViewTechTreeItem/Viewport/Content`：科技树项列表内容父节点
- `Panel/PanelTTI`：TTI 过滤与显示控制区
- `Panel/tiptext`：日志提示文本
- `Panel_TopMainMenu/Panel`：新建、复制、导出、重载、网格类型、颜色滑条等顶部工具区
- `Panel_TopMainMenu/Panel_Command`：命令输入实验区，当前存在遗留按钮绑定
- `Drawer`、`Image`：选择框/绘制辅助相关 UI

`Grid` 层级：

- `Grid/Tilemap`：运行时所有科技节点实例化到这里，`NodeManager.CreateNodeObject` 使用它作为父对象

### 11.2 UIReferences 场景引用

`UIReferences` 挂在 `UIManager` GameObject 上，是主场景中最重要的手动拖拽引用集合。迁移时可把它视为旧工程的 UI 依赖清单。

已绑定引用：

| 字段 | 场景对象 |
|---|---|
| `toggleTTI` | `ToggleTTI` |
| `toggleTTIFilter` | `ToggleTTIFilter` |
| `ifFilterFrom` | `IfFilterFrom` |
| `textDao` | `textDao` |
| `ifFilterTo` | `IfFilterTo` |
| `btnFilterClear` | `BtnFilterClear` |
| `btnClipBoard` | `ButtonClipBoard` |
| `btnExport` | `ButtonExport` |
| `btnUndo` | `Btn_Undo` |
| `btnRedo` | `Btn_Redo` |
| `scrollViewTechTreeItem` | `ScrollViewTechTreeItem` |
| `newNodeColorSlider` | `NewNodeColorSlider` |
| `tipText` | `tiptext` |
| `dpMainPage` | 未绑定 |
| `dpGridType` | `DropdownGridType` |
| `canvas` | `Canvas` |
| `tilemap` | `Tilemap` |
| `grid` | `Grid` |
| `camSence` | `CameraSence` |
| `nodePrefab` | `Assets/Prefab/Node.prefab` |
| `ghostNodePrefab` | `Assets/Prefab/ghostNode.prefab` |
| `panelNodeEditPrefab` | `Assets/Prefab/Panel.prefab` |
| `techTreeItemTextPrefab` | `Assets/Prefab/TechTreeItemTextPrefab.prefab` |

`UIManager.selectionBox` 绑定到场景中的 `Image` 对象。`UpdateSelectionBox()` 还硬编码查找名为 `CameraUI` 的相机，迁移时应改成显式引用。

### 11.3 场景内脚本挂载

`SampleScene.unity` 中挂载的项目自定义脚本：

| GameObject | 脚本 | 职责 |
|---|---|---|
| `MainManager` | `MainManager` | 启动流程，读取 `EditorConfig` |
| `DataManager` | `DataManager` | 数据加载/保存单例 |
| `UIManager` | `UIReferences` | 主 UI 与场景引用集合 |
| `UIManager` | `UIManager` | UI 逻辑、过滤、导出、选择框 |
| `UIManager` | `NodeManager` | 节点实例化/销毁 |
| `SelectionManager` | `SelectionManager` | 节点/锚点选择状态 |
| `CameraSence` | `GridDrawer` | 网格与辅助线绘制 |
| `CameraSence` | `CameraEventControll` | 中键平移、滚轮缩放 |
| `tiptext` | `TipText` | 订阅事件中心日志并显示 |
| `ButtonReload` | `BtnReload` | 调用重载占位逻辑 |
| `ButtonExport` | `BtnReload` | 额外挂载了 `BtnReload`，但导出按钮点击事件实际指向 `UIManager.SaveScience` |
| `postext` | `Postext` | 辅助/实验脚本 |
| `Drawer` | `DrawerDraw` | 旧/辅助绘制脚本 |
| `GameObject` | `Test` | 测试脚本 |

注意：`InputManager`、`CommandManager`、`EventCenter` 并没有作为稳定场景对象完整装配。`InputManager` 会在 `MainManager.Start()` 中动态添加到 `MainManager` 上；`CommandManager` 和 `EventCenter` 会通过单例访问时自动创建，或依赖场景中已有对象。

### 11.4 场景 UI 事件绑定

场景内已配置的关键 UGUI 事件：

- `NewNodeColorSlider.onValueChanged` -> `UIManager.NewNodeColorControll`
- `ButtonReload.onClick` -> `MainManager.ReloadSheets`
- `BtnFilterClear.onClick` -> `UIManager.ClearFilter`
- `IfFilterFrom.onValueChanged/endEdit` -> `UIManager.UpdateTTIFilterMin`
- `IfFilterTo.onValueChanged/endEdit` -> `UIManager.UpdateTTIFilterMax`
- `ToggleTTI.onValueChanged` -> `UIManager.ToggleTTI`
- `ToggleTTIFilter.onValueChanged` -> `UIManager.ToggleTTIFilter`
- `ButtonClipBoard.onClick` -> `DataManager.ScienceToClipBoard`
- `BtnNewNode.onClick` -> `UIManager.BtnNewNode`
- `ButtonExport.onClick` -> `UIManager.SaveScience`
- `Button_Command.onClick` -> `MainManager.btnCommandClicked`

`Button_Command` 绑定的方法 `MainManager.btnCommandClicked` 在当前 `MainManager.cs` 中不存在，属于场景遗留绑定。迁移时应删除该实验 UI，或补齐对应功能后再保留。

### 11.5 EditorConfig.asset

`Assets/Settings/EditorConfig.asset` 是核心配置文件，同时被 `MainManager.config` 和配置资产自身的预制体引用使用。

当前字段：

- `spritePath`：外部 FreeWorld 精灵目录
- `excelPath`：策划数据表目录
- `savePath`：导出目录
- `colorRed`、`colorOrange`、`colorYellow`、`colorGreen`、`colorBlue`、`colorWhite`：节点颜色映射
- `nodePrefab`：`Assets/Prefab/Node.prefab`
- `ghostNodePrefab`：`Assets/Prefab/ghostNode.prefab`
- `editPanelPrefab`：`Assets/Prefab/Panel.prefab`

注意：当前运行主链路实际使用的预制体引用来自 `UIReferences`，`EditorConfig` 中的预制体引用是可选/冗余配置。迁移时建议只保留一个配置来源，避免 `UIReferences` 与 `EditorConfig` 指向不同预制体。

### 11.6 预制体结构

`Assets/Prefab` 内当前与科技树工具相关的预制体如下：

| 预制体 | 主要脚本/组件 | Tag/Layer | 迁移含义 |
|---|---|---|---|
| `Node.prefab` | `Node`、`SpriteRenderer`、`PolygonCollider2D` | `Node` / Default | 科技节点主体 |
| `ghostNode.prefab` | `GhostNode`、`SpriteRenderer` | `Node` / Default | 新建节点时的鼠标跟随虚影 |
| `line.prefab` | `LineRenderer` | `NodeLine` / Default | 科技节点间的可视连线 |
| `anchor.prefab` | `SpriteRenderer`、`PolygonCollider2D`、`text` 子对象 | `Anchor` / Default | 连线中间锚点 |
| `Panel.prefab` | `PanelScienceEdit`、UGUI 控件 | Untagged / UI | 节点编辑面板 |
| `TechTreeItemTextPrefab.prefab` | `TechTreeItemText`、4 个文本子对象 | Untagged / UI | 左侧 TTI 引用列表项 |
| `Node bak.prefab` | 与 `Node.prefab` 基本相同 | `Node` / Default | 备份/实验版节点，不在场景引用链上 |

`Node.prefab` 是最核心的视觉与交互契约。根对象名为 `Node`，挂载 `Node.cs`，字段引用：

- `linesPrefab` -> `Assets/Prefab/line.prefab`
- `anchorPrefab` -> `Assets/Prefab/anchor.prefab`
- `BorderSprite` -> 节点外框 Sprite
- `SelectingSprite` -> 选中态 Sprite
- `OriginalSprite` -> 普通态 Sprite

`Node.prefab` 的关键子对象命名和坐标：

| 子对象 | 用途 | 局部坐标/尺寸 |
|---|---|---|
| `anc_top` | 默认上方向连接端点 | `(0, 60, 0)`，scale `(7.5, 7.5, 7.5)` |
| `anc_bottom` | 默认下方向连接端点 | `(0, -60, 0)`，scale `(7.5, 7.5, 7.5)` |
| `anc_left` | 默认左方向连接端点 | `(-65, 0, 0)`，scale `(7.5, 7.5, 7.5)` |
| `anc_right` | 默认右方向连接端点 | `(250, 0, 0)`，scale `(7.5, 7.5, 7.5)` |
| `text_up` | 节点 ID 显示 | 局部坐标 `(96.2, -7.6, 0)`，TextMesh，默认文本含 `id:888888` |
| `text_down` | 节点名称显示 | 局部坐标 `(127.4, 20, 0)`，TextMesh，默认文本为科技名占位 |
| `Techonology_item_namebg` | 名称背景 | 局部坐标 `(132.3, 0, 0)` |

`Node bak.prefab` 也是节点备份，但锚点坐标不同：`anc_bottom` 为 `(0, -50, 0)`、`anc_top` 为 `(0, 55, 0)`、`anc_left` 为 `(-55, 0, 0)`、`anc_right` 为 `(250, 0, 0)`。迁移时应以 `Node.prefab` 为准，并把 `Node bak.prefab` 视作旧位置备份，除非策划确认要回退节点尺寸。

`line.prefab` 使用 Unity `LineRenderer`：

- 初始 positions 为 `(-0.5, 0, 0)` 到 `(0, 0, 1)`
- `widthMultiplier` 为 `0.05`
- `m_UseWorldSpace` 为 `1`
- 默认材质是 Unity 内置材质，并没有使用项目内 `LineMaterial.mat`
- 颜色渐变默认从红到绿，但运行时 `Node.DrawLine()` 会按节点颜色更新线渲染

`anchor.prefab` 是连线中间点：

- 根对象名 `anchor`，tag 为 `Anchor`
- 根 Transform 旋转 30 度，scale `(20, 20, 20)`
- SpriteRenderer 使用六边形样式 Sprite，颜色接近黄绿色
- `PolygonCollider2D` 是触发器，六边形 6 点路径
- 子对象 `text` 反向旋转 -30 度，用 `TextMesh` 显示锚点序号，默认文本 `9`

`ghostNode.prefab`：

- 根对象名 `ghostNode`，tag 也是 `Node`
- scale `(75, 75, 75)`
- 挂载 `GhostNode.cs`
- 没有 Collider，仅用于新建节点预览

`Panel.prefab` 是编辑面板，根 RectTransform 大小为 `510 x 400`，挂载 `PanelScienceEdit`。字段引用：

- `content` -> `sv_TTI/Viewport/Content`
- `TTITPrefab` -> `TechTreeItemTextPrefab.prefab`

`Panel.prefab` 的关键子对象命名必须与 `PanelScienceEdit.initComponentAndScience()` 保持一致：

- `id`、`subtype`、`name`
- `x`、`y`
- `dp_icon_color`
- `dp_icon&line`
- `moudleId`
- `pre`
- `pre_path`
- `trigger`
- `s_material`
- `time`
- `build_unlock`
- `unbuild_unlock`
- `detail`
- `detail2`
- `toggle_apply`
- `btn_submit`
- `btn_quit`
- `sv_TTI/Viewport/Content`

`Panel.prefab` 中已绑定的 UGUI 事件主要直接调用 `PanelScienceEdit`：

| 控件 | 绑定方法 |
|---|---|
| `id` | `UpdateId` |
| `name` | `UpdateName` |
| `pre` | `UpdatePre` |
| `pre_path` | `UpdatePrePath` |
| `dp_icon_color` | `UpdateNodeColor` |
| `dp_icon&line` | `UpdateNodeScale` |
| `moudleId` | `UpdateIcon` |
| `trigger` | `UpdateTrigger` |
| `s_material` | `UpdateMaterials` |
| `time` | `UpdateTime` |
| `build_unlock` | `UpdateUnlockBuilding` |
| `unbuild_unlock` | `UpdateUnlockNoBuilding` |
| `detail` | `UpdateDescribe` |
| `detail2` | `UpdateAdditionNote` |
| `btn_submit` | `SavePanel` |
| `btn_quit` | `DestoryPanel` |

`toggle_apply` 当前存在 Toggle 组件，但序列化事件目标为空，没有实际绑定 `UpdateApply`。这和 `PanelScienceEdit` 中按名称读取 `toggle_apply` 的逻辑不冲突，但说明 Apply 字段主要靠提交/初始化读写，而不是 Toggle 即时事件。

`TechTreeItemTextPrefab.prefab` 根对象大小为 `360 x 32`，挂载 `TechTreeItemText`，包含：

- `times`：引用次数文本，默认 `0`
- `id`：科技树项 ID 文本，默认 `88888`
- `name`：科技树项名称文本
- `desc`：科技树项描述文本

迁移时要把这些“按名字查找子节点”和“Inspector 绑定事件”的约定显式化，否则 UI/预制体改名会导致运行时空引用。

### 11.7 资源、材质与 Shader

项目内直接相关的视觉资源目录：

- `Assets/Sprite`：科技节点、锚点、边框、节点名称背景、工具栏图标等核心 Sprite
- `Assets/Pic`：与 `Assets/Sprite` 部分重复的六边形/边框图片，当前主链路未直接引用
- `Assets/FlexUnit/SpaceGameIconSet`：导入的太空图标资源包，含 Demo 场景和 Readme，当前不是科技树核心装配
- `Assets/SkymonIconPackFree`、`Assets/Starfield Skybox`：资源包内容，当前主科技树功能没有代码引用
- 外部 `EditorConfig.spritePath`：真正的节点图标帧数据来源，`SpriteManager.Paint()` 会读取该目录下的 `.bin` 与贴图资源

项目内 Shader/Material：

| 文件 | 作用/状态 |
|---|---|
| `Assets/Resources/Shader/FairyShader_Base.shader` | `ResourceStatic.share_Stand()` 使用，运行时动态 Sprite/图标材质依赖 |
| `Assets/Resources/Shader/ScienceIcon_Shader.shader` | 科技图标透明/染色 Shader，当前代码未直接检索到引用 |
| `Assets/Shaders/SciFiLine.shader` | `LineMaterial.mat` 使用的发光流动线 Shader |
| `Assets/Materials/LineMaterial.mat` | 使用 `Custom/SciFi_ProceduralLine`，但 `line.prefab` 当前没有引用它 |
| `Assets/Shaders/AutoFitLine.shader` | 未跟踪实验 Shader，几何着色器生成屏幕宽度线 |
| `Assets/Materials/AutoFitLineMaterial.mat` | 未跟踪实验材质，使用 `Custom/AutoFitLine` |

`ResourceLoader` 当前实际走 `Resources.Load`，Addressables 相关代码全部注释。`ResourceStatic` 会尝试加载 `Shader/FairyShader_Base`、字体 `Font/SourceHanSansCN-Light.otf`、若干旧路径纹理；这些字体/纹理未在当前核心资源清单中完整出现，说明 Cat 资源系统有历史遗留依赖。迁移时建议把科技树节点图标加载抽象成独立 `IconProvider`，并让缺资源时有明确占位图和错误提示。

### 11.8 ProjectSettings 与工程装配

工程层关键设置：

- Unity 版本：`2023.2.15f1c1`
- Product Name：`test Get Materials 2`
- Company Name：`DefaultCompany`
- 默认窗口尺寸：`1600 x 900`
- Bundle Version：`0.1`
- Standalone scripting backend：Mono
- Android scripting backend：IL2CPP
- API Compatibility Level：`.NET Standard 2.1`
- Active Input Handler：Both，新旧输入系统同时启用

`ProjectSettings/TagManager.asset` 中只定义了 3 个自定义 Tag：

- `NodeLine`
- `Node`
- `Anchor`

这些 Tag 参与选择、射线命中、清理对象等逻辑，迁移到非 Unity 框架时应改为明确的对象类型枚举，不要继续依赖字符串标签。

`ProjectSettings/EditorBuildSettings.asset` 只启用了 `Assets/Scenes/SampleScene.unity`。`Assets/Scenes/ProductionList.unity` 存在于工作区，但未加入 Build Settings，当前应视为实验/旁支场景。

工程包含 Addressables 配置，但当前核心代码没有启用 Addressables 加载；`ResourceLoader` 中 Addressables 代码也处于注释状态。因此迁移资源系统时不用优先复刻 Addressables，除非后续项目框架本身已经使用它。

### 11.9 旁支与未接入内容

`Assets/Scenes/ProductionList.unity` 和 `Assets/Script/My/ProductionList/RecipeManager.cs` 是未接入主科技树编辑器的旁支内容：

- 场景包含 `Canvas`、`EventSystem`、`Scroll View`、`input_id`、`input_name`、`input_desc`、`recipe`、`in`、`out`、`Image_001/002/003` 等 UI 对象
- `RecipeManager.Start()` 只调用 `SpriteManager.PaintUI(GameObject.Find("Image_001"), "T_Materials", 0, 0)`
- 目前没有发现配方数据表读写、保存、校验、撤销等完整编辑逻辑

结论：`ProductionList` 更像生产列表/配方编辑器原型。迁移科技树核心功能时可以暂不纳入；如果要保留，应单独设计生产配方的数据模型和 UI，而不是直接并入科技树图编辑流程。

`Assets/DrawerDraw.cs`、`Assets/Script/My/Test/Test.cs`、`postext`、`Drawer` 等属于测试/实验脚本或场景对象，主要用于验证 `SpriteManager.Paint/PaintUI` 和坐标显示，不构成当前稳定功能。

## 12. 已知风险与不一致点

1. 面板编辑多数操作不可撤销，且关闭面板不会取消改动。
2. `Science` 被节点和面板直接共享引用，副作用范围较大。
3. 外部路径硬编码，换机器容易缺 Excel 或缺图标。
4. `PathNode` 注释写三位小数，但序列化中间点实际是 `F1`。
5. `PreTechnology` 正则疑似不匹配实际 `-1` 和普通 ID 列表写法，且不适配新版 string ID，需要重新设计。
6. `DataLoaded` 事件启动时触发两次。
7. `UIManager.UpdateTTIShow` 的 `newNotFound += newNotFound + ...` 会重复拼接已有内容。
8. `ToggleTTIFilter` 只处理引用次数为 1 的项目，过滤语义不完整。
9. `Node.UpdateNodeStyle` 中机器名 `DESKTOP-0418DES` 会直接跳过图标加载。
10. `SpriteManager` 每次刷新节点会销毁并重建 `fw_icon`，大规模节点时可能有性能压力。
11. `ExcelManager.SaveScience` 在后台线程调用 EPPlus，同时读取 `TypeRow/NameRow/ScienceDict`，Unity 对象不在线程里访问但数据并发仍需注意。
12. 删除节点销毁 GameObject，撤销时重建；外部持有旧节点引用会失效。
13. 截图包围盒只看节点，不看远离节点的中间锚点。
14. 多个单例会在缺失时自动创建空 GameObject，迁移调试时可能掩盖场景配置错误。
15. `SelectionManager.Awake` 不防重，若场景中多个实例会覆盖 `Instance`。
16. `ProductionList` 目录和场景当前像是未完成实验功能，迁移时需确认是否保留。
17. 新版所有表 `ID` 都改为 string 后，旧代码中的 `int.Parse`、`Dictionary<int, ...>`、负数临时 ID、按数字区间过滤等逻辑都需要统一改造。
18. `SampleScene.unity` 中 `Button_Command` 仍绑定不存在的 `MainManager.btnCommandClicked`，属于遗留事件。
19. `EditorConfig` 与 `UIReferences` 都保存了预制体引用，当前运行主链路以 `UIReferences` 为准，迁移时需要合并配置来源。
20. `ButtonExport` 上额外挂载了 `BtnReload` 脚本，但按钮点击事件指向 `UIManager.SaveScience`，应确认该额外组件是否可删除。
21. `Panel.prefab` 高度依赖控件名称和 Inspector 事件绑定，迁移时如果先重做 UI，需要逐项核对每个字段的读写方法。
22. `Panel.prefab` 的 `toggle_apply` 事件目标为空，和代码中 `UpdateApply` 方法不一致，需确认 Apply 字段是否需要即时响应。
23. `line.prefab` 当前没有引用项目内 `LineMaterial.mat`，而材质/Shader 目录又存在发光线与自适应线实验资源，迁移时要确认最终线样式。
24. `ghostNode.prefab` 使用 `Node` tag 但没有 Collider，迁移选择/命中逻辑时不要把它当作真实节点参与数据操作。
25. `Node bak.prefab` 是未接入备份资源，且锚点坐标与 `Node.prefab` 不同。迁移资源时应避免误用备份节点。
26. `ProductionList.unity` 与 `RecipeManager.cs` 未进入 Build Settings，当前只是生产列表/配方方向原型，不能当作已完成功能迁移。
27. Addressables 配置存在但核心代码未启用，复刻工程配置时不要误判为运行时资源依赖。

## 13. 迁移建议

### 13.1 建议保留的领域能力

- Excel 双表加载：`Science.xlsx`、`G_TechTreeItem.xlsx`
- 科技节点可视化
- 世界坐标保存
- 前置科技关系自动维护
- PathNode 新旧格式兼容
- 多网格吸附模式
- 节点与锚点拖拽
- 连线方向编辑
- TTI 引用统计与重复检测
- 导出新 Excel
- 撤销/重做
- 截图到剪贴板

### 13.2 建议重构边界

新框架下建议拆为：

- `TechTreeDocument`：纯数据文档，包含所有 Science、TechTreeItem、脏标记、版本
- `TechTreeSerializer`：Excel/JSON/其他格式导入导出
- `TechTreeGraphService`：维护前置、后继、路径、ID 替换、删除修复
- `TechTreeCommandService`：所有可变更操作统一走命令
- `TechTreeViewModel`：选择、活动线、过滤器、面板草稿
- `TechTreeRenderer`：节点、连线、锚点渲染
- `ResourceIconProvider`：隐藏 Cat `.bin` 或未来图标系统差异
- `EditorConfigService`：路径、颜色、快捷键、网格配置

### 13.3 数据格式迁移优先级

1. 先固化所有表 `ID` 均为 string 的全局规则，避免后续模块继续引入 int ID。
2. 固化 `Science` 19 列字段映射。
3. 固化 `PathNode` 解析和序列化单元测试，尤其是 string `preId`。
4. 固化 `Pre_technology` 与 `After_technology` 自动构建规则，集合类型改为 string。
5. 建立导入后校验：缺前置、重复 ID、缺解锁项、PathNode 引用不存在前置。
6. 再迁移 UI 交互和渲染。

### 13.4 建议补充测试

最值得补测：

- `ParsePathConnections` 新旧格式互转
- 添加/删除锚点后索引和路径字符串正确
- 删除节点后后继节点 `Pre_technology/PathNode` 正确移除
- 撤销删除节点后所有关系恢复
- 修改 ID 后所有关联字段和线名更新
- 字符串 ID 测试：纯数字、带前导零、字母数字混合、临时 ID、特殊值 `"-1"`/`"-2"`
- 导出 Excel 列顺序与原表一致
- `Apply` 空值兼容逻辑
- `Panel.prefab` 字段名到编辑方法的绑定完整性
- `Node.prefab` 四方向锚点坐标与连线起终点一致性
- `ghostNode` 只参与新建预览，不进入选择、导出、撤销数据
- `LineRenderer` 在不同缩放级别下的宽度、排序和颜色表现
- 缺少外部图标、字体、Shader 时的占位与报错流程

## 14. 迁移实施顺序建议

1. 建立新环境下的纯数据模型和 Excel 读写，并先把所有表 ID 类型定为 string。
2. 迁移并测试 `MyExtensions` 的路径解析能力，把所有 ID 解析从 int 改为 string。
3. 迁移图结构服务：创建、删除、改 ID、改前置、改路径。
4. 迁移命令系统，并把所有数据修改都接入命令。
5. 迁移节点/线/锚点渲染。
6. 迁移输入交互：选择、拖拽、框选、快捷键。
7. 迁移编辑面板，并改成草稿提交模型。
8. 迁移 TTI 列表与重复检测。
9. 迁移外部图标加载，或替换为新框架资源系统。
10. 迁移导出、截图、配置面板。

## 15. 功能完成度判断

稳定核心：

- 科技表加载
- 科技节点显示
- 节点移动
- 连线与路径点编辑
- 节点创建/删除
- 基础撤销重做
- Excel 导出

半完成/需整理：

- 面板级撤销/取消
- TTI 过滤语义
- 数据校验体系
- 图标资源路径配置
- 生产列表编辑器
- 快捷键说明与 UI 提示

迁移时应优先保护数据格式和编辑行为，其次再重做 UI 表现。这个项目的核心价值不在 Unity 场景本身，而在“Excel 科技树数据与可视化图编辑行为之间的映射关系”。
