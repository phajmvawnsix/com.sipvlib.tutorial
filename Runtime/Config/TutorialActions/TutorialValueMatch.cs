using System;
using SiPVLib.Config.Compare;

namespace SiPVLib.Tutorial.Config.TutorialActions
{
    /// <summary>
    /// Shared value-matching helpers for tutorial actions/conditions that compare a runtime value
    /// against configured expected fields, keyed by <see cref="EventDataType"/> flags.
    /// Reuses <see cref="CompareUtils"/> so compare semantics stay consistent across the library.
    /// </summary>
    internal static class TutorialValueMatch
    {
        /// <summary>True when any numeric/bool value flag is set — i.e. a <see cref="CompareMode"/> is meaningful.</summary>
        public static bool HasComparableValue(EventDataType type) =>
            type.HasFlag(EventDataType.ValueLong)   ||
            type.HasFlag(EventDataType.ValueInt)    ||
            type.HasFlag(EventDataType.ValueDouble) ||
            type.HasFlag(EventDataType.ValueFloat)  ||
            type.HasFlag(EventDataType.ValueBool);

        /// <summary>
        /// Matches a boxed runtime value (e.g. an event payload) against the configured expected fields.
        /// Returns true when <paramref name="type"/> is None (no value gate) or on a type mismatch
        /// (mismatched payload type should not block progression).
        /// </summary>
        public static bool Matches(EventDataType type, CompareMode mode, object value,
            long expectedLong, int expectedInt, double expectedDouble, float expectedFloat,
            string expectedString, bool expectedBool)
        {
            if (type == EventDataType.None) return true;

            if (type.HasFlag(EventDataType.ValueLong)   && value is long   l) return l.Compare(expectedLong,   mode);
            if (type.HasFlag(EventDataType.ValueInt)    && value is int    i) return i.Compare(expectedInt,    mode);
            if (type.HasFlag(EventDataType.ValueDouble) && value is double d) return d.Compare(expectedDouble, mode);
            if (type.HasFlag(EventDataType.ValueFloat)  && value is float  f) return f.Compare(expectedFloat,  mode);
            if (type.HasFlag(EventDataType.ValueString) && value is string s) return string.Equals(s, expectedString, StringComparison.Ordinal);
            if (type.HasFlag(EventDataType.ValueBool)   && value is bool   b) return b == expectedBool;

            return true; // type mismatch → don't block
        }
    }
}
