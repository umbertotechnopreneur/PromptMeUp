# Third-party notices

This inventory covers direct NuGet dependencies declared by PromptMeUp as of 2026-08-12. Package versions and license expressions are taken from the installed NuGet package metadata. Transitive packages retain their own notices and license files in the NuGet distribution.

## Runtime dependencies

| Package | Version | License |
| --- | ---: | --- |
| [Microsoft.Data.Sqlite](https://www.nuget.org/packages/Microsoft.Data.Sqlite/10.0.10) | 10.0.10 | [MIT](https://licenses.nuget.org/MIT) |
| [Microsoft.Extensions.DependencyInjection](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection/10.0.10) | 10.0.10 | [MIT](https://licenses.nuget.org/MIT) |
| [Microsoft.Extensions.Http](https://www.nuget.org/packages/Microsoft.Extensions.Http/10.0.10) | 10.0.10 | [MIT](https://licenses.nuget.org/MIT) |
| [Microsoft.Extensions.Logging](https://www.nuget.org/packages/Microsoft.Extensions.Logging/10.0.10) | 10.0.10 | [MIT](https://licenses.nuget.org/MIT) |
| [Serilog](https://www.nuget.org/packages/Serilog/4.4.0) | 4.4.0 | [Apache-2.0](https://licenses.nuget.org/Apache-2.0) |
| [Serilog.Extensions.Logging](https://www.nuget.org/packages/Serilog.Extensions.Logging/10.0.0) | 10.0.0 | [Apache-2.0](https://licenses.nuget.org/Apache-2.0) |
| [Serilog.Sinks.File](https://www.nuget.org/packages/Serilog.Sinks.File/7.0.0) | 7.0.0 | [Apache-2.0](https://licenses.nuget.org/Apache-2.0) |
| [Spectre.Console](https://www.nuget.org/packages/Spectre.Console/0.57.2) | 0.57.2 | [MIT](https://licenses.nuget.org/MIT) |
| [SQLitePCLRaw.bundle_e_sqlite3](https://www.nuget.org/packages/SQLitePCLRaw.bundle_e_sqlite3/2.1.12) | 2.1.12 | [Apache-2.0](https://licenses.nuget.org/Apache-2.0); bundled SQLite is public domain |
| [YamlDotNet](https://www.nuget.org/packages/YamlDotNet/18.1.0) | 18.1.0 | [MIT](https://licenses.nuget.org/MIT) |

## Development and test dependencies

| Package | Version | License |
| --- | ---: | --- |
| [Microsoft.NET.Test.Sdk](https://www.nuget.org/packages/Microsoft.NET.Test.Sdk/18.8.1) | 18.8.1 | [MIT](https://licenses.nuget.org/MIT) |
| [xunit](https://www.nuget.org/packages/xunit/2.9.3) | 2.9.3 | [Apache-2.0](https://licenses.nuget.org/Apache-2.0) |
| [xunit.runner.visualstudio](https://www.nuget.org/packages/xunit.runner.visualstudio/3.1.5) | 3.1.5 | [Apache-2.0](https://licenses.nuget.org/Apache-2.0) |

## Optional external tools and services

PromptMeUp does not bundle these components:

- OpenAI APIs are an optional external service used with the user's own credentials and account terms.
- [Oh My Posh](https://github.com/JanDeDobbeleer/oh-my-posh) is invoked only when the user explicitly requests Nerd Font installation and already has the command installed.
- JetBrainsMono Nerd Font is installed only through that explicit opt-in flow; the font is not distributed in this repository.

Run `hm --third-party` for the in-product direct runtime inventory.
