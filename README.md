# NuExt.Minimal.Mvvm

`NuExt.Minimal.Mvvm` is a **high-performance, dependency-free** MVVM core for .NET focused on **robust async flows** and **deterministic command execution**. It provides a **clear, minimal API** with Bindable/ViewModel base types, a self-validating command model (Relay/Async/Composite), and a lightweight service provider.

[![NuGet](https://img.shields.io/nuget/v/NuExt.Minimal.Mvvm.svg)](https://www.nuget.org/packages/NuExt.Minimal.Mvvm)
[![Build](https://github.com/IvanGit/NuExt.Minimal.Mvvm/actions/workflows/ci.yml/badge.svg)](https://github.com/IvanGit/NuExt.Minimal.Mvvm/actions/workflows/ci.yml)
[![License](https://img.shields.io/github/license/IvanGit/NuExt.Minimal.Mvvm?label=license)](https://github.com/IvanGit/NuExt.Minimal.Mvvm/blob/main/LICENSE)
[![Downloads](https://img.shields.io/nuget/dt/NuExt.Minimal.Mvvm.svg)](https://www.nuget.org/packages/NuExt.Minimal.Mvvm)

## Features

- **Core:**
  - `Minimal.Mvvm.BindableBase` — lightweight `INotifyPropertyChanged` base.
  - `Minimal.Mvvm.ViewModelBase` — lean ViewModel foundation with simple service access.

- **Command Model (self-validating):**
  All commands (`RelayCommand`, `RelayCommand<T>`, `AsyncCommand`, `AsyncCommand<T>`, `CompositeCommand`) validate their state internally: if `CanExecute(parameter)` is `false`, `Execute(parameter)` performs no action. This guarantees consistent behavior for both UI-bound and programmatic calls.

- **Command Implementations:**
  - `Minimal.Mvvm.RelayCommand` / `RelayCommand<T>` - classic, **synchronous** delegate-based commands with optional concurrency control.
  - `Minimal.Mvvm.AsyncCommand` / `AsyncCommand<T>` - full-featured **asynchronous** commands with **cancellation support**, reentrancy control, and predictable error propagation.
  - `Minimal.Mvvm.CompositeCommand` - aggregates multiple commands and executes them **sequentially**; awaits `ExecuteAsync(...)` and calls `Execute(...)` for non-async commands.

- **Async & Concurrency:**
  - Explicit separation: `ICommand.Execute` (fire-and-forget) vs `IAsyncCommand.ExecuteAsync` (awaitable with `CancellationToken`).
  - Built-in reentrancy control via `AllowConcurrentExecution`.
  - Comprehensive Exception Handling: Local (`UnhandledException`) and global (`AsyncCommand.GlobalUnhandledException`) events with `Handled` propagation control.

- **Service Provider Integration**:
  - **`Minimal.Mvvm.ServiceProvider`**: Lightweight service provider for registration and resolution of services, facilitating dependency injection within your application.

### Recommended Companion Package

For an enhanced development experience, we highly recommend using the [`NuExt.Minimal.Mvvm.SourceGenerator`](https://www.nuget.org/packages/NuExt.Minimal.Mvvm.SourceGenerator) package alongside this framework. It provides compile-time boilerplate generation for ViewModels.

### Installation

Via [NuGet](https://www.nuget.org/):

```sh
dotnet add package NuExt.Minimal.Mvvm
```

Or via Visual Studio:

1. Go to `Tools -> NuGet Package Manager -> Manage NuGet Packages for Solution...`.
2. Search for `NuExt.Minimal.Mvvm`.
3. Click "Install".

### Source Code Package

In addition to the standard package, there is also a source code package available: [`NuExt.Minimal.Mvvm.Sources`](https://www.nuget.org/packages/NuExt.Minimal.Mvvm.Sources). This package allows you to embed the entire framework directly into your application, enabling easier source code exploring and debugging.

To install the source code package, use the following command:

```sh
dotnet add package NuExt.Minimal.Mvvm.Sources
```

Or via Visual Studio:

1. Go to `Tools -> NuGet Package Manager -> Manage NuGet Packages for Solution...`.
2. Search for `NuExt.Minimal.Mvvm.Sources`.
3. Click "Install".

### Usage

#### Example: Advanced AsyncCommand with Concurrency and Cancellation

```csharp
public class SearchViewModel : ViewModelBase
{
    public IAsyncCommand<string> SearchCommand { get; }
    public ICommand CancelCommand { get; }

    public SearchViewModel()
    {
        SearchCommand = new AsyncCommand<string>(SearchAsync, CanSearch)
        {
            AllowConcurrentExecution = true
        };

        AsyncCommand.GlobalUnhandledException += (sender, e) =>
        {
            Logger.LogError(e.Exception, "Global command error");
            e.Handled = true;
        };

        CancelCommand = new RelayCommand(() => SearchCommand.Cancel());
    }

    private async Task SearchAsync(string query, CancellationToken cancellationToken)
    {
        await Task.Delay(1000, cancellationToken);
        Results = $"Results for: {query}";
    }

    private bool CanSearch(string query) => !string.IsNullOrWhiteSpace(query);

    private string _results;
    public string Results
    {
        get => _results;
        private set => SetProperty(ref _results, value);
    }
}
```

#### Example: Two-Tier Exception Handling

```csharp
public class DataViewModel : ViewModelBase
{
    public IAsyncCommand LoadDataCommand { get; }

    public DataViewModel()
    {
        LoadDataCommand = new AsyncCommand(LoadDataAsync);

        LoadDataCommand.UnhandledException += (sender, e) =>
        {
            if (e.Exception is HttpRequestException httpEx)
            {
                ShowError($"Network error: {httpEx.Message}");
                e.Handled = true;
            }
        };
    }

    private async Task LoadDataAsync(CancellationToken cancellationToken)
    {
        throw new HttpRequestException("Connection failed");
    }
}
```

#### Example Using Source Generator

To further simplify your ViewModel development, consider using the source generator provided by the `NuExt.Minimal.Mvvm.SourceGenerator` package. Here's an example:

```csharp
using Minimal.Mvvm;

public partial class ProductViewModel : ViewModelBase
{
    [Notify]
    private string _name = string.Empty;
    
    [Notify(Setter = AccessModifier.Private)]
    private decimal _price;
    
    public ProductViewModel()
    {
        SaveCommand = new AsyncCommand(SaveAsync);
    }
    
    [Notify]
    private async Task SaveAsync(CancellationToken token)
    {
        await Task.Delay(500, token);
        Price = 99.99m;
    }
}
```

This automation helps to maintain clean and efficient code, improving overall productivity. For details on installing and using the source generator, refer to the [NuExt.Minimal.Mvvm.SourceGenerator](https://github.com/IvanGit/NuExt.Minimal.Mvvm.SourceGenerator) documentation.

#### Example Using ServiceProvider

```csharp
public class MyService
{
    public string GetData() => "Hello from MyService!";
}

public class MyViewModel : ViewModelBase
{
    public IRelayCommand MyCommand { get; }

    public MyViewModel()
    {
        // Register services
        ServiceProvider.Default.RegisterService<MyService>();
        
        MyCommand = new RelayCommand(() =>
        {
            // Resolve and use services
            var myService = ServiceProvider.Default.GetService<MyService>();

            var data = myService.GetData();
            // Use the data
        });
    }
}
```

### Contributing

Issues and PRs are welcome. Keep changes minimal and performance-conscious.

### License

MIT. See LICENSE.