# Main town clarity upgrade — 2026-10-22

- Unity 2022.3.20f1c1, URP; SampleScene_25, Edit Mode.
- Baseline: panorama 2172x724, Point filtering, full source resolution already imported. Source detail density was insufficient for close-up rendering.
- Final artwork: 3642x1214 (1.677x resolution on each axis, 2.81x total pixels). Three image_gen region restorations, normalized to original crop coordinates and blended over 48-source-pixel overlaps. Generated fine details differ from original brushwork; this is restored artwork, not exact recovery of missing original detail.
- Scene: original panorama renderer disabled; two padded 1823x1214 RGB24 uncompressed Bilinear sprite textures enabled. No texture exceeds 2048. Combined world bounds retain original 85.3333x28.4583 dimensions.
- Reviewed overview and close-up captures of fountain/inn and castle steps. The central texture join crosses the fountain and has no visible split. Region composition remains aligned at overview scale; this does not constitute a complete manual traversal of every painted stair.
- Before/after exact EditorJsonUtility serialization comparison passed for all Collider2D components, Tilemaps, four climbable-ladder SpriteRenderers, and their transforms. CollisionFingerprint also matched during installation.
- Saved scene reloaded successfully: two live HD SpriteRenderer references, zero missing scripts. Unity compiled the authoring helper; Console returned zero errors/warnings after correction of importer settings access.
- SHA256 checks: persistent.unity, SampleScene_8.unity and SampleScene_24.unity unchanged.
- Full Play Mode traversal, standalone player build and device performance profiling were not performed for this static artwork replacement.

Backups: Scene-before-on-disk.unity.txt and Town-before-current.unity. The latter preserves the editor's pre-existing unsaved scene state before this task. Original panorama is untouched. To revert only the visual upgrade, remove the '05 High Resolution Town Panorama' child and enable its original SpriteRenderer.

Before-0..3.png and After-0..3.png use identical 1920x1080 camera framing. BeforeOverview.png and AfterOverview.png show the full town. ArtPrompt.md records the built-in image tool prompt and processing details.
