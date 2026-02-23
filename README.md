# NuExt.Minimal.Mvvm

`NuExt.Minimal.Mvvm` is a **high‑performance, dependency‑free** MVVM core for .NET focused on **robust async flows** and **deterministic command execution**. It provides a **minimal, clear API** with Bindable/ViewModel base types, a self‑validating command model (Relay/Async/Composite), and a lightweight service provider.

[![NuGet](https://img.shields.io/nuget/v/NuExt.Minimal.Mvvm.svg)](https://www.nuget.org/packages/NuExt.Minimal.Mvvm)
[![Build](https://github.com/nu-ext/NuExt.Minimal.Mvvm/actions/workflows/ci.yml/badge.svg)](https://github.com/nu-ext/NuExt.Minimal.Mvvm/actions/workflows/ci.yml)
[![License](https://img.shields.io/github/license/nu-ext/NuExt.Minimal.Mvvm?label=license)](https://github.com/nu-ext/NuExt.Minimal.Mvvm/blob/main/LICENSE)
[![Downloads](https://img.shields.io/nuget/dt/NuExt.Minimal.Mvvm.svg)](https://www.nuget.org/packages/NuExt.Minimal.Mvvm)

## Features

- **Core**
  - `Minimal.Mvvm.BindableBase` — lightweight `INotifyPropertyChanged` base.
  - `Minimal.Mvvm.ViewModelBase` — lean ViewModel foundation with simple service access.

- **Command model (self‑validating)**
  - All commands (`RelayCommand`, `RelayCommand<T>`, `AsyncCommand`, `AsyncCommand<T>`, `AsyncValueCommand`, `AsyncValueCommand<T>`, `CompositeCommand`) validate their state internally:
    if `CanExecute(parameter)` is `false`, `Execute(parameter)` does nothing. This guarantees consistent behavior for both UI‑bound and programmatic calls.

  **Semantics**
  - `AsyncCommand` provides cancellation and reentrancy control (`AllowConcurrentExecution`, default: **false**).
  - `AsyncValueCommand` / `AsyncValueCommand<T>` are `ValueTask`-based async commands.
  - Exceptions bubble to `UnhandledException` (per‑command) first; if not handled, to `AsyncCommand.GlobalUnhandledException`.
  - `Cancel()` signals the current operation via `CancellationToken`; if nothing is executing, it’s a no‑op.

- **Command implementations**
  - `RelayCommand` / `RelayCommand<T>` — classic **synchronous** delegate‑based commands (can be invoked concurrently from multiple threads).
  - `AsyncCommand` / `AsyncCommand<T>` — **asynchronous** commands with predictable error propagation and cancellation.
  - `AsyncValueCommand` / `AsyncValueCommand<T>` — **asynchronous** commands built on `ValueTask` for allocation‑sensitive scenarios.
  - `CompositeCommand` — aggregates multiple commands and executes them **sequentially**; awaits `ExecuteAsync(...)` and calls `Execute(...)` for non‑async commands.

- **Service Provider Integration**
  - `Minimal.Mvvm.ServiceProvider`: lightweight service provider for registration and resolution of services.

## Integrations & Companion Packages

- **Building WPF?** Use [**NuExt.Minimal.Mvvm.Wpf**](https://www.nuget.org/packages/NuExt.Minimal.Mvvm.Wpf) for document services, async dialogs, explicit view composition, and dispatcher‑safe APIs.
- **MahApps.Metro integration:** [NuExt.Minimal.Mvvm.MahApps.Metro](https://www.nuget.org/packages/NuExt.Minimal.Mvvm.MahApps.Metro)

### Recommended Companion

Use the [`NuExt.Minimal.Mvvm.SourceGenerator`](https://www.nuget.org/packages/NuExt.Minimal.Mvvm.SourceGenerator) for compile‑time boilerplate generation in ViewModels.

## Quick examples

### 1) Advanced `AsyncCommand` with concurrency and cancellation

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

        CancelCommand = new RelayCommand(() => SearchCommand.Cancel());
    }

    private async Task SearchAsync(string query, CancellationToken cancellationToken)
    {
        await Task.Delay(1000, cancellationToken);
        Results = $"Results for: {query}";
    }

    private bool CanSearch(string query) => !string.IsNullOrWhiteSpace(query);

    private string _results = string.Empty;
    public string Results
    {
        get => _results;
        private set => SetProperty(ref _results, value);
    }
}
```

### 2) Two‑tier exception handling

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
                e.Handled = true; // local tier handled
            }
        };
    }

    private async Task LoadDataAsync(CancellationToken cancellationToken)
    {
        throw new HttpRequestException("Connection failed");
    }

    private void ShowError(string message) { /* UI */ }
}
```
Global fallback: subscribe to `AsyncCommand.GlobalUnhandledException` **once** at app startup (composition root) for logging/telemetry.

```csharp
AsyncCommand.GlobalUnhandledException += (sender, e) =>
{
    Logger.LogError(e.Exception, "Global command error");
    e.Handled = true;
};
```

### 3) Using Source Generator

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

### 4) Using ServiceProvider

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

### 5) `AsyncValueCommand` for allocation‑sensitive hot paths

```csharp
public sealed class ValidateViewModel : ViewModelBase
{
    public IAsyncCommand ValidateCommand { get; }

    public ValidateViewModel()
    {
        // Often completes synchronously; ValueTask avoids allocations in that case.
        ValidateCommand = new AsyncValueCommand(ValidateAsync);
    }

    private ValueTask ValidateAsync(CancellationToken ct)
    {
        // synchronous fast path
        if (IsValidFast()) return ValueTask.CompletedTask;

        // fallback async path
        return SlowValidateAsync(ct);
    }

    private bool IsValidFast() => /* lightweight checks */;
    private async ValueTask SlowValidateAsync(CancellationToken ct)
    {
        await Task.Delay(50, ct);
        // heavy checks...
    }
}
```

## WPF + `[UseCommandManager]` example

In WPF, `[UseCommandManager]` wires a generated command property to `CommandManager.RequerySuggested`, so `CanExecute` is reevaluated automatically on typical UI events (focus changes, keyboard, window activation). You don’t need to call `RaiseCanExecuteChanged()` manually.

### ViewModel

```csharp
using Minimal.Mvvm;
using System.Threading;
using System.Threading.Tasks;

public partial class LoginViewModel : ViewModelBase
{
    [Notify] private string _userName = string.Empty;
    [Notify] private string _password = string.Empty;

    // Field-based pattern: generator creates the LoginCommand property.
    // [UseCommandManager] auto-subscribes the property setter to WPF CommandManager.RequerySuggested.
    [Notify, UseCommandManager]
    private IAsyncCommand? _loginCommand;

    public LoginViewModel()
    {
        LoginCommand = new AsyncCommand(LoginAsync, CanLogin);
    }

    private bool CanLogin() =>
        !string.IsNullOrWhiteSpace(UserName) &&
        !string.IsNullOrWhiteSpace(Password);

    private async Task LoginAsync(CancellationToken ct)
    {
        await Task.Delay(250, ct);
        // sign-in...
    }
}
```

> Alternatively, you can place `[Notify, UseCommandManager]` on a method that should become a command;
the generator will create the command property and wire WPF requery in the property setter as well.

### XAML

```xml
<Window x:Class="MyApp.Views.LoginView"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="clr-namespace:MyApp.ViewModels"
        Title="Login" Width="360" Height="220">
    <Window.DataContext>
        <vm:LoginViewModel/>
    </Window.DataContext>

    <StackPanel Margin="16" VerticalAlignment="Center">
        <TextBox Margin="0,0,0,8"
                 Text="{Binding UserName, UpdateSourceTrigger=PropertyChanged}"/>
        <TextBox Margin="0,0,0,12"
                 Text="{Binding Password, UpdateSourceTrigger=PropertyChanged}"/>

        <!-- CanExecute will re-evaluate automatically on UI changes thanks to [UseCommandManager] -->
        <Button Content="Sign in"
                Command="{Binding LoginCommand}"
                HorizontalAlignment="Right"
                MinWidth="96" Padding="12,6"/>
    </StackPanel>
</Window>
```
#### Notes

- `[UseCommandManager]` is **WPF‑only**; Avalonia/WinUI don’t have `CommandManager`.
- If you still need a manual requery (rare), call `CommandManager.InvalidateRequerySuggested()`.

## ValueTask commands on legacy targets

You have two options to enable `AsyncValueCommand*` on legacy TFMs:

- **Recommended**: install the binary add‑on  
  ```sh
  dotnet add package NuExt.Minimal.Mvvm.Legacy
  ```
  This adds `AsyncValueCommand*` for `net462`/`netstandard2.0` and pulls `System.Threading.Tasks.Extensions` **only** on legacy.

- **Sources‑only workflow**: if you consume `.Sources`, you can enable the commands explicitly on legacy:

```xml
<PropertyGroup>
  <DefineConstants>$(DefineConstants);NUEXT_ENABLE_VALUETASK</DefineConstants>
</PropertyGroup>
<ItemGroup>
  <PackageReference Include="System.Threading.Tasks.Extensions" Version="4.6.3" />
</ItemGroup>
```
The same source files compile on modern automatically and on legacy **only** when `NUEXT_ENABLE_VALUETASK` is defined.

## FAQ
**Q: How is this different from CommunityToolkit.Mvvm?**  
**A**: `NuExt.Minimal.Mvvm` focuses on a *smaller, deterministic core* with strict command semantics (no‑op `Execute` when `CanExecute` is false), explicit async error pipeline (local → global), and no external dependencies. CommunityToolkit.Mvvm provides a broader toolbox (messaging, DI helpers, attributes, etc.). If you need a minimal, performance‑oriented core with predictable async/command behavior, `NuExt.Minimal.Mvvm` is a good fit; if you want a feature‑rich toolkit, CommunityToolkit.Mvvm may be preferable.

**Q: Do I have to wire WPF CommandManager.RequerySuggested myself?**  
**A**: No. With the source generator, mark the command with `[UseCommandManager]`, and the generated property will auto‑subscribe/unsubscribe for requery.

**Q: What is the command error flow?**  
**A**: Exceptions raised during `AsyncCommand` execution are first published to `UnhandledException`; if not handled there, they flow to `AsyncCommand.GlobalUnhandledException`.

## Installation

Via [NuGet](https://www.nuget.org/):

```sh
dotnet add package NuExt.Minimal.Mvvm
```

Or via Visual Studio:

1. Go to `Tools -> NuGet Package Manager -> Manage NuGet Packages for Solution...`.
2. Search for `NuExt.Minimal.Mvvm`.
3. Click "Install".

### Source Code Package

A source package is also available: [`NuExt.Minimal.Mvvm.Sources`](https://www.nuget.org/packages/NuExt.Minimal.Mvvm.Sources). This package allows you to embed the entire framework directly into your application, enabling easier source code exploring and debugging.
See [**ValueTask commands on legacy targets**](#valuetask-commands-on-legacy-targets) for legacy opt‑in details.

To install the source code package, use the following command:

```sh
dotnet add package NuExt.Minimal.Mvvm.Sources
```

Or via Visual Studio:

1. Go to `Tools -> NuGet Package Manager -> Manage NuGet Packages for Solution...`.
2. Search for `NuExt.Minimal.Mvvm.Sources`.
3. Click "Install".

### Compatibility

- **.NET Standard 2.0+, .NET 8/9/10, .NET Framework 4.6.2+**

> **Legacy only:** to use `AsyncValueCommand*` on **.NET Framework 4.6.2** / **.NET Standard 2.0**, see
> [**ValueTask commands on legacy targets**](#valuetask-commands-on-legacy-targets) (binary add‑on or `.Sources` opt‑in with `NUEXT_ENABLE_VALUETASK` and `System.Threading.Tasks.Extensions`).

## Ecosystem

- [NuExt.Minimal.Behaviors.Wpf](https://github.com/nu-ext/NuExt.Minimal.Behaviors.Wpf)
- [NuExt.Minimal.Mvvm.SourceGenerator](https://github.com/nu-ext/NuExt.Minimal.Mvvm.SourceGenerator)
- [NuExt.Minimal.Mvvm.Wpf](https://github.com/nu-ext/NuExt.Minimal.Mvvm.Wpf)
- [NuExt.Minimal.Mvvm.MahApps.Metro](https://github.com/nu-ext/NuExt.Minimal.Mvvm.MahApps.Metro)
- [NuExt.System](https://github.com/nu-ext/NuExt.System)
- [NuExt.System.Data](https://github.com/nu-ext/NuExt.System.Data)
- [NuExt.System.Data.SQLite](https://github.com/nu-ext/NuExt.System.Data.SQLite)

## Contributing

Issues and PRs are welcome. Keep changes minimal and performance-conscious.

## License

MIT. See LICENSE.