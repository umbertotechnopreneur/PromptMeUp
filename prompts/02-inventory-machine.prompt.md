# Prompt — inventory the development machine

Inspect the Windows workstation from `{{WORKSPACE_PATH}}` using read-only `pwsh -NoProfile` checks.

Check, when present: Oh My Posh, PowerShell, Python and `py`, .NET SDKs/runtimes, Visual Studio/devenv, MSVC, Windows SDK, VS Code, Git, FFmpeg/FFprobe, Node/npm/pnpm, SQL Server `sqlcmd` and local SQL services, CMake/Ninja, Docker, Java, NuGet, Winget/Chocolatey, ADB, and Espressif/ESP-IDF/esptool.

Produce `machine-environment-inventory.md` with:

1. verification date and command scope;
2. a table of detected version, path, and practical status;
3. a separate “not detected” section;
4. architecture notes for x64/ARM64 and native builds;
5. a safe recheck command.

Do not print environment variables, tokens, connection strings, user secrets, or full process command lines. Distinguish “tool installed” from “service running” and from “tool available on PATH”.
