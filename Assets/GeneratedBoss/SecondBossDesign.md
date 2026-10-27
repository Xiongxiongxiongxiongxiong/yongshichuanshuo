# 第二关：幽谷菌主 · 孢冠长老

## 定位与风格

延续第一关“古林守卫·苔角王”的森林像素风、深色轮廓、棕绿奶白配色与翡翠符文。第二关对应现有森林峡谷 SampleScene_8 的潮湿谷底，采用巨型蘑菇与树根融合的轮廓：橙褐色白斑菌冠、苔藓树皮躯干、粗短根足、琥珀眼睛和胸前符文。身体始终贴近地面，玩家普通攻击可以命中。

苔角王考验冲锋躲避；孢冠长老考验读预警、选择落脚点以及抓住施法后的空档。首次挑战只增加空间选择，不叠加毒素、减速或小怪。

## 战斗规格（初始调参值，需实战验证）

- 生命 800，受击无敌 0.16 秒，普通攻击闪白但不打断技能。
- 根据水平距离随机选招，允许重复但同一攻击最多连续两次；后撤不会连续使用。距离小于 4 时将近战普攻与后撤加入随机池；4～6 单位之间只选藤根或孢子；距离至少 6 时才将攻击跃砸加入随机池。近地近距离基础权重为普攻 50、藤根 20、孢子 15、后撤 15；有连续限制或高度不满足时按剩余权重抽取。
- 半血进入强化，仅强化后续招式；不打断正在展示的预警，不立即造成伤害。
- 单次完整招式最多成功伤害同一玩家一次；沿用现有角色无敌与滑铲规则。
- 不设计常驻接触伤害；施法后的近身输出窗口保持安全。

| 招式 | 预警与命中 | 应对 | 反击窗口 |
|---|---|---|---|
| 根臂横扫（近战普攻） | 0.6 秒举臂预警后朝面前挥击，伤害 15；有效判定持续至 0.82 秒，同一招只扣一次血。起手锁定朝向，范围为面前中心 2.1、宽 2.8、高 2 单位 | 后退、滑铲或绕到身后；不追踪转身，不影响整个平台上方 | 1.1 秒动作结束后再停顿 0.6 秒 |
| 藤根突刺 | 锁定玩家当时所在位置，三处主地面金色标记，间隔 3.2 单位；0.95 秒后根刺升起，伤害 15 | 离开标记或站上悬空平台；平台顶面不生成根刺，平台底面限制根刺显示和伤害高度，边缘也检测完整刺宽 | 根刺消失后 1.25 秒 |
| 孢子散射 | 菌冠压低发亮 1.5 秒，向玩家方向发射三枚扇形孢子，速度 4.5，伤害 15；3 秒或触墙消失 | 利用弹道间隙、跳跃或滑铲靠近 | 发射后低头喘息 1.5 秒 |
| 菌冠跃砸 | 仅距离至少 6 时可随机释放；蹲伏并锁定一个地面落点，金圈预警 1.1 秒后落地，范围宽 4.2、伤害 20 | 离开金圈；飞行阶段不重新追踪，不额外释放全屏波 | 落地后菌冠下垂 1.8 秒 |

近距离后撤：选招时玩家距 Boss 小于 4，低权重随机选择后撤，而非必定起跳。抽中后跳向距玩家约 8 单位的安全地面；靠墙空间不足时换向。后撤不产生伤害或危险金圈，落地停顿 1 秒。距离和落点均在起跳前确定。

### 半血强化

- 藤根由三处变五处，间隔保持 3.2；保留两侧逃生空间，预警仍为 0.95 秒。
- 孢子发射两轮，间隔 0.65 秒；第二轮前菌冠再次闪亮，不同时追加藤根。
- 跃砸伤害不变，范围宽度增至 5；预警仍为 1.1 秒，落地硬直增至 2 秒。
- 血条显示【盛放】，符文由翠绿转为金绿；不能仅靠颜色区分阶段和预警。

## 独立场地与流程

已制作场景为 Assets/Scenes/BossScene_2.unity，入口来自 SampleScene_8 的终点 (99,18)。地面 X=-18..18、Y=0；封锁 X=±15.5；玩家出生 (-11,0.05)，Boss 出生 (3,0.02)。两侧各保留一个矮平台，顶面 Y=2；中央不放障碍，保证根刺预警可见。

