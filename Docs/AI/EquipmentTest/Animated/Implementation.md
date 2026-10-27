# Frame-locked frost sword — 2026-10-25

Supersedes the single-image rotation test from 2026-10-23.

The persistent player's TestWeaponSwap now has 108 serialized WeaponFrame bindings. Each maps the actual source body Sprite to its authored weapon Sprite, a calibrated hand position, front/back sorting offset and visibility. Animator and existing slide/skill scripts remain the only animation clock. LateUpdate reads their actual body Sprite and selects exactly the corresponding weapon frame. Authored mode uses identity rotation, not rotation tweening of one fixed sword.

B still alternates original integrated art and the frost weapon. The HUD suppresses B-to-bag while the temporary binding is enabled; the bag button remains available. Damage, cooldowns, stamina, colliders and animation-event timing were not changed. Shields remain part of the character artwork and are not replaceable yet.

Art:
- Assets/player/EquipmentTest/Animated/FrostMain.png: main posed weapon atlas.
- Assets/player/EquipmentTest/Animated/FrostExtra.png: slide/thrust/jump/skill weapon poses.
- Assets/player/EquipmentTest/Animated/FrostAttack.png: separately spaced eight-frame attack sequence, including painted cyan arcs and changed blade silhouettes/perspective.
- Frost_RedKnight*.asset: 108 stable Sprite assets. Some resting/recovery bindings intentionally reuse neighboring pose artwork where the sheet has an occluded or empty slot; this is not 108 entirely unique paintings. Climb/death/back-facing poses can hide the weapon.

Body sprites are created once from the existing weaponless textures. Their crop bounds remove disconnected neighboring character fragments while retaining the original foot/world anchor. Weapon slices isolate the main connected sword component to remove neighboring particles. The original source images and animation clips were not overwritten. A discarded grip-cover experiment is not included; ordinary holds use body occlusion through back sorting, attacks use front sorting.

Code:
- Assets/Script/Player/TestWeaponSwap.cs: runtime frame lookup, mirroring, B test, original-art restore and runtime sprite cleanup. Older configurations without animatedFrames can still use the legacy rotation fallback; all current 108 source frames have authored mappings.
- Assets/Editor/BuildFrostWeaponFrames.cs: repeatable initial asset/config authoring. Tools/Player/Build Frame Locked Frost Sword rebuilds generated poses and grip calibration. It will overwrite generated calibration; back up manual adjustments first.
- PlayerSwordSkills.OnDestroy now avoids destroying imported authoredFrames; only dynamically generated skill Sprites are destroyed. This fixes an observed exit-Play-Mode asset-destruction error.

Validation:
- 108 mappings, both facings: 216 combinations, including hidden poses and identity-rotation checks.
- Actual keyboard run/turn/attack/jump/crouch/slide/thrust/spin recording initially observed 50 distinct frames with zero body/weapon mismatches.
- Final gameplay pass after atlas/crop changes observed 35 weapon frames with zero mismatches, including attack, slide and both skills.
- Actual B input checked without opening bag.
- Real scene screenshots plus rendered eight-frame preview for visual inspection.
- FrostAttackPreview.gif is a looping rendered pose preview with illustrative timing, not a screen recording of input/combat. SlashFinal.png is a posed in-game screenshot. FinalValidation.txt and GameplayFinalValidation.txt hold evidence.
- No standalone build was run. Existing AI weaponless-body artwork can still differ from the original character drawing; future art polish can improve hand/arm gestures without changing the synchronized-frame architecture.

Art was generated with built-in image_gen; prompt specifications are recorded in ArtPrompts.md. Original art is retained. The failed missing-cell repair image was discarded and is not referenced by the project.
