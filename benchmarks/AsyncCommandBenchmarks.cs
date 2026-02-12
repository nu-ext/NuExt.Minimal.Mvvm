using BenchmarkDotNet.Attributes;

namespace Minimal.Mvvm.Benchmarks
{
    [MemoryDiagnoser]
    //[SimpleJob(RuntimeMoniker.Net10)]
    public class AsyncCommandBenchmarks
    {
        private AsyncCommand<int> _command = null!;

        [Params(1, 10, 100, 1000, 10000)]
        public int ConcurrentTasks;

        [GlobalSetup]
        public void Setup()
        {
            _command = new AsyncCommand<int>(async (param, token) =>
            {
                await Task.Delay(100, token);
            })
            {  AllowConcurrentExecution = true};
        }

        [Benchmark]
        public async Task ExecuteAsync_MultipleConcurrentTasks_NoCancellation()
        {
            var tasks = new Task[ConcurrentTasks];
            for (int i = 0; i < ConcurrentTasks; i++)
            {
                tasks[i] = _command.ExecuteAsync(0);
            }

            await Task.WhenAll(tasks);
        }

        [Benchmark]
        public async Task ExecuteAsync_MultipleConcurrentTasks_WithCancellation()
        {
            var cts = new CancellationTokenSource();
            var tasks = new Task[ConcurrentTasks];
            for (int i = 0; i < ConcurrentTasks; i++)
            {
                tasks[i] = _command.ExecuteAsync(0, cts.Token);
            }

            await Task.Delay(10);
            cts.Cancel();

            try
            {
                await Task.WhenAll(tasks);
            }
            catch (OperationCanceledException)
            {
            }
        }


        [Benchmark]
        public async Task Cancel_Performance_UnderLoad()
        {
            var tasks = new Task[ConcurrentTasks];
            for (int i = 0; i < ConcurrentTasks; i++)
            {
                tasks[i] = _command.ExecuteAsync(0);
            }

            await Task.Delay(10);

            _command.Cancel();

            try
            {
                await Task.WhenAll(tasks);
            }
            catch 
            {
            }
        }
    }
}
