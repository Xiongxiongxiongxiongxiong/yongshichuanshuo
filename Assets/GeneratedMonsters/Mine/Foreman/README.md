# 晶镐监工（矿洞精英怪）

已放置于 `Assets/Scenes/SampleScene_8.unity` 的水晶圣所前宽平台（x=38），独立预制体：`Assets/Prefabs/MineMonsters/CrystalPickaxeForeman.prefab`。

- 生命120；普通挥镐16；重镐近身22；短程地面冲击波14；三连碎晶每枚10。
- 普通挥镐蓄力0.45秒；震地重镐蓄力0.9秒；三连投蓄力0.7秒、每0.32秒一枚。技能后收招约1.2秒。
- 巡逻、视线发现玩家、追击、返回巡逻范围、崖边/墙体检测。
- 两个技能蓄力及施放期间可受伤、闪白，但不被普通攻击打断；平时受击可击退和打断。
- 冲击波约5.5单位射程，沿地面移动，遇墙或断崖消失，可跳过或滑铲穿过。碎晶有抛物线，碰撞地形消失。
- 继承普通矿精的血条、白闪、伤害事件与玩家攻击/滑铲免伤系统。无阶段变化、召唤或 Boss 封门。
- 死亡动画播放一次，停留后销毁，并清理自身技能特效。
- 角色8排×4槽动画；蓄力排使用两个有效关键姿势并保持末帧，避免原图空格造成闪隐。特效3排×4帧：冲击波、碎晶、落地碎石。
- `BuildCrystalForeman.Build()` 刷新本精英资源与预制体；`Place()` 保留场景中已存在的精英实例。
- `ValidateCrystalForeman.Start()` 在 persistent 运行模式中执行临时回归验证，结果存于 Editor SessionState 的 `ForemanValidation`。

## ImageGen 生成提示词

使用内置 ImageGen，两个资源均设置 `transparent_background=true`，生成后复制到项目，保留原生成文件。

### 角色

Production 2D pixel art game animation SPRITE SHEET with TRUE transparent alpha background. EXACT 4 columns x 8 rows, 32 isolated frames, canvas 1024x2048, each cell 256x256, no borders text or labels. SAME ONE ELITE CAVE ENEMY in every cell: Crystal Pickaxe Foreman, a stocky living dark slate stone miner, battered heavy iron helmet with warm yellow headlamp, luminous cyan eyes, big amber crystals protruding from shoulders and back, leather belt, both hands wield one heavy iron mining pickaxe. A stronger version of a small stone miner imp, NOT a huge boss. Detailed hand-pixelled fantasy platformer sprites, clear dark outlines, crisp pixel clusters, matching woodland boar and cave monsters. Side view facing RIGHT, torso centered at x=128 in each cell, planted feet at cell y=225, character about 175 pixels tall, generous margin for pickaxe, consistent anatomy and size. Rows from TOP to BOTTOM: 1 idle breathing 4 poses; 2 heavy walking 4 poses; 3 ordinary forward pickaxe swing 4 poses; 4 slow charged slam windup raise pickaxe amber crystals brighten 4 poses; 5 powerful downward ground slam impact and recovery 4 poses; 6 throws small amber crystal chunks forward from hand, 4 sequential poses; 7 recoil from damage 4 poses; 8 defeated stone body crumbles into rubble 4 poses. No background lighting or floor, alpha transparency including all gaps. Full feet and weapons contained in their own cells.

### 技能特效

Production pixel art game VFX sprite sheet on TRUE transparent alpha background, 4 columns x 3 rows EXACTLY 12 isolated frames, canvas 1024x768, 256x256 uniform cells, NO labels no text no borders. Crisp pixel clusters, outlined warm amber and golden orange quartz stone magic matching a cave miner monster in a detailed 2D fantasy platformer. Top row: FOUR sequential animation frames of a LOW ground shockwave traveling RIGHT, horizontal crescent of sharp amber crystal shards erupting just above floor with grey stone dust, wave remains low and spans 160x70 pixels, aligned ground baseline cell y=210. Middle row: FOUR rotation frames of ONE small chunky glowing amber mineral projectile, centered x128 y128, around 90x90 pixels, readable rocky core with bright crystal facets and tiny gold sparks, no background glow rectangle. Bottom row: FOUR sequential frames of pickaxe hitting rocky ground: first sharp amber flash, then scattered stone chips and grey dust puff, then dispersing fragments, then faint last dust, grounded at y210. Every VFX stays fully within its own cell with ample transparent margins.

