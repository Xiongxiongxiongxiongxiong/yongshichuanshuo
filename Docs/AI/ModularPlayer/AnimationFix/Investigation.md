# Run cycle and idle weapon correction

Observed in Unity 2022.3.20f1c1, SampleScene_8, 2026-10-20.

## Evidence and causes

- The old run row repeated the same leading-leg silhouette in many frames and
  omitted a convincing opposite half-stride. It played six frames at a fixed
  12 fps independently of travel speed. The sprite pivots were manually estimated.
- All six serialized idle hand offsets were exactly (0.4250, 0.8500), despite the
  drawn hands moving between frames. Pixel inspection of the skin region measured
  distinct hand centers. The weapon therefore remained stationary while the hand
  moved. Idle pivots also drifted sideways and the 5-to-0 wrap caused a visible jump.
- WeaponSocket is a sibling of Modular Body. Previously it applied only the hand
  coordinate and ignored the body's own local transform.

## Fix

- Dedicated eight-frame run sheet with opposing half-strides/counter-swing, aligned
  pelvis and ground baseline, and separate per-frame grip offsets. First generated
  draft was rejected for insufficient limb variation; the corrected edit is used.
- Distance-driven run phase with 4.2 local units per full cycle. It remains continuous
  when direction changes; stepping responds to actual horizontal velocity.
- Measured idle hand anchors, re-aligned idle sprite pivots and ping-pong breathing
  sequence avoid the old end-to-start discontinuity.
- The socket applies the body's transform to the selected frame's grip. Weapon
  selection, mirroring, attack timing and all gameplay physics remain unchanged.

## Validation

- Unity compiled without errors; pre-existing duplicate using directives and
  GameUIFont font warning remain unrelated.
- Actual Input System events in SampleScene_8 passed the existing 17 behavior
  checks after the correction. See RuntimeValidation.txt.
- Controlled runtime sampling at 30/60/120 updates per second visited all eight
  run frames and reached the same final phase after one second at 6 world units/s.
  Longest sampled frame hold was 0.0667 seconds. This verifies animation timing,
  not an overall game performance benchmark.
- At half movement speed, phase advanced by half as much (frame 2 versus frame 4
  after a quarter second).
- Both facings traversed all six idle poses. Grip position changed through 0.2
  local units and matched the current hand anchor to less than 0.0001 world units.
  A temporary body offset/rotation also carried the socket correctly.
- Actual rendered run frames were inspected; previews and live-game captures are
  saved alongside the existing integration evidence. Visual smoothness remains
  based on eight discrete sprite drawings, not interpolated limb artwork.
- Scene comparison changed only the existing ModularPlayerVisual serialized block;
  original movement, colliders, attack data, camera and other scene blocks match
  AnimationFix/persistent-before.unity.txt.
- Play Mode stopped after validation. No standalone build or target-device test.
