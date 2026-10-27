# Registered walkable foreground refinement

Built-in image_gen editing, four exact native-rendered portrait reference quarters per scene 9–17. Scene 18 reuses the four approved Scene 8 refinements because its source panorama is identical.

## Prompt applied to every quarter

precise-object-edit. EDIT TARGET: exact portrait quarter of an existing side-scrolling pixel-art level. Preserve framing, aspect ratio, all platform silhouettes, walking top positions, bridge curves, cliff shapes, gaps and scene composition EXACTLY: overlay alignment with authored collision is essential. Output portrait 3:4 at least1086x1448, twice the input pixel density. Refine foreground walkable stone/soil/wood ledges and their cliff faces with genuinely finer crisp pixel-art detail, small rock facets, cracks, wood grain, vegetation and clear rim highlights. Preserve THIS reference's palette and lighting; don't recolor or redesign. Keep far background composition identical and subdued. No new objects, no removal or movement, no characters, text or UI; no blurry upscale, smooth painting or photorealism. This is a registered detailed game texture, not a new scene.

## Saved assets

Each SceneN directory contains ReferenceQuarter1–4.png, WalkableQuarter1–4.png, four materials and four meshes, BeforeForegroundHD.unity and Overview.png. Scene 18 has no new reference renders because its existing detailed textures are reused.

Background source images are preserved. New images are displayed only through terrain-following foreground meshes. Materials use Yongshi/Walkable Foreground HD with quarter-edge, platform-end, top and bottom feathering. Source collision and gameplay objects are preserved.
