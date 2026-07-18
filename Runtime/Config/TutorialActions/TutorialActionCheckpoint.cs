namespace SiPVLib.Tutorial.Config.TutorialActions
{
    /// <summary>
    /// Save tutorial checkpoint to current node
    /// </summary>
    [TutorialActionLabel("Checkpoint", "#28A745")]
    [System.Serializable]
    public class TutorialActionCheckpoint : TutorialAction
    {
        public override string InvalidError() => null; // always valid

        protected override void OnStart()
        {
            if (_ownerNode != null && _ownerTutorialConfig != null)
                TutorialManager.Instance?.OnTutorialNodeCheckpoint(_ownerTutorialConfig.Id, _ownerNode.id);

            Complete();
        }

        protected override void OnComplete() { }
    }
}