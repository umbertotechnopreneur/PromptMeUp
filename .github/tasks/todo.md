## Task

- [ ] Fix the experimental review findings: reject stale consent updates, budget serialized skill instructions, report oversized selections, and enforce import capacity. Add regression coverage and validate before committing on the existing branch.

- [ ] Remove temporary source, publish, and payload directories under `artifacts/msix/install-20260918-chat-ui` while preserving the signed MSIX and verification records. Installation and standard .NET cleanup succeeded; automatic approval review blocked recursive directory deletion.

- [ ] Remove temporary source, publish, and payload directories under `artifacts/msix/install-20260917-memory` after preserving the signed MSIX and verification records. Installation and standard .NET cleanup succeeded; automatic approval review blocked recursive directory deletion.

- [ ] Remove ignored local package, IDE, and smoke-test directories. Standard .NET cleanup completed; recursive directory deletion was blocked by the execution policy.
