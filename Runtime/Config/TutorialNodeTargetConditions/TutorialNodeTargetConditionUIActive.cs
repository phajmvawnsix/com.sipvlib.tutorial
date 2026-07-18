using System;
using SiPVLib.UI;
using SiPVLib.UI.Component;

namespace SiPVLib.Tutorial.Config.TutorialNodeTargetConditions
{
    [TutorialConditionLabel("UI Active", "#17A2B8")]
    [Serializable]
    public class TutorialNodeTargetConditionUIActive : TutorialNodeTargetConditionViewActive
    {
        [UIRef(nameof(viewId))] public string uiId;

        public override bool IsConditionMet()
        {
            var isViewActive = base.IsConditionMet();
            
            if (!isViewActive) return false;
            
            var view = UIManager.Instance.GetActiveView(uiId, layer);
            
            if (view == null) return false;

            var allUI = view.GetComponentsInChildren<UIBase>(true);

            foreach (var ui in allUI)
            {
                if (string.Equals(ui.Id, viewId, StringComparison.Ordinal)) return ui.gameObject.activeInHierarchy;
            }

            return false;
        }

        public override string InvalidError()
        {
            if (string.IsNullOrWhiteSpace(viewId)) return "viewId (Screen) is empty.";
            if (string.IsNullOrWhiteSpace(uiId))   return "uiId (UI element) is empty.";
            return null;
        }

#if UNITY_EDITOR
        public override string EditorSummary => string.IsNullOrEmpty(uiId) ? "?" : $"{layer} | {viewId}/{uiId}";
#endif
    }
}