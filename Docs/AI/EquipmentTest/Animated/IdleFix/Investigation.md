# Default idle sword and inverted frost idle — 2026-10-25

Confirmed causes: default mode previously disabled the independent renderer for every frame, while original RedKnight_00 has no visible painted sword. The frost idle atlas has its blue blade below the grip (measured blue-pixel centroid minus grip Y: -19.35px for frame00, -20.25px for01). This is an artwork orientation issue, not body facing.

Fix: TestWeaponSwap.defaultIdleWeapon references the existing ShortSword sprite from AdventurerModularConcept-v1.png. It is shown for all six original idle frames using the calibrated hand anchor, mirrored facing, grip pivot and a 15-degree resting tilt. Original attack/movement artwork is unchanged.

WeaponFrame.flipVertical corrects the six main frost idle poses and six extra resting poses around the grip. All attack, thrust, jump and slide poses retain their original orientation; body flipX remains separate. Both the saved scene and BuildFrostWeaponFrames now carry this correction, so rebuilding the authored frames does not revert it.

Validation: all six idle frames x two weapon choices x both facings =24 presence/orientation checks; all108 authored frames x both facings =216 mapping/orientation checks. Real keyboard B selects the corrected frost idle without opening bag. Default.png and Frost.png show actual frame00 in the running town. No source PNG was modified, and no new artwork was generated. Final Console checks and Play Mode exit performed.
`nFollow-up: corrected horizontal orientation of the same 12 resting frost poses with WeaponFrame.flipHorizontal. Renderer facing now uses body.flipX XOR frame.flipHorizontal; flipVertical stays enabled. Attack/thrust/sliding frames and default sword retain their original horizontal orientation. Saved metadata and rebuild authoring agree. All216 pose/facing checks passed.
