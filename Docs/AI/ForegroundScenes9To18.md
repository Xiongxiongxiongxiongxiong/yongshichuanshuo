# SampleScene_9–18 foreground separation

Saved 2026-10-20. Applies the approved SampleScene_8 treatment to all ten content scenes: independently rendered higher-density walking surfaces/front faces, original panorama retained as a darker background, and feathering at texture quarters and exposed platform ends.

- Assets: Assets/GeneratedForegroundHD/Scene9 through Scene18. Each scene has four foreground meshes/materials/textures and a BeforeForegroundHD.unity backup. Overview.png is an isolated native Unity camera render.
- 36 new images generated with the built-in image_gen editor for scenes 9–17; four existing Scene8 images reused for Scene18's identical original panorama. Prompt set: Assets/GeneratedForegroundHD/GenerationPrompts.md.
- Textures are 1086x1448 except Scene10 quarter4, which is1086x1449. Uncompressed, Point, Clamp, no mipmaps, NPOT scaling disabled. Original background texture dimensions and sprite references unchanged.
- Same shader as Scene8: Yongshi/Walkable Foreground HD. Background tint .55/.60/.69. Foreground sorting order -6. Platform-end UV channel2 is present in every mesh. Scene16 has a wider1.5-world-unit face-end fade to subdue sunset-colored edge leakage; other scenes use.85.
- Paths, collision, ladders, treasure and progression are retained. Ten scenes passed comparison with backups and asset/missing-script checks. Detailed results: Foreground9To18Validation.txt.
- Authoring helpers: UpgradeLevelForeground.Build(number), LevelForegroundReference.Export(number), ValidateLevelForeground.ValidateAll(). Upgrade rejects already-upgraded scenes and unsaved edits.

Validation covers all ten saved scenes and native overview inspection; this is a visual update and does not constitute a complete playthrough of every route.
