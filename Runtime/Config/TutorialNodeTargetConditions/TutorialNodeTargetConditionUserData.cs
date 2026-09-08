using System;
using SiPVLib.Config.Compare;
using SiPVLib.Tutorial.Config.TutorialActions;
using SiPVLib.UserData;
using Alchemy.Inspector;

namespace SiPVLib.Tutorial.Config.TutorialNodeTargetConditions
{
    [TutorialConditionLabel("User Data", "#9C27B0")]
    [Serializable]
    public class TutorialNodeTargetConditionUserData : TutorialNodeTargetCondition
    {
        public string        dataKey;
        public EventDataType dataType;

        [ShowIf(nameof(ShouldSerializeCompareMode))]
        public CompareMode compareMode = CompareMode.Equal;

        [ShowIf(nameof(IsValueLongFlag))]
        public long   valueLong;
        [ShowIf(nameof(IsValueIntFlag))]
        public int    valueInt;
        [ShowIf(nameof(IsValueDoubleFlag))]
        public double valueDouble;
        [ShowIf(nameof(IsValueFloatFlag))]
        public float  valueFloat;
        [ShowIf(nameof(IsValueStringFlag))]
        public string valueString;
        [ShowIf(nameof(IsValueBoolFlag))]
        public bool   valueBool;

        public override bool IsConditionMet()
        {
            var mgr = UserDataManager.Instance;
            if (mgr == null || !mgr.IsInitialized) return false;

            if (dataType == EventDataType.None)
                return mgr.HasKey(dataKey);

            if (dataType.HasFlag(EventDataType.ValueLong))
                return mgr.Get<long>(dataKey).Compare(valueLong, compareMode);

            if (dataType.HasFlag(EventDataType.ValueInt))
                return mgr.Get<int>(dataKey).Compare(valueInt, compareMode);

            if (dataType.HasFlag(EventDataType.ValueDouble))
                return mgr.Get<double>(dataKey).Compare(valueDouble, compareMode);

            if (dataType.HasFlag(EventDataType.ValueFloat))
                return mgr.Get<float>(dataKey).Compare(valueFloat, compareMode);

            if (dataType.HasFlag(EventDataType.ValueString))
                return string.Equals(mgr.Get<string>(dataKey), valueString, StringComparison.Ordinal);

            if (dataType.HasFlag(EventDataType.ValueBool))
                return mgr.Get<bool>(dataKey) == valueBool;

            return false;
        }

        private bool ShouldSerializeCompareMode => TutorialValueMatch.HasComparableValue(dataType);

        private bool IsValueLongFlag => dataType.HasFlag(EventDataType.ValueLong);
        private bool IsValueIntFlag => dataType.HasFlag(EventDataType.ValueInt);
        private bool IsValueDoubleFlag => dataType.HasFlag(EventDataType.ValueDouble);
        private bool IsValueFloatFlag => dataType.HasFlag(EventDataType.ValueFloat);
        private bool IsValueStringFlag => dataType.HasFlag(EventDataType.ValueString);
        private bool IsValueBoolFlag => dataType.HasFlag(EventDataType.ValueBool);

        public override string InvalidError()
        {
            if (string.IsNullOrWhiteSpace(dataKey)) return "dataKey is empty.";
            return null;
        }

#if UNITY_EDITOR
        public override string EditorSummary
        {
            get
            {
                if (string.IsNullOrEmpty(dataKey)) return "?";
                return dataType == EventDataType.None
                    ? $"HasKey: {dataKey}"
                    : $"{dataKey} ({dataType}) {compareMode}";
            }
        }
#endif
    }
}