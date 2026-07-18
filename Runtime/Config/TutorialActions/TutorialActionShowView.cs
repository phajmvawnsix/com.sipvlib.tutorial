using Cysharp.Threading.Tasks;
using SiPVLib.UI;
using SiPVLib.UI.BaseTypes;
using SiPVLib.UI.Component;

namespace SiPVLib.Tutorial.Config.TutorialActions
{
    [TutorialActionLabel("Show View", "#17A2B8")]
    [System.Serializable]
    public class TutorialActionShowView : TutorialActionView
    {
        public UIParam uiParam;

        protected override void OnStart()
        {
            ShowAsync().Forget();
        }

        private async UniTaskVoid ShowAsync()
        {
            switch (viewLayer)
            {
                case ViewLayer.Screen:   UIManager.Instance.ShowScreen(viewId, uiParam);   break;
                case ViewLayer.Window:   UIManager.Instance.ShowWindow(viewId, uiParam);   break;
                case ViewLayer.Popup:    UIManager.Instance.ShowPopup(viewId, uiParam);    break;
                case ViewLayer.Top:      UIManager.Instance.ShowTop(viewId, uiParam);      break;
                case ViewLayer.Tutorial: UIManager.Instance.ShowTutorial(viewId, uiParam); break;
            }
            await UniTask.Yield(); // ensure spawned before Complete
            Complete();
        }
    }
}