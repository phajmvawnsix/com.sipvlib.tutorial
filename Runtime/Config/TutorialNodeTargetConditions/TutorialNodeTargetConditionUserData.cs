using System;
using SiPVLib.Config.Compare;
using SiPVLib.Tutorial.Config.TutorialActions;
using SiPVLib.UserData;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace SiPVLib.Tutorial.Config.TutorialNodeTargetConditions
{
    [TutorialConditionLabel("User Data", "#9C27B0")]
    [Serializable]
    public class TutorialNodeTargetConditionUserData : TutorialNodeTargetCondition
    {
        public string        dataKey;
        public EventDataType dataType;

#if ODIN_INSPECTOR
        [ShowIf(nameof(ShouldSerializeCompareMode))]
#endif
        public CompareMode compareMode = CompareMode.Equal;

#if ODIN_INSPECTOR
        [ShowIf("@(((int)dataType & (int)EventDataType.ValueLong) != 0)")]
#endif
        public long   valueLong;
#if ODIN_INSPECTOR
        [ShowIf("@(((int)dataType & (int)EventDataType.ValueInt) != 0)")]
#endif
        public int    valueInt;
#if ODIN_INSPECTOR
        [ShowIf("@(((int)dataType & (int)EventDataType.ValueDouble) != 0)")]
#endif
        public double valueDouble;
#if ODIN_INSPECTOR
        [ShowIf("@(((int)dataType & (int)EventDataType.ValueFloat) != 0)")]
#endif
        public float  valueFloat;
#if ODIN_INSPECTOR
        [ShowIf("@(((int)dataType & (int)EventDataType.ValueString) != 0)")]
#endif
        public string valueString;
#if ODIN_INSPECTOR
        [ShowIf("@(((int)dataType & (int)EventDataType.ValueBool) != 0)")]
#endif
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