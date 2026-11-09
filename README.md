# RadialButton

RadialButton is a plugin for Autodesk Revit that helps speed up and simplify your workflow.

Create your own custom radial menu with up to 8 Revit or third-party plugin commands. Access Revit tools from anywhere on the screen, create multiple command configurations, and ditch the standard Revit panels.

## Features

- Custom radial menus with up to 8 commands.
- Support for built-in Revit tools and third-party plugin commands.
- Multiple configurations for different workflows.
- Command search and assignment.
- Customizable color schemes.
- Import and export configurations.

## Installation

### Option 1: Installer (Recommended)

1. Close all running instances of Revit.
2. Run the `.exe` installer.
3. Follow the installation instructions.
4. Launch Revit.

### Option 2: Manual Installation

1. Close the target version of Revit.
2. Download the appropriate version:
   - `bin/2025+` — Revit 2025–2027.
   - `bin/2024-` — Revit 2022–2024.
3. Copy `RadialButton.addin` and the `RadialButton` folder to one of the following directories:
   - `%APPDATA%\Autodesk\Revit\Addins\<year>\` - for single user,
   - `C:\ProgramData\Autodesk\Revit\Addins\<year>\` - for all users.
4. If a previous version is installed, replace its files. Make sure only one RadialButton `.addin` file exists for each Revit version.
5. Launch Revit.

## Getting Started

1. Open **RadialButton Settings** from the **Add-Ins** tab in Revit.
2. Create or select a configuration.
3. Click a **Command** field to search for and assign a Revit command.
4. Customize the appearance and color scheme if needed.
5. Click **Save & Close**.

To open the radial menu, run **RadialButton Open** from **External Tools**. You can assign a keyboard shortcut to this command using Revit's keyboard shortcut settings.

## Compatibility

| Revit Version | Framework | Build Configuration |
|---|---|---|
| 2025–2026 | .NET 8 | Release / Debug |
| 2022–2024 | .NET Framework 4.8 | Legacy |

Third-party commands require their corresponding plugins to be installed. Some commands may be unavailable depending on the active Revit context or selection.

## Building from Source

**Requirements:**
- .NET SDK with .NET 8 support.
- .NET Framework 4.8 Developer Pack.
- Autodesk Revit 2025 and/or 2022 API assemblies.

Build using:

```powershell
dotnet build RadialButton.sln -c Release
dotnet build RadialButton.sln -c Legacy
```

Alternatively, run `Build-All.cmd` to build both versions.

The compiled files are located in `bin/2025+` and `bin/2024-`.

## Configuration Files

User settings and configurations are stored in:

`%APPDATA%\RadialButton\`

Configurations can be exported and imported as JSON files. Missing third-party plugins must be installed separately.