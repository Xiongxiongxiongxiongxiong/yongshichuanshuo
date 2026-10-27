# 焱烬行者 — persistent title menu

Implemented from approved TitleScreen-v2.png. Open Assets/Scenes/persistent.unity and press Play. The title Canvas is serialized directly in persistent, sorting order 6 below the existing fade Canvas (10). Existing SceneLoader continues to load the addressable Menu scene; only that scene's legacy Canvas and Memu presenter are disabled. Player, HUD, travel data, scene GUIDs and checkpoint format are retained.

- Four real uGUI buttons: 开始冒险, 继续旅程, 设置, 退出游戏. Missing/invalid checkpoint disables Continue. Existing NewGame/ContinueGame own all checkpoint/travel behavior.
- Built-in image_gen produced the clean TitleBackground.png from the approved concept. The game logo/subtitle remain artwork; menu labels and controls are live TMP/uGUI elements. EmberMenuFrame draws resolution-independent brass frames, hover/selection, pressed and disabled states.
- Settings: master, music and effects volume, fullscreen preference, mute control. Audio respects authored BGM/FX source gains. Preferences use EmberWalker.Settings.* and save on settings close, travel, mute/fullscreen changes and application quit. Settings use the persistent AudioManager sources; no mixer changes.
- Fixed 1920x1080 composition fits the Canvas in LateUpdate after CanvasScaler; non-16:9 views letterbox without cropping the title or buttons.
- Installer: Assets/Editor/InstallEmberTitleMenu.cs, rejects a duplicate installation. Runtime: EmberTitleMenu.cs and EmberMenuFrame.cs. SceneLoader adds only the read-only CanContinue property for this change.

Validation in Play Mode:

- Startup loads Menu, title alpha 1, Start selected; exactly one EventSystem.
- GraphicRaycaster hit Settings; pointer click opened the modal. UI navigation moved Start → Continue. Queued Input System Enter opened Settings. Escape was queued next; its close result was not captured before the later editor interruption.
- Master/music/effects sliders set to .65/.4/.75; actual gains .65/.1104/.75 (authored music base .276). Closing saved preferences; Play Mode restart restored all three values. Mute set master output to zero; fullscreen preference/label toggled. Native fullscreen behavior requires a standalone player and was not tested in Editor.
- Continue loaded Game Scene25 with live player and title alpha 0; ReturnToMainMenu restored title alpha 1. New Game also loaded town and hid title; return succeeded. Temporarily missing checkpoint disabled Continue and restored correctly.
- Final captures reviewed at 1920x1080, 1366x768 and 1440x1080. Settings-Final.png shows the corrected slider geometry and opaque modal background. Title-Runtime-1.png and *-Final.png are final visual evidence; earlier captures show intermediate layouts.
- Scripts compiled successfully. Final editor reconnection showed saved title component present in persistent and zero Console errors.
- A one-off validation command used an incorrect index while removing temporary Game View resolutions and stalled the editor. This command was not saved in project code. The editor restarted/reconnected; temporary recovery helpers were removed and saved scene verified. A bounded cleanup later found no temporary Game View entries. No such loop exists in the shipped scripts.
- Test-created audio/fullscreen preferences removed after verification; current valid town checkpoint retained. Initial checkpoint backup was in SessionState, which was lost on editor restart, so byte-for-byte restoration of the original checkpoint could not be independently confirmed. Both Continue before New Game and subsequent New Game loaded the same town scene.
- No standalone build or application quit invocation (which would close the session) was performed. Application.Quit is wired to the exit button.

Backups: persistent-before.unity and Menu-before.unity.txt. Restore those plus remove the new title scripts/art to revert this menu change. Do not overwrite later gameplay edits with these backups.
