# Base-02 HUD replacement

Replaced the shared player HUD in Assets/Scenes/persistent.unity using five named sprite slices from Assets/Legacy-Fantasy - High Forest 2.3/HUD/Base-02.png. Source image and GUID preserved. Uses the brown leaf portrait/frame, red health, gold damage trail and yellow stamina fill. Left/top anchoring and bar geometry adapted to the new artwork. PlayerStatBar references and runtime scripts unchanged.

Validation: Unity Play Mode in the main town; full HUD screenshot HUD-runtime.png. Set health through OnHealthChange(0.5), damage trail to 0.85, and stamina through TrySpend to 0.4; verified Image Filled horizontal rendering and valid health/stamina references, screenshot HUD-partial.png. No Unity Console errors during HUD runtime validation. After stopping Play Mode and restoring the editing scene, Console reported asset-destruction and GameUIFont dynamic-font cleanup errors; these are outside the modified sprite/HUD code and remain unresolved. Play Mode stopped and original SampleScene_8 editing scene restored. Menu, completion screens and boss HUDs are outside this player HUD replacement.

