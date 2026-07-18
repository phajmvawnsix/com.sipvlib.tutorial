using System;
using System.Collections.Generic;
using System.Linq;
using SiPVLib.Tutorial.Config;
using UnityEditor;
using UnityEngine;

namespace SiPVLib.Tutorial.Editor
{
    internal static class TutorialLabelUtils
    {
        // ── Node labels ──────────────────────────────────────────────────────

        public static (string Name, string HexColor) GetNodeLabel(Type type)
        {
            var attr = type.GetCustomAttributes(typeof(TutorialNodeLabelAttribute), false)
                          .FirstOrDefault() as TutorialNodeLabelAttribute;
            return attr != null ? (attr.Name, attr.HexColor) : (type.Name, "#4A90D9");
        }

        // ── Action labels ─────────────────────────────────────────────────────

        public static (string Name, string HexColor) GetActionLabel(Type type)
        {
            var attr = type.GetCustomAttributes(typeof(TutorialActionLabelAttribute), false)
                          .FirstOrDefault() as TutorialActionLabelAttribute;
            return attr != null ? (attr.Name, attr.HexColor) : (type.Name, "#6A9153");
        }

        // ── Condition labels ─────────────────────────────────────────────────

        public static (string Name, string HexColor) GetConditionLabel(Type type)
        {
            var attr = type.GetCustomAttributes(typeof(TutorialConditionLabelAttribute), false)
                          .FirstOrDefault() as TutorialConditionLabelAttribute;
            return attr != null ? (attr.Name, attr.HexColor) : (type.Name, "#FF8C00");
        }

        // ── Type discovery ───────────────────────────────────────────────────

        /// <summary>All concrete TutorialNode subclasses (including TutorialNode itself), ordered by display name.</summary>
        public static IEnumerable<Type> GetAllNodeTypes() =>
            TypeCache.GetTypesDerivedFrom<TutorialNode>()
                     .Prepend(typeof(TutorialNode))
                     .Where(t => !t.IsAbstract)
                     .OrderBy(t => GetNodeLabel(t).Name);

        /// <summary>All concrete TutorialAction subclasses, ordered by display name.</summary>
        public static IEnumerable<Type> GetAllActionTypes() =>
            TypeCache.GetTypesDerivedFrom<TutorialAction>()
                     .Where(t => !t.IsAbstract)
                     .OrderBy(t => GetActionLabel(t).Name);

        /// <summary>All concrete TutorialNodeTargetCondition subclasses, ordered by display name.</summary>
        public static IEnumerable<Type> GetAllConditionTypes() =>
            TypeCache.GetTypesDerivedFrom<TutorialNodeTargetCondition>()
                     .Where(t => !t.IsAbstract)
                     .OrderBy(t => GetConditionLabel(t).Name);

        // ── Color parsing ────────────────────────────────────────────────────

        public static Color ParseColor(string hex) =>
            ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.white;
    }
}

