# 沙漠与地牢敌人

8个独立预制体位于 Assets/Prefabs/DesertDungeonMonsters。复用 Character、MonsterHealthBar、DamageFlash 和现有玩家攻击/滑铲系统，不修改红发角色、武器图集、伤害数值或主城入口。

| Prefab | 名称 | HP | 行为 |
|---|---|---|---|
|RedtailScorpion|赤尾沙蝎|35|尾刺10，0.5秒预警，0.8秒恢复|
|SandJawWorm|潜沙颚虫|28|0.8秒预警后扑咬12，移动约2米，恢复1.2秒；潜伏可受击|
|ThornballCactus|棘球仙人掌|30|0.7秒预警，三刺散射每刺7，同轮最多命中一次|
|RustSkeleton|锈甲骷髅兵|40|0.5秒举剑预警，横斩12，恢复0.8秒，可打断|
|SoulLanternWraith|魂灯怨灵|24|0.8秒预警，魂火弹9，射程5米；不穿墙|
|ChainAmbushBeast|锁链伏兽|32|0.7秒预警后扑击11，约2.5米，恢复1秒；检查悬崖|
|SandPincerGuard|砂甲钳卫|110|夹击14；突进18，0.7秒蓄力、3.5米、撞墙眩晕1.3秒；半血扬沙每股10，同轮一次|
|SoulboundJailer|缚魂狱卒|120|横扫15；0.9秒重砸22+魂波12共享命中；半血魂链8，安全牵引最多1米|

普通怪6行×4帧，精英8行×4帧。两个技能特效图集各4行×4帧。砂甲钳卫额外使用 SandPincerCharge 四帧独立冲刺条，修复原图冲刺数量不足的问题。原始图保留；Sprite资产在Frames中，Point过滤、无压缩、透明背景。切片按每列透明间隔识别，保留较高的蓄力动作；骷髅和狱卒按下肢锚点对齐。

精英主技能冷却6秒、半血技能8秒；技能期间受伤仍扣血和闪白但不打断。攻击停止时有明确收招。仙人掌散射、扬沙三股和重砸/魂波共用每次施法成功命中名单，滑铲期间不消耗命中次数。

魂链仅在成功造成伤害后启用 PlayerSoulPull，0.4秒内最多拉1米。保留输入，使用CapsuleCollider2D.Cast检查墙体、前脚落点检测检查悬崖；攀爬、滑铲、死亡和场景切换时停止。空中没有落脚支持时不强制拖动，优先防止被拉入深坑。

SampleScene_19–21各放三只沙漠小怪和一只砂甲钳卫；SampleScene_22–24各放三只地牢小怪和一只缚魂狱卒，共24只。精英位置检查左右各4米平地。地形、梯子、宝箱和关卡出口不重建。

编辑器 Tools > Monsters > Build Desert And Dungeon Enemies；Place Desert And Dungeon Enemies In Scenes19-24。已有遭遇分组会跳过，避免覆盖手动调整。

Backups/PersistentBeforeValidation.unity 保存本次检查中出现的内存UI布局状态；原 persistent 场景未保存这些布局变动。

## ImageGen 提示词

### RedtailScorpion
Production crisp detailed pixel art sprite sheet for 2D side scrolling fantasy platformer, matches Legacy Fantasy High Forest pixel style. TRANSPARENT background, no text borders labels floor or drop shadows. Orange brown desert scorpion, layered sandstone shell and dark red curved tail sting, low squat body and two pincers. All frames face RIGHT, consistent body scale and body center, feet on same baseline inside each equal cell, generous clear margins for weapons and effects. Exactly FOUR columns and SIX rows, 24 separate complete sprites. Rows top to bottom: 1 idle, 2 moving or alert, 3 clear attack windup, 4 attack, 5 hurt recoil, 6 dying collapse. Four sequential frames each row. Keep every sprite fully inside its own cell. 1024x1536 portrait.

