# BossScene_3–5 场景优化

2026-10-26，沿用前两关与沙漠／地牢竞技场的美术工作流，覆盖现有三份场景。

| 场景 | 新主题 | 配套细节 |
|---|---|---|
| BossScene_3 | 琥珀矿核 | 巨型琥珀晶簇、废弃矿架与深层矿穴；玄武岩晶石平台、琥珀岩封路柱 |
| BossScene_4 | 月瀑幽潭 | 天然钟乳岩拱、月光与层叠瀑布；湿苔石台、青脉岩柱 |
| BossScene_5 | 月纹茧丝巢 | 淡紫洞穴、远景月纹巢、空茧与丝幔；缚丝石台、茧丝封路柱 |

三套背景均为38×22世界单位，按各图实测地面基线标定，保持y=0主地面和y=2平台脚面。配套平台和封路柱为独立透明Sprite，不改变碰撞；背景中的水面、远景栈道和丝幔均为装饰。

旧背景、旧重复地砖、旧封路图素、世界标题仅隐藏渲染。BOSS本体素材、战斗脚本、伤害、动画、技能参数、镜头和所有碰撞配置保持原样。保存前通过逐场Collider2D快照及BOSS组件序列化比较。

CrystalbackHud、AzureSalamanderHud、MoonveilMothHud新增默认false的useTopLayout，仅本轮三场设为true，血条移至上方，避免覆盖玩家技能栏。

## 入口与素材

- 现有Tools > Boss > Play Third Boss、Play Wilderness Cave Boss、Play Moonveil Moth Boss菜单继续可用。
- RefreshCaveBossArenas.Apply可重新覆盖这三场的美术层，保存前会检查场景未保存改动以及碰撞/BOSS配置变化。
- Backups/BossScene_3_BeforeVisualV2.unity至BossScene_5_BeforeVisualV2.unity保存覆盖前版本。
- 原场景GUID和GameSceneSO不变，原入口、Addressables、检查点和重试继续使用原数据。
- 使用内置image_gen生成AmberCoreMine.png、MoonfallGrotto.png、MoonveilSilkNest.png、CaveArenaProps.png。已复制到项目内，Point、无压缩、无mipmap，六个道具Sprite保留原生透明度。完整最终提示词见Prompts.json。

## Unity验证

- 编译无新增错误；三场均实际进入Play Mode并检查最终画面，镜头稳定于(0,5,-10)，顶部无黑边。
- 三场均通过平台站立：玩家y=2.0405、生命100。
- 岩螈低雾在新湿苔石台下方时，台上玩家生命100。
- 蛾后落丝位于y=0.02，高0.55；玩家在新缚丝石台上保持生命100。
- 三只均通过击败、双门解除、完成面板和满血重试。
- 最终截图：Validation/AmberCoreMine.png、MoonfallGrottoFinal.png、MoonveilSilkNest.png。
- 检查场景Missing Script、相机/完成面板/重试引用和Addressable GUID。
- 验证后退出Play Mode，恢复原玩家检查点与原编辑场景。
- 本次为场景视觉优化，未进行目标平台打包或重调战斗难度。
