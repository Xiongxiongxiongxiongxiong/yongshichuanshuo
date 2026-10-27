# Amberfall Mine V2

SampleScene_8 was remade with newly generated artwork. The scene has zero dependencies on leve2_1, leve2_2 or leve2_BG.

## Assets and structure

- Built-in image_gen generated Assets/GeneratedMineV2/MinePanorama.png and MineProps.png. Exact generation prompts are preserved in Assets/GeneratedMineV2/GenerationPrompts.md.
- The main environment is a single painted panoramic sprite. Ground profiles, climbable ladders and interactive chests are separate scene objects. This is not a modular terrain tile set; changing the illustrated terrain substantially requires editing/regenerating the panorama and retracing collision.
- MinePanoramaWater.shader adds subtle animation in the blue waterfall region.
- Twelve shallow polygon terrain paths feed a static CompositeCollider2D. This also allows the existing PlayerController ladder collision handling to work.
- Four climbable ladders connect the lower mine and eastern terraces. Three chests use the newly generated closed/open sprites, retaining the existing Chest interaction and upgrade behavior.
- Play using Tools > Levels > Play Mine SampleScene_8. The editor launcher starts persistent, loads the existing Addressable scene, and uses the updated entrance position.
- Previous version backup: Assets/Scene8Preview/SampleScene_8_BeforeV2.unity.
- Final overview: Assets/Scene8Preview/MineV2_Overview.png. MineV2_CollisionReview.png shows traced collision during construction.

## Observed validation

- Unity compiled without errors; final Console error query returned zero entries.
- Scene saved with zero missing scripts and zero leve2 texture dependencies.
- In Play Mode through persistent: player settled at (4.83,13.75), grounded=true.
- Virtual keyboard W input exercised the lower-mine ladder: player reached the upper route at y=15.37 and settled grounded=true. Validation keyboard was removed afterward.
- Lower treasure TryOpen returned true in player proximity, changing the displayed sprite to ChestOpen.
- Entering the exit trigger opened LevelCompleteUI and set timeScale to zero. Play Mode was stopped afterward.
- Collision silhouettes were rendered over the panorama and visually checked.
- Existing GameUIFont texture warning appeared during Play Mode. No related exception occurred.
- Full manual traversal, every jump, retry-button click and standalone build were not tested.
