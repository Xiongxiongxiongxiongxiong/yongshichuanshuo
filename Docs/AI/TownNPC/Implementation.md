# Town NPC implementation — 2026-10-26

Scene: Assets/Scenes/SampleScene_25.unity, root Town NPCs.
Assets: Assets/GeneratedTown/NPC. Three reusable prefabs, three Animator controllers, looping Idle clips, eighteen Sprite assets, three transparent source atlases.
Authoring: Assets/Editor/InstallTownNPCs.cs (rejects repeat installation and dirty scenes).

Positions (original panorama coordinates): Blacksmith (445,449), Angel (1170,474), WarehouseKeeper (1260,611). Heights 2.6, 2.9, 2.5 world units. Characters are presentation only; no commerce, storage, dialogue or collider behavior is added.

Each six-frame atlas is alpha-bound cropped, centered and bottom-pivoted. A five-frames-per-second ping-pong clip loops for two seconds. Uses standard Animator; no new runtime scripts or packages. Transparent artwork generated using built-in image_gen with the approved three-character concept as design reference. Source alpha preserved.

Prompt set: blacksmith matching left approved design, six breathing frames with apron, hammer and beard settling; angel matching center approved design, six breathing/wing/hair idle frames with white/gold robe and halo; keeper matching right approved design, six breathing/blinking/glasses/keyring settling frames with green vest and ledger. All requested exact 3x2 grids, true transparent backgrounds, consistent full-body front three-quarter game sprites, planted feet, no text/scenery/ground shadows. No CLI fallback used.

Validation: Unity compilation succeeded. Actual main town loaded through PlayMainTown. All three animators observed displaying all six distinct frames. Ground raycasts measured gaps 0.002/0.000/0.000 units. Overview and three closeups rendered and visually inspected using town URP overview camera during Play Mode. Console read during validation returned zero warnings/errors (initial baseline had two GameUIFont warnings). Saved YAML comparison: only existing SceneRoots record changed, fourteen records added; original terrain, ladders and scene objects unchanged. No standalone player build performed.

Backup: SampleScene_25_BeforeNPC.unity.backup. Runtime validation details: Validation.txt. Overview.png and *Runtime.png are visual evidence. No additional setup required. Preview gameplay through Tools > Levels > Play Main Town - Twilight River.