### SandJawWorm
Production crisp detailed pixel art sprite sheet for 2D side scrolling fantasy platformer, matches Legacy Fantasy High Forest pixel style. TRANSPARENT background, no text borders labels floor or drop shadows. Plump pale yellow burrowing sand worm, shovel-shaped mandibles, three dark armor plates. Idle row shows its recognizable head barely peeking from a small trembling sand mound, not invisible. All frames face RIGHT, consistent body scale and body center, feet on same baseline inside each equal cell, generous clear margins for weapons and effects. Exactly FOUR columns and SIX rows, 24 separate complete sprites. Rows top to bottom: 1 idle, 2 moving or alert, 3 clear attack windup, 4 attack, 5 hurt recoil, 6 dying collapse. Four sequential frames each row. Keep every sprite fully inside its own cell. 1024x1536 portrait.

### ThornballCactus
Production crisp detailed pixel art sprite sheet for 2D side scrolling fantasy platformer, matches Legacy Fantasy High Forest pixel style. TRANSPARENT background, no text borders labels floor or drop shadows. Squat dark green ball cactus creature with three ribs, orange red flower and tiny wooden feet. No humanoid arms. Idle stationary, flower closes and spines rise during windup, attack emits thorns. All frames face RIGHT, consistent body scale and body center, feet on same baseline inside each equal cell, generous clear margins for weapons and effects. Exactly FOUR columns and SIX rows, 24 separate complete sprites. Rows top to bottom: 1 idle, 2 moving or alert, 3 clear attack windup, 4 attack, 5 hurt recoil, 6 dying collapse. Four sequential frames each row. Keep every sprite fully inside its own cell. 1024x1536 portrait.

### RustSkeleton
Production crisp detailed pixel art sprite sheet for 2D side scrolling fantasy platformer, matches Legacy Fantasy High Forest pixel style. TRANSPARENT background, no text borders labels floor or drop shadows. Small rusty armored skeleton soldier carrying a chipped short sword, one blue soul-fire eye. Walk sword dragging, windup raises sword, attack forward sword slash. All frames face RIGHT, consistent body scale and body center, feet on same baseline inside each equal cell, generous clear margins for weapons and effects. Exactly FOUR columns and SIX rows, 24 separate complete sprites. Rows top to bottom: 1 idle, 2 moving or alert, 3 clear attack windup, 4 attack, 5 hurt recoil, 6 dying collapse. Four sequential frames each row. Keep every sprite fully inside its own cell. 1024x1536 portrait.

### SoulLanternWraith
Production crisp detailed pixel art sprite sheet for 2D side scrolling fantasy platformer, matches Legacy Fantasy High Forest pixel style. TRANSPARENT background, no text borders labels floor or drop shadows. Small floating haunted rusty lantern containing blue ghost flame, torn cloth and short dangling chains. Hover wingless. Windup blue lantern glow, attack releases a blue fireball. All frames face RIGHT, consistent body scale and body center, feet on same baseline inside each equal cell, generous clear margins for weapons and effects. Exactly FOUR columns and SIX rows, 24 separate complete sprites. Rows top to bottom: 1 idle, 2 moving or alert, 3 clear attack windup, 4 attack, 5 hurt recoil, 6 dying collapse. Four sequential frames each row. Keep every sprite fully inside its own cell. 1024x1536 portrait.

### ChainAmbushBeast
Production crisp detailed pixel art sprite sheet for 2D side scrolling fantasy platformer, matches Legacy Fantasy High Forest pixel style. TRANSPARENT background, no text borders labels floor or drop shadows. Small crouching gargoyle stone beast, four legs, rusty iron back ring and broken chains wrapped around limbs. Idle eyes dark, windup eyes red chains taut, attack horizontal ground pounce. All frames face RIGHT, consistent body scale and body center, feet on same baseline inside each equal cell, generous clear margins for weapons and effects. Exactly FOUR columns and SIX rows, 24 separate complete sprites. Rows top to bottom: 1 idle, 2 moving or alert, 3 clear attack windup, 4 attack, 5 hurt recoil, 6 dying collapse. Four sequential frames each row. Keep every sprite fully inside its own cell. 1024x1536 portrait.

