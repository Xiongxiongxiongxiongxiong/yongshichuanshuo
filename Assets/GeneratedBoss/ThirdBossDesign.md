# 第三关：矿脉吞噬者 · 晶背掘岩兽

## 生物与美术

废弃矿井深处的掘地生物，长期吞食矿石，矿物在背甲和胸腹沉积。轮廓结合鼹鼠的钝长吻、穿山甲的层叠矿甲与铲形巨爪，四足贴地移动。灰紫岩甲、褐色皮毛、琥珀小眼和橙金矿晶，继续采用前两关的粗像素、深色轮廓与清楚的攻击剪影。

性格沉重、敏感于地面震动。它不是矿工或机械体；木支架、轨道和矿灯属于环境。战斗核心是“看预警选落脚点，等它出土或震击后反击”，与首关冲锋、第二关藤根控场形成区别。

## 战斗规则

- 初始生命 1000，受击无敌 0.16 秒，无常驻接触伤害；普通攻击可击中躯干，受击闪白不打断技能。
- 招式间隙以 1.7 单位/秒向玩家步行，距玩家 3.2 单位停步，播放四帧行走。间隙随机为 1.4～2.2 秒；蓄力与收招停步。
- 根据距离带权重随机选招，同招最多连续两次，钻地突袭不能连续使用。近身优先爪击，远处可钻地或震落矿晶。不会因为玩家贴身就必定逃离。
- 每个完整招式最多成功伤害同一玩家一次，遵循现有角色无敌及滑铲规则。无中毒、减速、杂兵或永久破坏场地。

| 招式 | 预警与效果 | 玩家应对 | 反击窗口 |
|---|---|---|---|
| 掘岩爪击 | 4 单位内可选；举爪 0.7 秒后朝面前横扫，伤害 15，朝向起手锁定 | 后退、滑铲或绕到背后 | 动作结束后 0.8 秒 |
| 钻地突袭 | 至少 5 单位才可选；钻入地面，提前锁定一处主地面金色矿纹圈，1.2 秒后在该处破土，伤害 20，宽 4.2、高 1.3 | 离开矿纹圈或站上岩台；不追踪后来移动的玩家 | 出土动作结束后 1.7 秒 |
| 矿震碎岩 | 抬爪 1 秒后向左右释放贴地碎岩波，速度 5.5，伤害 15，遇岩台侧壁消散 | 跳跃或登上两侧岩台 | 震击后 1.4 秒 |
| 矿晶震落 | 背晶发亮；锁定玩家当时位置附近三个落点，间隔 3 单位。每处金圈提前至少 1.1 秒，矿晶依次落下，伤害 15 | 离开标记；岩台顶面也可能成为落点，不能一直站台不动 | 招式后 1.5 秒 |

半血进入【矿核过载】：只强化下一招，保留当前预警时长。矿晶变五处，仍保留两侧逃生空间；碎岩波速度增至 7；破土范围变宽至 4.8，但伤害保持 20，出土后的反击窗口延长至 2 秒。不会额外插入没有预警的攻击。

## 独立矿洞战斗场地

已制作独立场景 Assets/Scenes/BossScene_3.unity，数据 Game BossScene3，Addressables 地址 BossScene_3。与未来矿洞探索关卡分开制作，现有第一、第二关路线不变。编辑器菜单 Tools → Boss → Play Third Boss 会从 persistent 启动并进入本战斗，启动前应保存当前场景修改。

地面 Y=0、X=-18..18，封锁 X=±15.5；玩家出生 (-11,0.05)，Boss 出生 (3,0.02)。两侧岩台顶面 Y=2，中央留出走位与钻地落点。矿洞背景包含岩顶、支架、矿灯、轨道和琥珀矿簇；当前战斗地面沿用项目岩块并调为冷灰色，后续可替换成正式矿洞 Tilemap。

正式运行依赖 persistent 的玩家、摄像机、HUD 和 SceneLoader。进场补满角色状态并记录 CrystalbackEntrance；角色死亡与结算重试回本关。Boss 死亡取消全部特效，倒地动画 1.8 秒后解锁，再延迟 0.65 秒显示结算。

