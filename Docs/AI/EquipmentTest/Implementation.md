Current implementation (2026-10-25): superseded by Animated/Implementation.md; current frost weapon uses authored frame-locked sprites. Historical prototype description follows.

# Temporary weapon swap prototype — 2026-10-23

Assets/player/EquipmentTest/FrostSword.png is the independent generated ice sword. BodyMain.png and BodyExtra.png are new AI-edited weaponless versions of the existing red knight sheets, retaining shields. Original sheets and existing animation references remain untouched.

TestWeaponSwap maps the 108 source sprites to runtime-created body sprites and draws the independent sword in LateUpdate, with frame-specific approximate grip points, facing mirroring and sword angles. B alternates original integrated weapon artwork and the independent frost sword. It changes appearance only: attack timing, damage, stamina and hitboxes are unchanged. Climbing and death hide the independent sword. Shield replacement is not implemented in this prototype.

Temporary B binding is controlled by TestWeaponSwap.temporaryBKey. Disable that flag to restore the HUD's B-to-bag shortcut. The bag remains clickable. Additional weapons can be assigned to the weapon sprite field (upright artwork; pivot at the grip). Multiple new items/equipment inventory and save persistence are outside this test.

Generated body art is an approximation, not a lossless pixel extraction; hand poses and grip anchors need art-direction polish before final production. Runtime sprites are cached and destroyed on teardown. No standalone build was run.

Generated with built-in image_gen. Weapon prompt: Single production 2D fantasy game weapon sprite, an ice-blue longsword for a chibi red-haired knight, upright tip up, silver steel blade with cyan central rune, gold guard, leather grip and blue gem pommel; transparent background, no hands/text/shadow. Sheet edit prompts: preserve original canvas, frame positions and poses; remove every sword, handle and slash/fire effect; reconstruct gripping hands and occluded body; retain shields, armor, hair, cape and boots; transparent background. Inputs: char_red_3.png and char_red_4.png respectively. The original requested canvas sizes were adapted by normalized source rectangles to the actual generated dimensions.

Validation: keyboard B selects new weapon without opening bag; 108/108 sprite mappings; compile and Console checks. Screenshot: FrostSword.png beside this document. Grip alignment is approximate and has not been exhaustively playtested through every attack transition.