### SandPincerGuard
Production crisp detailed pixel art sprite sheet for 2D side scrolling fantasy platformer, matches Legacy Fantasy High Forest pixel style. TRANSPARENT background, no text borders labels floor or drop shadows. Large stout desert beetle elite with layered sandstone armor, orange glowing shell cracks and two broad shovel pincers. Idle on six legs; melee pincers clamp; skill windup pincers dig ground; skill dash lowered body; second cast raises pincers to fan sand. All frames face RIGHT, consistent body scale and body center, feet on same baseline inside each equal cell, generous clear margins for weapons and effects. Exactly FOUR columns and EIGHT rows, 32 separate complete sprites. Rows top to bottom: 1 idle breathing, 2 walking, 3 basic melee attack, 4 main skill windup, 5 main skill attack, 6 second skill cast, 7 hurt recoil, 8 dying collapse. Four sequential frames each row. Keep every sprite fully inside its own cell. 1024x2048 portrait.

### SoulboundJailer
Production crisp detailed pixel art sprite sheet for 2D side scrolling fantasy platformer, matches Legacy Fantasy High Forest pixel style. TRANSPARENT background, no text borders labels floor or drop shadows. Tall bulky rusty armored dungeon jailer elite, faceless iron helmet, blue soul lantern embedded in chest, right arm carries chain-attached iron mace. Idle dragging mace; melee horizontal mace sweep; skill windup lifts mace; skill attack slams mace ground; second cast extends empty left hand casting a soul chain. All frames face RIGHT, consistent body scale and body center, feet on same baseline inside each equal cell, generous clear margins for weapons and effects. Exactly FOUR columns and EIGHT rows, 32 separate complete sprites. Rows top to bottom: 1 idle breathing, 2 walking, 3 basic melee attack, 4 main skill windup, 5 main skill attack, 6 second skill cast, 7 hurt recoil, 8 dying collapse. Four sequential frames each row. Keep every sprite fully inside its own cell. 1024x2048 portrait.

### SandPincerCharge
Use this exact sandstone armored beetle creature design, create a clean animation strip with EXACTLY FOUR separate charging beetle poses in a SINGLE ROW of FOUR equal cells, 1024x256 wide. Transparent background. ALL FACE RIGHT. One complete beetle entirely inside each cell with generous transparent margins. Same body scale and same centered hip and grounded foot baseline in all four cells. Lowered body with shovel pincers forwards, four sequential leg positions running fast. NO dust clouds, NO particles, NO additional rows, no text borders. Crisp pixel art matching the reference creature. Keep each complete head pincers and rear legs inside its own cell.
Reference: SandPincerGuard.png, built-in ImageGen edit.

### Combat effects
Production crisp pixel art VFX sprite sheet TRANSPARENT background, exactly FOUR columns FOUR rows equal cells, sixteen separate complete effects, no text labels borders ground or shadows, generous transparent padding, 1024x1024 square. For 2D side scrolling fantasy platformer matching Legacy Fantasy High Forest.
Desert: Row1 four long thin cactus thorn projectiles pointing RIGHT centered in cell. Row2 four LOW traveling sand surges flowing RIGHT along grounded baseline, small fan of dust. Row3 four grounded sand burst impact frames. Row4 four glowing orange dust warning pulses at ground.
Dungeon: Row1 four blue flame orb flight frames centered in cell. Row2 four LOW traveling soul waves flowing RIGHT along grounded baseline. Row3 four soul-chain projectile frames pointing RIGHT, bright hook at front, short connected links behind, centered in cell. Row4 four grounded blue magic impact frames.

