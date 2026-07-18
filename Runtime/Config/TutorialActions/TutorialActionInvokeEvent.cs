using System;
using SiPVLib.Event;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace SiPVLib.Tutorial.Config.TutorialActions
{
    [Serializable, Flags]
    public enum EventDataType
    {
        None        = 0,
        ValueLong   = 1 << 0,
        ValueInt    = 1 << 1,
        ValueDouble = 1 << 2,
        ValueFloat  = 1 << 3,
        ValueString = 1 << 4,
        ValueBool   = 1 << 5,
        TargetUI    = 1 << 6,
        ClassObject = 1 << 7,
    }

    /// <summary>
    /// Invoke an event using EventManager. If <see cref="targetId"/> is set the targeted overload is used.
    /// Completes immediately after firing.
    /// </summary>
    [TutorialActionLabel("Invoke Event", "#E91E63")]
    [Serializable]
    public class TutorialActionInvokeEvent : TutorialAction
    {
        public string        eventName;
        public string        targetId;
        public EventDataType eventDataType;
        
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
            string.IsNullOrWhiteSpace(eventName) ? "[InvokeEvent] eventName is empty" : null;

        protected override void OnStart()
        {
            var targeted = !string.IsNullOrWhiteSpace(targetId);

            if (eventDataType == EventDataType.None)
            {
                if (targeted) EventManager.Invoke(eventName, targetId);
                else          EventManager.Invoke(eventName);
            }
            else if (eventDataType.HasFlag(EventDataType.ValueLong))
            {
                if (targeted) EventManager.Invoke(eventName, targetId, valueLong);
                else          EventManager.Invoke(eventName, valueLong);
            }
            else if (eventDataType.HasFlag(EventDataType.ValueInt))
            {
                if (targeted) EventManager.Invoke(eventName, targetId, valueInt);
                else          EventManager.Invoke(eventName, valueInt);
            }
            else if (eventDataType.HasFlag(EventDataType.ValueDouble))
            {
                if (targeted) EventManager.Invoke(eventName, targetId, valueDouble);
                else          EventManager.Invoke(eventName, valueDouble);
            }
            else if (eventDataType.HasFlag(EventDataType.ValueFloat))
            {
                if (targeted) EventManager.Invoke(eventName, targetId, valueFloat);
                else          EventManager.Invoke(eventName, valueFloat);
            }
            else if (eventDataType.HasFlag(EventDataType.ValueString))
            {
                if (targeted) EventManager.Invoke(eventName, targetId, valueString);
                else          EventManager.Invoke(eventName, valueString);
            }
            else if (eventDataType.HasFlag(EventDataType.ValueBool))
            {
                if (targeted) EventManager.Invoke(eventName, targetId, valueBool);
                else          EventManager.Invoke(eventName, valueBool);
            }
            else
            {
                if (targeted) EventManager.Invoke(eventName, targetId);
                else          EventManager.Invoke(eventName);
            }

            Complete();
        }

        protected override void OnComplete() { }

#if UNITY_EDITOR
        public override string EditorSummary => eventName;
#endif
    }
}