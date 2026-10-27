# 第一关：古林守卫 · 苔角王

当前正式关卡为 Assets/Scenes/BossScene_1.unity，Boss出生于场地中央(0,0.02)，
战斗范围X-15.5..15.5，地面Y0，左右各有一个矮平台。
原SampleScene_7中的Boss与旧战斗区已归档停用；该场景保留为探索关卡，
最右侧终点自动切入BossScene_1，角色出生于(-11,0.05)。
进入后符文封锁入口、切换战斗镜头，
显示 Boss 名称、血条、技能提示；击败后倒地、解除封锁并进入现有结算界面。

## 战斗参数

- 生命600，受击无敌0.16秒，普通攻击闪白但不打断招式。
- 蓄力冲锋：预警0.8秒，速度10，伤害20。撞墙眩晕1.5秒。
- 裂地重踏：预警0.85秒，向两侧发出速度6的贴地冲击波，伤害15。跳跃或高台可躲避。
- 苔岩震落：三个固定落点，每个先显示0.95秒金色预警，再落石。伤害15。
- 每个完整招式最多成功伤害同一玩家一次，滑铲仍可躲避，不绕过玩家现有无敌时间。
- 血量<=300时进入狂暴：冲锋速度12，连续两次冲锋，中间仍有0.6秒预警，最终撞墙眩晕1.9秒。
- 死亡动画非循环，倒地1.8秒后解除封锁，再等待0.65秒显示通关界面。
- 角色死亡后沿用现有检查点和场景重载，重新进入关卡时Boss重置。

## 新素材

内置图像生成工具生成，源文件保留透明通道、Point、无mipmap、无压缩。

- MossTuskKing.png：1024x1536，4列8排，依次待机、行走、冲锋蓄力、冲锋、重踏、施法、眩晕、死亡，每组4帧。
- MossTuskEffects.png：1536x1024，4列4排，依次冲击波、落石、金色落点预警、符文封锁，每组4帧。
- MossTuskKingFrames/、MossTuskEffectsFrames/：独立Sprite资源，校准脚底/地面锚点。
- 场地沿用现有草地、岩壁和senlinBG；Boss血条沿用角色HUD素材。

## 生成提示词

### Boss

Production 2D forest pixel-art boss animation sheet. Reference is existing game's small wild boar.
Design MOSS TUSK KING: massive squat brown wild boar, moss green rock plates covering back,
two ivory tusks glowing emerald at roots, amber eyes, single bright emerald rune crystal
centered in armored back. Strong silhouette, chunky low-resolution pixel art and dark outlines
matching reference; no smooth painting. SIDE VIEW facing RIGHT in every frame. Transparent
background, NO text NO labels NO grid lines, exactly FOUR columns and EIGHT rows =32 sprites.
Every sprite contained in its own equal rectangular cell with margin. Constant size, consistent
anatomy colors and feet baseline at85% cell height. Row1: idle4 breathing frames. Row2: walk4
stepping frames. Row3: charge windup4 frames pawing ground lowering tusks green glow. Row4:
charge run4 frames low head forward driving legs, green streaks. Row5: stomp4 frames front feet
rise then heavy impact then recovery. Row6: rock-summon4 frames back rune flashes green, head
raised, green leaf sparks, settle. Row7: stunned4 frames slumped dizzy golden small sparks.
Row8: death4 frames knees buckle, fall on side, collapsed, motionless collapsed with unlit dark
rune. Consistent body scale; do NOT zoom between frames, no background scenery, no extra
creatures. Match reference woodland brown/olive/cream palette.

### 特效

Sprite sheet of2D woodland pixel art game VFX for moss-armored boar boss. Exactly4 columns
by4 rows =16 equal rectangular cells, transparent alpha background NO text NO labels NO grid,
each object completely inside its cell with generous margin. Crisp chunky pixels dark outlines
earthy brown moss green gold and emerald. Row1 four expanding frames of low horizontally
traveling ground shockwave: jagged brown stone shards with emerald energy at ground level,
side view moving right. Row2 four frames of a single falling boulder: brown mossy crag with
bright small rune, rotation and trailing green sparks, isolated side view. Row3 four frames
of floor danger telegraph: flat horizontal elliptical golden-orange glowing warning ring and
a few upward sparks, grows brighter before impact; side-scroller perspective. Row4 four
frames of emerald rune pillar barrier sigil: upright vertical forest magic crystal thin
glowing stone column, thick enough to block an arena entrance, pulsing green light, no
characters. Match retro fantasy forest platformer enemy effects. Use same scale and anchored
centers across frames. No scenery, no UI lettering.

## 验证记录

Unity2022.3编辑器编译；检查X52.2、56、69、84、87地面碰撞连续。
运行模式：进场锁门/激活镜头；普攻Boss600->590、不打断蓄力；
冲锋伤害100->80，重复接触仍80；半血进入双冲锋；重踏生成左右两道波；
落石生成三个预警和三个石块；死亡时结算未出现，倒地结束后出口开启，
调用结算流程后UI显示且Time.timeScale=0。已截图检查森林背景、Boss比例和HUD。
完整操作手感与战斗难度仍需玩家实战调节。

另经正常NewGame入口检查：场景仅一个Boss，自动切入Boss Arena Camera，
入口封锁生效，屏幕血条Canvas激活、名称与招式预警显示正确。

历史原型的低平台迁移检查：Boss双脚位于Y3.02，台阶预警匹配Y3/Y5。
当前独立关卡截图为Validation/BossScene_1_Layout.png。

## 独立关卡流程验证

- SampleScene_7运行时活跃Boss数量为0；终点触发器目的地为Game BossScene1。
- 经真实触发器切换后仅加载persistent与BossScene_1。
- 入场保存Boss入口记录，首次进入/重试恢复生命和体力。
- 实际角色死亡后重新加载BossScene_1并复活。
- 实际结算按钮调用Retry后重新加载本关；加载完成回调记录Boss出生(0,0.02)，
  Boss600血，角色100血、100体力。
- SceneLoader支持重启后识别Boss入口存档；新游戏仍从SampleScene_7开始。
- 测试结束后恢复测试前的存档记录。
