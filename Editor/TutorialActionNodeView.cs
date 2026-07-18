using System;
using SiPVLib.Tutorial.Config;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace SiPVLib.Tutorial.Editor
{
    /// <summary>
    /// Graph canvas node that represents a single <see cref="TutorialAction"/>.
    /// Connects to a <see cref="TutorialNodeView"/> via its <c>▶ Actions</c> or <c>✓ OnComplete</c> port.
    /// </summary>
    public class TutorialActionNodeView : Node
    {
        // ── Public state ─────────────────────────────────────────────────────

        public TutorialAction Data           { get; }
        /// <summary>The TutorialNode that owns this action (null when disconnected).</summary>
        public TutorialNode   OwnerNode      { get; set; }
        /// <summary>"actions" or "onComplete"</summary>
        public string         Slot           { get; set; }
        public int            OwnerNodeIndex { get; set; }
        public int            ActionIndex    { get; set; }

        public Port InputPort { get; private set; }

        private SerializedObject _serializedObject;
        private string           _propertyPath;
        private Action           _onChanged;

        // ── Construction ─────────────────────────────────────────────────────

        public TutorialActionNodeView(TutorialAction data,
            SerializedObject serializedObject = null, string propertyPath = null, Action onChanged = null)
        {
            Data              = data;
            _serializedObject = serializedObject;
            _propertyPath     = propertyPath;
            _onChanged        = onChanged;

            BuildTitle();
            BuildPorts();
            BuildInlineInspector();

            SetPosition(new Rect(data.graphPosition, Vector2.zero));
        }

        // ── Title bar ────────────────────────────────────────────────────────

        private void BuildTitle()
        {
            var la = TutorialLabelUtils.GetActionLabel(Data.GetType());
            title = la.Name;

            // Color-coded type indicator dot
            var dot = new Label("●") { pickingMode = PickingMode.Ignore };
            dot.style.color                   = TutorialLabelUtils.ParseColor(la.HexColor);
            dot.style.fontSize                = 10;
            dot.style.unityFontStyleAndWeight = FontStyle.Bold;
            dot.style.marginLeft              = 2;
            dot.style.marginRight             = 4;
            titleContainer.Insert(0, dot);

            // Tint title bar to match action color (darkened)
            var c    = TutorialLabelUtils.ParseColor(la.HexColor);
            var tint = new Color(c.r * 0.25f, c.g * 0.25f, c.b * 0.25f, 1f);
            titleContainer.style.backgroundColor = new StyleColor(tint);

            // Summary line (e.g., viewId, eventName, dataKey) — shown when collapsed
            var summary = Data.EditorSummary;
            if (!string.IsNullOrEmpty(summary))
            {
                var lbl = new Label(summary) { pickingMode = PickingMode.Ignore };
                lbl.style.fontSize      = 8;
                lbl.style.color         = new Color(0.80f, 0.80f, 0.80f);
                lbl.style.paddingLeft   = 6;
                lbl.style.paddingRight  = 6;
                lbl.style.paddingTop    = 2;
                lbl.style.paddingBottom = 2;
                titleContainer.Add(lbl);
            }
        }

        // ── Ports ─────────────────────────────────────────────────────────────

        private void BuildPorts()
        {
            // Single input — accepts edges from TutorialNodeView action ports (type = TutorialAction)
            InputPort          = Port.Create<Edge>(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(TutorialAction));
            InputPort.portName = "";
            InputPort.userData = "actionIn";
            inputContainer.Add(InputPort);
        }

        // ── Inline inspector ──────────────────────────────────────────────────

        private void BuildInlineInspector()
        {
            // Solid background so the data area is never transparent
            extensionContainer.style.backgroundColor = new StyleColor(new Color(0.18f, 0.18f, 0.18f, 1f));

            var container = new IMGUIContainer(() =>
            {
                if (_serializedObject == null || string.IsNullOrEmpty(_propertyPath))
                {
                    EditorGUILayout.HelpBox("Click '↺ Rebuild' to restore inline inspector.", MessageType.None);
                    return;
                }
                _serializedObject.Update();
                var prop = _serializedObject.FindProperty(_propertyPath);
                if (prop == null) return;
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(prop, includeChildren: true);
                if (EditorGUI.EndChangeCheck() && _serializedObject.ApplyModifiedProperties())
                {
                    RefreshTitle();
                    _onChanged?.Invoke();
                }
            });
            container.style.paddingLeft   = 4;
            container.style.paddingRight  = 4;
            container.style.paddingTop    = 4;
            container.style.paddingBottom = 4;
            extensionContainer.Add(container);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        public void SyncPosition() => Data.graphPosition = GetPosition().position;

        public void RefreshTitle()
        {
            var la = TutorialLabelUtils.GetActionLabel(Data.GetType());
            title  = la.Name;
        }

        public void SetExpanded(bool value)
        {
            extensionContainer.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}

