using System;
using SiPVLib.Config.Compare;
using SiPVLib.Event;
using SiPVLib.UserData;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

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

#if UNITY_EDITOR
        public override string EditorSummary => dataKey;
#endif
    }
}