using System.Linq;
using SiPVLib.Tutorial.Config;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.UIElements;

namespace SiPVLib.Tutorial.Editor
{
    public class TutorialGraphEditorWindow : EditorWindow
    {
        private TutorialConfig         _tutorialConfig;
        private SerializedObject       _serializedTutorial;
        private TutorialGraphView      _graphView;
        private IMGUIContainer         _inspectorPanel;
        private TutorialNodeView       _selectedNodeView;
        private TutorialActionNodeView _selectedActionView;
        private Label                  _statusLabel;
        private bool                   _autoSave;

        // ── Open ──────────────────────────────────────────────────────────────

        [MenuItem("SiPV/Tutorial Graph Editor")]
        public static void OpenMenu() => GetWindow<TutorialGraphEditorWindow>("Tutorial Graph");

        [OnOpenAsset]
        public static bool OnOpenAsset(int instanceId, int line)
        {
            var path = AssetDatabase.GetAssetPath(instanceId);
            var tutorial = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path) as TutorialConfig;
            if (tutorial == null) return false;
            Open(tutorial);
            return true;
        }

        public static void Open(TutorialConfig tutorialConfig)
        {
            var window = GetWindow<TutorialGraphEditorWindow>("Tutorial Graph");
            window.LoadTutorial(tutorialConfig);
            window.Focus();
        }

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable()
        {
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
        }

        private void OnDestroy()
        {
            // Auto-save when the window is closed so no work is lost
            if (_tutorialConfig != null && _graphView != null)
            {
                _serializedTutorial?.ApplyModifiedProperties();
                _graphView.SaveNodePositions();
                EditorUtility.SetDirty(_tutorialConfig);
                AssetDatabase.SaveAssets();
            }
        }

        private void OnUndoRedo()
        {
            if (_tutorialConfig == null || _graphView == null) return;
            _serializedTutorial = new SerializedObject(_tutorialConfig);
            _graphView?.BuildGraph(_tutorialConfig, _serializedTutorial);
            _selectedNodeView   = null;
            _selectedActionView = null;
            _inspectorPanel?.MarkDirtyRepaint();
            SetStatus("↺ Undo/Redo applied");
        }

        private void LoadTutorial(TutorialConfig tutorialConfig)
        {
            // Save the currently loaded asset before switching to a new one
            if (_tutorialConfig != null && _tutorialConfig != tutorialConfig && _graphView != null)
            {
                _serializedTutorial?.ApplyModifiedProperties();
                _graphView.SaveNodePositions();
                EditorUtility.SetDirty(_tutorialConfig);
                AssetDatabase.SaveAssets();
            }

            _tutorialConfig     = tutorialConfig;
            _serializedTutorial = new SerializedObject(tutorialConfig);
            titleContent        = new GUIContent($"Tutorial — {tutorialConfig.name}");
            _selectedNodeView   = null;
            _selectedActionView = null;
            _graphView?.BuildGraph(tutorialConfig, _serializedTutorial);
            _inspectorPanel?.MarkDirtyRepaint();
        }

        // ── CreateGUI ─────────────────────────────────────────────────────────

