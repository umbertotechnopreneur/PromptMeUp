## Task

- [ ] Refine fullscreen navigation and shared screen chrome in small, separately committed changes toward the 1.0 beta. Keep each screen's editing behavior intact.

- [ ] Remove `artifacts/msix/debug/0.0.1.2211/publish`, `publish-nosymbols`, and `x64/payload` while preserving the signed MSIX and installation record. Installation and standard .NET cleanup succeeded; execution policy blocked recursive directory deletion.

- [ ] Remove `artifacts/msix/direct-20260921-debug/publish` and `artifacts/msix/direct-20260921-debug/package/payload` while preserving the signed MSIX and installation record. Installation and standard .NET cleanup succeeded; execution policy blocked recursive directory deletion.

- [ ] Remove `artifacts/msix/banner-ef06355-debug-publish` and `artifacts/msix/banner-ef06355-debug/payload` while preserving the signed MSIX and installation verification record. Installation and standard .NET cleanup succeeded; automatic approval review blocked recursive directory deletion.

- [ ] Remove `artifacts/msix/chat-68142c7-debug-publish` and `artifacts/msix/chat-68142c7-debug/payload` while preserving the signed MSIX and installation verification record. Installation and standard .NET cleanup succeeded; automatic approval review blocked recursive directory deletion.

- [ ] Remove temporary source, publish, and payload directories under `artifacts/msix/install-20260918-chat-ui` while preserving the signed MSIX and verification records. Installation and standard .NET cleanup succeeded; automatic approval review blocked recursive directory deletion.

- [ ] Remove temporary source, publish, and payload directories under `artifacts/msix/install-20260917-memory` after preserving the signed MSIX and verification records. Installation and standard .NET cleanup succeeded; automatic approval review blocked recursive directory deletion.

- [ ] Remove ignored local package, IDE, and smoke-test directories. Standard .NET cleanup completed; recursive directory deletion was blocked by the execution policy.
