# SiPV.Tutorial

Graph-authored, data-driven tutorials. A tutorial is a `TutorialConfig` asset holding a list of
**nodes**; each node runs **actions** (show a view, wait for an event, force a tap, …) and then
routes to the next node — linearly or by evaluating **conditions**. Progress (checkpoints,
completion, active/paused state) persists through `SiPV.UserData`, and every lifecycle step is
broadcast through `SiPV.Event` so the rest of the game can react without referencing this module.

Depends on `SiPV.Config` (`GameConfig`/`ConfigManager`/`ConfigRef`, `CompareUtils`),
`SiPV.Event` (`EventManager`), `SiPV.UserData` (`UserDataManager`), `SiPV.UI`
(`UIManager`/`UIView`/`ViewConfig`), `SiPV.Debugging` (`CustomLog`), `SiPV.Utilities`
(`MonoSingleton`, `DeepClone`), Odin, and UniTask.

---

## Concepts

| Type | Role |
|---|---|
| `TutorialConfig` | One tutorial asset. Holds `priority`, `isRepeatable`, `isSkippable`, and the `nodes` list. Cloned per run via `CloneAsRuntimeInstance()` (Odin binary `DeepClone`) so the asset is never mutated at runtime. |
| `TutorialNode` | A step. Optional `delayTime`, `blockInput`, `actions[]` (run in parallel), `onCompleteActions[]` (run after), and `nextNodeId`. |
| `TutorialNodeConditional` | A node that branches: evaluates `conditions[]` in order and jumps to the first met condition's `targetNodeId`, else falls back to `nextNodeId`. |
| `TutorialAction` | A unit of work inside a node. Completes synchronously or asynchronously; the node advances once all its actions complete. |
| `TutorialNodeTargetCondition` | A predicate used by conditional nodes (UserData value, view active, tutorial completed, …). |

Nodes/actions/conditions are discovered by reflection (`TypeCache`), so **any new subclass with a
`[TutorialActionLabel]` / `[TutorialNodeLabel]` / `[TutorialConditionLabel]` attribute appears in
the graph editor automatically** — no editor code changes.

## Editing — Tutorial Graph

`SiPV ▸ Tutorial Graph Editor`, or double-click a `TutorialConfig` asset. Right-click the canvas to
add nodes; select a node to add actions / conditions. Drag ports to wire flow (Next / Condition) and
actions. Toolbar: Save, Validate, Auto Layout, Entry, Rebuild, Expand/Collapse, Auto-Save. Node 0 is
the entry node. `Validate` reports missing ids, dangling references, unreachable nodes, and
misconfigured actions/conditions.

## Quick start

```csharp
// 1. Boot (after UserDataManager is initialized so checkpoints/resume work).
await TutorialManager.Instance.Init();

// 2. Start a tutorial by its config Id.
TutorialManager.Instance.StartTutorial("intro");

// 3. React to lifecycle from anywhere.
EventManager.Add<TutorialEndEvent>(TutorialManager.EventTutorialEnd, e =>
{
    if (e.tutorialId == "intro" && e.isFirstTime)
        GrantFirstTimeReward();
});
```

## Manager API

| Method | Effect |
|---|---|
| `UniTask<bool> Init(onSuccess, onError)` | Loads + sorts configs, builds lookup, and **auto-resumes** any tutorial still marked active from a previous session (restoring paused state). |
| `bool StartTutorial(id)` | Clones + starts if eligible (`IsCanStart`); resumes from the saved checkpoint if one exists. Fires `Start`. |
| `bool SkipTutorial(id)` | If `isSkippable`, cancels and fires `Skip` + `End(isSkipped:true)`. |
| `bool PauseTutorial(id)` | Halts node-to-node progression, persists paused state, fires `Pause`. |
| `bool ResumeTutorial(id)` | Resumes, clears paused state, fires `Resume`. |
| `bool IsTutorialActive(id)` / `bool IsTutorialPaused(id)` | Query state. |

## Events

All fire via `EventManager.Invoke<T>`; subscribe with `EventManager.Add<T>(key, handler)`.

| Key constant | Payload | When |
|---|---|---|
| `EventTutorialStart` | `TutorialStartEvent { tutorialId, isFirstTime }` | After a tutorial starts. |
| `EventTutorialEnd` | `TutorialEndEvent { tutorialId, isSkipped, isFirstTime }` | Completed or skipped. |
| `EventTutorialSkip` | `TutorialSkipEvent { tutorialId }` | Skipped (also raises `End`). |
| `EventTutorialPause` | `TutorialPauseEvent { tutorialId, nodeId }` | Paused. |
| `EventTutorialResume` | `TutorialResumeEvent { tutorialId, nodeId }` | Resumed. |
| `EventTutorialNodeEnter` | `TutorialNodeEvent { tutorialId, nodeId }` | A node becomes current. |
| `EventTutorialNodeComplete` | `TutorialNodeEvent { tutorialId, nodeId }` | A node finishes its actions. |
| `EventTutorialCheckpoint` | `TutorialCheckpointEvent { tutorialId, nodeId }` | A checkpoint is saved. |

