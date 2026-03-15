# ArxisStudio.Markup

`ArxisStudio.Markup` is a model-driven UI pipeline for Avalonia.

Instead of authoring visual structure directly in XAML, the UI is stored as structured `.arxui` documents and turned into ordinary C# at build time through a Roslyn source generator. The goal is not to replace Avalonia XAML feature-for-feature. The goal is to provide a format that is predictable for tools, easy to transform, and generic enough to work with unknown controls, custom controls, and project-specific types without hardcoding them into the library.

## What It Is

The core idea is simple:

- XAML is a good authoring format for humans.
- A visual designer needs a structured model, not a text language.
- `.arxui` is that structured persistence format.
- The source generator compiles the model into `InitializeComponent()`.
- The editor can inspect and manipulate the same model without rewriting markup text.

This makes the project suitable for:

- visual form and screen designers
- property inspectors
- drag-and-drop layout tooling
- model transforms and refactoring
- round-trip serialization
- build-time generation of Avalonia UI code

## Main Property

The most important architectural property of the library is that it does not need built-in knowledge about a fixed catalog of controls.

It works from type metadata:

- `.arxui` stores CLR type names and structured property values
- the generator resolves types and members through Roslyn
- the editor resolves and previews the model at runtime

This means the library can work with:

- standard Avalonia controls
- custom controls from the user project
- controls from referenced libraries
- project-specific resources and style files

without adding hardcoded support for every new type.

## Repository Layout

- `ArxisStudio.Contracts`
  Contracts and JSON serialization for the `.arxui` model.

- `ArxisStudio.Generator/ArxisStudio.Generator`
  Incremental Roslyn source generator that reads `.arxui` from `AdditionalFiles`, validates documents, resolves types and properties, and generates `InitializeComponent()` implementations.

- `ArxisStudio.Editor`
  Experimental visual editor and previewer. It edits `.arxui`, loads project context, and renders a design-time preview without executing user event code.

- `ArxisStudio.Sample`
  Sample Avalonia application using the generator.

- `ArxisStudio.Template`
  Template Avalonia project intended to be opened inside `ArxisStudio.Editor`.

- `ArxisStudio.Tests`
  Generator and serializer tests.

## Supported Document Kinds

The model currently supports:

- `Application`
- `Control`
- `Window`
- `Styles`
- `ResourceDictionary`

`Kind` defines the semantic category of the document. The concrete CLR root type is still declared through `Root.TypeName`.

## `.arxui` Model Features

Current model support includes:

- object creation by CLR type name
- nested object graphs
- scalar properties
- collection properties such as `Children`
- attached properties such as `Grid.Row` or `Canvas.Left`
- resources via `$resource`
- bindings via `$binding`
- asset loading via `$asset`
- host-level `Styles` and `Resources`
- `StyleInclude`
- merged resource dictionaries
- local keyed resources on root and nested nodes

This now covers:

- `Application`-level styling
- `Window` and `UserControl` trees
- local resources inside the visual tree
- hybrid scenarios where styles stay in `.axaml` and layout stays in `.arxui`

## What The Project Is Not

This is not a complete XAML implementation.

It intentionally does not try to mirror the whole Avalonia XAML language:

- no full markup extension system
- no full template/style/animation parity
- no XAML namespace/prefix authoring model
- no attempt to be a drop-in replacement for the Avalonia XAML compiler

That tradeoff is intentional. The model is designed for tooling first.

## How Build-Time Generation Works

The usual workflow is:

1. Declare a normal Avalonia `partial` class.
2. Create a matching `.arxui` file.
3. Set `Class` to the full CLR name of the target type.
4. Add `.arxui` files to the consuming project as `AdditionalFiles`.
5. The generator validates the document and emits `InitializeComponent()`.

The generator checks:

- whether the document parses
- whether the root type exists
- whether `Kind` matches the root type
- whether `Class` exists when required
- whether the target type is `partial`
- whether the target type matches the document root
- whether duplicate assets target the same CLR type

## Example

`partial` class:

```csharp
public partial class SolidColorBrush : ContentControl
{
    public SolidColorBrush()
    {
        InitializeComponent();
    }
}
```

Matching `.arxui`:

