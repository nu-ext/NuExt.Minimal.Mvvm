# NuExt.Minimal.Mvvm.Legacy

**Legacy‑only add‑on** for **`NuExt.Minimal.Mvvm`** targeting **.NET Framework 4.6.2+** and **.NET Standard 2.0**.  
It provides **backports** that are available on modern .NET in the base package.

> This package is **not deprecated code**. It exists to support older TFMs and should only be referenced by projects targeting .NET Framework 4.6.2 or .NET Standard 2.0.

[![NuGet](https://img.shields.io/nuget/v/NuExt.Minimal.Mvvm.Legacy.svg)](https://www.nuget.org/packages/NuExt.Minimal.Mvvm.Legacy)
[![License](https://img.shields.io/github/license/nu-ext/NuExt.Minimal.Mvvm?label=license)](https://github.com/nu-ext/NuExt.Minimal.Mvvm/blob/main/LICENSE)

## Included backports

- **`AsyncValueCommand` / `AsyncValueCommand<T>`** — `ValueTask`-based async commands (backport of the modern feature available in `NuExt.Minimal.Mvvm` on .NET 5+/netstandard2.1).

## Compatibility & Dependencies

- **Targets:** `net462`, `netstandard2.0`.
- **Dependency:** `System.Threading.Tasks.Extensions`, which provides `ValueTask` support for the targeted legacy frameworks. 

## Installation

Add both the base package and the legacy add‑on to your legacy project:

```sh
dotnet add package NuExt.Minimal.Mvvm
dotnet add package NuExt.Minimal.Mvvm.Legacy
```

That’s it — `AsyncValueCommand*` become available on legacy targets.

Multi-targeted projects should reference this package only for legacy target frameworks:

```xml
<ItemGroup Condition="'$(TargetFrameworkIdentifier)' == '.NETFramework' or '$(TargetFramework)' == 'netstandard2.0'">
  <PackageReference Include="NuExt.Minimal.Mvvm.Legacy" Version="..." />
</ItemGroup>
```

On legacy targets, this package provides `AsyncValueCommand` and `AsyncValueCommand<T>`; the base package does not include them there.

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