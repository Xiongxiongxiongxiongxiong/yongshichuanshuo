# Resolution and language settings — restart recovery check

## Final follow-up — 2026-10-25

The user stopped the previous turn while the asynchronous Addressables build was pending. It completed successfully: ContentBuild-Recovery.txt contains SUCCESS. The latest build layout is buildlayout_2026.10.24.13.10.26.json, BuildError is empty and duration is 52.86 seconds. All 12 bundles exist and match the report's sizes (480,557,068 bytes combined); catalog references and its hash agree with the report. The content includes persistent, three StringTables, locale/font assets and all three title backgrounds. Related feature files were saved before that successful build.

Rechecked on disk: both dropdown references resolve, all three table/font/title-art arrays have valid asset GUIDs, and 171 keys ×3 locales have 513 nonempty values. Existing scene/prefab UI labels are covered by the exact/subphrase translation rules. No unfinished implementation or content-build step was found. The current Editor is connected and has persistent open in Edit Mode.

Still outside the completed Editor/content checks: full standalone player runtime validation of actual window resolution/fullscreen. Ordinary Build Settings currently have no enabled boot scene; a release/player build must explicitly include persistent as its startup scene. No Build Settings were changed during this follow-up.

Verified 2026-10-23 after the user's forced computer restart. The initial implementation had been saved in persistent: five resolution options, three language options, three font assets and three 160-entry StringTables. The initial task had not reached complete validation.

Completed during recovery:

- Added 11 missing scene/legacy UI translation keys; now 171 unique source keys in each of zh-Hans, en and zh-Hant. Mechanical check: 513 table values checked, no missing/empty values. Eight additions cover actual Boss/forest/entrance world labels.
- Scan TMP text outside Canvas as well as Canvas TMP, legacy UI.Text and TextMesh. Runtime presenters retain their source text; adapters translate new dynamic values and apply locale fonts. Added English/CJK auto-size support for longer labels.
- Corrected a locale-event ordering issue when a dropdown or presenter changes its own text immediately before switching locale.
- Improved dropdown arrows, popup contrast, option hit area (44 reference pixels) and placement above the control. Escape closes a dropdown before closing the settings modal.
- Added localized English and Traditional title/background art, including subtitle, so text baked into the original picture also follows language selection. Original approved Simplified background retained.
- Menu, settings, HUD, character panel, map, location names, gameplay hints and Boss warning phrases use the three-language data. Repeated runtime source updates do not overwrite the selected display language.

Observed runtime checks:

- Resolution dropdown expanded with five items: 1280×720, 1366×768, 1600×900, 1920×1080, 2560×1440. Selecting the actual popup Toggle changed caption and saved preference.
- Language popup expanded with 简体中文 / English / 繁體中文. Selecting Traditional via the popup changed locale, displayed label and TMP font to GameUI-zh-Hant.
- Restarting Play Mode restored Traditional and 1280×720, including the matching Traditional background image.
- Continue loaded town; HUD alpha 1, live player, map opened successfully. English map showed all 17 translated destination buttons. Active text scan found no remaining Chinese in English mode. Character panel's numeric health/stamina/damage and skill descriptions translated correctly. Switching to Traditional updated map and HUD again.
- Traditional glyph preparation reported an empty missing-character list. The final actual ScreenCapture images were visually reviewed; the old all-black Traditional-Options.png is superseded by Recovery-LanguageOptions-Final.png.
- Current preferences and checkpoint were backed up to Prefs-recovery-before.txt and restored afterwards. Play Mode exited; no gameplay scene runtime state was saved.

Evidence images:

- Recovery-LanguageOptions-Final.png — final Traditional settings and language popup.
- Recovery-ResolutionDropdown.png — English resolution popup (before final row-height polish).
- Recovery-Map-English.png / Recovery-Map-Traditional.png — live HUD and adventure map.
- Recovery-Character-English.png — live character details.

Limits: Screen.SetResolution deliberately runs in a standalone player, not the Unity Editor Game View; Editor validation confirms selection, saving and restoration, not physical window-size changes. Native fullscreen and a complete standalone player build were not tested. The title images were localized with the built-in image_gen tool; prompts and generated source images are preserved in the project/chat.

The first automated dropdown Show attempt ran in the same frame as enabling the modal, before TMP's Start had initialized its tween runner. Retesting after normal control initialization passed. This was test timing, not a persistent Console error. ContentBuild-Recovery.txt records the separate Addressables content-build result.
