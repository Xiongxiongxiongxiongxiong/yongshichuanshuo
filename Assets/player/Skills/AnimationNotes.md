# Green swordsman skills

Generated using the built-in image generation tool, referencing
`Assets/player/png/green/char_green_4.png`.

Prompt: Same chibi silver-gray-haired swordsman, olive/brown leather outfit,
dark round shield and silver sword. Transparent pixel-art animation sheet,
four columns and two rows. Top: windup, rightward dash/stab, strong emerald
thrust, recovery. Bottom: windup, sweeping slash, emerald circular whirlwind,
recovery. Preserve body scale and feet anchors, no text/grid/background.

The generated effects have variable widths. PlayerSwordSkills creates eight
sprites with individual rectangles and body/feet pivots, calibrated to the
existing player's sprite height. Source PNG is retained for editing.

U: wind thrust, 30 stamina, 3 second cooldown, 0.35 seconds, 2x normal damage.
I: whirlwind, 40 stamina, 5 second cooldown, 0.6 seconds, two 0.8x damage pulses.
Integer health rounds each pulse to at least one damage. Existing enemy hit
reactions and invulnerability remain in effect. Skills do not grant invincibility.
