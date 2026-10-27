# Climbable ladder proportions

2026-10-20: Fixed48 ladders across SampleScene_8–18. Previous Simple SpriteRenderers stretched a single whole ladder from fixed width to arbitrary height (Scene8 longest scale.2927x/.9512y). This changed rung spacing and thickness.

Created LadderRepeat.asset from one92-pixel-high middle rung in the existing MineProps texture, using a full rectangular Sprite. Tiled/Continuous SpriteRenderers now repeat that segment at uniform world X/Y scale, with renderer.size supplying the original height. No new raster image was generated. Width, world bounds, position, sorting, materials, collision and hidden Forest Decorations climbing Tilemap are preserved.

Updated the mine/forest/common panorama builders to use FixClimbableLadderProportions.Configure, preventing future regenerated ladders from stretching. Sprite and draw mode are assigned before scale because the Editor otherwise adjusts scale during the sprite transition.

Each scene has a pre-fix backup under Assets/LadderProportionFix. Assertions verify unchanged ladder bounds, terrain collision fingerprint and serialized climbing Tilemap. All48 saved renderers checked for uniform scale and Continuous tiling. Scene8_After_Closeup.png provides native Unity visual verification.
