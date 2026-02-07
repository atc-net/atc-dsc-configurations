# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This monorepo contains two things:
1. **DSC v3 configuration profiles** (`configurations/`) — YAML files for automating Windows developer environment setup
2. **`atc-dsc` CLI/TUI tool** (`src/`) — a .NET 10 tool for browsing, testing, and applying those profiles

## Build & Test

```powershell
# Build (Release mode enforces all analyzer rules as errors)
dotnet build -c Release

# Run tests
dotnet test

# Run the CLI tool locally
dotnet run --project src/Atc.Dsc.Configurations.Cli
```

## Repository Structure

```
configurations/           # DSC v3 YAML profiles + extension manifests
  *.dsc.yaml              # 17 profiles (os, dotnet, azure, web, python, etc.)
  *-vscode-extensions.json
  .vsconfig
src/Atc.Dsc.Configurations.Cli/
  Program.cs              # Entry point, DI registration, Spectre.Console.Cli app
  Commands/               # Spectre.Console CLI commands (list, show, test, apply, update)
  Contracts/              # Records: Profile, Resource, ExecutionResult, etc.
  Extensions/             # Extension methods (CommandApp config, ProfileFileName helpers)
  Parsers/                # YAML profile parser (IProfileParser / YamlProfileParser)
  Repositories/           # Profile repositories (GitHub, Caching)
  Clients/                # DSC CLI client (IDscClient / DscClient)
  Diagnostics/            # Environment detection (IEnvironmentDetector / EnvironmentDetector)
  Tui/                    # Terminal.Gui v2 interactive UI (MainWindow, ExecutionDialog)
  GlobalUsings.cs         # All using directives centralized here (ATC220/221)
test/Atc.Dsc.Configurations.Cli.Tests/
```

## Architecture

**CLI tool** — uses Spectre.Console.Cli for command routing and Terminal.Gui v2 for the TUI. Key patterns:
- `IProfileRepository` chain: `GitHubProfileRepository` -> `CachingProfileRepository`
- `IDscClient` / `DscClient` wraps the `dsc` CLI via CliWrap
- `IProfileParser` / `YamlProfileParser` parses DSC v3 YAML via YamlDotNet

**DSC v3 YAML profiles** follow this schema:
```yaml
$schema: https://aka.ms/dsc/schemas/v3/bundled/config/document.json
resources:
  - name: ...
    type: Microsoft.WinGet/Package | Microsoft.DSC.Transitional/RunCommandOnSet | ...
```

## Coding Conventions

- **.NET 10**, C# 14, `.slnx` solution format
- **ATC coding rules** (atc-net/atc-coding-rules) — `TreatWarningsAsErrors` in Release
- **All using directives in GlobalUsings.cs** (ATC220/221) — do NOT add per-file usings
- **No underscore-prefixed fields** (SA1309) — use `this.field = field` in constructors
- **One type per file** (MA0048) — file name must match type name
- **Expression body** for single-return methods (ATC210)
- **No method chains** with 2+ calls on same line (ATC203) — break into separate statements
- **Conventional commits**: `feat:`, `fix:`, `chore:`, `docs:`
- **Do NOT modify .editorconfig files** — fix code to comply with rules
- `Terminal.Gui.Drawing.Attribute` fully qualified (conflicts with `System.Attribute`)
- `Contracts.Profile` fully qualified (conflicts with `Spectre.Console.Profile`)
- `Spectre.Console.Cli.CommandContext` fully qualified (conflicts with `Terminal.Gui.Input.CommandContext`)
- `System.Drawing.Size` fully qualified in ColoredOutputView (conflicts with `Spectre.Console.Size`)
- `CliWrap.Cli.Wrap()` fully qualified (namespace conflict with `Atc.Dsc.Configurations.Cli`)

## Key Commands

```powershell
# Apply a DSC profile directly
dsc config set --file configurations/<profile>.dsc.yaml
dsc config test --file configurations/<profile>.dsc.yaml
```