# NuExt.Minimal.Mvvm

`NuExt.Minimal.Mvvm` is a **high-performance, dependency-free** MVVM core for .NET focused on **robust async flows** and **deterministic command execution**. It provides a **minimal, clear API** with Bindable/ViewModel base types, a self-validating command model (Relay/Async/Composite), and a lightweight, UI-focused service provider with trim- and AOT-friendly factory registration.

[![NuGet](https://img.shields.io/nuget/v/NuExt.Minimal.Mvvm.svg)](https://www.nuget.org/packages/NuExt.Minimal.Mvvm)
[![Ask DeepWiki](https://img.shields.io/badge/Ask-DeepWiki-blue)](https://deepwiki.com/nu-ext/NuExt.Minimal.Mvvm)
[![Build](https://github.com/nu-ext/NuExt.Minimal.Mvvm/actions/workflows/ci.yml/badge.svg)](https://github.com/nu-ext/NuExt.Minimal.Mvvm/actions/workflows/ci.yml)
[![License](https://img.shields.io/github/license/nu-ext/NuExt.Minimal.Mvvm?label=license)](https://github.com/nu-ext/NuExt.Minimal.Mvvm/blob/main/LICENSE)
[![Downloads](https://img.shields.io/nuget/dt/NuExt.Minimal.Mvvm.svg)](https://www.nuget.org/packages/NuExt.Minimal.Mvvm)

## Features

- **Core**
  - `Minimal.Mvvm.BindableBase` — lightweight, UI-aware `INotifyPropertyChanged` base with cached event arguments and allocation-free generated notification paths.
  - `Minimal.Mvvm.ViewModelBase` — a ViewModel foundation with parent-aware service resolution, a lazily created local service scope, parameter support, and serialized asynchronous lifecycle.
  - Property notifications are marshaled through an explicitly configured or captured `SynchronizationContext`; otherwise, they are raised on the caller's thread.
  - `Minimal.Mvvm.WeakEvent` — lightweight weak-event storage for `(object sender, TEventArgs)` handlers.

- **Command model (self-validating)**
  - `RelayCommand`, `AsyncCommand`, `AsyncValueCommand`, their generic variants, and `CompositeCommand` validate their execution state internally.
  - If `CanExecute(parameter)` returns `false`, `Execute(parameter)` does nothing, ensuring consistent behavior for UI-bound and programmatic calls.

  **Semantics**
  - Async commands provide cancellation and reentrancy control (`AllowConcurrentExecution`, default: **false**).
  - `AsyncValueCommand` / `AsyncValueCommand<T>` provide the same execution semantics using `ValueTask`.
  - Exceptions bubble to `UnhandledException` (per-command) first; if not handled, to `AsyncCommand.GlobalUnhandledException`.
  - `Cancel()` signals all active executions through their individual `CancellationToken` instances; if nothing is executing, it’s a no-op.

- **Command implementations**
  - `RelayCommand` / `RelayCommand<T>` — classic **synchronous** delegate-based commands (can be invoked concurrently from multiple threads).
  - `AsyncCommand` / `AsyncCommand<T>` — **asynchronous** commands with predictable error propagation and cancellation.
  - `AsyncValueCommand` / `AsyncValueCommand<T>` — **asynchronous** commands built on `ValueTask` for allocation-sensitive scenarios.
  - `CompositeCommand` — aggregates multiple commands and executes them **sequentially**; awaits `ExecuteAsync(...)` and calls `Execute(...)` for non-async commands.

- **UI-focused Service Provider**
  - `Minimal.Mvvm.ServiceProvider` is a lightweight, mutable service container designed for hierarchical UI scopes.
  - Hierarchical lookup order for ViewModels: **local → fallback → parent → default**.
  - Supports instance, lazy singleton, transient, named, and runtime-replaceable registrations.
  - Factory and instance registrations are trim- and AOT-friendly. Type activation is available as a convenience path and requires a preserved public parameterless constructor.
  - Lifetime basics for UI apps:
    - **Singleton (lazy)** — stored per container, created on the first successful resolution.
    - **Transient** — new instance per resolution, not cached.
    - **Scope** — a **ViewModel** acts as a scope; `ViewModelBase.Services` is created lazily, and created local singletons are released during uninitialization.
    - **Application scope**
      - `ServiceProvider.Default` acts as the **application-level** container. Prefer registering app-wide services there; ViewModels may register local overrides in their own scope.

## Why NuExt.Minimal.Mvvm?

`NuExt.Minimal.Mvvm` is designed for UI applications that need predictable asynchronous behavior without adopting a full application framework. Commands validate their own execution state, track concurrent invocations independently, expose explicit cancellation and error handling, and support deterministic asynchronous composition.

ViewModels combine UI-aware property notifications with serialized asynchronous lifecycle and hierarchical service resolution. Each ViewModel can act as a lazy local scope, inherit context-specific services from its parent, and fall back to application-level services without hiding mandatory dependencies that belong in constructors.

The framework keeps runtime behavior explicit and performance-oriented: notification arguments are cached, frequent command and resolution paths avoid unnecessary allocations, service registrations remain mutable, and factory-based composition is trim- and AOT-friendly.

## Integrations & Companion Packages

- **Building WPF?** Use [**NuExt.Minimal.Mvvm.Wpf**](https://www.nuget.org/packages/NuExt.Minimal.Mvvm.Wpf) for document services, async dialogs, explicit view composition, and dispatcher-safe APIs.
- **MahApps.Metro integration:** [NuExt.Minimal.Mvvm.MahApps.Metro](https://www.nuget.org/packages/NuExt.Minimal.Mvvm.MahApps.Metro)

### Recommended Companion

Use the [`NuExt.Minimal.Mvvm.SourceGenerator`](https://www.nuget.org/packages/NuExt.Minimal.Mvvm.SourceGenerator) for compile-time boilerplate generation in ViewModels.

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

### 2) Two-tier exception handling

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

### 4) Service registration for ViewModels

Register application-wide services once at the composition root:

```csharp
ServiceProvider.Default.RegisterService<IMyService, MyService>();
```

```csharp
public sealed class MyViewModel : ViewModelBase
{
    public MyViewModel(IServiceContainer fallback) : base(fallback)
    {
        // ViewModel scope (local container); override per-VM if needed.
        Services.RegisterService<IMyService, MyVmSpecificService>();
        Services.RegisterTransient<INotification>(() => new Toast());
    }

    public void Use()
    {
        // Resolution order: local → fallback → parent → default
        var service = GetService<IMyService>();
        var notification = GetService<INotification>();
    }
}
```
> **Notes (UI lifetimes)**:  
> - **Scope** = the ViewModel instance. Local services are stored in its lazy container and released during cleanup.
> - **Application scope** = `ServiceProvider.Default`.
> - **Singleton (lazy)** = created on first successful resolution and cached in the owning container.
> - **Transient** = new instance per resolution (not cached).
> - **Cleanup**: default uninitialization cleans up created local singletons; override `UninitializeAsyncCore(...)` to customize cleanup.

### 5) `AsyncValueCommand` for allocation-sensitive hot paths

```csharp
public sealed class ValidateViewModel : ViewModelBase
{
    public IAsyncCommand ValidateCommand { get; }

    public ValidateViewModel()
    {
        // Often completes synchronously; ValueTask can avoid a Task allocation in that case.
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

## Migrating from other DI containers (UI context)

- **Application services:** register into `ServiceProvider.Default` at startup.
- **Per-VM overrides:** register in `ViewModelBase.Services` (scope = the ViewModel).
- **External DI as parent:** wrap your existing container as a parent for this provider:
  `var app = new ServiceProvider((System.IServiceProvider)existing);`
  Then either **set it as application scope** (`ServiceProvider.Default = app`) or **pass as a ViewModel fallback** (`: base(app)`).
- **Named registrations:** use `name` overloads to keep side-by-side implementations.
- **Concept mapping:**  
  - App “Singleton” → `ServiceProvider.Default.RegisterService<T>(...)`  
  - Per-View/ViewModel “Scoped” → `Services.RegisterService<T>(...)`  
  - “Transient” → `RegisterTransient<T>(...)`

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

- `[UseCommandManager]` is **WPF-only**; Avalonia/WinUI don’t have `CommandManager`.
- If you still need a manual requery (rare), call `CommandManager.InvalidateRequerySuggested()`.

## ValueTask commands on legacy targets

You have two options to enable `AsyncValueCommand*` on legacy TFMs:

- **Recommended for legacy-only projects**: install the binary add-on:  
  `dotnet add package NuExt.Minimal.Mvvm.Legacy`

  This adds `AsyncValueCommand` and `AsyncValueCommand<T>` for .NET Framework 4.6.2 and .NET Standard 2.0 and references `System.Threading.Tasks.Extensions`.
- **Sources-only workflow**: opt in through `NuExt.Minimal.Mvvm.Sources` by defining `NUEXT_ENABLE_VALUETASK` and referencing `System.Threading.Tasks.Extensions`.

## FAQ

**Q: How is this different from CommunityToolkit.Mvvm?**  
**A:** `NuExt.Minimal.Mvvm` focuses on deterministic command execution, independently tracked concurrent operations, serialized ViewModel lifecycle, and hierarchical UI service scopes. `CommunityToolkit.Mvvm` provides a broader set of general-purpose MVVM components, including messaging and validation. Choose NuExt.Minimal.Mvvm when these runtime semantics and UI-oriented composition model are important.

**Q: Do I have to wire WPF CommandManager.RequerySuggested myself?**  
**A:** No. With the source generator, mark the command with `[UseCommandManager]`, and the generated property will auto-subscribe/unsubscribe for requery.

**Q: What is the command error flow?**  
**A:** Exceptions raised during `AsyncCommand` execution are first published to `UnhandledException`; if not handled there, they flow to `AsyncCommand.GlobalUnhandledException`.

**Q: Why does `ViewModelBase` provide hierarchical service access?**  
**A:** Stable, mandatory dependencies should still be passed through constructors. Hierarchical services are intended for UI-context dependencies that may depend on the current ViewModel tree, such as navigation, dialogs, document context, selection, or local runtime overrides. Each ViewModel can expose a lazy local scope while resolving missing services through **fallback → parent → default**.

**Q: How does the built-in service provider differ from Microsoft.Extensions.DependencyInjection?**  
**A:** It is a mutable, hierarchical container designed for UI scopes, local overrides, and explicit factory registrations. It does not build arbitrary constructor dependency graphs or replace a general-purpose application container. Use it independently for lightweight UI composition, or place an existing `IServiceProvider` behind it as a parent.

**Q: How do I integrate my existing DI container?**
**A:** Wrap it as a parent via `new ServiceProvider(System.IServiceProvider parent)`.  
Use it in one of two ways:
- **Application scope:** set `ServiceProvider.Default = new ServiceProvider(parent)`, so all ViewModels can resolve app-wide services via the default provider.
- **Per-VM fallback:** pass it to specific ViewModels (`: base(fallback)`) when you need a VM to resolve “up” into the external container while still allowing local overrides in that VM scope.

**Q: How are UI scopes organized?**  
**A:** `ServiceProvider.Default` is the application scope. Each ViewModel can lazily create a local scope through `ViewModelBase.Services`, with resolution following **local → fallback → parent → default**. Transient registrations create a new instance for each resolution.

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
See [**ValueTask commands on legacy targets**](#valuetask-commands-on-legacy-targets) for legacy opt-in details.

To install the source code package, use the following command:

```sh
dotnet add package NuExt.Minimal.Mvvm.Sources
```

Or via Visual Studio:

1. Go to `Tools -> NuGet Package Manager -> Manage NuGet Packages for Solution...`.
2. Search for `NuExt.Minimal.Mvvm.Sources`.
3. Click "Install".

## Compatibility

- **.NET Standard 2.0/2.1, .NET 8/9/10, .NET Framework 4.6.2+**
- **Language**: C# 12+

> **Legacy only:** to use `AsyncValueCommand*` on **.NET Framework 4.6.2** / **.NET Standard 2.0**, see
> [**ValueTask commands on legacy targets**](#valuetask-commands-on-legacy-targets) (binary add-on or `.Sources` opt-in with `NUEXT_ENABLE_VALUETASK` and `System.Threading.Tasks.Extensions`).

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