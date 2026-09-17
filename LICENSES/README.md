# Upstream license texts

PromptMeUp's own code and documentation use the root MIT license. The files here preserve the licenses and notices of the projects that power the application; they do not change those projects' terms.

The legacy Lenna terminal portrait has separate [attribution](lenna-NOTICE.txt) and [source terms](lenna-legacy-LICENSE.txt). Both are included in release exports alongside the dependency notices.

These texts were retrieved from the following upstream sources. Review them when changing dependencies, including transitive packages.

| File | Source |
| --- | --- |
| [dotnet-LICENSE.txt](dotnet-LICENSE.txt) | [Upstream text](https://raw.githubusercontent.com/dotnet/runtime/v10.0.10/LICENSE.TXT) |
| [spectre-console-LICENSE.txt](spectre-console-LICENSE.txt) | [Upstream text](https://raw.githubusercontent.com/spectreconsole/spectre.console/bbbb5729dde27b58deee44f447a788eea46ee451/LICENSE.md) |
| [yamldotnet-LICENSE.txt](yamldotnet-LICENSE.txt) | [Upstream text](https://raw.githubusercontent.com/aaubry/YamlDotNet/748334a8fa7c227740018b284b71ad95cc6b7fc7/LICENSE.txt) |
| [libyaml-LICENSE.txt](libyaml-LICENSE.txt) | [Upstream text](https://raw.githubusercontent.com/aaubry/YamlDotNet/748334a8fa7c227740018b284b71ad95cc6b7fc7/LICENSE-libyaml) |
| [sqlitepclraw-LICENSE.txt](sqlitepclraw-LICENSE.txt) | [Upstream text](https://raw.githubusercontent.com/ericsink/SQLitePCL.raw/v3.0.5/LICENSE.TXT) |
| [sqlitepclraw-NOTICE.txt](sqlitepclraw-NOTICE.txt) | [Upstream text](https://raw.githubusercontent.com/ericsink/SQLitePCL.raw/v3.0.5/NOTICE.TXT) |
| [sqlite-LICENSE.txt](sqlite-LICENSE.txt) | [License supplied with SQLite 3.53.4](https://www.nuget.org/packages/SQLite/3.53.4/License) |
| [serilog-LICENSE.txt](serilog-LICENSE.txt) | [Upstream text](https://raw.githubusercontent.com/serilog/serilog/497f80fda4f9e8f98b9c13ba34b1f0530f8c4449/LICENSE) |
| [serilog-extensions-logging-LICENSE.txt](serilog-extensions-logging-LICENSE.txt) | [Upstream text](https://raw.githubusercontent.com/serilog/serilog-extensions-logging/538cf2fd64baf760950e202a00dd426c6b76e18c/LICENSE) |
| [serilog-sinks-file-LICENSE.txt](serilog-sinks-file-LICENSE.txt) | [Upstream text](https://raw.githubusercontent.com/serilog/serilog-sinks-file/23c732a8658a0df2a5434fe69b0011800b14f0da/LICENSE) |

The release notice exporter also copies license and notice files shipped inside every resolved application NuGet package and the exact .NET runtime pack used for the selected platform. `THIRD_PARTY_INVENTORY.json` in each archive records versions, declared licenses, authors, copyright, and source repositories. Development-only test tools are listed in the root `THIRD_PARTY_NOTICES.md` and are not shipped in application archives.

SQLite's deliverable library is [dedicated to the public domain](https://www.sqlite.org/copyright.html). SQLitePCLRaw has its own Apache-2.0 terms and upstream notices, preserved here. The libyaml notice is retained with YamlDotNet.

The exporter also recognizes the native `SQLite` package introduced by
SQLitePCLRaw 3.0.5. That package declares a license file rather than an SPDX
expression. Its text must match the reviewed `sqlite-LICENSE.txt` before export;
a missing or changed declaration stops packaging. This attribution support does
not change the application's selected package versions.

No fonts, OpenAI service credentials, or external command-line tools are bundled. Artwork provenance is documented in the repository's [artwork guide](https://github.com/umbertotechnopreneur/PromptMeUp/blob/main/docs/assets/README.md).
