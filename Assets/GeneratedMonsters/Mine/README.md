# 矿洞小怪资源与接入

三只小怪已各放置一只在 `Assets/Scenes/SampleScene_8.unity` 的 `Mine Creatures` 下。
可拖入其他关卡的独立预制体位于 `Assets/Prefabs/MineMonsters/`。

| 资源名 | 中文名 | HP | 伤害 | 行为 |
| --- | --- | --- | --- | --- |
| CrystalCrawler | 晶背爬虫 | 30 | 12 | 地面巡逻、视线发现玩家、晶体亮起蓄力、冲撞、撞墙/到崖边停止并眩晕 |
| LanternBat | 矿灯蝙蝠 | 18 | 8 | 倒挂待机、发现玩家后亮灯、锁定位置俯冲、返回原处 |
| PickaxeImp | 镐牙矿精 | 40 | 近战14 / 矿石10 | 短距离巡逻、近距离挥镐、远距离抛有抛物线的矿石 |

- 每张图为透明背景 1024×1536，4列×6排，共24帧；`Frames` 中为 Unity 可直接引用的 Sprite 资源。
- 动画排顺序：待机（蝙蝠倒挂）、移动/飞行、蓄力、攻击、受击、死亡。
- 伤害、无敌、滑铲免伤与玩家 J 键/技能沿用 `Character` 和 `Attack`；怪物层沿用项目已有的 `Enetmy`。
- 头顶血条复用 `MonsterHealthBar`，白闪复用 `DamageFlash`；受击打断攻击并击退，死亡动画单次播放后销毁。
- 普通伤害源的 `Attack` 组件保持禁用，由 AI 在明确的攻击窗口调用伤害；`Attack.TryHit` 显式拒绝禁用组件收到的 Unity 触发消息。
- 飞行怪在场景中的初始位置为返回点；地面怪的初始位置为巡逻中心。请将它们放在真实碰撞表面上，飞行怪放在有俯冲空间的洞顶下方。
- `BuildMineCreatures.Build()` 可以刷新本次资源及预制体；`PlaceInMine()` 仅在场景没有 `Mine Creatures` 根节点时放置三只，保留已有实例的人工编辑。
- `ValidateMineCreatures.Start()` 在 persistent 场景运行模式中进行真实 AI、双向 J 攻击、血条、闪白与死亡回归验证；测试对象只存在于运行模式，不保存到场景。

## 图像生成方式与提示词

使用内置 ImageGen，分别生成三张图，`transparent_background=true`。生成后复制到当前项目，原始生成文件保留。

### CrystalCrawler

A production game sprite sheet, transparent background, 4 columns x 6 rows exactly, 24 equally spaced cells, canvas 1024x1536, each cell 256x256. Same one small six-legged armored cave beetle in every frame, SIDE VIEW FACING RIGHT, brown segmented legs, dark slate blue carapace, clusters of luminous cyan and violet quartz crystals on its back, tiny amber eyes. Charming dangerous small enemy for a detailed 2D pixel-art fantasy platformer, crisp pixel clusters with dark outline and restrained shading, no gradients/background. Body occupies about 170x125 pixels of each cell, grounded feet at y=210 within each cell, consistent size and body center x=128, large padding no overlap. Rows top to bottom: 1 idle breathing 4 frames; 2 walking legs cycle 4 frames; 3 anticipatory brace and crystals light up 4 frames; 4 rightward charging low body pose cycle 4 frames; 5 recoiling from damage 4 frames; 6 collapsing then lying still broken crystals death 4 frames. Keep body anchored and feet aligned, no texts, no labels, no grid lines. Genuine alpha transparency.

### LanternBat

A production game sprite sheet, transparent background, 4 columns x 6 rows exactly, 24 equally spaced cells, canvas 1024x1536, each cell 256x256. Same one small cave bat in every frame, SIDE VIEW FACING RIGHT, charcoal and dusty indigo fur, leathery violet wings, little fangs, a warm yellow luminous gemstone on forehead like miner lamp, expressive amber eyes. Detailed 2D pixel-art fantasy platformer enemy, crisp pixel clusters dark outlines, same artistic family as brown forest boars and green moss insects, no backgrounds. Body center x=128 y=138 within every cell, full wingspan at most 210 pixels, consistent body size and ample padding. Rows top to bottom: 1 hanging upside down sleeping then blinking 4 frames, feet anchored at top center of cell; 2 hovering flight wing flap loop 4 frames; 3 waking and lamp shines as attack telegraph 4 frames; 4 diagonal rightward dive attack, body angled downward with folded wings 4 frames; 5 air hurt recoil 4 frames; 6 defeated bat tumbles then lies still wings folded 4 frames. No text no labels no grid lines, genuine alpha transparency.

### PickaxeImp

A production game sprite sheet, transparent background, 4 columns x 6 rows exactly, 24 equally spaced cells, canvas 1024x1536, each cell 256x256. Same one squat tiny living-stone miner imp in every frame, SIDE VIEW FACING RIGHT, stocky dark grey rocky body with rusty iron ore flecks, tattered tan leather miner helmet with small golden lamp, turquoise eyes, one arm ends in an iron pickaxe, other hand holds a small ore chunk. Cute but dangerous small enemy for detailed 2D pixel-art forest-fantasy platformer, crisp pixel clusters, dark outline, restrained shading, not smooth vector. Character around 135 pixels tall, grounded feet baseline y=210 within each cell, body center x=128, consistent scale and padding for pickaxe swing. Rows top to bottom: 1 idle lamp flicker 4 frames; 2 short heavy walking cycle 4 frames; 3 raising pickaxe overhead and telegraph 4 frames; 4 overhead forward pickaxe swing ending in recovery 4 frames (also usable as ore throwing action); 5 damage recoil 4 frames; 6 crumbles into a small pile of stones death 4 frames. No text no labels no grid lines, genuine alpha transparency.
