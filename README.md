# Atc.Dsc.Configurations

DSC v3 configuration profiles for automating Windows developer environment setup, plus a .NET CLI/TUI tool (`atc-dsc`) for browsing, testing, and applying them.

Using DSC v3 configuration files, you can consolidate manual machine setup and project onboarding to a single command that is reliable and repeatable. This repository provides:

- 📄 YAML-formatted DSC v3 configuration files that list all of the software versions, packages, tools, dependencies, and settings required to set up the desired state of the development environment on your Windows machine.
- ⚙️ PowerShell Desired State Configuration (DSC) to automate the configuration of your Windows operating system.
- 🖥️ An interactive .NET CLI/TUI tool (`atc-dsc`) with a two-panel interface for browsing, testing, and applying profiles.

## Table of Contents

- [Atc.Dsc.Configurations](#atcdscconfigurations)
  - [✨ Features](#-features)
  - [📋 Requirements](#-requirements)
  - [🚀 Applying Profiles](#-applying-profiles)
  - [💡 Use Case Scenarios](#-use-case-scenarios)
  - [🤝 How to contribute](#-how-to-contribute)

## ✨ Features

- 🖥️ **Interactive TUI** — two-panel interface with profile list, detail view, search/filter, and vim-style keyboard navigation
- ⌨️ **CLI commands** — `list`, `show`, `test`, `apply`, `update` for scripting and CI/CD
- 🔗 **GitHub integration** — fetches profiles from GitHub with local file-based caching
- 📦 **18 profiles** — covering OS settings, .NET, Azure, web, Python, Java, containers, AI, embedded devices, and more
- 🛡️ **Graceful degradation** — not admin? Browse and test, but apply is blocked with a clear message

## 📋 Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [PowerShell 7](https://github.com/PowerShell/PowerShell/releases) (pwsh)
- [DSC v3 Preview](https://github.com/PowerShell/DSC/releases) (>= 3.2.2) — install via `winget install Microsoft.DSC.Preview`
- Windows (admin recommended for applying profiles)

> **Note:** DSC v3 Preview >= 3.2 is required for the `Microsoft.DSC.Transitional/RunCommandOnSet` resource used by VSCode extension, dotnet tool, npm package, pip package, Azure CLI extension, and PowerShell module installation. The stable DSC v3 release (3.1.x) does not include this resource.

> **Known issue — DSC v3 `3.2.0-preview.12` (version `3.2.12.0`):** This version has a bug where `dsc config test` and `dsc config set` fail with `"JSON: trailing characters at line 2 column 1"` for all WinGet resources. DSC successfully invokes `winget dscv3 package --test` and gets valid results, but then crashes during its own JSON verification step (exit code 2, no output). This affects every profile containing `Microsoft.WinGet/Package` resources. The `atc-dsc` tool handles this gracefully by showing "Failed to parse DSC output as JSON". To work around this, downgrade to a working version:
>
> ```powershell
> winget uninstall Microsoft.DSC.Preview
> winget install Microsoft.DSC.Preview --version 3.2.2
> ```

## 🚀 Applying Profiles

You can apply profiles using the `atc-dsc` CLI tool (recommended) or direct DSC CLI execution.

### Interactive Mode (default)

```powershell
dotnet tool install -g atc-dsc
atc-dsc
```

Launches the TUI with keyboard shortcuts:

| Key | Action |
|-----|--------|
| `j`/`k` | Navigate profile list |
| `Space` | Toggle profile selection |
| `Enter` | Apply selected profiles |
| `t` | Test selected profiles |
| `/` | Filter profiles |
| `Tab` | Switch panel focus |
| `?` | Help |
| `q` | Quit |

### CLI Commands

```powershell
# 📋 List available profiles
atc-dsc list
atc-dsc list --json --verbose

# 🔍 Show profile details
atc-dsc show dotnet
atc-dsc show dotnet --raw

# ✅ Test profiles (read-only)
atc-dsc test os dotnet azure
atc-dsc test os --json --verbose

# 🚀 Apply profiles (requires admin)
atc-dsc apply os dotnet azure
atc-dsc apply --all --yes
atc-dsc apply --file ./custom.dsc.yaml

# 🔄 Force refresh from GitHub
atc-dsc update
```

### Direct CLI Execution

```powershell
dsc config set --file .\configurations\<profile>.dsc.yaml
dsc config test --file .\configurations\<profile>.dsc.yaml
```

## 💡 Use Case Scenarios

### 🔷 DotNet Azure Developer

For a DotNet Azure Developer, the configuration contains the essential components for a seamless development experience. This includes setting up the operating system with the [`os`](configurations/os-configuration.dsc.yaml) configuration, integrating Azure-specific tools and settings via the [`azure`](configurations/azure-configuration.dsc.yaml) profile, and tailoring the environment for DotNet development with the [`dotnet`](configurations/dotnet-configuration.dsc.yaml) configuration.

### 🌐 Web Developer

If you're a Web Developer, the configuration is crafted to cater to your specific needs. Start with the operating system setup using the [`os`](configurations/os-configuration.dsc.yaml) configuration, and then move on to apply the [`web`](configurations/web-configuration.dsc.yaml) profile, which includes a range of tools and settings optimized for web development tasks.

## 🤝 How to contribute

[Contribution Guidelines](https://atc-net.github.io/introduction/about-atc#how-to-contribute)

[Coding Guidelines](https://atc-net.github.io/introduction/about-atc#coding-guidelines)