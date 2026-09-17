## Task

- [ ] Integrate the memory and chat pull requests with current main, resolve CI failures, commit the requested local MSIX changes, and complete protected squash merges.

- [ ] Remove temporary source, publish, and payload directories under `artifacts/msix/install-20260917-memory` after preserving the signed MSIX and verification records. Installation and standard .NET cleanup succeeded; automatic approval review blocked recursive directory deletion.

- [ ] Remove ignored local package, IDE, and smoke-test directories. Standard .NET cleanup completed; recursive directory deletion was blocked by the execution policy.
