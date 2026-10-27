# Modular player integration — 2026-10-20

Implemented in Assets/Scenes/persistent.unity so the shared player uses the new
adventurer across gameplay scenes. Only the player's component/child lists changed
in existing serialized blocks. Original physics, attacks, animations, level setup
and other existing scene data match persistent-before.unity.txt.

Runtime code: Assets/Script/Player/ModularPlayerVisual.cs. The original renderer stays
available to legacy animation/skill code but is hidden. Child body and weapon
renderers present new art. PlayerSwordSkills gained only a read-only IsWhirlwind
property. No input bindings, stamina costs, damage values or hitboxes were changed.

The 24-frame action sheet and separate ShortSword sprite use transparent,
uncompressed Point-filtered imports. Body and sword are independently sliced sprite
subassets. EquipWeapon(sprite, scale) replaces presentation; null removes the sword.
Damage/type mechanics remain separate future work. Grip pivots must match the
upward-pointing sword convention described in Assets/player/ModularV1/DesignNotes.md.

## Validation

- Unity 2022.3.20f1c1 compiled the implementation without errors.
- Actual Input System events in the running SampleScene_8 exercised left/right run,
  J attack, Space jump and landing, S crouch, K slide, U thrust, I whirlwind, and
  ladder climbing. Weapon upgrade completed with its normal animation duration.
- Hurt/death presentation, weapon unequip/re-equip, and disabling/re-enabling the
  modular visual were checked directly. These were presentation checks rather than
  new damage/death gameplay tests.
- All 17 recorded checks passed after the final hand-anchor correction; see
  RuntimeValidation.txt. Screenshots were inspected for idle, running, attack,
  thrust, whirlwind and climb. Rendered closeups use a temporary camera in the live
  mine, without modifying the saved scene camera.
- Displacement tests reset to the known-safe spawn before each slide/skill. An
  earlier combined left-slide/thrust sequence left the map and killed the test
  player; it was a test setup issue, and the final run passed.
- No Console errors were present. The existing GameUIFont font-texture warning
  remains and was also present before this task.
- Play Mode was stopped after validation. No standalone player build was run.

Current art limitations: climbing holds a single pose; slide reuses crouch; weapon
upgrade reuses the raised-hand pose. Combat remains the game's existing sword
combat even if the weapon sprite is changed.

To reproduce: open persistent, use Tools > Levels > Play Mine SampleScene_8, then
execute ValidateModularPlayer.Start() through Unity MCP. The tests inject keyboard
events for about 12 seconds and write screenshots/report here; do not operate the
keyboard during that run. Stop Play Mode afterward to discard test state.

To roll back only the visual, disable ModularPlayerVisual. For full scene recovery,
the pre-integration source is persistent-before.unity.txt; preserve subsequent scene
edits before using it. The installer menu saves persistent and should only be run
when intentionally reapplying this art setup.
