using SiPVLib.UI.Component;
using UnityEngine;

namespace SiPVLib.Tutorial
{
    /// <summary>
    /// Parameter passed to <see cref="TutorialOverlayView"/> so it knows which element to highlight
    /// and which visual aids to show. Created by <c>TutorialActionForceTouch</c>.
    /// </summary>
    public sealed class TutorialOverlayParam : UIParam
    {
        public RectTransform target;
        public bool          dimScreen;
        public bool          showFinger;
    }
}
