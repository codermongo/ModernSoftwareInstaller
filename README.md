# Modern Software Installer

A Windows software installer with WinGet integration and a modern user interface.

## Features

**Installation**
- Categorized software selection (Gaming, Browser, Drivers, Tools)
- Batch installation of multiple programs
- WinGet as backend for package management
- Automatic administrator rights

**Updates**
- Scan for available updates
- Tabular display with version comparison
- Bulk update function

**Uninstallation**
- WinGet-based uninstallation
- Confirmation dialog

**System Checks**
- Detection of .NET Framework and VC++ Redistributables
- Dependency check

**Profiles**
- Export as batch script
- JSON configuration files
- Reusable installation lists

## Installation

**Requirements:**
- Windows 10/11 x64
- .NET 8.0 Runtime
- Administrator rights

**Setup:**
```bash
git clone https://github.com/codermongo/ModernSoftwareInstaller.git
cd ModernSoftwareInstaller
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

## Supported Software

**Gaming:** Steam, Epic Games, EA App, Ubisoft Connect, Battle.net  
**Browser:** Chrome, Firefox, Brave, Edge, Opera, LibreWolf, Tor  
**Drivers:** Intel Graphics, AMD Software, NVIDIA GeForce Experience  
**Tools:** VS Code, Discord, OBS Studio, WinRAR, Spotify, PyCharm

## Usage

1. Start the executable (admin rights are requested automatically)
2. Select software in the categories
3. Start installation via WinGet

## Technical Details

- **Frontend:** WPF with ModernWpfUI
- **Backend:** WinGet CLI integration
- **Deployment:** Single-file EXE
- **Theme:** Black-purple color scheme
- **Profile Export:** IExpress for EXE wrapper

## Contributing

Standard GitHub workflow: Fork → Branch → Commit → Push → Pull Request

## License

MIT License