```json
{
  "SchemaVersion": 1,
  "Kind": "Control",
  "Class": "MyApp.Views.SolidColorBrush",
  "Root": {
    "TypeName": "Avalonia.Controls.ContentControl",
    "Properties": {
      "Width": 400,
      "Height": 250,
      "Content": {
        "TypeName": "Avalonia.Controls.Border",
        "Properties": {
          "Name": "NamedBorder",
          "BorderThickness": "3",
          "BorderBrush": "Gray"
        }
      }
    }
  }
}
```

At build time the generator turns this into C# that creates the visual tree and assigns properties in `InitializeComponent()`.

## Application-Level Documents

The library now supports `Application` documents as well.

That means `App.axaml` can be replaced by `.arxui` in supported scenarios. The template project demonstrates this with:

- [App.arxui](/home/deck/Desktop/SourceGenerationExample/ArxisStudio.Template/Metadata/App.arxui)
- [App.arxui.cs](/home/deck/Desktop/SourceGenerationExample/ArxisStudio.Template/App.arxui.cs)

This makes it possible to model:

- application-level theme setup
- application-level styles
- application-level merged dictionaries

using the same persistence format as the rest of the designer pipeline.

## Hybrid Styling Strategy

The intended styling strategy is hybrid:

- `.arxui` owns the structural UI model
- `.axaml` remains a good place for human-authored styles and resource dictionaries
- `.arxui` references those files through `StyleInclude` and merged dictionaries

This avoids turning styles into an uncomfortable JSON authoring experience while still making them visible to tooling.

## Editor Model

`ArxisStudio.Editor` is a design-time tool, not an application runner.

The intended design rule is:

- use the project as a source of types, resources, and style files
- do not execute user event handlers or application logic

The editor currently:

- loads `.csproj` or `.sln`
- indexes `.arxui` and `.axaml` files
- opens `.arxui` files directly from project context
- writes changes back to the opened `.arxui`
- applies local resources and styles during preview
- loads external `.axaml` style/resource files for preview

The editor currently does not aim for full runtime parity yet:

- preview is still partial
- project assembly loading is not yet fully isolated
- custom runtime logic must not be executed

## Live Preview Roadmap

The target workflow is:

1. Open `.sln` or `.csproj`
2. Discover project types, resources, `.arxui`, and `.axaml`
3. Double-click an `.arxui` document
4. See a design-time preview close to the real Avalonia result

Remaining steps to reach that state:

1. Complete project-context handling in the editor
2. Add assembly-based type resolution for custom project types
3. Improve preview parity for resources, styles, templates, and bindings
4. Keep preview safe by avoiding user runtime logic
5. Add diagnostics and incremental refresh

## Template Project

`ArxisStudio.Template` exists specifically to test editor workflows.

It includes:

- `.arxui` layout documents
- external `.axaml` styles
- external `.axaml` resource dictionaries
- an `Application` document in `.arxui`

That project is the current reference case for:

- project loading in the editor
- style/resource-aware preview
- source generation over multiple document kinds

## Build And Run

Requirements:

- .NET SDK 9.0

Commands:

```bash
dotnet build ArxisStudio.Markup.sln
dotnet test ArxisStudio.Markup.sln
dotnet run --project ArxisStudio.Sample/ArxisStudio.Markup.Sample.csproj
dotnet run --project ArxisStudio.Editor/ArxisStudio.Markup.Json.Loader.csproj
dotnet run --project ArxisStudio.Template/ArxisStudio.Markup.Template.csproj
```

## Current Status

- the solution builds successfully
- generator tests pass
- build-time generation supports `Application`, `Control`, `Window`, `Styles`, and `ResourceDictionary`
- generator supports host-level `Styles` and `Resources`
- generator supports `StyleInclude` and merged resource dictionaries
- `ArxisStudio.Sample` contains working `.arxui` examples
- `ArxisStudio.Template` uses `.arxui` for both `Application` and screen UI
- `ArxisStudio.Editor` can load project context and open `.arxui` files from the project
- editor preview now applies local styles and resources, but still does not have full build/runtime parity

## Notes

- `Class` is required for build-time generation of `Application`, `Control`, and `Window` documents.
- `SchemaVersion: 1` should still be treated as an internal evolving contract.
- The editor should be understood as an active prototype, not a finished IDE-grade designer.
