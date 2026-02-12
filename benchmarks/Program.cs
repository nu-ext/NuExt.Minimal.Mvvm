using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace Minimal.Mvvm.Benchmarks
{
    internal class Program
    {
        static void Main()
        {
            var version = typeof(AsyncCommand).Assembly
                .GetName().Version?.ToString() ?? "1.0.0";
            var config = DefaultConfig.Instance
                .WithArtifactsPath($@"{version}")
                .WithOption(ConfigOptions.DisableOptimizationsValidator, true);
            BenchmarkRunner.Run<AsyncCommandBenchmarks>(config);
            Console.ReadKey();
        }
    }
}
