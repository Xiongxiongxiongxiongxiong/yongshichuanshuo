# 野外山洞 BOSS：幽潭洞主 · 苍脊岩螈

## 生物与定位

天然山洞暗潭中的巨型洞螈，青灰湿润皮肤、浅色腹部、四足矮身、粗壮长尾与幽蓝背脊。长尾常盘在身侧，保持紧凑而清楚的像素剪影。与矿洞晶背掘岩兽区分：它依靠灵活的尾、舌、扑跃与吐息狩猎，环境为苔岩、钟乳石、地下潭和微光菌类。

延续项目粗像素、深色轮廓和有限配色。场地主题可适配苔溪或霜雾洞穴。现有 SampleScene_15/16/17 三种野外洞穴的路线及出口保持原样，先制作独立战斗场地，便于后续选择接入位置。

## 战斗规则

- 初始生命 1200，受击无敌 0.16 秒；普通攻击可命中，无常驻接触伤害。
- 招式间隙主动走近玩家，速度 1.9 单位/秒，距玩家 3.1 单位停步；四帧行走以 8 FPS 播放。间隙随机 1.4～2.2 秒。
- 按距离随机选招，同招最多连用两次，扑击不能连续使用。玩家贴近后随机扫尾或吐息，不会强制跳走。
- 每个完整招式最多成功伤害同一玩家一次，沿用无敌和滑铲判定；幽雾没有额外持续扣血、减速或叠层。

| 招式 | 预警与伤害 | 应对 | 收招窗口 |
|---|---|---|---|
| 围身扫尾 | 4 单位内可选；盘尾预警 0.7 秒，随后覆盖身前及身后低处，宽 6.6、高 1.3，伤害 15 | 跳跃、滑铲或离开身侧；绕后仍在扫尾范围内 | 动作结束后 0.8 秒 |
| 长舌突刺 | 3.5～8.5 单位可选；喉囊发亮 0.95 秒后向起手朝向刺出，长度 7.5、高 0.45、伤害 15。遇实体地形或封锁截断 | 跳跃、滑铲或绕后；岩台顶面高于舌线 | 动作后 1.2 秒 |
| 跃岩扑击 | 至少 5 单位可选；起手锁定主地面金圈，1.25 秒后扑落，宽 4.2、高 1.3，伤害 20；途中不追踪玩家 | 离开金圈或站到岩台上 | 落地后 1.8 秒 |
| 幽雾吐息 | 蓄力 1.2 秒，向面前主地面喷出宽 4.5、高 1.2 的蓝紫低雾，持续 2 秒，单次伤害 15 | 登岩台、跳跃或离开前方；不会在平台顶面生成 | 招式后 1 秒 |

半血进入【幽脊共鸣】：只强化下一招，保持原预警；行走速度 2.1，长舌长度 9、可选距离上限 10，幽雾宽 6；扑击范围宽 4.8，但伤害保持 20，落地收招延长至 2 秒。

## 独立战斗场地与后续接入

场景 Assets/Scenes/BossScene_4.unity，数据 Assets/DataSO/GameScene/Game BossScene4.asset，Addressables 地址 BossScene_4。技术编号 4 表示本项目第四个独立 BOSS 场景，不强制规定探索关卡顺序。

地面 Y=0、范围 X=-18..18，封锁 X=±15.5，玩家出生 (-11,0.05)，岩螈出生 (3,0.02)。两侧岩台顶面 Y=2，可躲低雾、长舌与低处扑击。背景为独立天然溶洞暗潭图；碰撞地面先复用项目岩块并调成冷青色，可在正式关卡接入时替换。

沿用 persistent 的角色、摄像机、HUD 和 SceneLoader。入口保存 AzureSalamanderEntrance；角色死亡和结算重试回本关。Boss 死亡清场，倒地 1.8 秒后解除封锁，再延迟 0.65 秒显示结算。

编辑器菜单 Tools → Boss → Play Wilderness Cave Boss 用于直接试玩；启动前保存当前场景修改。后续选定探索洞穴，在终点放置 BossLevelEntrance，将 destination 指向 Game BossScene4，spawnPosition 设为 (-11,0.05,0)。

## 素材与实现

