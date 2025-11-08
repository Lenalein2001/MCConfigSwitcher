# MC Config Switcher

A Windows desktop application for managing Minecraft server configurations across multiple locations without requiring administrator privileges.

## Overview

MC Config Switcher simplifies the management of Minecraft server configuration files (such as `server.properties`) by allowing you to define profiles with location-specific settings. Switch between different IP addresses and configuration values with a single click.

## Features

- **Profile-Based Management**: Create multiple profiles for different locations (home, work, etc.)
- **Dynamic IP Configuration**: Define and manage named IP addresses with an intuitive interface
- **Safe Modifications**: Automatic backup creation before applying changes
- **Dry-Run Preview**: Review changes before applying them to your configuration files
- **One-Click Revert**: Restore previous configuration from backups
- **Dark Theme UI**: Clean, modern interface optimized for extended use
- **No Admin Required**: Works without elevated permissions

## Getting Started

### Installation

1. Download the latest release from the [Releases](../../releases) page
2. Extract the ZIP file to your preferred location
3. Run `MCConfigSwitcher.exe`

### Basic Usage

1. **Create a Profile**
   - Click the "Add" button in the Profiles panel
   - Enter a descriptive name for your profile

2. **Configure IP Addresses**
   - In the Profile Editor, click "Add IP" to create named locations
   - Enter a name (e.g., "Home", "Office") and the corresponding IP address
   - Select the active IP from the dropdown

3. **Set Target File**
   - Click "Browse" to select your `server.properties` or other configuration file
   - The path will be saved with the profile

4. **Apply Configuration**
   - **Dry Run**: Click to preview changes without modifying files
   - **Apply**: Click to apply changes and create a backup
   - **Revert**: Click to restore the most recent backup

## Profile Configuration

### Profile Schema

Profiles are stored as JSON files in `%APPDATA%\MCConfigSwitcher\Profiles\`. Each profile contains:

- **Name**: Display name for the profile
- **IpEntries**: Collection of named IP addresses
- **ActiveIpChoice**: Currently selected IP location
- **Targets**: Configuration files to modify
- **Variables**: Dynamic values used in replacement rules

### Replacement Rules

The application supports three types of replacement strategies:

#### Line-Key Replacement
Matches lines in the format `key=value` and replaces the value portion.

**Example**:
```
Rule: LineKey="server-ip", Value="${SERVER_IP}"
File: server.properties
Before: server-ip=192.168.1.100
After:  server-ip=10.0.0.50
```

#### Regex Replacement
Uses regular expression patterns for complex replacements.

**Example**:
```json
{
  "Kind": "Regex",
  "Pattern": "^(server-ip)=.*$",
  "Replacement": "$1=${SERVER_IP}"
}
```

#### JSON Path Replacement
Modifies specific properties in JSON configuration files.

**Example**:
```json
{
  "Kind": "JsonPath",
  "JsonPath": "server.address",
  "JsonValue": "${SERVER_IP}"
}
```

### Variable Substitution

The `${SERVER_IP}` variable is automatically populated from the selected active IP address. Additional variables can be defined in the profile's `Variables` dictionary.

## Backup and Safety

- **Automatic Backups**: A timestamped backup (`.bak`) is created before each Apply operation
- **Dry-Run Mode**: Preview all changes before committing
- **Revert Functionality**: Quickly restore the previous configuration
- **Validation**: IP addresses are validated before allowing Apply

## Requirements

- Windows 10 or later (x64)
- [.NET Desktop Runtime 9.0 (x64)](https://dotnet.microsoft.com/download/dotnet/9.0)

To build from source:
- [.NET SDK 9.0](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Visual Studio](https://visualstudio.microsoft.com/vs/) (optional)

## Building from Source

### Prerequisites
- [.NET SDK 9.0](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Visual Studio](https://visualstudio.microsoft.com/vs/) (optional)

### Build Commands

```powershell
# Debug build
dotnet build .\src\MCConfigSwitcher\MCConfigSwitcher.csproj -c Debug

# Release build
dotnet build .\src\MCConfigSwitcher\MCConfigSwitcher.csproj -c Release

# Publish for distribution
dotnet publish .\src\MCConfigSwitcher\MCConfigSwitcher.csproj -c Release -o .\publish
```

## Architecture

Built with:
- **Framework**: .NET 9.0 WPF
- **Pattern**: MVVM (Model-View-ViewModel)
- **Libraries**:
  - CommunityToolkit.Mvvm - MVVM infrastructure
  - Newtonsoft.Json - Profile serialization
  - DiffPlex - Dry-run diff generation

## License

This project is provided as-is for personal and commercial use.

## Support

For issues, questions, or feature requests, please use the [GitHub Issues](../../issues) page.

---

**Note**: This application modifies configuration files. Always ensure you have backups of critical data before using automated configuration tools.

