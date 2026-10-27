# 独立Boss关卡

场景：Assets/Scenes/BossScene_1.unity。
场景数据：Assets/DataSO/GameScene/Game BossScene1.asset。
Addressables组：Scenes；地址：BossScene_1。

场地中心为X0，地面Y0。Boss出生(0,0.02)，角色出生(-11,0.05)。
地面覆盖X-18..18，战斗封锁位于X-15.5与15.5。
左右矮平台位于X-10与10，顶面Y2。背景为现有senlinBG森林素材。
Boss战镜头中心(0,5)，OrthographicSize9.5。

从persistent场景启动游戏，新游戏仍进入SampleScene_7；
走到探索关卡最右侧的“进入首领关卡”终点触发区即可切换。
进场补满生命和体力，并保存Boss入口记录。
死亡后重试Boss关卡；通关界面“重新开始”也重试当前Boss关卡。
原SampleScene_7低平台上的Boss与旧扩展战斗区保留为停用归档，避免重复生成。

本关沿用项目架构：玩家、主摄像机、HUD和场景加载器由persistent场景提供。
单独打开BossScene_1可编辑场地；正式运行应从persistent启动。

BuildStandaloneBossLevel为一次性编辑器搭建脚本，已存在场景时不会覆盖。
动画、特效与参数详见BossDesignAndValidation.md。
