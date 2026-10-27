# 岩脊洞熊 / Stoneback Cave Bear

野外山洞精英，100HP；普通横扫14；碎岩重拍蓄力0.8秒，近处20，前方岩波12，岩波最多移动3米，释放后喘息1.2秒。同一次重拍和岩波共享成功命中名单，不会叠加伤害。

半血及以下启用抱岩防守：锁定朝向1.5秒，正面减伤50%，背后完整伤害，随后0.6秒起身，防守开始后冷却8秒。普通受击可以打断普通攻击；重拍与防守期间只受伤闪白不打断；起身不再减伤。

新增可选 DirectionalDamageGuard，通过现有 Character.TakeDamage 应用减伤。无此组件的角色保持原伤害。复用现有血条、DamageFlash、J攻击与U/I技能、滑铲免伤。

Prefab: Assets/Prefabs/WildernessCaveMonsters/StonebackCaveBear.prefab

SampleScene_15 (16,6.05) 下层洞室，SampleScene_16 (70,24.16) 上层岩棚，SampleScene_17 (72,18.46) 远端岩拱。每处检查至少8米平地；第一只避开梯子落脚位置。保留现有地形、路线与怪物。三关使用青苔、暖砂岩、冷霜岩色调。

动画8行各4帧：待机、走路、横扫、重拍蓄力、重拍、防守、受击、死亡。美术原图中抬爪动作比其它行高，编辑器切片使用明确行边界保留完整手爪。两个特效行各4帧：岩波、拍地。Point/透明/无压缩，独立Sprite资产在Frames目录。

编辑器重建：Tools > Monsters > Build Stoneback Cave Bear；Place Stoneback Cave Bear In Caves 15-17。放置工具不会重复生成或重建地形。

## ImageGen creature prompt
Production pixel art animation sprite atlas for 2D side scrolling fantasy woodland cave game, matching Legacy Fantasy High Forest. A stout powerful quadruped cave bear with dark brown fur, large gray layered stone armor along shoulders and back, patches of moss and small glowing cyan mineral cracks, large forepaws, soft unarmored tan belly. Strong readable side profile, ALL frames face RIGHT. Transparent background. Exactly FOUR columns by EIGHT rows equal grid cells, 32 separate complete sprites, consistent scale and fixed hip center and grounded paw baseline, generous transparent margins, no text labels borders or floor shadows. Row1 four idle breathing frames on all fours. Row2 four slow walking steps. Row3 four forward front paw melee sweep attack frames. Row4 four windup frames gradually raising both forepaws high with glowing minerals. Row5 four heavy slam frames paws strike ground then recover. Row6 four defensive crouch frames head facing right forepaws planted armor shoulders raised, final frame brighter cracks as it starts rising. Row7 four hurt recoils. Row8 four death frames collapsing to lying on belly. Rich detailed crisp pixels earthy gray stone, moss green and brown fur. 1024x2048 portrait sheet.

## ImageGen effect prompt
Production pixel art VFX sprite atlas transparent background for 2D side scrolling woodland cave game. Exactly FOUR columns by TWO rows equal cells, eight complete separated effects. No text labels borders floor background or shadows. Row1 four frames of compact gray brown rocks and teal mineral fragments propelled to RIGHT along ground as a knee-high wave, mossy stones and dust, centered above grounded baseline, readable traveling low silhouette. Row2 four frames of large bear double-paw ground slam impact: rocks erupt and green-cyan crystal cracks flash then dust and fragments settle, anchored at same ground baseline. Both rows must have generous clear transparent margins in each cell. Crisp detailed pixel art matching Legacy Fantasy High Forest. 1024x512 landscape.