沿用 persistent 提供角色、主摄像机、HUD 和 SceneLoader。入场补满生命体力、保存本关入口、封锁两侧、启用战斗镜头。死亡和结算重试回到第二关 Boss 入口。Boss 死亡立即取消全部攻击与特效，播放非循环倒地动画，1.8 秒后解除封锁，再延迟 0.65 秒显示结算。第一关原场景与数据保持独立。

## 素材清单和动画约定

- SecondBoss/SporeCrownElder.png：1024×1536 透明背景，4 列 × 8 排，32 帧。每排依次为待机、移动、藤根施法、孢子施法、跃砸、阶段施法、疲惫、死亡。第二排已接入招式间隙的实际行走；第六排用于入场施法。
- SecondBoss/SporeCrownEffects.png：1254×1254 透明背景，4 列 × 4 排，16 帧。每排依次为根刺、飞行孢子、落点预警、落地爆发。生成图四排高度不等，搭建器采用实测行边界，不能直接等高切割。
- SecondBoss/SporeCrownMelee.png：1536×1024，新增根臂横扫透明动画表，4 列 × 2 排、8 帧，依次准备、收臂、举臂、挥出、命中、跟随、收回、复位。ImportSporeMelee.Import() 按现有待机高度统一比例、逐帧校准脚底锚点并接入场景，独立保存源图与帧资产。
- 所有角色帧面向右侧，运行时翻转；同一比例、脚底锚点校准，死亡最后一帧定格。
- 导入 Point、Alpha、无 mipmap、无压缩。逐格检查透明边缘、裁切与动作基线后才能投入使用。
- 伤害由战斗逻辑控制，禁止依据图片中的发光区域直接判伤。

## 验收清单

2026-10-16，Unity 2022.3.20f1c1 编辑器与 Play Mode 验证：

- 素材透明通道、32 个角色 Sprite 与 16 个特效 Sprite 已导入，Point、无压缩、无 mipmap；以可见轮廓校准脚底。实机截图为 SecondBoss/Validation/SporeCrownEncounter.png。
- 从 persistent 启动，经峡谷终点真实触发器加载 BossScene_2；双方封锁激活，战斗镜头优先级 50，入口存档 ID 为 SporeCrownEntrance。
- 受控招式验证：普通根刺 3、强化根刺 5；强化孢子首轮 3、总计 6；跃砸锁定后移动玩家，目标不改变。
- 普攻生命 800→790；单次招式重复命中玩家 100→85→85；角色无敌时不扣血。滑铲使用现有 PlayerSlide.IsSliding 检查，未做人工按键手感测试。
- 致死攻击后立即进入 Dead、特效列表清空、封锁仍在且未结算；随后 Defeated、封锁解除、结算打开且 Time.timeScale=0。
- 结算 Retry 与实际角色死亡后都重新加载 BossScene_2，Boss 恢复 800 血。
- 退出再重新进入 Play Mode，新的 SceneLoader 成功识别第二关入口存档，ContinueGame 实际加载 BossScene_2；首关引用仍为 Game BossScene1。测试后恢复原存档。
- 初次编译无脚本错误；Console 存在测试前已出现的 GameUIFont 动态字体纹理报错，非本次新增。

仍需人工实战调节整体难度、滑铲节奏和边缘平台走位；未执行独立平台打包。部分招式验证通过编辑器受控调用完成，不等同于完整玩家实战。

### 本轮行为调整验证

- 修复前射线命中右平台顶面 Y=2，确认此前平台顶面生成根刺的原因。改为仅在主地面附近向下寻找落点。
- 受控计时 1.49/1.5/2.14/2.15 秒，强化孢子已发射轮数依次为 0/1/1/2。
- 远距离随机选招采样 40 次出现三种攻击，未固定循环；中距离采样 30 次无攻击跃砸。测试后还原 Unity 随机状态。
- 近距离后撤落点距玩家 8 单位，靠墙时换边后仍距玩家 8 单位；将玩家移至后撤落点，生命保持 100。
- 玩家站在 X=10、Y=2.05 的平台上，三个根刺都从 Y=0 生成，平台下根刺高度及画面顶部限制为约 1.568，小于平台底面 Y=1.65；玩家生命保持 100。同一位置移回地面后生命变为 85。
- X=8.4 和 X=11.6 的刺宽与平台边缘相交时，也按平台底面限制高度。

