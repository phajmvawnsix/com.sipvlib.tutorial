using System;
using System.Collections.Generic;
using SiPVLib.Tutorial.Config;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace SiPVLib.Tutorial.Editor
{
    public class TutorialNodeView : Node
    {
        // ── Public state ─────────────────────────────────────────────────────

        public TutorialNode Data      { get; }
        public int          NodeIndex { get; }

        public Port       InputPort      { get; private set; }
        public Port       NextPort       { get; private set; }
        public Port       ActionsPort    { get; private set; }
        public Port       OnCompletePort { get; private set; }
        public List<Port> ConditionPorts { get; private set; } = new();

        private SerializedObject _serializedObject;
        private string           _propertyPath;
        private Action           _onChanged;
        private Action<Type>     _onAddCondition;
        private Action           _onNeedsRebuild;

        // Invisible marker: new condition ports are inserted before it
        private VisualElement _conditionEndMarker;

        // ── Construction ─────────────────────────────────────────────────────

        public TutorialNodeView(TutorialNode data, int index, bool isEntry,
            SerializedObject serializedObject = null, string propertyPath = null,
            Action onChanged = null, Action<Type> onAddCondition = null, Action onNeedsRebuild = null)
        {
            Data              = data;
            NodeIndex         = index;
            _serializedObject = serializedObject;
            _propertyPath     = propertyPath;
            _onChanged        = onChanged;
            _onAddCondition   = onAddCondition;
            _onNeedsRebuild   = onNeedsRebuild;

            BuildTitle(isEntry);
            BuildPorts();
            BuildInlineInspector();

            SetPosition(new Rect(data.graphPosition, Vector2.zero));
        }

        // ── Title bar ────────────────────────────────────────────────────────

        private void BuildTitle(bool isEntry)
        {
            var la = TutorialLabelUtils.GetNodeLabel(Data.GetType());
            title  = string.IsNullOrWhiteSpace(Data.id) ? $"Node_{NodeIndex}" : Data.id;

            var badge = new Label(la.Name) { pickingMode = PickingMode.Ignore };
            badge.style.color                   = TutorialLabelUtils.ParseColor(la.HexColor);
            badge.style.fontSize                = 9;
            badge.style.unityFontStyleAndWeight = FontStyle.Bold;
            badge.style.marginLeft              = 4;
            badge.style.marginRight             = 4;
            titleContainer.Insert(1, badge);

            if (isEntry)
            {
                var gold = new StyleColor(new Color(1f, 0.8f, 0f));
                var w    = new StyleFloat(2f);
                style.borderTopColor    = gold;  style.borderTopWidth    = w;
                style.borderBottomColor = gold;  style.borderBottomWidth = w;
                style.borderLeftColor   = gold;  style.borderLeftWidth   = w;
                style.borderRightColor  = gold;  style.borderRightWidth  = w;

                var startTag = new Label("● START") { pickingMode = PickingMode.Ignore };
                startTag.style.color                   = new Color(1f, 0.8f, 0f);
                startTag.style.fontSize                = 9;
                startTag.style.unityFontStyleAndWeight = FontStyle.Bold;
                startTag.style.marginLeft              = 4;
                titleContainer.Insert(2, startTag);
            }
        }

        // ── Ports ─────────────────────────────────────────────────────────────

        private void BuildPorts()
        {
            // Input — TutorialNode flow
            InputPort          = MakePort(typeof(TutorialNode), Direction.Input, Port.Capacity.Multi, "In");
            InputPort.userData = "in";
            inputContainer.Add(InputPort);

            // Condition output ports (TutorialNodeConditional only)
            if (Data is TutorialNodeConditional conditional && conditional.conditions != null)
            {
                for (var i = 0; i < conditional.conditions.Length; i++)
                {
                    var port = CreateConditionPort(i, conditional.conditions[i]);
                    outputContainer.Add(port);
                }
            }

            // Invisible marker — dynamic condition ports are inserted before this
            _conditionEndMarker               = new VisualElement { pickingMode = PickingMode.Ignore };
            _conditionEndMarker.style.height  = 0;
            outputContainer.Add(_conditionEndMarker);

            // Actions ports — connect to TutorialActionNodeView
            ActionsPort          = MakePort(typeof(TutorialAction), Direction.Output, Port.Capacity.Multi, "▶ Actions");
            ActionsPort.userData = "actions";
            outputContainer.Add(ActionsPort);

            OnCompletePort          = MakePort(typeof(TutorialAction), Direction.Output, Port.Capacity.Multi, "✓ OnComplete");
            OnCompletePort.userData = "onComplete";
            outputContainer.Add(OnCompletePort);

            // Next — TutorialNode flow
            NextPort          = MakePort(typeof(TutorialNode), Direction.Output, Port.Capacity.Single, "Next →");
            NextPort.userData = "next";
            outputContainer.Add(NextPort);
        }

        // Creates a condition port and registers it in ConditionPorts (does NOT add to container)
        private Port CreateConditionPort(int index, TutorialNodeTargetCondition condition)
        {
            var la      = condition != null
                ? TutorialLabelUtils.GetConditionLabel(condition.GetType())
                : (Name: "?", HexColor: "#FF8C00");
            var summary = condition?.EditorSummary;
            var label   = string.IsNullOrEmpty(summary)
                ? $"Cond {index}: {la.Name}"
                : $"Cond {index}: {la.Name} [{summary}]";
            var port = MakePort(typeof(TutorialNode), Direction.Output, Port.Capacity.Single, label);
            port.userData = $"condition_{index}";

            // Color the port label to match the condition type color
            var colorDot = new Label("●") { pickingMode = PickingMode.Ignore };
            colorDot.style.color                   = TutorialLabelUtils.ParseColor(la.HexColor);
            colorDot.style.fontSize                = 9;
            colorDot.style.unityFontStyleAndWeight = FontStyle.Bold;
            colorDot.style.marginRight             = 3;
            port.Insert(1, colorDot);

            ConditionPorts.Add(port);
            return port;
        }

        /// <summary>Dynamically adds a new condition port for the given condition at runtime.</summary>
        public Port AddConditionPort(TutorialNodeTargetCondition condition)
        {
            var  index = ConditionPorts.Count;
            var  port  = CreateConditionPort(index, condition);
            var  mi    = outputContainer.IndexOf(_conditionEndMarker);
            if (mi >= 0) outputContainer.Insert(mi, port);
            else         outputContainer.Add(port);
            return port;
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

                // "＋ Add Condition" button — only for conditional nodes
                if (Data is TutorialNodeConditional && _onAddCondition != null)
                {
                    EditorGUILayout.Space(2);
                    if (GUILayout.Button("＋ Add Condition", GUILayout.Height(22)))
                    {
                        var menu = new GenericMenu();
                        foreach (var type in TutorialLabelUtils.GetAllConditionTypes())
                        {
                            var t  = type;
                            var la = TutorialLabelUtils.GetConditionLabel(t);
                            menu.AddItem(new GUIContent(la.Name), false,
                                () => _onAddCondition.Invoke(t));
                        }
                        menu.ShowAsContext();
                    }
                }

                if (EditorGUI.EndChangeCheck() && _serializedObject.ApplyModifiedProperties())
                {
                    RefreshTitle();

                    // If conditions array shrank/changed externally, trigger a graph rebuild
                    if (Data is TutorialNodeConditional cond)
                    {
                        var actual = cond.conditions?.Length ?? 0;
                        if (actual != ConditionPorts.Count)
                            _onNeedsRebuild?.Invoke();
                    }

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

        private static Port MakePort(Type portType, Direction dir, Port.Capacity cap, string name)
        {
            var p = Port.Create<Edge>(Orientation.Horizontal, dir, cap, portType);
            p.portName = name;
            return p;
        }

        public void SyncPosition() => Data.graphPosition = GetPosition().position;

        public void RefreshTitle()
        {
            title = string.IsNullOrWhiteSpace(Data.id) ? $"Node_{NodeIndex}" : Data.id;
        }

        public void SetExpanded(bool value)
        {
            extensionContainer.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