- WildernessCaveBoss/AzureSalamander.png：4 列 8 排，32 帧，待机、行走、扫尾、吐舌施法、扑跃、幽雾施法、喘息、死亡。
- WildernessCaveBoss/AzureSalamanderEffects.png：4 列 4 排，16 帧，长舌、幽雾、金圈预警、碎石水花。
- WildernessCaveBoss/NaturalCaveBackground.png：不透明天然山洞背景，钟乳石、地下潭、苔岩、微光菌类与远处洞口天光。
- 使用内置 image_gen 生成，角色与特效保留透明通道，Point、无 mipmap、无压缩。按实际 Alpha 间隙切帧；吐息行的相邻光雾使用实测列边界，脚底锚点与角色比例校准。舌与雾由独立战斗逻辑判伤。
- AzureSalamanderBoss 管理状态与距离选招；AzureSalamanderEffect 管理舌、低雾和临时特效；AzureSalamanderHud 沿用现有 uGUI 血条。BuildAzureSalamanderBossLevel 是一次性搭建器，已有场景时保留编辑内容。

## 图像提示词摘要

角色：Production chunky pixel-art natural cave salamander boss, heavy four-legged squat newt, slate teal-blue pebbled skin, cream belly, small jade eyes, short glowing cyan dorsal ridge, muscular curled tail, no armor/crystals/mining gear; side view facing right; exactly four columns by eight rows: idle/walk/tail sweep/tongue cast/leap/mist breath/exhausted/death, four frames each, constant scale and feet baseline, transparent gutters, no text/scenery.

特效：Exactly four columns by four rows, fleshy mauve tongue lash to the right with cyan tip, low blue-violet floor mist, flat gold warning ellipse, blue-gray rock and water landing burst; transparent alpha, chunky pixels, no text/grid.

背景：Wide natural wilderness mountain grotto, slate-blue stalactites, moss rocks, distant underground pool, faint glowing fungi and a high daylight opening, quiet gameplay foreground; no mining structures or characters.

## 验证记录

2026-10-19，Unity 2022.3.20f1c1 编辑器与 Play Mode 受控验证：

- 无新增脚本编译错误；场景中 32 个角色帧、16 个特效帧引用完整。自动读取 Alpha 间隙切帧，吐息行按实测列边界修正，保留已创建 Sprite 的 GUID。天然洞窟背景、岩台、封锁、1200 血条和战斗镜头均已运行显示，截图 WildernessCaveBoss/Validation/AzureCaveEncounter.png。
- 实际行走至 X≈-1.14，播放 AzureSalamander_1_0；扑击中途位置约 (1.78,3.07)，显示 AzureSalamander_4_1，本体保持可见，证明为真实弧线运动。
- 扫尾 0.69 秒玩家 100 血，0.7 秒变 85，再次判定仍 85；身后同范围可命中。长舌 0.94 秒未生成，0.95 秒发射，正常长度 7.5，命中后 85 血。
- 实体墙将长舌截为约 2.02，墙后玩家保持 100。实际滑铲启动成功时，被舌线覆盖仍保持 100。
- 岩台上受到低雾覆盖时玩家保持 100；同位置回到地面后变 85，持续覆盖不再重复扣血。普通雾宽 4.5，半血雾宽 6。
- 起跳锁定 (6,0.02) 后移动玩家，落点不变；落地命中玩家 100→80，并恢复本体碰撞盒。中距离采样 60 次出现吐息、长舌和扑击，其中扑击 14 次、无连续扑击；近距离采样 60 次无扑击。随机状态测试后还原。
- 致死后特效列表清空，死亡动画 1.79 秒仍封锁、1.8 秒解锁，随后延迟显示结算且 Time.timeScale=0。Retry 回到 BossScene_4，Boss 1200 血。
- 实际角色死亡后重新加载本战并恢复存活。退出再重新进入 Play Mode，新 SceneLoader 识别 AzureSalamanderEntrance，ContinueGame 成功加载 BossScene_4，第三关场景引用保留。测试后恢复原存档。

部分招式测试通过编辑器受控调用完成，不等同于完整人工实战。未进行平台打包；选定野外探索场景接入、最终地形美术和人工实战调参需后续完成。