        private void CreateGUI()
        {
            // ── Toolbar ────────────────────────────────────────────────────────
            var toolbar = new VisualElement();
            toolbar.style.flexDirection   = FlexDirection.Row;
            toolbar.style.backgroundColor = new StyleColor(new Color(0.19f, 0.19f, 0.19f));
            toolbar.style.paddingLeft     = 6;
            toolbar.style.paddingRight    = 6;
            toolbar.style.paddingTop      = 3;
            toolbar.style.paddingBottom   = 3;
            toolbar.style.height          = 30;
            toolbar.style.flexShrink      = 0;

            Button Btn(string text, System.Action onClick)
            {
                var b = new Button(onClick) { text = text };
                b.style.marginRight = 4;
                return b;
            }

            toolbar.Add(Btn("💾 Save",        OnSave));
            toolbar.Add(Btn("✔ Validate",    OnValidateClicked));
            toolbar.Add(Btn("⟳ Auto Layout", OnAutoLayout));
            toolbar.Add(Btn("⌖ Entry",       OnCenterEntry));
            toolbar.Add(Btn("↺ Rebuild",     OnRebuild));
            toolbar.Add(Btn("⊞ Expand All",  () => SetAllExpanded(true)));
            toolbar.Add(Btn("⊟ Collapse All",() => SetAllExpanded(false)));

            // Auto-save toggle
            var autoSaveToggle = new Toggle("⚡ Auto-Save") { value = _autoSave };
            autoSaveToggle.style.marginLeft  = 8;
            autoSaveToggle.style.marginRight = 4;
            autoSaveToggle.RegisterValueChangedCallback(evt =>
            {
                _autoSave = evt.newValue;
                if (_autoSave) OnSave();
            });
            toolbar.Add(autoSaveToggle);

            _statusLabel = new Label { pickingMode = PickingMode.Ignore };
            _statusLabel.style.flexGrow       = 1;
            _statusLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            _statusLabel.style.paddingLeft    = 10;
            _statusLabel.style.color          = new StyleColor(new Color(0.7f, 0.7f, 0.7f));
            toolbar.Add(_statusLabel);

            rootVisualElement.Add(toolbar);

            // ── Split (fixedPaneIndex=1 → right panel fixed at 340px) ─────────
            var split = new TwoPaneSplitView(1, 340f, TwoPaneSplitViewOrientation.Horizontal);
            split.style.flexGrow = 1;
            rootVisualElement.Add(split);

            // Left: graph
            _graphView = new TutorialGraphView();
            _graphView.OnNodeSelected   += OnNodeSelected;
            _graphView.OnActionSelected += OnActionSelected;
            _graphView.OnNeedsRebuild   += OnRebuild;
            _graphView.OnGraphChanged   += () =>
            {
                SetStatus("● Unsaved changes");
                if (_autoSave) OnSave();
            };
            var graphContainer = new VisualElement();
            graphContainer.style.flexGrow = 1;
            graphContainer.Add(_graphView);
            split.Add(graphContainer);

            // Right: inspector panel
            var rightPanel = new VisualElement();
            rightPanel.style.minWidth        = 280;
            rightPanel.style.borderLeftWidth = 1;
            rightPanel.style.borderLeftColor = new StyleColor(new Color(0.12f, 0.12f, 0.12f));

            var header = new Label("Inspector");
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.paddingLeft             = 8;
            header.style.paddingTop              = 6;
            header.style.paddingBottom           = 4;
            header.style.borderBottomWidth       = 1;
            header.style.borderBottomColor       = new StyleColor(new Color(0.15f, 0.15f, 0.15f));
            rightPanel.Add(header);

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.flexGrow = 1;
            _inspectorPanel = new IMGUIContainer(DrawInspector);
            _inspectorPanel.style.flexGrow = 1;
            scroll.Add(_inspectorPanel);
            rightPanel.Add(scroll);
            split.Add(rightPanel);

            if (_tutorialConfig != null)
            {
                _serializedTutorial ??= new SerializedObject(_tutorialConfig);
                _graphView.BuildGraph(_tutorialConfig, _serializedTutorial);
            }
        }

        // ── Toolbar handlers ──────────────────────────────────────────────────

        private void OnSave()
        {
            if (_tutorialConfig == null || _graphView == null) { SetStatus("Nothing to save."); return; }
            _graphView.SaveNodePositions();
            _serializedTutorial?.ApplyModifiedProperties();
            EditorUtility.SetDirty(_tutorialConfig);
            AssetDatabase.SaveAssets();
            SetStatus($"✔ Saved  {System.DateTime.Now:HH:mm:ss}");
        }

        private void OnValidateClicked()
        {
            if (_tutorialConfig == null || _graphView == null) return;
            var issues   = _graphView.Validate();
            var errCount = issues.Count(s => s.StartsWith("[Error]"));
            var wrnCount = issues.Count - errCount;

            if (issues.Count == 0)
            {
                SetStatus("✔ No issues.");
                EditorUtility.DisplayDialog("Validate Tutorial", "✔  No problems detected.", "OK");
            }
            else
            {
                SetStatus($"⚠ {errCount} error(s), {wrnCount} warning(s)");
                EditorUtility.DisplayDialog("Validate Tutorial", string.Join("\n\n", issues.ToArray()), "OK");
            }
        }

        private void OnAutoLayout()
        {
            if (_tutorialConfig == null || _graphView == null) return;
            _graphView.AutoLayout();
            SetStatus("Auto-layout applied.");
        }

        private void OnCenterEntry() => _graphView?.CenterOnEntry();

        private void OnRebuild()
        {
            if (_tutorialConfig == null || _graphView == null) return;
            _serializedTutorial?.ApplyModifiedProperties();
            _graphView.SaveNodePositions();
            _serializedTutorial = new SerializedObject(_tutorialConfig);
            _graphView.BuildGraph(_tutorialConfig, _serializedTutorial);
            _selectedNodeView   = null;
            _selectedActionView = null;
            _inspectorPanel?.MarkDirtyRepaint();
        }

