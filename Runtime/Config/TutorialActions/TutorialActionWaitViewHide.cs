using System;
using SiPVLib.Event;
using SiPVLib.UI;
using SiPVLib.UI.BaseTypes;

namespace SiPVLib.Tutorial.Config.TutorialActions
{
    [TutorialActionLabel("Wait: View Hide", "#FD7E14")]
    [Serializable]
    public class TutorialActionWaitViewHide : TutorialActionView
    {
        [NonSerialized] private Action<UIHideData> _handler;

        protected override void OnStart()
        {
            // Complete immediately if view is already gone (non-Screen layers only)
            if (viewLayer != ViewLayer.Screen && !UIManager.Instance.IsViewActive(viewId, viewLayer))
            {
                Complete();
                return;
            }

            _handler = data =>
            {
                if (data.id == viewId && data.layer == viewLayer)
                    Complete();
            };
            EventManager.Add(UIManager.EventUiViewHide, _handler);
        }

        protected override void OnComplete()
        {
            if (_handler == null) return;
            EventManager.Remove(UIManager.EventUiViewHide, _handler);
            _handler = null;
        }
    }
}