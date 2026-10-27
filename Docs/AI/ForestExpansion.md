# Three forest scenes

Created 2026-10-18 using the approved full-panorama environment with separate playable geometry and interactive objects.

| Scene | Theme | Ladders | Chests | Tools > Levels play menu |
| --- | --- | --- | --- | --- |
| SampleScene_12 | 林间溪谷 / Sunlit Forest Creek | 4 | 3 | Play Forest 12 - Creek Valley |
| SampleScene_13 | 古树村落 / Ancient Tree Village | 5 | 3 | Play Forest 13 - Ancient Tree Village |
| SampleScene_14 | 月光遗迹 / Moonlit Forest Ruins | 4 | 3 | Play Forest 14 - Moonlit Ruins |

Each scene includes its own terrain profiles, CompositeCollider2D for ladder compatibility, climbable ladder Tilemap, three interactive treasure chests, camera bounds, fall/deep-water hazard, exit trigger, completion UI and retry scene reference. Addressables registration is in the existing Scenes group.

## Source and usage

- New artwork and overview/collision-review renders are in Assets/GeneratedForestExpansion. Exact built-in image_gen generation prompts are saved in GenerationPrompts.md in that folder.
- Interactive chest/ladder artwork is reused from the approved Assets/GeneratedMineV2 sprites. Environment panoramas are newly generated for these forest scenes.
- GameSceneSO assets are Assets/DataSO/GameScene/Game Scene12.asset through Game Scene14.asset.
- Editor builder: Assets/Editor/BuildForestExpansion.cs. It rejects existing target scene assets to protect manual changes.
- Editor play menus: Assets/Editor/PlayForestExpansion.cs. These start persistent and wait for the initial Menu load before requesting the selected scene. Existing New Game configuration was not modified.
- As with the approved mine workflow, scenery is a single painted panoramic sprite, with independent collision and interactive objects. Major illustrated terrain changes require panorama edits and collision updates; this is not a modular tilemap scenery set.

## Verification observed

- All scenes compiled/imported/saved, with zero missing scripts and three Chest components each.
- Collision outlines were overlaid and visually checked against the painted route.
- Physical ray probes hit all terrain profiles: scene12 13/13, scene13 12/12, scene14 12/12.
- Retry references each point to the matching GameSceneSO; all scenes are registered as Addressable.
- Actual Play Mode loading through persistent succeeded for each scene.

| Scene | Spawn / grounded | Tested ladder top / grounded | Chest opened | Exit completion |
| --- | --- | --- | --- | --- |
| 12 | (4.00,12.83), true | (66.50,22.08), true | Root Grotto Cache, true | UI open, timeScale=0 |
| 13 | (4.00,14.04), true | (43.50,23.83), true | High Treehouse Cache, true | UI open, timeScale=0 |
| 14 | (4.00,10.96), true | (52.50,18.37), true | Moon Shrine Cache, true | UI open, timeScale=0 |

Ladder tests used a temporary virtual W keyboard that was removed afterward. Chest TryOpen ran with the player in range. Exit tests moved the player into the real trigger. Full manual traversal of all jumps, every branch, all retry buttons and standalone builds were not performed. The existing dynamic GameUIFont texture warning appeared again; no scene-specific runtime exception was observed.
