# 荆棘角卫

森林精英怪，已放置于 SampleScene_7 桥前 (26, 5.04)。Prefab: Assets/Prefabs/ThornAntlerGuard.prefab。

生命110；普通角击14；荆角突进蓄力0.7秒、冲刺约4米、伤害20，撞墙眩晕1.3秒。缠根锁定施法时玩家落点，地面预警0.9秒，命中伤害12并减速45%持续1秒。技能蓄力与施放期间受伤不会打断。普通受击、血条、死亡和滑铲免疫接入既有系统。

图集：ThornGuard.png（8行×4帧：待机、行走、角击、蓄力、突进、缠根、受击、死亡），ThornRoots.png（2行×4帧：预警、荆棘）。Point/无压缩，透明底。独立Sprite资产在Frames目录。重建入口 BuildThornAntlerGuard.Build；Place不会重复放置，不重建地形。

美术通过内置 ImageGen 生成，提示词如下。

## Creature prompt
Game production sprite atlas, transparent background. Pixel art 2D side view forest fantasy elite stag guardian, mossy brown bark armor, thorn wrapped antlers, luminous emerald chest rune, compact strong deer quadruped. Exactly 4 columns by 8 rows equal grid, each cell one complete creature facing RIGHT, fixed size, centered hips and consistent hoof baseline within every cell, no text no borders no shadows outside cell. 32 frames. Row1 idle breathing; row2 walking four steps; row3 antler melee sweep; row4 green antler glow charge windup; row5 low forward charging gallop; row6 stomp roots magic casting; row7 hurt recoil; row8 dying collapse. Consistent anatomy and scale every frame, generous transparent margins to avoid clipping antlers. Detailed crisp pixel art matching Legacy Fantasy High Forest, warm bark, lime moss, emerald magic. 1024x2048 sheet.

## Effect prompt
Transparent pixel art VFX sprite sheet, exactly 4 columns 2 rows equal cells, no text borders or background. 2D side view forest fantasy green thorn root magic. Row1 four frames: a low wide glowing emerald vine warning ellipse drawn on ground, thin tangled roots, pulsing increasingly bright, maximum height one quarter cell. Row2 four frames: thorn roots erupt upward from same ellipse, grow to knee height, peak then dissolve to green leaf particles. Each effect completely inside its own cell with transparent padding, consistent grounded baseline near bottom center. Crisp detailed pixel clusters matching Legacy Fantasy High Forest forest game. 1024x512.

