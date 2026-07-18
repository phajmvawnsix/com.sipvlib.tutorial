namespace SiPVLib.Tutorial.Config.TutorialActions
{
    /// <summary>
    /// Checkpoint to a specific node. Useful for looping or skipping parts of the tutorial.
    /// </summary>
    [TutorialActionLabel("Checkpoint To", "#20C997")]
    [System.Serializable]
    public class TutorialActionCheckpointTo : TutorialAction
    {
        public string targetNodeId;

        public override string InvalidError() =>
            string.IsNullOrWhiteSpace(targetNodeId) ? "[CheckpointTo] targetNodeId is empty" : null;

        protected override void OnStart()
        {
            if (_ownerTutorialConfig != null && !string.IsNullOrWhiteSpace(targetNodeId))
                TutorialManager.Instance?.OnTutorialNodeCheckpoint(_ownerTutorialConfig.Id, targetNodeId);

            Complete();
        }

        protected override void OnComplete() { }

#if UNITY_EDITOR
        public override string EditorSummary => targetNodeId;
#endif
    }
}