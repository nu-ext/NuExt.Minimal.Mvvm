namespace Minimal.Mvvm.Tests.Infrastructure
{

    /// <summary>
    /// Helpers to record execution order and create delay tasks.
    /// </summary>
    internal static class TestHelpers
    {
        public static Func<CancellationToken, Task> AsyncStep(List<string> log, string tag, int delayMs = 10)
        {
            return async ct =>
            {
                log.Add(tag + ":start");
                await Task.Delay(delayMs, ct).ConfigureAwait(false);
                log.Add(tag + ":end");
            };
        }

        public static Action SyncStep(List<string> log, string tag)
        {
            return () => log.Add(tag);
        }
    }

}
