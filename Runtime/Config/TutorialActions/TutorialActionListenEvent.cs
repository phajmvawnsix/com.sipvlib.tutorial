using System;
using SiPVLib.Config.Compare;
using SiPVLib.Event;
using Alchemy.Inspector;

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

        private bool IsValueLongFlag => eventDataType.HasFlag(EventDataType.ValueLong);
        private bool IsValueIntFlag => eventDataType.HasFlag(EventDataType.ValueInt);
        private bool IsValueDoubleFlag => eventDataType.HasFlag(EventDataType.ValueDouble);
        private bool IsValueFloatFlag => eventDataType.HasFlag(EventDataType.ValueFloat);
        private bool IsValueStringFlag => eventDataType.HasFlag(EventDataType.ValueString);
        private bool IsValueBoolFlag => eventDataType.HasFlag(EventDataType.ValueBool);

#if UNITY_EDITOR
        public override string EditorSummary => eventName;
#endif
    }
}