        private void SetAllExpanded(bool value)
        {
            if (_graphView == null) return;
            foreach (var v in _graphView.NodeViews)   v.SetExpanded(value);
            foreach (var v in _graphView.ActionViews) v.SetExpanded(value);
        }

        // ── Selection ─────────────────────────────────────────────────────────

        private void OnNodeSelected(TutorialNodeView view)
        {
            _selectedNodeView   = view;
            _selectedActionView = null;
            _inspectorPanel?.MarkDirtyRepaint();
        }

        private void OnActionSelected(TutorialActionNodeView view)
        {
            _selectedActionView = view;
            _selectedNodeView   = null;
            _inspectorPanel?.MarkDirtyRepaint();
        }

        // ── Inspector (dispatch) ──────────────────────────────────────────────

        private void DrawInspector()
        {
            if (_tutorialConfig == null)
            {
                EditorGUILayout.HelpBox("No Tutorial asset loaded.\nDouble-click a Tutorial asset to open it.", MessageType.Info);
                return;
            }

            if (_selectedActionView != null) { DrawActionInspector(); return; }
            if (_selectedNodeView   != null) { DrawNodeInspector();   return; }

            EditorGUILayout.HelpBox("Select a node or action on the graph to inspect and edit it.", MessageType.None);
        }

        // ── Node inspector ────────────────────────────────────────────────────

        private void DrawNodeInspector()
        {
            _serializedTutorial ??= new SerializedObject(_tutorialConfig);
            _serializedTutorial.Update();

            var nodesProp = _serializedTutorial.FindProperty("nodes");
            var idx       = _selectedNodeView.NodeIndex;

            if (nodesProp == null || idx < 0 || idx >= nodesProp.arraySize)
            {
                EditorGUILayout.HelpBox("Node index out of range — click '↺ Rebuild' to refresh.", MessageType.Warning);
                return;
            }

            var la = TutorialLabelUtils.GetNodeLabel(_selectedNodeView.Data.GetType());
            GUILayout.Space(2);
            EditorGUILayout.LabelField($"[{idx}]  {_selectedNodeView.Data.id}  —  {la.Name}", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            EditorGUILayout.PropertyField(nodesProp.GetArrayElementAtIndex(idx), includeChildren: true);

            if (_serializedTutorial.ApplyModifiedProperties())
            {
                _selectedNodeView.RefreshTitle();
                SetStatus("● Unsaved changes");
            }
        }

        // ── Action inspector ──────────────────────────────────────────────────

        private void DrawActionInspector()
        {
            _serializedTutorial ??= new SerializedObject(_tutorialConfig);
            _serializedTutorial.Update();

            var av = _selectedActionView;
            var la = TutorialLabelUtils.GetActionLabel(av.Data.GetType());

            GUILayout.Space(2);
            EditorGUILayout.LabelField($"Action — {la.Name}", EditorStyles.boldLabel);

            if (av.OwnerNode != null)
                EditorGUILayout.LabelField(
                    $"Owner: {av.OwnerNode.id}   Slot: {(av.Slot == "actions" ? "▶ Actions" : "✓ OnComplete")}",
                    EditorStyles.miniLabel);
            else
                EditorGUILayout.HelpBox("Action is not connected to any node.", MessageType.Warning);

            EditorGUILayout.Space(4);

            // O(1) lookup using cached OwnerNodeIndex + ActionIndex + Slot
            var  i        = av.OwnerNodeIndex;
            var  j        = av.ActionIndex;
            var inBounds = i >= 0 && i < _tutorialConfig.nodes.Count && j >= 0;

            if (inBounds)
            {
                var slotProp  = av.Slot == "actions" ? "actions" : "onCompleteActions";
                var nodesProp = _serializedTutorial.FindProperty("nodes");
                var slotArr   = nodesProp.GetArrayElementAtIndex(i).FindPropertyRelative(slotProp);
                if (slotArr != null && j < slotArr.arraySize)
                {
                    EditorGUILayout.PropertyField(slotArr.GetArrayElementAtIndex(j), includeChildren: true);
                    if (_serializedTutorial.ApplyModifiedProperties())
                    {
                        av.RefreshTitle();
                        SetStatus("● Unsaved changes");
                    }
                    return;
                }
            }

            EditorGUILayout.HelpBox("Action index out of range — click '↺ Rebuild'.", MessageType.Warning);
        }

        // ── Utils ─────────────────────────────────────────────────────────────

        private void SetStatus(string msg) { if (_statusLabel != null) _statusLabel.text = msg; }
    }
}

