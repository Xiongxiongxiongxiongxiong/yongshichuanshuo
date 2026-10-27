# Modular adventurer v1

Transparent design draft generated with the built-in image generation tool.
The unarmed body and standalone sword are separate silhouettes on one source image.
The concept remains as a reference. The standalone ShortSword sprite is now sliced
from its right-hand side; the body is animated using AdventurerActions-v1.png.

## Integrated player

The persistent scene player now has a ModularPlayerVisual component, a Modular Body
child, and WeaponSocket/Equipped Weapon. The body sheet contains 24 empty-handed
frames: six idle, six run, six attack, then jump, fall, crouch, hurt, death and climb.
Running now uses the dedicated eight-frame AdventurerRun-v2.png (135 pixels per
unit), replacing the six original run frames. The original six remain unused for
normal running. The run phase advances by travel distance: one cycle per 4.2 local
units scaled into world space. Idle plays 0,1,2,3,4,5,4,3,2,1 and uses individually
measured hand positions. Idle sprite pivots have been aligned to remove lateral
drift. WeaponSocket inherits the body's transform before applying the mirrored
per-frame grip, so authored idle movement carries the weapon.
Slide reuses crouch; weapon upgrade reuses the raised-hand pose. Climbing currently
uses one held pose rather than a full climbing cycle.

The original root SpriteRenderer is hidden via forceRenderingOff, while the existing
Animator, skill/slide scripts, colliders, stamina and attack timing continue unchanged.
New child renderers follow the root's facing, color, material and sorting layer.
Attack trails are white; sword skills have green trails. Legacy character art is not
shown by skills, sliding, climbing or upgrading.

To replace a weapon in the Inspector, change the sprite on Equipped Weapon. Runtime:
`GetComponent<ModularPlayerVisual>().EquipWeapon(sprite, scale)`; pass null to unequip.
Author replacement weapon sprites pointing upward, with their pivot at the center
of the grip. Weapon swaps here change presentation only; damage and weapon-specific
combat behavior remain owned by the existing gameplay scripts.

Import uses Point filtering, uncompressed textures, no mipmaps, alpha transparency.
The body is 80 pixels per unit; the sword is 280 pixels per unit. Measured per-frame
pelvis/foot anchors and hand offsets are in InstallModularPlayer.cs. Reapply using
Tools > Player > Apply Modular Adventurer with persistent open, outside Play Mode.
The installer intentionally saves the persistent scene. Disable ModularPlayerVisual
to restore the original visual. The pre-integration scene backup is retained under
Docs/AI/ModularPlayer/persistent-before.unity.txt.
For only the run/idle correction, use Tools > Player > Fix Run and Idle Weapon.
The focused correction changes only the visual component's scene data and the
owned sprite imports; it preserves weapon selection and other gameplay settings.

Built-in image generation was used for both concept and actions. Original source
images are preserved. The sprites are independent subassets, not separately rewritten
bitmap files.

## Generation prompt

Use case: stylized-concept. Asset type: modular pixel art character asset design for a 2D side-scrolling fantasy Unity game. Reference image: existing green-clothed small pixel adventurer in the conversation, use only as genre and scale inspiration. Create a redesigned young adventurer, ash brown tousled hair, muted sage green short tunic, brown leather belt bracers and boots, readable charming silhouette, restrained classic pixel art, dark outlines, crisp square pixels, no painterly texture. Transparent background. Layout exactly two widely spaced isolated subjects: on LEFT a single full-body unarmed adventurer in right-facing three-quarter side-view idle pose, BOTH HANDS EMPTY and visible, forward hand posed naturally to receive an interchangeable weapon; on RIGHT a SINGLE standalone steel short sword matching the same pixel scale and art style, vertical blade pointing up, simple brown grip, unobstructed entire silhouette, no hand attached. Character approximately 2 times sword height. Large transparent gap between body and sword, neither touching. No shield, no weapon on the character, no sheath or back weapon, no effects, no shadows, no ground, no text, no labels, no grid, no extra poses. This is a clean modular asset design draft, not a screenshot or illustrated scene.