### 近战与后撤随机化验证

- 修复前 ChooseAttack() 在距离小于 4 时无条件 BeginLeap(true) 并返回，绕过随机池；现改为带权重的统一随机选招，且连续后撤权重为零。
- 固定随机种子受控采样 100 次近距离选招：普攻 43、藤根 23、孢子 17、后撤 17，无连续后撤。测试后还原随机状态。
- 近战 0.59 秒时玩家 100 血；0.6 秒命中后 85 血，0.7 秒再次判定仍 85。背后、范围外与无敌状态保持 100；向左挥击也能正确扣至 85。
- 实际 PlayerSlide.TryStartSlide() 启动成功时，普攻击中判定后生命保持 100。场景运行时 8 个近战 Sprite 引用完整、Point 导入，挥击状态使用 SporeCrownMelee_3/4；实机画面记录于 SecondBoss/Validation/SporeCrownMelee.png。

近战素材提示词：Use the existing Spore Crown Elder as design reference; create eight sequential close-range rightward root-arm swipe sprites, four columns by two rows, neutral/draw back/raise fist/begin swing/full punch/follow through/retract/neutral; identical mushroom cap, moss roots, amber eye and jade rune, chunky forest pixel art, transparent alpha, consistent body scale and foot baseline. All complete sprites and orange swipe trails must fit in the central 70% of each cell with clear transparent gutters. No scenery, text or grid. 使用内置 image_gen 生成，再以 Unity Sprite API 切帧。

### 行走行为与动画

- 招式间隙以每秒 1.8 单位缓步接近玩家，水平距离 3 单位时停步；距离较远时行走间隙随机为 1.8～2.6 秒，近距离为 0.9～1.4 秒，之后仍按原权重随机选招。
- 实际移动时以 8 FPS 播放第二排四帧行走，按方向翻转；停止、受阻、攻击蓄力和收招时停止行走动画。原有半血、近战及后撤随机规则保留。
- 运动在 FixedUpdate 通过 Rigidbody2D.MovePosition 执行；对完整碰撞盒检查平台侧面、前方地面和场地边界，停止时清除速度。
- Play Mode 受控验证：玩家 X=7、Boss 从 X=0 走到 X=4 停下，距离为 3；向左追近时 X=-0.756，正在播放 SporeCrownElder_1_3，flipX=true。
- 右平台侧面 X=8.5，Boss 右边缘停在 X=8.445，没有穿入平台；进入孢子蓄力后位置变化为 0，显示第三技能排动画。
- 跃砸后与平台外侧轻微重叠时，允许朝外脱离；复测从 X=13.1 走至 X=13.295，右边缘 X=15.045，仍在场地封锁以内。

## 实现入口

SporeCrownBoss.cs 管理状态、预警、伤害去重和结算；SporeCrownEffect.cs 管理根刺/孢子等临时特效；SporeCrownHud.cs 沿用首关血条样式。BuildSporeBossLevel.Build() 为一次性搭建器，已有关卡时会保留现有内容。SceneLoader 增加第二关存档场景引用，保留首关引用和旧存档 ID。场景注册在 Scenes Addressables 组，地址 BossScene_2；正式启动使用 persistent。

## 生成方式与提示词

使用内置 image_gen，角色参考第一关动画表的像素密度、森林配色和深色轮廓。角色提示词：Production 2D woodland pixel-art boss animation sheet, Spore Crown Elder, broad burnt-orange ivory-spotted mushroom cap, moss bark body, thick root limbs, amber eyes, jade chest rune; chunky pixels and dark outlines; side view facing right; true transparent background; exactly four columns by eight rows, idle/walk/root cast/spore cast/leap slam/enraged cast/exhausted/death, four frames each; identical scale, aligned feet, transparent gutters; no text, scenery or grid.

特效提示词：Production woodland pixel-art VFX sheet matching Spore Crown Elder; exactly four columns by four rows; four frames each of rising moss root spike, golden-green airborne spore, flat gold floor warning ellipse, amber-green landing burst; transparent background, dark outlines, earthy colors, generous gutters; no text, scenery or grid.
