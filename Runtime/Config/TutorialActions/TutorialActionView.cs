using SiPVLib.Config;
using SiPVLib.UI.BaseTypes;
using SiPVLib.UI.Config;

namespace SiPVLib.Tutorial.Config.TutorialActions
{
    [TutorialActionLabel("View", "#607D8B")]
    public abstract class TutorialActionView : TutorialAction
    {
        [ConfigRef(typeof(ViewConfig))] public string viewId;
        public ViewLayer viewLayer = ViewLayer.Popup;

        public override string InvalidError() =>
            string.IsNullOrWhiteSpace(viewId) ? $"[{GetType().Name}] viewId is empty" : null;

        protected override void OnComplete() { }

#if UNITY_EDITOR
        public override string EditorSummary => viewId;
#endif
    }
}