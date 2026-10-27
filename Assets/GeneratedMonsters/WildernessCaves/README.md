# 野外山洞小怪

三套独立预制体位于 Assets/Prefabs/WildernessCaveMonsters。每只角色24帧（待机、移动、蓄力、攻击、受击、死亡各4帧），共享12帧孢石/破裂/孢雾特效。美术通过内置 ImageGen 生成，透明、Point、无压缩，Sprite资产在 Frames 中。

| 名称 | HP | 行为 |
|---|---|---|
| 苔壳穴蜥 CaveMossLizard |35|蓄力0.5秒，向前扑咬约2米，伤害10，扑空恢复0.8秒；检查悬崖与墙壁|
| 雾翼蛾 CaveMistMoth |22|蓄力0.6秒，锁定方向掠袭，伤害8；低空恢复1秒后返回巡游，不持续追踪|
| 裂岩喷囊 CaveRockSpitter |30|蓄力0.8秒，抛射孢石直击12；落地留下2秒孢雾，接触成功伤害5且每团只命中一次；休息1.5秒|

接入已有Character、J攻击、U/I剑技、PlayerSlide免伤、DamageFlash和MonsterHealthBar。飞蛾使用单独的血条高度，旧怪物默认高度不变。无额外接触伤害。死亡/禁用清理自产飞弹和孢雾。

SampleScene_15、16、17各放置一只，共9只。赤岩场景偏暖，霜雾场景偏冷。地面怪物通过真实地面射线落地，飞蛾出生时检查地形重叠。放置工具发现已有怪物分组后跳过，不重建地形；不修改新游戏入口或关卡出口。

编辑器：Tools > Monsters > Build Wilderness Cave Creatures；Place Wilderness Cave Creatures In Scenes 15-17。Play Cave 15/16/17 位于 Tools > Levels。

## ImageGen prompts

## CaveMossLizard
Production 2D side view pixel art sprite atlas for a woodland cave platformer matching Legacy Fantasy High Forest. Transparent background, no ground shadow, no text or labels or grid lines. EXACTLY 4 columns by 6 rows, equal rectangular cells, 24 complete separate sprites, consistent size and centered body, consistent baseline in each cell, generous transparent margins. ALL frames face RIGHT. Crisp pixel clusters, rich earthy palette, readable silhouette. 1024x1536 portrait sprite sheet. A small low quadruped cave lizard, moss coated gray stone plates on its back, small glowing turquoise mushrooms on tail, blunt friendly-dangerous reptile snout, NOT a bug. Row1 four idle breathing frames crouched; row2 four walking steps; row3 four telegraphed crouch preparing leap with bright eyes; row4 four horizontal pouncing biting frames, legs extend then land; row5 four hurt recoiling frames; row6 four death frames collapsing onto belly. Each cell same size creature, feet anchored at bottom center.

## CaveMistMoth
Production 2D side view pixel art sprite atlas for a woodland cave platformer matching Legacy Fantasy High Forest. Transparent background, no ground shadow, no text or labels or grid lines. EXACTLY 4 columns by 6 rows, equal rectangular cells, 24 complete separate sprites, consistent size and centered body, consistent baseline in each cell, generous transparent margins. ALL frames face RIGHT. Crisp pixel clusters, rich earthy palette, readable silhouette. 1024x1536 portrait sprite sheet. A small magical cave moth, wide tattered leaf-shaped gray blue wings, furry dark teal body, two antennae and luminous icy cyan abdomen, no humanoid features. Row1 four gentle hovering wingbeat frames; row2 four forward flying wingbeat frames; row3 four warning windup frames wings flare cyan brighter; row4 four angled forward swooping attack frames wings swept backwards; row5 four hurt flutter recoils; row6 four dying falling then curled wings frames. Keep torso at same cell center in all flight frames, equal wing span.

## CaveRockSpitter
Production 2D side view pixel art sprite atlas for a woodland cave platformer matching Legacy Fantasy High Forest. Transparent background, no ground shadow, no text or labels or grid lines. EXACTLY 4 columns by 6 rows, equal rectangular cells, 24 complete separate sprites, consistent size and centered body, consistent baseline in each cell, generous transparent margins. ALL frames face RIGHT. Crisp pixel clusters, rich earthy palette, readable silhouette. 1024x1536 portrait sprite sheet. A small squat stationary cave creature, round cracked gray brown rock shell held by four stubby dark legs, visible round translucent amber green fungal sac inside a central split and tiny glowing eyes. It is an animal shell creature rather than a plant or humanoid. Row1 four closed-shell idle breathing frames; row2 four subtle alert rocking frames feet stay planted; row3 four winding up sac inflates and glows increasingly bright; row4 four shell opens spits a round glowing mossy pebble then closes, pebble remains near mouth inside cell; row5 four hurt recoils; row6 four death deflates and stone shell crumbles. Grounded at bottom center in every cell.

## CaveSporeEffects
Production pixel art game VFX atlas transparent background. Exactly 4 columns x 3 rows equal cells, no text no borders. Woodland cave magic, teal moss and pale amber glows, crisp pixel clusters, side view. Row1 four rotating small mossy glowing round stone projectiles, each centered. Row2 four impact sprites: a small pebble splitting into moss, chips and spore particles, ground anchored. Row3 four low wide softly glowing cyan-green spore cloud frames spreading along ground, rounded puff with floating motes, only low knee height, grounded same baseline. Clearly separated complete effects each in its cell with ample transparent padding. 1024x768 landscape sheet.

