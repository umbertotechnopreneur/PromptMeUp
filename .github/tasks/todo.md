## Task

- [ ] Remove temporary source, publish, and payload directories under `artifacts/msix/install-20260917-memory` after preserving the signed MSIX and verification records. Installation and standard .NET cleanup succeeded; automatic approval review blocked recursive directory deletion.

- [ ] Publish GitHub prerelease `v0.1.8` through the Release workflow after correcting Inno Setup version detection. PR #34 is merged and `v0.1.7` remains on its original commit; its first release run failed before creating the Windows installers or a draft. Verify all six portable archives and both Windows EXE installers before publishing the new draft as a prerelease.

- [ ] Remove ignored local package, IDE, and smoke-test directories. Standard .NET cleanup completed; recursive directory deletion was blocked by the execution policy.
