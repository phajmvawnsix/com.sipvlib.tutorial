using System;
using System.Collections.Generic;
using System.Linq;
using SiPVLib.Tutorial.Config;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace SiPVLib.Tutorial.Editor
{
    public class TutorialGraphView : GraphView
    {
        private TutorialConfig   _tutorialConfig;
        private SerializedObject _serializedObject;
        private readonly Dictionary<string, TutorialNodeView>              _nodeViews   = new();
        private readonly Dictionary<TutorialAction, TutorialActionNodeView> _actionViews = new();

        public event Action<TutorialNodeView>       OnNodeSelected;
        public event Action<TutorialActionNodeView> OnActionSelected;
        public event Action                         OnGraphChanged;
        public event Action                         OnNeedsRebuild;

        public IEnumerable<TutorialNodeView>       NodeViews   => _nodeViews.Values;
        public IEnumerable<TutorialActionNodeView> ActionViews => _actionViews.Values;

        public TutorialGraphView()
        {
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            var grid = new GridBackground();
            grid.StretchToParentSize();
            Insert(0, grid);
            var minimap = new MiniMap { anchored = true };
            minimap.SetPosition(new Rect(10, 30, 180, 120));
            Add(minimap);
            graphViewChanged += OnGraphViewChanged;
            style.flexGrow = 1;
        }

        // ── Build ─────────────────────────────────────────────────────────────

        public void BuildGraph(TutorialConfig tutorialConfig, SerializedObject serializedObject = null)
        {
            _tutorialConfig   = tutorialConfig;
            _serializedObject = serializedObject;
            ClearGraph();
            if (tutorialConfig?.nodes == null || tutorialConfig.nodes.Count == 0) return;

            // Pass 1: TutorialNodeViews
            for (var i = 0; i < tutorialConfig.nodes.Count; i++)
            {
                var node = tutorialConfig.nodes[i];
                if (node == null) continue;
                AddElement(CreateNodeView(node, i));
            }

            // Pass 2: TutorialActionNodeViews
            for (var i = 0; i < tutorialConfig.nodes.Count; i++)
            {
                var node = tutorialConfig.nodes[i];
                if (node == null) continue;
                if (node.actions != null)
                    for (var j = 0; j < node.actions.Length; j++)
                        CreateAndRegisterActionView(node.actions[j], node, "actions", i, j);
                if (node.onCompleteActions != null)
                    for (var j = 0; j < node.onCompleteActions.Length; j++)
                        CreateAndRegisterActionView(node.onCompleteActions[j], node, "onComplete", i, j);
            }

            // Pass 3: Node flow edges (Next, Condition)
            foreach (var node in tutorialConfig.nodes)
            {
                if (node == null || string.IsNullOrWhiteSpace(node.id)) continue;
                if (!_nodeViews.TryGetValue(node.id, out var fromView)) continue;
                ConnectNodeToNode(fromView.NextPort, node.nextNodeId);
                if (node is TutorialNodeConditional cond && cond.conditions != null)
                    for (var c = 0; c < cond.conditions.Length; c++)
                    {
                        if (c >= fromView.ConditionPorts.Count) break;
                        ConnectNodeToNode(fromView.ConditionPorts[c], cond.conditions[c]?.targetNodeId);
                    }
            }

            // Pass 4: Action edges (ActionsPort / OnCompletePort → ActionNodeView.InputPort)
            foreach (var node in tutorialConfig.nodes)
            {
                if (node == null || !_nodeViews.TryGetValue(node.id, out var nodeView)) continue;
                if (node.actions != null)
                    foreach (var action in node.actions)
                        ConnectNodeToAction(nodeView.ActionsPort, action);
                if (node.onCompleteActions != null)
                    foreach (var action in node.onCompleteActions)
                        ConnectNodeToAction(nodeView.OnCompletePort, action);
            }

            FrameAll();
        }

        private void ClearGraph()
        {
            // Unsubscribe during clear so DeleteElements does NOT fire OnGraphViewChanged
            // (which would call SyncEdge and corrupt the old asset's data)
            graphViewChanged -= OnGraphViewChanged;
            DeleteElements(graphElements.ToList());
            graphViewChanged += OnGraphViewChanged;
            _nodeViews.Clear();
            _actionViews.Clear();
        }

        private TutorialNodeView CreateNodeView(TutorialNode node, int index)
        {
            var propPath = $"nodes.Array.data[{index}]";
            TutorialNodeView view = null;
            view = new TutorialNodeView(node, index, index == 0,
                _serializedObject, propPath,
                () => OnGraphChanged?.Invoke(),
                (type) => AddConditionToNode(view, type),
                () => OnNeedsRebuild?.Invoke());
            view.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button == 0) OnNodeSelected?.Invoke(view);
            });
            _nodeViews[node.id] = view;
            return view;
        }

        private void CreateAndRegisterActionView(TutorialAction action, TutorialNode owner, string slot, int nodeIdx, int actionIdx)
        {
            if (action == null || _actionViews.ContainsKey(action)) return;
            var slotField = slot == "actions" ? "actions" : "onCompleteActions";
            var propPath  = $"nodes.Array.data[{nodeIdx}].{slotField}.Array.data[{actionIdx}]";
            var av = new TutorialActionNodeView(action, _serializedObject, propPath, () => OnGraphChanged?.Invoke())
            {
                OwnerNode      = owner,
                Slot           = slot,
                OwnerNodeIndex = nodeIdx,
                ActionIndex    = actionIdx,
            };
            av.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button == 0) OnActionSelected?.Invoke(av);
            });
            _actionViews[action] = av;
            AddElement(av);
        }

        private void ConnectNodeToNode(Port fromPort, string targetId)
        {
            if (string.IsNullOrWhiteSpace(targetId)) return;
            if (!_nodeViews.TryGetValue(targetId, out var toView)) return;
            AddElement(fromPort.ConnectTo(toView.InputPort));
        }

        private void ConnectNodeToAction(Port fromPort, TutorialAction action)
        {
            if (action == null || !_actionViews.TryGetValue(action, out var av)) return;
            AddElement(fromPort.ConnectTo(av.InputPort));
        }

        // ── Port compatibility ────────────────────────────────────────────────

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter) =>
            ports.ToList().Where(p =>
                p.direction != startPort.direction &&
                p.node      != startPort.node      &&
                p.portType  == startPort.portType
            ).ToList();

        // ── Context menu ──────────────────────────────────────────────────────

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            var mouseWorld       = viewTransform.matrix.inverse.MultiplyPoint(evt.localMousePosition);
            var selectedNodeView = selection.OfType<TutorialNodeView>().FirstOrDefault();

            // Add Node submenu
            foreach (var type in TutorialLabelUtils.GetAllNodeTypes())
            {
                var t  = type;
                var la = TutorialLabelUtils.GetNodeLabel(t);
                evt.menu.AppendAction($"Add Node/{la.Name}", _ => AddNodeToGraph(t, mouseWorld));
            }

            // Add Action submenu (only when a TutorialNode is selected)
            if (selectedNodeView != null)
            {
                var nodeId = selectedNodeView.Data.id;
                evt.menu.AppendSeparator();
                foreach (var type in TutorialLabelUtils.GetAllActionTypes())
                {
                    var t  = type;
                    var la = TutorialLabelUtils.GetActionLabel(t);
                    evt.menu.AppendAction($"Add Action [{nodeId}]/{la.Name}",
                        _ => AddActionToGraph(t, selectedNodeView, "actions", mouseWorld));
                    evt.menu.AppendAction($"Add OnComplete Action [{nodeId}]/{la.Name}",
                        _ => AddActionToGraph(t, selectedNodeView, "onComplete", new Vector2(mouseWorld.x + 240f, mouseWorld.y)));
                }
            }

            evt.menu.AppendSeparator();
            base.BuildContextualMenu(evt);
        }

        // ── Add Node ─────────────────────────────────────────────────────────

        private void AddNodeToGraph(Type nodeType, Vector2 position)
        {
            if (_tutorialConfig == null) return;
            Undo.RecordObject(_tutorialConfig, "Add Tutorial Node");
            var node = (TutorialNode)Activator.CreateInstance(nodeType);
            node.id            = GenerateUniqueNodeId();
            node.graphPosition = position;
            _tutorialConfig.nodes.Add(node);
            EditorUtility.SetDirty(_tutorialConfig);
            var view = CreateNodeView(node, _tutorialConfig.nodes.Count - 1);
            view.SetPosition(new Rect(position, new Vector2(220, 0)));
            AddElement(view);
            OnGraphChanged?.Invoke();
        }

        // ── Add Condition ─────────────────────────────────────────────────────

        private void AddConditionToNode(TutorialNodeView view, Type conditionType)
        {
            if (_tutorialConfig == null || view == null) return;
            var node = view.Data as TutorialNodeConditional;
            if (node == null) return;

            Undo.RecordObject(_tutorialConfig, "Add Condition");

            // Instantiate and append to conditions array
            var condition = (TutorialNodeTargetCondition)Activator.CreateInstance(conditionType);
            var arr       = node.conditions ?? Array.Empty<TutorialNodeTargetCondition>();
            Array.Resize(ref arr, arr.Length + 1);
            arr[arr.Length - 1] = condition;
            node.conditions = arr;

            // Dynamically add the port — no rebuild needed
            view.AddConditionPort(condition);

            EditorUtility.SetDirty(_tutorialConfig);
            OnGraphChanged?.Invoke();
        }

        // ── Add Action ────────────────────────────────────────────────────────

        private void AddActionToGraph(Type actionType, TutorialNodeView nodeView, string slot, Vector2 position)
        {
            if (_tutorialConfig == null) return;
            Undo.RecordObject(_tutorialConfig, "Add Tutorial Action");

            var action = (TutorialAction)Activator.CreateInstance(actionType);
            action.graphPosition = position;

            // Append to the node's array
            var node = nodeView.Data;
            if (slot == "actions")
            {
                var arr = node.actions ?? Array.Empty<TutorialAction>();
                Array.Resize(ref arr, arr.Length + 1);
                arr[arr.Length - 1] = action;
                node.actions = arr;
            }
            else
            {
                var arr = node.onCompleteActions ?? Array.Empty<TutorialAction>();
                Array.Resize(ref arr, arr.Length + 1);
                arr[arr.Length - 1] = action;
                node.onCompleteActions = arr;
            }

            var nodeIdx   = _tutorialConfig.nodes.IndexOf(node);
            var actionIdx = slot == "actions"
                ? (node.actions?.Length ?? 1) - 1
                : (node.onCompleteActions?.Length ?? 1) - 1;

            CreateAndRegisterActionView(action, node, slot, nodeIdx, actionIdx);
            var av = _actionViews[action];
            av.SetPosition(new Rect(position, new Vector2(200, 0)));

            // Wire edge from node action port to action node
            var outPort = slot == "actions" ? nodeView.ActionsPort : nodeView.OnCompletePort;
            AddElement(outPort.ConnectTo(av.InputPort));

            EditorUtility.SetDirty(_tutorialConfig);
            OnGraphChanged?.Invoke();
        }

        // ── graphViewChanged ──────────────────────────────────────────────────

        private GraphViewChange OnGraphViewChanged(GraphViewChange change)
        {
            if (_tutorialConfig == null) return change;
            var dirty = false;

            if (change.elementsToRemove != null)
            {
                // Edges first (updates OwnerNode/Slot before node views are removed)
                foreach (var edge in change.elementsToRemove.OfType<Edge>())
                    dirty |= SyncEdge(edge, false);

                foreach (var nv in change.elementsToRemove.OfType<TutorialNodeView>())
                {
                    Undo.RecordObject(_tutorialConfig, "Remove Tutorial Node");
                    _tutorialConfig.nodes.Remove(nv.Data);
                    _nodeViews.Remove(nv.Data.id);
                    dirty = true;
                }

                foreach (var av in change.elementsToRemove.OfType<TutorialActionNodeView>())
                {
                    _actionViews.Remove(av.Data);
                    dirty = true;
                }
            }

            if (change.edgesToCreate != null)
                foreach (var edge in change.edgesToCreate)
                {
                    AddElement(edge);
                    dirty |= SyncEdge(edge, true);
                }

            if (dirty) { EditorUtility.SetDirty(_tutorialConfig); OnGraphChanged?.Invoke(); }
            return change;
        }

        private bool SyncEdge(Edge edge, bool connect)
        {
            // ── Case 1: TutorialNode → TutorialNode (flow: next / condition) ──
            if (edge.output?.node is TutorialNodeView fromNode &&
                edge.input?.node  is TutorialNodeView toNode)
            {
                var portKey  = edge.output.userData as string ?? "next";
                var targetId = connect ? toNode.Data.id : string.Empty;
                if (portKey == "next")
                {
                    Undo.RecordObject(_tutorialConfig, connect ? "Connect Next Node" : "Disconnect Next Node");
                    fromNode.Data.nextNodeId = targetId;
                    return true;
                }
                if (portKey.StartsWith("condition_") && fromNode.Data is TutorialNodeConditional cond)
                {
                    if (int.TryParse(portKey.Substring("condition_".Length), out var idx) &&
                        cond.conditions != null && idx < cond.conditions.Length)
                    {
                        Undo.RecordObject(_tutorialConfig, connect ? "Connect Condition" : "Disconnect Condition");
                        cond.conditions[idx].targetNodeId = targetId;
                        return true;
                    }
                }
                return false;
            }

            // ── Case 2: TutorialNode action port → TutorialActionNodeView ────
            if (edge.output?.node is TutorialNodeView actionOwner &&
                edge.input?.node  is TutorialActionNodeView actionView)
            {
                var slot = edge.output.userData as string; // "actions" or "onComplete"
                if (connect)
                {
                    // Remove from previous owner (reroute case)
                    if (actionView.OwnerNode != null)
                        RemoveActionFromNodeArray(actionView.Data, actionView.OwnerNode, actionView.Slot);
                    AddActionToNodeArray(actionView.Data, actionOwner.Data, slot);
                    actionView.OwnerNode = actionOwner.Data;
                    actionView.Slot      = slot;
                }
                else
                {
                    RemoveActionFromNodeArray(actionView.Data, actionOwner.Data, slot);
                    actionView.OwnerNode = null;
                    actionView.Slot      = null;
                }
                return true;
            }

            return false;
        }

        // ── Array helpers ─────────────────────────────────────────────────────

        private void AddActionToNodeArray(TutorialAction action, TutorialNode node, string slot)
        {
            Undo.RecordObject(_tutorialConfig, "Connect Action");
            if (slot == "actions")
            {
                var arr = node.actions ?? Array.Empty<TutorialAction>();
                if (Array.IndexOf(arr, action) >= 0) return;
                Array.Resize(ref arr, arr.Length + 1);
                arr[arr.Length - 1] = action;
                node.actions = arr;
            }
            else
            {
                var arr = node.onCompleteActions ?? Array.Empty<TutorialAction>();
                if (Array.IndexOf(arr, action) >= 0) return;
                Array.Resize(ref arr, arr.Length + 1);
                arr[arr.Length - 1] = action;
                node.onCompleteActions = arr;
            }
        }

        private void RemoveActionFromNodeArray(TutorialAction action, TutorialNode node, string slot)
        {
            Undo.RecordObject(_tutorialConfig, "Disconnect Action");
            if (slot == "actions" && node.actions != null)
                node.actions = node.actions.Where(a => a != action).ToArray();
            else if (slot != "actions" && node.onCompleteActions != null)
                node.onCompleteActions = node.onCompleteActions.Where(a => a != action).ToArray();
        }

        // ── Auto layout ───────────────────────────────────────────────────────

        public void AutoLayout()
        {
            if (_tutorialConfig?.nodes == null || _tutorialConfig.nodes.Count == 0) return;

            // ── Constants ─────────────────────────────────────────────────────
            const float nodeW      = 240f;
            const float actionW    = 200f;
            const float nodeH      = 160f;
            const float actionH    = 90f;
            const float hGap       = 40f;   // gap between tutorial node and its actions
            const float vGap       = 8f;    // vertical gap between stacked action cards
            const float rowGap     = 80f;   // vertical gap between depth rows
            const float branchGap  = 40f;   // horizontal gap between sibling branches
            const float orphanColW = 260f;  // col 0 (unlinked) width
            const float startX     = 60f;
            const float startY     = 60f;

            // Each leaf column holds: tutorial node + its actions side-by-side
            var slotStride = nodeW + actionW + hGap + branchGap;
            var mainAreaX  = startX + orphanColW + hGap;

            // ── Step 1: BFS — collect all reachable nodes ─────────────────────
            var reachable = new HashSet<string>();
            var bfsQ      = new Queue<TutorialNode>();
            var entry     = _tutorialConfig.nodes.FirstOrDefault(n => n != null);
            if (entry != null) bfsQ.Enqueue(entry);
            while (bfsQ.Count > 0)
            {
                var n = bfsQ.Dequeue();
                if (n == null || reachable.Contains(n.id)) continue;
                reachable.Add(n.id);
                if (!string.IsNullOrEmpty(n.nextNodeId))
                {
                    var nx = _tutorialConfig.nodes.FirstOrDefault(x => x?.id == n.nextNodeId);
                    if (nx != null) bfsQ.Enqueue(nx);
                }
                if (n is TutorialNodeConditional cnd && cnd.conditions != null)
                    foreach (var c in cnd.conditions)
                    {
                        if (string.IsNullOrEmpty(c?.targetNodeId)) continue;
                        var bn = _tutorialConfig.nodes.FirstOrDefault(x => x?.id == c.targetNodeId);
                        if (bn != null) bfsQ.Enqueue(bn);
                    }
            }

            // ── Step 2: Tree DFS — assign X center and depth per node ─────────
            // Straight chains share one column (leaf slot).
            // TutorialNodeConditional fans its children into separate slots;
            // the conditional node is centered over all its children's leaf span.
            var nodeX      = new Dictionary<string, float>(); // X relative to mainAreaX
            var nodeDepth  = new Dictionary<string, int>();
            var leafIndex  = 0;
            var dfsVisited = new HashSet<string>();

            TutorialNode FindNode(string id) =>
                _tutorialConfig.nodes.FirstOrDefault(x => x?.id == id);

            void AssignLayout(string nodeId, int depth)
            {
                if (string.IsNullOrEmpty(nodeId) || dfsVisited.Contains(nodeId)) return;
                if (!reachable.Contains(nodeId)) return;
                dfsVisited.Add(nodeId);
                nodeDepth[nodeId] = depth;

                var node = FindNode(nodeId);
                if (node == null) { dfsVisited.Remove(nodeId); return; }

                if (node is TutorialNodeConditional condNode)
                {
                    // Children = condition targets + nextNodeId (fallback), deduplicated
                    var childIds = new List<string>();
                    if (condNode.conditions != null)
                        foreach (var c in condNode.conditions)
                            if (!string.IsNullOrEmpty(c?.targetNodeId) && !childIds.Contains(c.targetNodeId))
                                childIds.Add(c.targetNodeId);
                    if (!string.IsNullOrEmpty(condNode.nextNodeId) && !childIds.Contains(condNode.nextNodeId))
                        childIds.Add(condNode.nextNodeId);

                    if (childIds.Count == 0)
                    {
                        nodeX[nodeId] = leafIndex * slotStride;
                        leafIndex++;
                    }
                    else
                    {
                        var firstLeaf = leafIndex;
                        foreach (var cid in childIds)
                        {
                            if (string.IsNullOrEmpty(cid) || !reachable.Contains(cid)) continue;
                            if (dfsVisited.Contains(cid))
                                leafIndex++;  // virtual leaf for already-placed node (convergence)
                            else
                                AssignLayout(cid, depth + 1);
                        }
                        // Center this node over the full leaf span of its children
                        nodeX[nodeId] = (firstLeaf + leafIndex - 1) / 2.0f * slotStride;
                    }
                }
                else
                {
                    // Regular node — stays in the same column as its downstream chain
                    var nextId = node.nextNodeId;
                    if (!string.IsNullOrEmpty(nextId) && reachable.Contains(nextId))
                    {
                        if (!dfsVisited.Contains(nextId))
                        {
                            AssignLayout(nextId, depth + 1);
                            nodeX[nodeId] = nodeX.TryGetValue(nextId, out var nx) ? nx : leafIndex * slotStride;
                        }
                        else
                        {
                            // Convergence: next already placed by another branch — new leaf
                            nodeX[nodeId] = leafIndex * slotStride;
                            leafIndex++;
                        }
                    }
                    else
                    {
                        // Terminal node
                        nodeX[nodeId] = leafIndex * slotStride;
                        leafIndex++;
                    }
                }
            }

            if (entry != null) AssignLayout(entry.id, 0);

            // ── Step 3: Row Y — per-depth row heights ─────────────────────────
            var maxDepth  = nodeDepth.Values.Count > 0 ? nodeDepth.Values.Max() : 0;
            var totalRows = maxDepth + 1;
            var maxActUp   = new int[totalRows];
            var maxActDown = new int[totalRows];
            foreach (var node in _tutorialConfig.nodes)
            {
                if (node == null || !nodeDepth.TryGetValue(node.id, out var d) || d >= totalRows) continue;
                maxActUp[d]   = Mathf.Max(maxActUp[d],   node.actions?.Length           ?? 0);
                maxActDown[d] = Mathf.Max(maxActDown[d], node.onCompleteActions?.Length ?? 0);
            }
            var rowBaseY = new float[totalRows];
            var cumY = startY;
            for (var r = 0; r < totalRows; r++)
            {
                rowBaseY[r] = cumY + maxActUp[r] * (actionH + vGap);
                cumY = rowBaseY[r] + nodeH + maxActDown[r] * (actionH + vGap) + rowGap;
            }

            // ── Step 4: Place main-flow nodes + their actions ─────────────────
            Undo.RecordObject(_tutorialConfig, "Auto Layout Tutorial Graph");
            foreach (var node in _tutorialConfig.nodes)
            {
                if (node == null || !nodeX.TryGetValue(node.id, out var relX)) continue;
                var   d  = nodeDepth[node.id];
                var x  = mainAreaX + relX;
                var y  = rowBaseY[d];
                var ax = x + nodeW + hGap;  // actions sit to the right of the tutorial node

                if (_nodeViews.TryGetValue(node.id, out var nv))
                {
                    nv.SetPosition(new Rect(new Vector2(x, y), new Vector2(nodeW, 0)));
                    node.graphPosition = new Vector2(x, y);
                }

                if (node.actions != null)
                {
                    var cnt = node.actions.Length;
                    for (var k = 0; k < cnt; k++)
                    {
                        var a = node.actions[k];
                        if (a == null || !_actionViews.TryGetValue(a, out var av)) continue;
                        var ay  = y - cnt * (actionH + vGap) + k * (actionH + vGap);
                        var  aPos = new Vector2(ax, ay);
                        av.SetPosition(new Rect(aPos, new Vector2(actionW, 0)));
                        a.graphPosition = aPos;
                    }
                }
                if (node.onCompleteActions != null)
                {
                    for (var k = 0; k < node.onCompleteActions.Length; k++)
                    {
                        var a = node.onCompleteActions[k];
                        if (a == null || !_actionViews.TryGetValue(a, out var av)) continue;
                        var ay  = y + nodeH + vGap + k * (actionH + vGap);
                        var  aPos = new Vector2(ax, ay);
                        av.SetPosition(new Rect(aPos, new Vector2(actionW, 0)));
                        a.graphPosition = aPos;
                    }
                }
            }

            // ── Step 5: Place unlinked nodes + actions in col 0 ──────────────
            var orphY = startY;
            foreach (var node in _tutorialConfig.nodes)
            {
                if (node == null || reachable.Contains(node.id)) continue;
                if (!_nodeViews.TryGetValue(node.id, out var view)) continue;
                var pos = new Vector2(startX, orphY);
                view.SetPosition(new Rect(pos, new Vector2(nodeW, 0)));
                node.graphPosition = pos;
                orphY += nodeH + rowGap;
            }
            foreach (var av in _actionViews.Values)
            {
                var ownerInFlow = av.OwnerNode != null && reachable.Contains(av.OwnerNode.id);
                if (ownerInFlow) continue;
                var aPos = new Vector2(startX, orphY);
                av.SetPosition(new Rect(aPos, new Vector2(actionW, 0)));
                av.Data.graphPosition = aPos;
                orphY += actionH + vGap;
            }

            EditorUtility.SetDirty(_tutorialConfig);
        }

        // ── Validation ────────────────────────────────────────────────────────

        public List<string> Validate()
        {
            var errors = new List<string>();
            if (_tutorialConfig?.nodes == null) return errors;
            var ids = new HashSet<string>();

            for (var i = 0; i < _tutorialConfig.nodes.Count; i++)
            {
                var node = _tutorialConfig.nodes[i];
                if (node == null)                             { errors.Add($"[Error] nodes[{i}] is null."); continue; }
                if (string.IsNullOrWhiteSpace(node.id))         errors.Add($"[Error] nodes[{i}] has no ID.");
                else if (!ids.Add(node.id))                     errors.Add($"[Error] Duplicate ID: '{node.id}'.");
                if (!string.IsNullOrWhiteSpace(node.nextNodeId) &&
                    !_tutorialConfig.nodes.Any(n => n?.id == node.nextNodeId))
                    errors.Add($"[Error] '{node.id}' → nextNodeId '{node.nextNodeId}' not found.");
                if (node is TutorialNodeConditional cond && cond.conditions != null)
                {
                    if (cond.conditions.Length == 0)
                        errors.Add($"[Warning] '{node.id}' Conditional node has no conditions defined.");

                    for (var c = 0; c < cond.conditions.Length; c++)
                    {
                        var condition = cond.conditions[c];
                        if (condition == null)
                        {
                            errors.Add($"[Error] '{node.id}' condition[{c}] is null.");
                            continue;
                        }

                        // Validate condition data fields
                        var condErr = condition.InvalidError();
                        if (!string.IsNullOrEmpty(condErr))
                            errors.Add($"[Error] '{node.id}' condition[{c}] ({condition.GetType().Name}): {condErr}");

                        // Validate targetNodeId reference
                        var t = condition.targetNodeId;
                        if (string.IsNullOrWhiteSpace(t))
                            errors.Add($"[Warning] '{node.id}' condition[{c}] has no targetNodeId connected.");
                        else if (!_tutorialConfig.nodes.Any(n => n?.id == t))
                            errors.Add($"[Error] '{node.id}' condition[{c}] → '{t}' not found.");
                    }
                }

                // Check actions
                void CheckActions(TutorialAction[] arr, string label)
                {
                    if (arr == null) return;
                    foreach (var a in arr)
                    {
                        if (a == null) { errors.Add($"[Error] '{node.id}' has null action in {label}."); continue; }
                        var err = a.InvalidError();
                        if (!string.IsNullOrEmpty(err)) errors.Add($"[Error] '{node.id}' {label}: {err}");
                    }
                }
                CheckActions(node.actions,           "actions");
                CheckActions(node.onCompleteActions, "onCompleteActions");
            }

            // Reachability
            foreach (var node in _tutorialConfig.nodes.Where(n => n != null))
            {
                var reachable = _tutorialConfig.nodes.IndexOf(node) == 0 ||
                                _tutorialConfig.nodes.Any(n =>
                                    n?.nextNodeId == node.id ||
                                    (n is TutorialNodeConditional c && c.conditions != null &&
                                     c.conditions.Any(cd => cd?.targetNodeId == node.id)));
                if (!reachable) errors.Add($"[Warning] Node '{node.id}' is unreachable.");
            }

            // Disconnected action nodes
            foreach (var av in _actionViews.Values)
                if (av.OwnerNode == null)
                    errors.Add($"[Warning] Action '{TutorialLabelUtils.GetActionLabel(av.Data.GetType()).Name}' is disconnected (not in any node's action list).");

            return errors;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        public void SaveNodePositions()
        {
            foreach (var v  in _nodeViews.Values)   v.SyncPosition();
            foreach (var av in _actionViews.Values) av.SyncPosition();
        }

        public void CenterOnEntry() => FrameAll();

        private string GenerateUniqueNodeId()
        {
            var n = 0; string id;
            var cfgNodes = _tutorialConfig.nodes;
            do { id = $"Node_{n++:D3}"; } while (cfgNodes.Any(node => node?.id == id));
            return id;
        }
    }
}
