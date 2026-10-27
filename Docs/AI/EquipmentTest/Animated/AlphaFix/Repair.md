# Chest weapon transparency repair (2026-10-26)

Cause: FrostMain upgrade poses contain baked gray matte pixels around the sword and cyan flame. Alpha import was already enabled; importer settings cannot remove painted matte.

Fix: generated dedicated clean four-pose FrostUpgrade.png through built-in imagegen background extraction. Only Frost_RedKnight_54..61 Sprite assets were rebound in place, preserving GUIDs, hands, body crop, facing and timing. Pose sequence: 0,0,1,2,3,1,2,3. Other weapon animations unchanged. Builder now reapplies this atlas; targeted RepairUpgradeTransparency avoids rebuilding other bindings.

Validation: new atlas 1,209,442 fully transparent pixels and only 11 opaque near-neutral midgray pixels. All 16 upgrade-frame/facing combinations use the new atlas. InGame.png confirms transparent edges against town scenery. Actual PlayerAnimtion.PlayWeaponUpgradeAnimation coroutine invoked and completion/Animator restoration checked. No Unity Console errors after import and runtime validation. No standalone build performed.

Art: built-in imagegen. Final prompt: Same four game sword animation sprites transparent background. Complete clipped bottoms; all four full pommels/handles visible with padding, four equal horizontal cells; preserve sword and cyan progression; no gray/checker background, alpha zero, wide 4:1 row.

Discarded whole-sheet edit was not installed. Original atlas remains available. Before.png retains affected source crop for comparison.