## Checkpoints, resume & persistence

Persisted in `UserData` under keys built by `TutorialKeys`:

- **Checkpoint** — a `Checkpoint` / `Checkpoint To` action saves the current (or a target) node id.
  On the next `StartTutorial`, the run resumes from there. Repeatable tutorials clear the checkpoint
  on start so they replay from the beginning.
- **Completion** — set when the last node finishes. Non-repeatable tutorials won't restart while set;
  `isFirstTime` is computed *before* the flag is written, so the first natural completion reports `true`.
- **Active / Paused** — `StartTutorial` marks a tutorial active; `Pause`/`Resume` toggle the paused
  marker. `Init` re-launches any tutorial left active by a killed app, re-entering the paused state if
  it was paused. Completion/skip clears both markers.

## Pause semantics

`PauseTutorial` gates the **node-to-node advance** and the start of the next node's actions — a wait
already in flight may still receive its event, but the tutorial will not move on until `ResumeTutorial`.
This is driven by `TutorialRuntimeContext` (one per run); a skip cancels the context so any pause-wait
unwinds. All tutorial code runs on the Unity main thread (UniTask player loop), so no locking is needed.

## Action catalog

| Action | Purpose |
|---|---|
| `Show View` / `Hide View` | Show/hide a `ViewConfig` on a chosen `ViewLayer`. |
| `Wait: View Show` / `Wait: View Hide` | Block until a view appears/disappears (completes immediately if already in that state). |
| `Invoke Event` | Fire an `EventManager` event — parameterless or with a typed payload (`valueLong/int/double/float/string/bool`) selected by `eventDataType`. Optional `targetId`. |
| `Listen Event` | Wait for an `EventManager` event. With `eventDataType != None`, subscribes to the matching **typed** overload and only completes when the payload satisfies `compareMode`. |
| `Wait: User Data` | Wait for a `UserData.Save` matching `dataKey` (+ optional value/`compareMode`). |
| `Checkpoint` / `Checkpoint To` | Save the current node (or a specific node) as the resume point. |
| `Force Touch` | Require the user to tap a tagged element before continuing (see below). |

Value matching for the payload/UserData actions and the UserData condition goes through
`TutorialValueMatch`, which reuses `SiPV.Config.CompareUtils` — one comparison implementation library-wide.

## Force Touch + `TutorialTarget`

Guides the user to tap a specific UI element.

1. **Tag the element.** Add a `TutorialTarget` component to the button/element and give it a unique
   `TargetId`. It registers itself in `TutorialTargetRegistry` while enabled. A `Button` is auto-hooked;
   other raycast-target elements report taps via `IPointerClickHandler`.
2. **Add the action.** In the graph, add a `Force Touch` action and set `targetId`. Options:
   - `overlayViewId` — optional Tutorial-layer overlay prefab (dim + finger).
   - `dimScreen`, `showFinger` — overlay toggles.
   - `softHintDelay` — seconds to wait before the hint; `0` waits forever.
   - `onTimeout` — `Skip` or `Pause` the tutorial if the user doesn't tap in time.

   The action completes when the target is tapped; it tears down the overlay and unsubscribes on
   complete/cancel. If the target id isn't currently registered it logs a warning and completes (it
   never hard-blocks the tutorial).
3. **Overlay prefab (optional).** Put `TutorialOverlayView` on the root of a UI prefab, register it as a
   `ViewConfig`, and wire its serialized fields: `_dim` (full-screen panel GameObject) and `_finger`
   (an arrow/finger `RectTransform`). The overlay tracks the target each frame and bobs the finger; it's
   purely cosmetic — input blocking and tap detection are handled by the action + `TutorialTarget`.
   The overlay canvas should share the target canvas's render mode/camera for correct positioning.

## Adding a custom action

```csharp
using System;

namespace SiPVLib.Tutorial.Config.TutorialActions
{
    [TutorialActionLabel("My Step", "#3F51B5")]
    [Serializable]
    public class TutorialActionMyStep : TutorialAction
    {
        public override string InvalidError() => null;

        protected override void OnStart()
        {
            // ... do work, then:
            Complete();          // advances the node; or stay running until an async callback
        }

        protected override void OnComplete() { /* unsubscribe / cleanup — also called on Cancel */ }
    }
}
```

It appears in the graph's *Add Action* menu automatically.
