# Player and angel idle polish — 2026-10-26

Confirmed causes:
- Player six idle drawings drift 12 source pixels horizontally and 1 vertically at the feet. Fixed-grid pivots turn that into sliding and a loop reset at 10 fps.
- Angel pivots used the changing asymmetric wing silhouette midpoint. Face anchor varied from -5.17 to 33.32 pixels. The source six drawings depict an opening/closing cycle, but the animation also reversed the entire sequence and held frame zero twice.

Changes:
- Six standalone player Sprite metadata assets preserve all original pixels/rects/PPU and align planted feet. Source PNG and importer metadata untouched. Idle clip uses these assets, with 0,1,2,3,4,5,4,3,2,1,0 and eased holds; length approximately 1.78s versus 0.6s. Weaponless body pivots and sword hand offsets track the same correction; persistent scene only changes six idle bindings and initial sprite.
- Angel Sprite GUIDs and original pixels preserved; pivots follow the face/body reference, keeping feet planted. Six-frame complete wing cycle repeats in approximately 1.46s. No physics/root motion changes.
- Reapply via PolishIdleAnimations.Apply after resetting character setup. Utility backs up original affected assets once under this folder.

Validation:
- Six player planted-foot coordinates identical (-6.140,6.000 source pixels from pivot); six angel face anchors identical 12.821 pixels.
- Both clips first/last sprites identical, looping enabled; rendered 72 paired preview frames from saved curves.
- 24 player idle combinations (6 frames x 2 facings x original/frost weapon) had visible equipped sprites.
- Runtime town angel sampled at six times with non-null sprites. Run animation and return to idle confirmed. Jump state probe was immediately transitioned by the grounded Animator, so no claim of full jump gameplay testing.
- No Unity Console errors. Temporary preview objects removed; Play stopped. No standalone build.

Idle preview renders body drawings only (swords are checked separately in runtime). Source frame shapes still change discretely; this correction removes positional jitter and improves timing without generating replacement artwork.
