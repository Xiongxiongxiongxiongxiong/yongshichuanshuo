# Ten dao — 2026-10-27

Approved designs: 旧铁柳叶刀、厚背砍刀、翠玉雁翎刀、青纹破阵刀、霜月刀、沧浪刀、紫月幽刃、暗雷斩魄刀、炎龙偃月刀、曜日凤翎刀. Five rarities, two dao each.

Each weapon has its own independently generated Main and Supplement transparent animation atlas. Main follows the prior FrostMain layout; Supplement contains skill/slide/jump poses plus eight attack poses and four upgrade poses. These use the same 108 calibrated source-body mappings as the ten swords and the original frost sword. Shared resting/recovery artwork is permitted where the prior implementation reused poses; this is not 108 entirely unique drawings per weapon. Direction, hand anchor, sorting and hiding are driven by the real body sprite, with identity runtime weapon rotation.

BuildDaoCollection delegates to the same BuildSwordCollection authoring path; no parallel equipment runtime is introduced. IDs are dao.*; sword.* items remain registered. Measured Supplement row boundaries are saved in Slices.json. Source artwork/prompts are under Assets/player/DaoCollection. Equipment is visual; existing damage, costs, cooldowns and input remain unchanged.

All ten dao were granted once to the current backpack without duplicating existing sword items. The backpack now contains twenty unique single-quantity weapons; the warehouse remains empty. Repeating either explicit grant does not duplicate owned items. The current equipped item is dao.phoenix_cleaver (曜日凤翎刀). No automatic grant was installed.

Validation: Unity compilation and Console zero errors. Twenty independently generated dao atlases passed transparent-alpha checks; all1080 dao bindings retain body hand/crop/sorting configuration. All2160 dao pose/facing cases passed actual runtime sprite selection, visibility, grip transform, identity rotation, flip and sorting checks; ten equip/save/reload cases and deposit/withdraw/re-equip/discard ownership passed with test data restored. Sword2160 cases also passed, total4320. ValidateBladeGameplay observed80 real gameplay action sequences across all20 weapons (U thrust, I whirlwind, K slide, J attack), with multiple synchronized body/weapon poses in each. See Docs/AI/WeaponCollection/GameplayValidation.txt.

Visual QA: IdleContactSheet.png, IdleFrame03.png and AttackContactSheet.png render the actual body/weapon sprites. Corrected36 higher-rarity dao idle drawings to their authored upright orientation and calibrated their lower-left grips; body hand anchors remain unchanged. BuildSwordCollection.RepairRestDirections and the authoring path preserve these corrections. All2160 dao mapping checks were repeated after the correction. See Docs/AI/WeaponCollection/RestDirectionValidation.txt.

Actual B key opens the20-item backpack and pauses gameplay; selecting slot20 and using the existing button successfully unequips/re-equips the dao, while close restores time/controls. BackpackTwentyWeapons.png records the final state. The15 existing player animation clips match their pre-validation SHA256 hashes. No standalone build was produced.

Fresh Play Mode restored all20 unique weapons and the equipped phoenix dao with the authored renderer active. Final Play Mode was stopped. Persistent scene comparison with the task baseline contains only the20 item-definition references. Item descriptions show rarity/type and themed flavor text rather than implementation details.
