## Task

- [ ] Remove `artifacts/msix/debug/1.0.0.0-oobe-20260924-021718/publish` and `x64/payload`, preserving the signed MSIX, checksums, package metadata, and installation record. Standard .NET cleanup succeeded; automatic approval review blocked recursive deletion.

- [ ] Review and submit the generated x64 and ARM64 MSIX packages from `artifacts/store/1.0.0.0/` in Partner Center; resolve any remaining Store validation findings.

- [ ] Rebuild and submit x64 and ARM64 Store MSIX packages to confirm the manifest no longer triggers the headless-app validation error; resolve any remaining Store validation findings.


- [ ] Remove `artifacts/msix/debug/0.0.1.2211/publish`, `publish-nosymbols`, and `x64/payload` while preserving the signed MSIX and installation record. Installation and standard .NET cleanup succeeded; execution policy blocked recursive directory deletion.

- [ ] Remove `artifacts/msix/direct-20260921-debug/publish` and `artifacts/msix/direct-20260921-debug/package/payload` while preserving the signed MSIX and installation record. Installation and standard .NET cleanup succeeded; execution policy blocked recursive directory deletion.

- [ ] Remove `artifacts/msix/banner-ef06355-debug-publish` and `artifacts/msix/banner-ef06355-debug/payload` while preserving the signed MSIX and installation verification record. Installation and standard .NET cleanup succeeded; automatic approval review blocked recursive directory deletion.

- [ ] Remove `artifacts/msix/chat-68142c7-debug-publish` and `artifacts/msix/chat-68142c7-debug/payload` while preserving the signed MSIX and installation verification record. Installation and standard .NET cleanup succeeded; automatic approval review blocked recursive directory deletion.

- [ ] Remove temporary source, publish, and payload directories under `artifacts/msix/install-20260918-chat-ui` while preserving the signed MSIX and verification records. Installation and standard .NET cleanup succeeded; automatic approval review blocked recursive directory deletion.

- [ ] Remove temporary source, publish, and payload directories under `artifacts/msix/install-20260917-memory` after preserving the signed MSIX and verification records. Installation and standard .NET cleanup succeeded; automatic approval review blocked recursive directory deletion.

- [ ] Remove ignored local package, IDE, and smoke-test directories. Standard .NET cleanup completed; recursive directory deletion was blocked by the execution policy.
