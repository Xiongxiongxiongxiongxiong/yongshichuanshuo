# Original player: red hair

The restored original player now uses PlayerRedHair.mat / Game/PlayerHairColor.
Source PNGs, sprite assets, animation clips, character proportions, clothing,
weapons and gameplay data are preserved. No generated character redesign is active.

PlayerHairColor chooses a pre-inspected neutral-color hair region for the current
sprite and supplies UV bounds to the URP 2D lit shader. The shader changes neutral
hair tones to a red shadow/highlight ramp while retaining alpha and lighting.
Its late presentation update follows existing skill/slide/climb sprite writes.
Metal shields are excluded by preferring compact regions near the head; this was
visually checked against the representative jump/attack/death states in States.png.

ApplyPlayerRedHair inspects source atlases in the Editor and serializes region
metadata. Original textures do not require CPU readability at runtime. It saves
the persistent scene; menu: Tools > Player > Apply Red Hair.

Validation: Unity C# and shader compilation passed. Final shader supported with
zero diagnostics. All 81 Animator/dynamic ladder/upgrade/sword-skill sprites had
hair regions. Representative poses were rendered and inspected. Idle.png shows
the live mine player. No standalone build was run. Existing GameUIFont warning
and duplicate using directives are unrelated. Play Mode was stopped afterward.
