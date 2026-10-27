# Verdant cleaver horizontal facing — 2026-10-27

User reported the green-jade heavy dao mirrored horizontally in its resting pose. Confirmed current save equips dao.verdant_cleaver, with18 backpack entries; preserved this user state.

The asymmetric resting artwork inherited the frost weapon's flipHorizontal=true and flipVertical=true. Changed only flipHorizontal to false in twelve resting bindings (six ordinary idle frames and six skill rest/recovery frames). Vertical flags, hand positions, sprites, body crops, sorting and all non-resting action bindings are unchanged. Saved item diff is12 lines changed. BuildSwordCollection's build and repair paths include the same item-specific correction, so reauthoring does not undo it.

Before.png / After.png show actual runtime body and weapon rendering for both character facings. All108 source frames x2 facings passed runtime mapping, mirroring, grip transform, visibility, sorting and identity-rotation checks (216 cases); deposit/withdraw/re-equip/discard checks restored original save. Actual thrust and whirlwind entry succeeded. Console zero errors. Current user save remains18 bag entries with the same equipped dao. Play Mode stopped after validation.
