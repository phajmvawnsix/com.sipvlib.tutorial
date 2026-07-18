using System;
using SiPVLib.Config.Compare;
using SiPVLib.Event;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace SiPVLib.Tutorial.Config.TutorialActions
{
    /// <summary>
    /// Waits for an event to be fired by EventManager. If <see cref="targetId"/> is set, only targeted events match.
    /// </summary>
    [TutorialActionLabel("Listen Event", "#9C27B0")]
    [Serializable]
    public class TutorialActionListenEvent : TutorialAction
    {
        public string        eventName;
        public string        targetId;
        public EventDataType eventDataType;

#if ODIN_INSPECTOR
        [ShowIf(nameof(ShouldSerializeCompareMode))]
#endif
        public CompareMode compareMode = CompareMode.Equal;

#if ODIN_INSPECTOR
        [ShowIf("@(((int)eventDataType & (int)EventDataType.ValueLong) != 0)")]
#endif
        public long   valueLong;
#if ODIN_INSPECTOR
        [ShowIf("@(((int)eventDataType & (int)EventDataType.ValueInt) != 0)")]
#endif
        public int    valueInt;
#if ODIN_INSPECTOR
        [ShowIf("@(((int)eventDataType & (int)EventDataType.ValueDouble) != 0)")]
#endif
        public double valueDouble;
#if ODIN_INSPECTOR
        [ShowIf("@(((int)eventDataType & (int)EventDataType.ValueFloat) != 0)")]
#endif
        public float  valueFloat;
#if ODIN_INSPECTOR
        [ShowIf("@(((int)eventDataType & (int)EventDataType.ValueString) != 0)")]
#endif
        public string valueString;
#if ODIN_INSPECTOR
        [ShowIf("@(((int)eventDataType & (int)EventDataType.ValueBool) != 0)")]
#endif
        public bool   valueBool;

        public override string InvalidError() =>
            string.IsNullOrWhiteSpace(eventName) ? "[ListenEvent] eventName is empty" : null;

        // Removes whatever overload was subscribed in OnStart (typed or parameterless, targeted or not).
        [NonSerialized] private Action _unsubscribe;

        protected override void OnStart()
        {
            var targeted = !string.IsNullOrWhiteSpace(targetId);

            if (eventDataType == EventDataType.None)     { SubscribeParameterless(targeted); return; }
            if (eventDataType.HasFlag(EventDataType.ValueLong))   { Subscribe<long>(targeted);   return; }
            if (eventDataType.HasFlag(EventDataType.ValueInt))    { Subscribe<int>(targeted);    return; }
            if (eventDataType.HasFlag(EventDataType.ValueDouble)) { Subscribe<double>(targeted); return; }
            if (eventDataType.HasFlag(EventDataType.ValueFloat))  { Subscribe<float>(targeted);  return; }
            if (eventDataType.HasFlag(EventDataType.ValueString)) { Subscribe<string>(targeted); return; }
            if (eventDataType.HasFlag(EventDataType.ValueBool))   { Subscribe<bool>(targeted);   return; }

            // TargetUI / ClassObject and any unhandled flag: fall back to firing on the bare event.
            SubscribeParameterless(targeted);
        }

        private void SubscribeParameterless(bool targeted)
        {
            Action handler = Complete;
            if (targeted)
            {
                EventManager.Add(eventName, targetId, handler);
                _unsubscribe = () => EventManager.Remove(eventName, targetId, handler);
            }
            else
            {
                EventManager.Add(eventName, handler);
                _unsubscribe = () => EventManager.Remove(eventName, handler);
            }
        }

        private void Subscribe<T>(bool targeted)
        {
            Action<T> handler = value =>
            {
                if (TutorialValueMatch.Matches(eventDataType, compareMode, value,
                        valueLong, valueInt, valueDouble, valueFloat, valueString, valueBool))
                    Complete();
            };
            if (targeted)
            {
                EventManager.Add(eventName, targetId, handler);
                _unsubscribe = () => EventManager.Remove(eventName, targetId, handler);
            }
            else
            {
                EventManager.Add(eventName, handler);
                _unsubscribe = () => EventManager.Remove(eventName, handler);
            }
        }

        protected override void OnComplete()
        {
            _unsubscribe?.Invoke();
            _unsubscribe = null;
        }

        private bool ShouldSerializeCompareMode => TutorialValueMatch.HasComparableValue(eventDataType);

#if UNITY_EDITOR
        public override string EditorSummary => eventName;
#endif
    }
}