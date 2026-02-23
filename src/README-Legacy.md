# NuExt.Minimal.Mvvm.Legacy

**Legacy‑only add‑on** for **`NuExt.Minimal.Mvvm`** targeting **.NET Framework 4.6.2+** and **.NET Standard 2.0**.  
It provides **backports** that are available on modern .NET in the base package.

> This package is **not deprecated code**. It exists to **support older TFMs**.  
> Modern .NET (.NET 5+/netstandard2.1) does **not** use this package (no compatible assets).

[![NuGet](https://img.shields.io/nuget/v/NuExt.Minimal.Mvvm.Legacy.svg)](https://www.nuget.org/packages/NuExt.Minimal.Mvvm.Legacy)
[![License](https://img.shields.io/github/license/nu-ext/NuExt.Minimal.Mvvm?label=license)](https://github.com/nu-ext/NuExt.Minimal.Mvvm/blob/main/LICENSE)

## Included backports

- **`AsyncValueCommand` / `AsyncValueCommand<T>`** — `ValueTask`-based async commands (backport of the modern feature available in `NuExt.Minimal.Mvvm` on .NET 5+/netstandard2.1).

## Compatibility & Dependencies

- **Targets:** `net462`, `netstandard2.0`.
- **Dependency:** `System.Threading.Tasks.Extensions` (provides `ValueTask` on legacy TFMs).  
  This dependency is **not added** on modern TFMs because the package exposes **no assets** there.

## Installation

Add both the base package and the legacy add‑on to your legacy project:

```sh
dotnet add package NuExt.Minimal.Mvvm
dotnet add package NuExt.Minimal.Mvvm.Legacy
```

That’s it — `AsyncValueCommand*` become available on legacy targets.

> Multi‑TFM projects: reference both packages.
> - On modern TFMs, `NuExt.Minimal.Mvvm` provides `AsyncValueCommand*`; the legacy package is ignored (no assets).
> - On legacy TFMs, this package provides `AsyncValueCommand*`; the base package does not include them there.

## Example

```csharp
public sealed class ValidateViewModel : ViewModelBase
{
    public IAsyncCommand ValidateCommand { get; }

    public ValidateViewModel()
    {
        ValidateCommand = new AsyncValueCommand(ValidateAsync);
    }

    private ValueTask ValidateAsync(CancellationToken ct)
    {
        if (IsValidFast()) return ValueTask.CompletedTask;
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

## Notes

If you consume the `.Sources` of the base package on legacy and prefer not to use this add‑on, you can opt‑in manually by adding:

```xml
<PropertyGroup>
  <DefineConstants>$(DefineConstants);NUEXT_ENABLE_VALUETASK</DefineConstants>
</PropertyGroup>
<ItemGroup>
  <PackageReference Include="System.Threading.Tasks.Extensions" Version="4.6.3" />
</ItemGroup>
```

- Do not combine `.Sources` opt‑in and this legacy package on the same legacy TFM to avoid duplicate type definitions.

## License

MIT. See LICENSE.