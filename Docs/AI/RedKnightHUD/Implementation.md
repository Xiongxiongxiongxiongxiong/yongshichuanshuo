# Red Knight HUD

Implemented 2026-10-22 in Assets/Scenes/persistent.unity.

The saved Red Knight HUD Canvas is editable in the Hierarchy. It uses uGUI, TextMeshPro, 1920x1080 reference scaling, a safe-area root and a horizontal action layout. Presentation reads the existing player's Character, PlayerStamina, PlayerSwordSkills, PlayerSlide and SceneLoader. The old PlayerStatBar component and its children are disabled but retained so existing UIManager event references remain valid.

Features: red-haired portrait, real HP/stamina values and fills, trailing damage fill, action buttons J/K/U/I, radial cooldown masks and numeric countdowns, stamina/cooldown disabled states, area title, map/return-town control, pause, character summary and inventory information. Keyboard shortcuts C/B/M/Esc accompany mouse buttons. All 9 button callbacks are serialized UnityEvents. Modals restore their prior timescale and input state. Adventure Map retains its existing navigation behavior and its Esc dismissal does not reopen pause.

The bag is an informational panel, not an inventory system. The current game applies chest rewards directly to weapon damage. No item storage, equipment mechanics or new combat abilities were invented.

Primary files:
- Assets/Script/UI/RedKnightHud.cs: runtime bindings, cooldowns, modal/input lifecycle.
- Assets/Editor/InstallRedKnightHud.cs: initial authoring, art import and CJK font creation. Do not reinstall over the existing HUD; edit the saved hierarchy.
- Assets/UI/RedKnight/RedKnightAtlas.png: portrait, action and utility artwork.
- Assets/UI/RedKnight/IronFrame.png: stable single-sprite nine-slice frame, extracted from the atlas.
- Assets/UI/RedKnight/Fonts: Chinese TMP font atlas and its local font source.
- PlayerController.AttackFromHud forwards to the existing guarded attack path.
- TownHubNavigation.useExternalHud hides only the duplicated travel button and moves the existing hint away from the action bar.
- Sign.cs initializes its input in OnEnable as well as Awake and guards OnDisable, fixing an observed reload-time null reference.

ApprovedConcept.png is the approved image. The lower component showcase from that design is a design presentation, not part of the in-game HUD.

Art used the built-in image_gen tool. The production prompt is in ArtPrompt.md. Font source is the installed local SimHei font.

Validation logs and screenshots are in this directory. Health 70/100 and stamina 55/100 are temporary runtime test values; no player balance or saved scene stats were changed. The before-scene snapshot is persistent-before.unity.txt. No standalone build was produced.

Validation note: during iterative hot reload, transient Addressables handles became invalid; a fresh Play Mode run successfully completed the mine/town round trip and menu transitions. A legacy GameUIFont texture warning was resolved by reimporting that font asset. Final Console check returned zero errors. Diff whitespace check found only a pre-existing trailing blank line in PlayerController.cs. Tests are runtime/editor validation, not a standalone build.
