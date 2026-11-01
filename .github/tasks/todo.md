## Task

- [ ] Verify the HelpMe package identity against its Partner Center registration before any Store submission. Capture updated screenshots when a real HelpMe session is authorized; current screenshots show PromptMeUp.

- [ ] Remove transient output under `artifacts/msix/debug/command-output-20261019/`: `publish`, `package/payload`, `build`, `imports`, `scratch`, `logs`, and `empty-feed`. Preserve pending deliverables and required import-scan records.

- [ ] Validate the Store trial countdown and expiration gate with an acquired Store license; confirm the 30-day trial setting in Partner Center and verify purchase, offline behavior, x64, and ARM64.

- [ ] Deliver the Microsoft Store MSIX workflow through a reviewed pull request, build version 1.0.0.0 on GitHub Actions, submit the x64 and ARM64 packages in Partner Center, and verify availability through the WinGet msstore source.

- [ ] Remove the disposable welcome profile under `artifacts/smoke/layout-refinement-20260927/`.

- [ ] Remove temporary publish and payload directories from `artifacts/msix/debug/1.0.0.0-startup-fix-20260924/` and the superseded `1.0.0.1-startup-fix-20260924/` build. Preserve pending deliverables.

- [ ] Remove `artifacts/msix/debug/1.0.0.0-oobe-20260924-021718/publish` and `x64/payload`, preserving pending deliverables.

- [ ] Review and submit the generated x64 and ARM64 MSIX packages from `artifacts/store/1.0.0.0/` in Partner Center; resolve any remaining Store validation findings.

- [ ] Rebuild and submit x64 and ARM64 Store MSIX packages to confirm the manifest no longer triggers the headless-app validation error; resolve any remaining Store validation findings.


- [ ] Remove `artifacts/msix/debug/0.1.8.5/publish` and `x64/payload` while preserving pending deliverables.

- [ ] Remove `artifacts/msix/debug/0.0.1.2211/publish`, `publish-nosymbols`, and `x64/payload` while preserving pending deliverables.

- [ ] Remove `artifacts/msix/direct-20260921-debug/publish` and `artifacts/msix/direct-20260921-debug/package/payload` while preserving pending deliverables.

- [ ] Remove `artifacts/msix/banner-ef06355-debug-publish` and `artifacts/msix/banner-ef06355-debug/payload` while preserving pending deliverables.

- [ ] Remove `artifacts/msix/chat-68142c7-debug-publish` and `artifacts/msix/chat-68142c7-debug/payload` while preserving pending deliverables.

- [ ] Remove temporary source, publish, and payload directories under `artifacts/msix/install-20260918-chat-ui` while preserving pending deliverables.

- [ ] Remove temporary source, publish, and payload directories under `artifacts/msix/install-20260917-memory` while preserving pending deliverables.

- [ ] Remove disposable local package, IDE, and smoke-test directories while preserving pending deliverables and user data.

- [ ] Remove the temporary `publish`, `x64/payload`, and disposable local test-data backup `appdata-backup` directories under `artifacts/msix/debug/1.0.0.0-layout-5a68a80-20260926/`, preserving pending deliverables.
