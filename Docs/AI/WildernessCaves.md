# Three wilderness mountain cave levels

Created and saved 2026-10-18, continuing the approved painted panorama plus independent collision/interaction workflow.

| Scene | Theme | Ladders | Chests | Tools > Levels test menu |
| --- | --- | --- | --- | --- |
| SampleScene_15 | 苔岩溪洞 / Moss Creek Cave | 4 | 3 | Play Cave 15 - Moss Creek |
| SampleScene_16 | 赤岩峡洞 / Red Rock Canyon Cave | 5 | 3 | Play Cave 16 - Red Rock Canyon |
| SampleScene_17 | 霜雾山洞 / Frost Mist Mountain Cave | 4 | 3 | Play Cave 17 - Frost Mist |

These levels emphasize natural cave mouths, open-air mountain ravines and interior shelter ledges. They are distinct from the industrial mine levels. All include camera bounds, terrain collision, ladder tilemaps, treasure, fall/deep-water hazards, exit triggers, completion UI and their own retry scene data. Existing New Game configuration is unchanged.

## Assets

- Scene files: Assets/Scenes/SampleScene_15.unity through SampleScene_17.unity.
- Newly generated panoramas, final overview images, collision-review renders and exact built-in image_gen prompts: Assets/GeneratedWildernessCaves.
- Approved interactive ladder/chest sprites are reused from GeneratedMineV2.
- GameSceneSO assets: Assets/DataSO/GameScene/Game Scene15.asset through Game Scene17.asset. All three scenes are registered in the existing Scenes Addressables group.
- Builder: Assets/Editor/BuildWildernessCaves.cs, using the editor-only PanoramaLevelAuthoring helper. Building rejects existing target scene/data assets to preserve manual work.
- Runtime preview launchers: Assets/Editor/PlayWildernessCaves.cs; they start persistent and wait for the initial menu transition before requesting the target scene.
- The environment uses a single panoramic sprite with separately authored geometry and interaction objects, rather than a modular terrain tile set.

## Observed validation

- Unity scripts compiled and assets imported successfully.
- Each scene saved with zero missing scripts, three Chest components and matching retry scene data.
- Physical ray probes hit every terrain profile: scene15 12/12, scene16 13/13, scene17 11/11.
- Collision outlines were rendered over each panorama and visually reviewed.
- Actual Play Mode loading through persistent succeeded for every scene.

| Scene | Spawn / grounded | Ladder endpoint / grounded | Chest interaction | Exit |
| --- | --- | --- | --- | --- |
| 15 | (4.00,13.62), true | (69.50,22.08), true | High Moss Cave Cache opened | Completion UI open, timeScale=0 |
| 16 | (4.00,17.71), true | (73.50,24.17), true | High Red Shelter Cache opened | Completion UI open, timeScale=0 |
| 17 | (4.00,14.83), true | (51.50,21.67), true | Alpine Lookout Cache opened | Completion UI open, timeScale=0 |

Ladder tests used a temporary virtual W keyboard, removed after each test. Chest TryOpen ran with the player in range. Exit tests moved the player into each real trigger. Full manual traversal of every jump and branch, all retry buttons and standalone builds were not performed. The existing dynamic GameUIFont texture warning recurred; no scene-specific runtime exception was observed.
