# Architecture

## Goals

`NuExt.Minimal.Mvvm` provides a lightweight MVVM foundation focused on asynchronous initialization, deterministic lifetime management, and explicit ownership of application objects.

## Layers

The ecosystem is divided into the following layers:

- `NuExt.Minimal.Mvvm` provides UI-independent ViewModel infrastructure.
- `NuExt.Minimal.Mvvm.Wpf` integrates ViewModels with WPF views and UI services.
- UI-specific packages integrate external frameworks without exposing their types to application ViewModels.

## ViewModel lifecycle

ViewModels have a serialized asynchronous lifecycle:

1. Create the ViewModel.
2. Assign its parent and initialization parameter.
3. Initialize the ViewModel asynchronously.
4. Use the initialized ViewModel.
5. Uninitialize the ViewModel asynchronously.
6. Release resources owned by the ViewModel.

Platform-specific packages extend this lifecycle by associating ViewModels with views and UI hosts.

## UI services

Application ViewModels interact with UI containers through service interfaces instead of directly referencing controls.

Concrete UI services are provided by platform-specific packages. The core package defines the hierarchical service resolution model without depending on UI framework types.

## Ownership

Objects registered in a ViewModel's local service scope are owned by that scope according to their registration lifetime. Created local singletons are released during ViewModel uninitialization.

Platform-specific hosting services may define additional ownership-transfer rules for dynamically created views and ViewModels.

## Framework integrations

Framework-specific packages build on the core lifecycle and service model without introducing their UI types into `NuExt.Minimal.Mvvm`.

Application ViewModels can therefore use platform services without the core package depending on WPF, AvalonDock, MahApps.Metro, or concrete UI container types.