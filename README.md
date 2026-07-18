# com.sipvlib.tutorial

Part of [SiPVLib](https://github.com/phajmvawnsix/SiPVLib). A node-graph-based in-game tutorial/onboarding sequencing system (`TutorialManager`/`TutorialConfig`) with pluggable actions (force-touch, show/hide view, invoke/listen/wait-for events, checkpoints) and branch conditions (UserData, view/UI state, tutorial active/completed), plus an optional Odin-powered Tutorial Graph editor window for authoring flows visually.

## Install

Add to your project's `Packages/manifest.json`:

```json
"com.sipvlib.tutorial": "https://github.com/phajmvawnsix/com.sipvlib.tutorial.git",
"com.sipvlib.config": "https://github.com/phajmvawnsix/com.sipvlib.config.git",
"com.sipvlib.debugging": "https://github.com/phajmvawnsix/com.sipvlib.debugging.git",
"com.sipvlib.event": "https://github.com/phajmvawnsix/com.sipvlib.event.git",
"com.sipvlib.pool": "https://github.com/phajmvawnsix/com.sipvlib.pool.git",
"com.sipvlib.ui": "https://github.com/phajmvawnsix/com.sipvlib.ui.git",
"com.sipvlib.userdata": "https://github.com/phajmvawnsix/com.sipvlib.userdata.git",
"com.sipvlib.utilities": "https://github.com/phajmvawnsix/com.sipvlib.utilities.git",
"com.cysharp.unitask": "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask"
```

UPM does not automatically resolve nested git dependencies — you must add the `com.sipvlib.*` and UniTask entries above yourself alongside this package. `com.unity.visualscripting` resolves automatically from Unity's package registry.

## Optional: Odin Inspector

This package integrates with [Odin Inspector](https://odininspector.com) (Sirenix) if you have it installed, but does NOT require it and does NOT bundle it — Odin is a paid Unity Asset Store asset and cannot be redistributed here.

- **Without Odin installed**: All runtime action/condition classes (e.g. `TutorialActionForceTouch`, `TutorialActionInvokeEvent`, `TutorialActionListenEvent`, `TutorialActionWaitUserData`, `TutorialNodeTargetConditionUserData`) work fully — their fields (`onTimeout`, `compareMode`, `valueLong`/`valueInt`/`valueDouble`/`valueFloat`/`valueString`/`valueBool`, etc.) are still serialized and editable via the plain Unity Inspector, just without Odin's conditional `[ShowIf]` show/hide behavior — all fields are always visible instead of being hidden based on the selected data type/comparison mode. The `SiPV.Tutorial.Editor` assembly (the visual Tutorial Graph editor window) is unavailable entirely — its assembly won't compile without Odin.
- **With Odin installed** (purchase + import from the Asset Store, which auto-defines the `ODIN_INSPECTOR` scripting define symbol): the `[ShowIf]` attributes light up so only the relevant value field for the selected data type/comparison mode is shown, and the Tutorial Graph editor window (`SiPV/Tutorial Graph Editor` menu) becomes available for visually authoring tutorial node graphs.

No manual setup is needed beyond installing Odin itself — detection is automatic via the `ODIN_INSPECTOR` define.

## Documentation
- [Usage guide](USAGE.md) — original module documentation carried over from the SiPVLib monolith
