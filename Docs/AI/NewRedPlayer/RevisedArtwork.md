# Revised red player artwork

2026-10-21: Reimported both source sheets and reapplied character setup after the artwork update.

char_red_3 remains 1069x1472 with 68 slices. All 73 sprite keys in the 14 used Animator clips are explicitly rebound on reapplication, preserving key times and events.

char_red_4 is now 1341x1173 with seven occupied rows (8, 2, 8, 4, 8, 8, 2 frames). It provides 40 slices. Slide uses frames 10-17, thrust uses 4-7, spin uses 22-25, and climbing uses 30-39. Existing action durations, damage and stamina logic are unchanged. Four jump poses 18-21 and unused slash/stance frames remain available without changing the existing jump state machine.

Verified the full extra-sheet slice preview, actual slide playback in town, successful thrust and spin starts and deterministic presentation samples, climbing presentation sample, and all 73 Animator references. Unity Console returned zero errors. Stopped Play Mode after validation. Runtime images and results are alongside this file.

Reapply with Tools/Player/Apply New Red Knight. This is a manual reapplication command, not a background file watcher. Proportional source resizes are supported for slice bounds; rearranged poses require updating the row map.
