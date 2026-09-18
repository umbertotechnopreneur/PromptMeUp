## Task

- [ ] Build and install a signed local Debug Windows x64 MSIX for the current Settings and chat UI updates; preserve the package and verification record under ignored `artifacts/msix`.

- [ ] Remove temporary source, publish, and payload directories under `artifacts/msix/install-20260918-chat-ui` while preserving the signed MSIX and verification records. Installation and standard .NET cleanup succeeded; automatic approval review blocked recursive directory deletion.

- [ ] Remove temporary source, publish, and payload directories under `artifacts/msix/install-20260917-memory` after preserving the signed MSIX and verification records. Installation and standard .NET cleanup succeeded; automatic approval review blocked recursive directory deletion.

- [ ] Remove ignored local package, IDE, and smoke-test directories. Standard .NET cleanup completed; recursive directory deletion was blocked by the execution policy.
