# SampleScene_8 mine level

Created using leve2_1, leve2_2 and leve2_BG. Existing project water and treasure artwork is reused; no image generation was necessary.

- Main path: entrance → minecart shelf → stepped gallery → waterfalls → summit → exit.
- Optional paths: upper treasure cache and lower cavern, connected by six ladders.
- Three interactive chests, three waterfalls, deep-water hazards, physical ground/bridge colliders and a completion trigger.
- Play from `Tools > Levels > Play Mine SampleScene_8`, which starts persistent and loads this scene via its existing GameSceneSO. Directly playing a content scene does not instantiate the shared player.
- Original unsaved scene was preserved at `Assets/Scene8Preview/SampleScene_8_BeforeMine.unity` before conversion.
- Overview: `Assets/Scene8Preview/Mine_Overview.png`.

## Validation performed

- Unity C# compilation and final Console inspection: no errors.
- Scene serialization: saved, no missing scripts or SpriteRenderer sprite references.
- Addressables: SampleScene_8 is registered in the existing Scenes group, preserving the existing scene GUID and GameSceneSO.
- Actual Play Mode loading through persistent succeeded; player settled at (3, 8.04) with grounded=true and the shared camera following.
- Player relocation to the gallery and lower cavern settled correctly at y=12.04 and y=0.04, grounded=true.
- Entering the exit trigger opened the existing LevelCompleteUI and paused the game (timeScale=0).
- Overview rendered and visually inspected after final terrain texture adjustment. Terrain colliders were unchanged by this adjustment.
- A dynamic GameUIFont texture warning appeared during Play Mode. No related runtime exception occurred.
- Full manual traversal, ladder input, chest opening, retry-button clicks and a standalone player build were not tested.
