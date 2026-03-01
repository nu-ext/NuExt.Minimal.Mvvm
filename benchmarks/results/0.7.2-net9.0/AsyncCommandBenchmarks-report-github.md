```

BenchmarkDotNet v0.15.8, Windows 10 (10.0.19045.6456/22H2/2022Update)
Intel Core i7-8700T CPU 2.40GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
.NET SDK 10.0.103
  [Host]     : .NET 9.0.13 (9.0.13, 9.0.1326.6317), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 9.0.13 (9.0.13, 9.0.1326.6317), X64 RyuJIT x86-64-v3


```
| Method                                                | ConcurrentTasks | Mean      | Error    | StdDev   | Gen0      | Gen1      | Gen2     | Allocated  |
|------------------------------------------------------ |---------------- |----------:|---------:|---------:|----------:|----------:|---------:|-----------:|
| **ExecuteAsync_MultipleConcurrentTasks_NoCancellation**   | **1**               | **106.29 ms** | **1.567 ms** | **1.466 ms** |         **-** |         **-** |        **-** |      **984 B** |
| ExecuteAsync_MultipleConcurrentTasks_WithCancellation | 1               |  17.41 ms | 0.347 ms | 0.890 ms |         - |         - |        - |     3200 B |
| Cancel_Performance_UnderLoad                          | 1               |  17.71 ms | 0.352 ms | 0.670 ms |         - |         - |        - |     3048 B |
| **ExecuteAsync_MultipleConcurrentTasks_NoCancellation**   | **10**              | **105.52 ms** | **2.018 ms** | **2.402 ms** |         **-** |         **-** |        **-** |     **7464 B** |
| ExecuteAsync_MultipleConcurrentTasks_WithCancellation | 10              |  17.28 ms | 0.342 ms | 0.827 ms |         - |         - |        - |    23281 B |
| Cancel_Performance_UnderLoad                          | 10              |  16.73 ms | 0.333 ms | 0.829 ms |         - |         - |        - |    22265 B |
| **ExecuteAsync_MultipleConcurrentTasks_NoCancellation**   | **100**             | **106.40 ms** | **2.027 ms** | **1.991 ms** |         **-** |         **-** |        **-** |    **72264 B** |
| ExecuteAsync_MultipleConcurrentTasks_WithCancellation | 100             |  16.17 ms | 0.321 ms | 0.528 ms |   31.2500 |         - |        - |   222422 B |
| Cancel_Performance_UnderLoad                          | 100             |  16.58 ms | 0.331 ms | 0.836 ms |   31.2500 |         - |        - |   212766 B |
| **ExecuteAsync_MultipleConcurrentTasks_NoCancellation**   | **1000**            | **106.19 ms** | **2.021 ms** | **1.890 ms** |         **-** |         **-** |        **-** |   **720264 B** |
| ExecuteAsync_MultipleConcurrentTasks_WithCancellation | 1000            |  17.11 ms | 0.340 ms | 0.671 ms |  343.7500 |  250.0000 |        - |  2209655 B |
| Cancel_Performance_UnderLoad                          | 1000            |  16.81 ms | 0.330 ms | 0.543 ms |  312.5000 |  281.2500 |        - |  2113552 B |
| **ExecuteAsync_MultipleConcurrentTasks_NoCancellation**   | **10000**           | **110.91 ms** | **1.137 ms** | **1.063 ms** | **1000.0000** |  **800.0000** |        **-** |  **7200264 B** |
| ExecuteAsync_MultipleConcurrentTasks_WithCancellation | 10000           |  79.14 ms | 1.582 ms | 3.009 ms | 4428.5714 | 2571.4286 | 857.1429 | 22209495 B |
| Cancel_Performance_UnderLoad                          | 10000           |  70.74 ms | 1.372 ms | 2.255 ms | 4000.0000 | 2285.7143 | 714.2857 | 21250641 B |
