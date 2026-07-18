using System;
using SiPVLib.Event;
using SiPVLib.UI;
using SiPVLib.UI.BaseTypes;

namespace SiPVLib.Tutorial.Config.TutorialActions
{
    [TutorialActionLabel("Wait: View Show", "#FFC107")]
    [Serializable]
    public class TutorialActionWaitViewShow : TutorialActionView
    {
        [NonSerialized] private Action<UIShowData> _handler;

        protected override void OnStart()
        {
            // Complete immediately if view is already active (non-Screen layers only)
            if (viewLayer != ViewLayer.Screen && UIManager.Instance.IsViewActive(viewId, viewLayer))
            {
                Complete();
                return;
            }

            _handler = data =>
            {
                if (data.id == viewId && data.layer == viewLayer)
                    Complete();
            };
            EventManager.Add(UIManager.EventUiViewShow, _handler);
        }

        protected override void OnComplete()
        {
            if (_handler == null) return;
            EventManager.Remove(UIManager.EventUiViewShow, _handler);
            _handler = null;
        }
    }
}