using Cysharp.Threading.Tasks;

namespace SiPVLib.Tutorial.Config
{
    /// <summary>
    /// Shared per-run state for a single tutorial runtime instance: the pause flag and a
    /// cancellation signal. Created in <see cref="TutorialConfig.Start"/>, one per run.
    /// All access is on the Unity main thread (tutorials are driven by the UniTask player loop),
    /// so no locking is required.
    /// </summary>
    public sealed class TutorialRuntimeContext
    {
        /// <summary>True while the tutorial is paused. Node-to-node progression is gated on this.</summary>
        public bool IsPaused { get; private set; }

        /// <summary>True once the run has been cancelled (skipped/aborted).</summary>
        public bool IsCancelled { get; private set; }

        public void Pause()  => IsPaused = true;
        public void Resume() => IsPaused = false;

        /// <summary>Marks the run cancelled and releases any pause-wait so awaiters can unwind.</summary>
        public void Cancel()
        {
            IsCancelled = true;
            IsPaused    = false; // release WaitWhilePaused so it observes cancellation and returns
        }

        /// <summary>
        /// Awaits until the run is resumed or cancelled. Yields once per frame while paused, so
        /// it allocates nothing per poll. Returns immediately when not paused.
        /// </summary>
        public async UniTask WaitWhilePaused()
        {
            while (IsPaused && !IsCancelled)
                await UniTask.Yield(PlayerLoopTiming.Update);
        }
    }
}
