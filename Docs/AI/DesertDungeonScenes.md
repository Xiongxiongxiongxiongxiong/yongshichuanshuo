# Desert and dungeon scenes

Created and saved2026-10-21 using the established generated panorama + traced collision + independent foreground workflow.

| Scene | Theme | Route emphasis |
|---|---|---|
| SampleScene_19 | Oasis Caravan Trail / 绿洲古道 | Palm market, oasis islands, lower stone grotto, upper caravan terrace |
| SampleScene_20 | Wind-carved Canyon / 风蚀峡谷 | Rope crossing, fossil mesa, sheltered smugglers, high wind shrine |
| SampleScene_21 | Lost Sapphire Sand City / 失落沙城 | Moonlit mosaic arcades, buried crypt, upper observatory |
| SampleScene_22 | Ironchain Prison / 铁链监牢 | Chain guardwalk, barred cells, stone pillars above spike pit |
| SampleScene_23 | Underground Waterworks / 地下水道 | Pump deck, dry maintenance passage, cistern stepping pillars |
| SampleScene_24 | Shadow Royal Crypt / 暗影王陵 | Upper shrine, lower candlelit crypt, grave pillars and rune doorway |

Each scene includes three interactive chests, three or four correctly proportioned Tiled ladders, existing climbing Tilemap and composite terrain conventions, camera bounds, a fall hazard, completion/retry UI and Addressables scene/data registration. Prison has a separate spike-pit death trigger; waterworks has a separate deep-water trigger. New Game routing and existing scenes/player are unchanged.

Art, generation prompts, collision-review renders and final overviews are in Assets/GeneratedDesertDungeon. Six image_gen outputs are natively2172x724. Original files are retained. Foreground uses a separate native-resolution texture and terrain-following mesh; background importer is limited to1024 pixels and tinted. Shader includes exposed-platform-end, top/bottom and image-edge feathering. No quarter boundaries, and no artificial upscaling of the new foreground images.

Authoring: BuildDesertDungeonScenes.BuildAll; rejects overwriting existing scenes. DesertDungeonForeground.Build applies separated rendering. PlayDesertDungeonScenes supplies six Tools > Levels > Play Desert19–21 / Play Dungeon22–24 menu entries. Those load persistent and use the existing SceneLoader event and individual spawn positions.

Validation: all six saved scenes passed solid-spawn-floor, proportional-ladder, chest-count, completion/retry link, Addressables, supported-foreground-shader and missing-script checks. All six loaded in Play Mode with a live player standing at spawn and active foreground. Actual player entry into Scene24's goal opened completion UI and paused time. Console showed only the known GameUIFont texture issue. Final overviews visually inspected. This does not claim a complete manual playthrough of all routes; enemies and custom combat encounters were not added as part of environment authoring.

Evidence: Assets/GeneratedDesertDungeon/Validation.txt and RuntimeValidation.txt. Existing global shader, player, menus, scene routing and combat code were not changed.