未来矿洞终点只需添加 BossLevelEntrance，将 destination 指向 Assets/DataSO/GameScene/Game BossScene3.asset，出生点设为 (-11,0.05,0)。目前不接入尚不存在的探索场景，也不替换第二关终点。

## 素材与实现

- ThirdBoss/CrystalbackBeast.png：32 帧，四列八排，依次待机、行走、爪击、钻入、破土、震击、矿晶施法、死亡。
- ThirdBoss/CrystalbackEffects.png：16 帧，四列四排，依次碎岩波、下落矿晶、金色落点预警、破土爆发。
- ThirdBoss/MineArenaBackground.png：独立不透明矿洞背景。
- 内置 image_gen 生成；角色和特效保持真实 Alpha，Point、无 mipmap、无压缩。生成图的行列间距有偏差，BuildCrystalbackBossLevel 使用实测透明间隙切帧，角色锚点保持同列中心与脚底基线，避免邻帧污染。
- CrystalbackBoss 管理状态、距离选招、行走、钻地、伤害去重与结算；CrystalbackEffect 管理矿晶及碎岩波；CrystalbackHud 沿用前两关的 uGUI 血条样式。

## 生成提示词摘要

角色：Production chunky 2D fantasy pixel-art mine boss, Crystalback Burrow Beast, stocky mole-pangolin, slate violet layered rock armor, broad shovel claws, blunt mole snout, tiny amber eyes, amber quartz back crystals and orange mineral seam. Exactly four columns by eight rows: idle, walk, claw swipe, burrow, emerge, slam, crystal cast, death; four frames each, side view facing right, constant scale and foot baseline, transparent gutters, no text or scenery.

特效：Four columns by four rows, four frames each of low traveling slate/amber rock wave, falling quartz crystal, flat gold elliptical warning rune, dirt/mineral emergence burst; chunky pixel art, transparent background, no text or grid.

背景：Wide 2D pixel-art abandoned mine cavern, dark slate-violet rock, timber supports at edges, distant tunnel and rails, sparse amber quartz and mine lanterns, quiet dark gameplay area, no characters, UI or text.

## 验证记录

2026-10-17，Unity 2022.3.20f1c1 编辑器及 Play Mode 受控验证：

- 无新增脚本编译错误；32 个角色与 16 个特效 Sprite 引用完整，源图角点 Alpha=0。按透明轮廓实际测量行列间隙，避免爪击及出土帧相互污染。
- 菜单 Play Third Boss 成功启动 persistent 并加载 BossScene_3；Boss 1000 血、双方封锁、战斗镜头 Priority=50，镜头最终中心 (0,5)、尺寸 9.5，矿洞背景生效。最终实机截图 ThirdBoss/Validation/CrystalbackMineFinal.png。
- 实际行走从 X=3 向玩家接近到 X≈0.688，播放 CrystalbackBeast_1_2；沿用碰撞盒扫掠、前方地面和边界检查。
- 爪击 0.69 秒玩家 100 血，0.7 秒扣至 85，重复判定仍 85。沿用角色无敌与滑铲判定。
- 矿晶普通 3 枚、半血 5 枚；碎岩波速度普通 5.5、强化 7。
- 钻地中途隐藏本体并关闭碰撞盒；玩家移走后原落点不变；1.2 秒出土恢复本体和碰撞盒。远距离抽样 60 次出现 19 次钻地，无连续钻地；测试后恢复随机状态。
- 矿晶被平台阻挡时，台下玩家保持 100 血；位于上方暴露落点时扣至 85。碎岩波贴地，可跳跃或用岩台躲避。
- 致死后进入 Dead、特效列表清空，1.79 秒时仍封锁；1.8 秒解锁并保持结算延迟，之后显示结算且 Time.timeScale=0。Retry 重新加载本关且 Boss 1000 血。
- 实际玩家死亡后重新加载本关，角色恢复存活；退出并重新进入 Play Mode，新 SceneLoader 识别 CrystalbackEntrance，ContinueGame 成功回 BossScene_3，第一、第二关引用保留。测试结束恢复原入口存档。

上述部分招式验证通过编辑器受控调用完成，未执行独立平台打包。人工实战手感、整体难度与未来正式矿洞探索场景接入仍需后续验证；当前岩台与地表使用复用岩块，而非尚未制作的正式矿洞地形。
