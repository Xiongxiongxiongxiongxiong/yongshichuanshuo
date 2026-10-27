# Scene8 high-density walkable foreground prompts

Built-in image_gen edited four exact quarter references rendered from the existing scene. Each input is543x724; each output is1086x1448. The original2172x724 panorama remains the background texture. Only collider-derived foreground surface bands sample these new textures.

## Common edit prompt

Use case: precise-object-edit, high-resolution game-art refinement. Edit THIS EXACT portrait quarter of an existing 2D side scrolling mine level. Preserve the COMPLETE framing, aspect ratio, camera, composition, silhouette, top ledge positions, bridge angles, all platform locations and every gap EXACTLY. Do NOT redesign, add, move, enlarge or remove any object. Produce a genuinely more detailed HIGH PIXEL DENSITY version, portrait3:4, ideally1536x2048 or at least1086x1448. It must align as an overlay on the original crop. The only creative change is increasing foreground detail and clarity: sharply resolved individual stone chips and layered slate facets on the front rock faces under the horizontal walkable tops, tiny cracks, crisp warm edge highlights, detailed timber grain on walkable decks, clearly readable rock rim pixels. Preserve the rich pixel-art style, blue-violet rock, amber crystals/lantern illumination. This is high density PIXEL ART refinement, not smooth oil paint, not blurry interpolation, no photorealism. Background caves, water, far structures and hanging ceiling should stay compositionally identical and subdued; do not spend detail changing them. Walkable front cliff surfaces and decks should be clearer and slightly brighter than far scenery, but preserve original lighting direction and palette. No outlines or guide lines, no labels, no UI, no characters, no cropping, no new objects. EXACT spatial registration is critical because terrain collision is already authored.

## Per-quarter targets

1. Mine entry and lower treasure cave.
2. Sagging bridge, amber sanctuary plateau, lower timber passage and cave floor; preserve bridge/floor registration.
3. Waterfall stepping islands and eastern terrace; preserve island top heights and silhouettes.
4. High timber shelf and descending mine exit gallery; preserve ledge heights and exit slope.

