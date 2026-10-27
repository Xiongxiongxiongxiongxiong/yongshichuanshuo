# Ten frame-locked swords

User correction: use the existing FrostMain / FrostExtra authored animation approach. The preliminary 16-pose atlases are not final assets and are not used.

Each sword has an independently painted Main atlas (8 columns x 11 rows, matching FrostMain) and a Supplement atlas (8 columns x 9 rows). Supplement rows 0–6 match FrostExtra; row 7 contains eight isolated attack poses; row 8 contains four upgrade poses. Source body sprites, clip timing/events, hand anchors, body crops and sorting derive from the existing 108 calibrated bindings. Each sword has its own 108 saved bindings. Climb/death and other hidden bindings remain hidden.

Equipment changes presentation only; combat damage, stamina and cooldown remain the existing values. Backpack owns equipment selection and saves the equipped ID with existing v1 inventory data. No repeating starter-item grant is added.
