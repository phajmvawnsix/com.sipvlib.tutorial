using Cysharp.Threading.Tasks;
using SiPVLib.UI;
using SiPVLib.UI.BaseTypes;

namespace SiPVLib.Tutorial.Config.TutorialActions
{
    [TutorialActionLabel("Hide View", "#6C757D")]
    [System.Serializable]
    public class TutorialActionHideView : TutorialActionView
    {
        protected override void OnStart()
        {
            HideAsync().Forget();
        }

        private async UniTaskVoid HideAsync()
        {
            if (viewLayer == ViewLayer.Screen)
            {
                await UIManager.Instance.HideScreenAsync();
            }
            else
            {
                var view = UIManager.Instance.GetActiveView(viewId, viewLayer);
                if (view == null) { Complete(); return; }

                switch (viewLayer)
                {
                    case ViewLayer.Window:   await UIManager.Instance.HideWindowAsync(view);   break;
                    case ViewLayer.Popup:    await UIManager.Instance.HidePopupAsync(view);    break;
                    case ViewLayer.Top:      await UIManager.Instance.HideTopAsync(view);      break;
                    case ViewLayer.Tutorial: await UIManager.Instance.HideTutorialAsync(view); break;
                }
            }

            Complete();
        }
    }
}
