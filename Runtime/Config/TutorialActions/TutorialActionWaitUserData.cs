using System;
using SiPVLib.Config.Compare;
using SiPVLib.Event;
using SiPVLib.UserData;
using Alchemy.Inspector;

namespace SiPVLib.Tutorial.Config.TutorialActions
{
    /// <summary>
    /// Waits for a UserData save event matching <see cref="dataKey"/> and an optional value condition.
    /// </summary>
    [TutorialActionLabel("Wait: User Data", "#DC3545")]
    [Serializable]
    public class TutorialActionWaitUserData : TutorialAction
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

        public override string InvalidError() =>
            string.IsNullOrWhiteSpace(dataKey) ? "[WaitUserData] dataKey is empty" : null;

        [NonSerialized] private Action<UserDataSaveEvent> _handler;

        protected override void OnStart()
        {
            _handler = evt =>
            {
                if (evt.key != dataKey || !evt.success) return;
                if (TutorialValueMatch.Matches(dataType, compareMode, evt.value,
                        valueLong, valueInt, valueDouble, valueFloat, valueString, valueBool))
                    Complete();
            };
            EventManager.Add(UserDataManager.EventUserDataSave, _handler);
        }

        protected override void OnComplete()
        {
            if (_handler == null) return;
            EventManager.Remove(UserDataManager.EventUserDataSave, _handler);
            _handler = null;
        }

        private bool ShouldSerializeCompareMode => TutorialValueMatch.HasComparableValue(dataType);

        private bool IsValueLongFlag => dataType.HasFlag(EventDataType.ValueLong);
        private bool IsValueIntFlag => dataType.HasFlag(EventDataType.ValueInt);
        private bool IsValueDoubleFlag => dataType.HasFlag(EventDataType.ValueDouble);
        private bool IsValueFloatFlag => dataType.HasFlag(EventDataType.ValueFloat);
        private bool IsValueStringFlag => dataType.HasFlag(EventDataType.ValueString);
        private bool IsValueBoolFlag => dataType.HasFlag(EventDataType.ValueBool);

#if UNITY_EDITOR
        public override string EditorSummary => dataKey;
#endif
    }
}