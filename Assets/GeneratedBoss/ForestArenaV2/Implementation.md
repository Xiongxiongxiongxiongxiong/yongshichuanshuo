# BossScene_1 / BossScene_2 场景重制

2026-10-26，按用户要求覆盖两份现有场景，保留原场景GUID、加载数据和战斗逻辑。

- BossScene_1「古林石环圣域」：巨树、断裂石环、远景瀑布、暖色林光，搭配苔角王。地面改为根系缠绕的古代石台。
- BossScene_2「幽蓝菌冠深潭」：根拱、紫色巨菌、冷色深潭与暖色菌灯，搭配孢冠长老。水体位于战斗地面后方，不是新增危险区。
- 两关采用独立生成的三层景深画面、独立苔石／菌石平台和藤根封路外观。旧森林背景、地砖、符文圆盘及重复世界标题仅隐藏显示。
- 地面与平台碰撞、镜头、伤害、BOSS动作与技能参数不变。平台脚面仍为y=2，地面为y=0；新平台Sprite按图中的平整顶面标定pivot。
- 两关血条移到上方。MossBossHud和SporeCrownHud新增useTopLayout，默认false，只在这两个场景启用，避免影响原探索场景中的同类BOSS。
- 背景覆盖38×22世界单位，按像素基线对齐地面，避免镜头顶部黑边。

## 使用

Tools > Boss > Play First Boss - Forest Sanctuary

Tools > Boss > Play Second Boss - Spore Grotto

RefreshForestBossArenas.Apply可重新应用本版美术；此工具会覆盖这两场的美术层，使用前保存场景。它在保存前比对碰撞和BOSS配置，发现变动会中止。原场景备份位于Backups/BossScene_1_BeforeVisualV2.unity与BossScene_2_BeforeVisualV2.unity。

## 素材来源

使用内置image_gen生成，原图已复制到本目录：AncientForestSanctuary.png、SporeCrownGrotto.png、ForestArenaProps.png。Point、无压缩、无mipmap；道具保留原生透明度。完整最终提示词见Prompts.json。

## 验证

- Unity编译通过；覆盖保存时，所有Collider2D配置与BOSS组件序列化快照一致。
- 两关实际进入Play Mode，战斗镜头为(0,5,-10)，血条顶部显示，新平台及封门正常显示。
- 第一关玩家站在新平台实测y=2.0405，生命100；击败后双门关闭状态解除、完成面板显示，重试后BOSS满血。
- 第二关玩家站在菌石平台实测y=2.0405、生命100；台下藤根高度1.5675，不穿平台。两侧无平台根刺仍为1.6。
- 第二关击败开门、完成面板、满血重试通过。
- 最终画面见Validation/ForestSanctuaryV2.png与SporeGrottoV2.png；补足背景覆盖后无顶部黑边。
- 已退出Play Mode，恢复测试前检查点及原编辑场景。
- 本次未做目标平台打包或重新调整战斗难度。
