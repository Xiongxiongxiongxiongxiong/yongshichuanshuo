# Three mine expansion levels

Created 2026-10-18 using the approved SampleScene_8 V2 workflow. Each newly generated panorama has its own traced collision profiles, ladders, interactive chests, camera bounds, fall/water hazard, completion trigger, retry configuration and Addressable scene data. No leve2 source artwork is used.

| Scene | Theme | Ladders | Chests | Play menu under Tools > Levels |
| --- | --- | --- | --- | --- |
| SampleScene_9 | Blue Quartz Mine / 蓝晶矿脉 | 5 | 3 | Play Mine 9 - Blue Quartz |
| SampleScene_10 | Abandoned Minecart Foundry / 废弃矿车工坊 | 5 | 3 | Play Mine 10 - Minecart Foundry |
| SampleScene_11 | Amethyst Ruins Mine / 紫晶遗迹 | 4 | 3 | Play Mine 11 - Amethyst Ruins |

The editor play menus start persistent and wait for the menu loading transition to finish before requesting the selected scene. The saved New Game destination remains SampleScene_8 at its mine entrance.

## Asset locations

- Scenes: Assets/Scenes/SampleScene_9.unity, SampleScene_10.unity, SampleScene_11.unity.
- New panoramas, materials, overview renders, collision review renders and exact generation prompts: Assets/GeneratedMineExpansion.
- Existing generated ladder/chest sprites from Assets/GeneratedMineV2 are reused for consistent interactive artwork.
- GameSceneSO assets: Assets/DataSO/GameScene/Game Scene9.asset, Game Scene10.asset, Game Scene11.asset.
- Builder: Assets/Editor/BuildMineExpansion.cs. It rejects existing scene paths to protect authored changes.
- Preview launchers: Assets/Editor/PlayMineExpansion.cs.

As with the approved V2, the environment is a single panoramic painted sprite with independent collision and interaction objects. This is not a modular tile set; major illustrated terrain changes require regenerating/editing the panorama and updating collision.

## Validation observed in Unity

All three scenes compiled, saved and imported. Scene inspection found zero missing scripts and three Chest components per scene. Collision profiles were rendered over each panorama and visually reviewed. Previews isolate the rendered scene from other loaded scenes.

Final physics ray probes hit terrain for every authored profile: scene 9 = 12/12, scene 10 = 14/14, scene 11 = 10/10. Retry references point to their own GameSceneSO assets. An actual New Game button regression check loaded SampleScene_8 with the player grounded at (4.83,13.75).

Actual Play Mode checks through the persistent loader:

| Scene | Spawn position / grounded | Tested ladder endpoint / grounded | Chest interaction | Exit |
| --- | --- | --- | --- | --- |
| 9 | (4.08,14.87), true | (56.50,20.37), true | Lower Quartz Cache opened | LevelCompleteUI opened, timeScale=0 |
| 10 | (3.96,18.50), true | (68.50,16.46), true | Hoist Island Cache opened | LevelCompleteUI opened, timeScale=0 |
| 11 | (3.96,15.12), true | (55.50,16.83), true | Crystal Spire Cache opened | LevelCompleteUI opened, timeScale=0 |

Ladder tests used a temporary virtual W keyboard, removed after each test. Chest TryOpen was exercised while the player was in interaction range. Exit checks moved the player into the actual trigger. These checks do not constitute full manual traversal of every jump or every alternate route.

The pre-existing dynamic GameUIFont texture warning recurred during Play Mode. No new scene-specific exception was observed. Standalone builds, all retry buttons and every route have not been tested.
