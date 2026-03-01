```

BenchmarkDotNet v0.15.8, Windows 10 (10.0.19045.6456/22H2/2022Update)
Intel Core i7-8700T CPU 2.40GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
.NET SDK 10.0.103
  [Host]     : .NET 10.0.3 (10.0.3, 10.0.326.7603), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.3 (10.0.3, 10.0.326.7603), X64 RyuJIT x86-64-v3


```
| Method                                                | ConcurrentTasks | Mean      | Error    | StdDev   | Gen0      | Gen1      | Gen2     | Allocated  |
|------------------------------------------------------ |---------------- |----------:|---------:|---------:|----------:|----------:|---------:|-----------:|
| **ExecuteAsync_MultipleConcurrentTasks_NoCancellation**   | **1**               | **106.62 ms** | **1.879 ms** | **1.758 ms** |         **-** |         **-** |        **-** |      **912 B** |
| ExecuteAsync_MultipleConcurrentTasks_WithCancellation | 1               |  17.11 ms | 0.339 ms | 0.882 ms |         - |         - |        - |     3000 B |
| Cancel_Performance_UnderLoad                          | 1               |  17.09 ms | 0.341 ms | 0.946 ms |         - |         - |        - |     2784 B |
| **ExecuteAsync_MultipleConcurrentTasks_NoCancellation**   | **10**              | **106.31 ms** | **1.722 ms** | **1.611 ms** |         **-** |         **-** |        **-** |     **7464 B** |
| ExecuteAsync_MultipleConcurrentTasks_WithCancellation | 10              |  16.65 ms | 0.330 ms | 0.859 ms |         - |         - |        - |    23294 B |
| Cancel_Performance_UnderLoad                          | 10              |  16.59 ms | 0.321 ms | 0.895 ms |         - |         - |        - |    22195 B |
| **ExecuteAsync_MultipleConcurrentTasks_NoCancellation**   | **100**             | **104.13 ms** | **1.928 ms** | **1.804 ms** |         **-** |         **-** |        **-** |    **72320 B** |
| ExecuteAsync_MultipleConcurrentTasks_WithCancellation | 100             |  16.53 ms | 0.330 ms | 0.904 ms |   31.2500 |         - |        - |   222444 B |
| Cancel_Performance_UnderLoad                          | 100             |  16.49 ms | 0.327 ms | 0.808 ms |   31.2500 |         - |        - |   212705 B |
| **ExecuteAsync_MultipleConcurrentTasks_NoCancellation**   | **1000**            | **106.18 ms** | **2.059 ms** | **2.022 ms** |         **-** |         **-** |        **-** |   **720264 B** |
| ExecuteAsync_MultipleConcurrentTasks_WithCancellation | 1000            |  16.04 ms | 0.321 ms | 0.633 ms |  343.7500 |  250.0000 |        - |  2209641 B |
| Cancel_Performance_UnderLoad                          | 1000            |  16.79 ms | 0.335 ms | 0.809 ms |  328.1250 |  281.2500 |        - |  2113488 B |
| **ExecuteAsync_MultipleConcurrentTasks_NoCancellation**   | **10000**           | **110.80 ms** | **2.211 ms** | **2.365 ms** | **1000.0000** |  **800.0000** |        **-** |  **7200264 B** |
| ExecuteAsync_MultipleConcurrentTasks_WithCancellation | 10000           |  68.11 ms | 1.354 ms | 2.299 ms | 4285.7143 | 2571.4286 | 857.1429 | 22201647 B |
| Cancel_Performance_UnderLoad                          | 10000           |  66.08 ms | 1.313 ms | 2.826 ms | 4125.0000 | 2500.0000 | 875.0000 | 21241539 B |
