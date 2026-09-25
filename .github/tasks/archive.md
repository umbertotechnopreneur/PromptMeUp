# Task Archive

## 2026-09-24 — Accept the bundled command guide in Windows CI packaging

- Fixed the existing Windows installer validation used by the quality gate: allow and require the exact `docs/promptmeup-quick-reference.pdf` payload path, matching the MSIX contract.
- Preserved rejection of unrelated files and the existing credential and local-data guards. The installer template already copies nested payload files.

Validation: PowerShell syntax and scoped diff checks passed. No local builds, installers, or automated tests were run; PR checks validate the Windows packaging step after push.

## 2026-09-24 — Improve onboarding and add a printable command guide

- Reworked the first-run steps into a disposable, collapsible terminal surface when supported. Completed prompts now leave compact headers, while the active step uses a horizontal cyan-to-pink gradient rule; scrolling terminals keep a simpler fallback without redundant choice separators.
- Expanded the final welcome with three practical starter commands, localized recovery guidance, and explicit opening of the installed two-page command guide in all six supported languages.
- Added a print-friendly A4 quick reference with essential commands on page one and advanced workflows, global switches, chat controls, and safety guidance on page two. Both pages carry the official application icon.
- Included the guide in build and publish output and made it an allowed, required MSIX payload file. Added offline opening from Help, including Help reached through the home menu, with localized recovery messages if the guide or PDF reader is unavailable.

Validation: repository preflight, formatting verification, XML method-comment check, MSIX script syntax, and Release build with warnings treated as errors passed. The publish inventory contains exactly one guide at `docs/promptmeup-quick-reference.pdf`; the build copy matches the source SHA-256. Both branded PDF pages were rendered and visually inspected. Automated tests and CLI smoke tests were not run; regression sources compiled. No MSIX was produced or installed during this task. Release build output was cleaned after validation.

## 2026-09-24 — Replace the installed Debug MSIX at version 1.0.0.0

- Removed the existing current-user PromptMeUp package after the owner's explicit authorization and installed the newly signed x64 Debug MSIX from commit `bd27f53`.
- Preserved the owner-selected package version `1.0.0.0`; no version component was changed.

Validation: `Get-AppxPackage` reports version 1.0.0.0, x64 architecture, `Ok` status, and a Developer signature for `UmbertoGiacobbiDotBiz.PromptMeUp_1.0.0.0_x64__aa9ddh7dsmn36`.

## 2026-09-24 — Prepare a redacted email support bundle

- Added `hm --prepare-logs` as a standalone local command available before onboarding and database initialization.
- The command creates a new timestamped ZIP on the current user's desktop with a privacy-limited machine summary, a contents note, and up to the 14 available daily logs. Each log contributes at most its latest 4 MiB.
- Applies credential redaction again while packaging and replaces current-user path roots with neutral tokens. Excludes the database, conversations, memories, environment values, API keys, user and computer names, network addresses, and personal-file inventories.
- Added six-language confirmation copy, command help, CLI and privacy documentation. Creating the ZIP sends nothing; the user inspects it and attaches it to an email manually.

Validation: 24 focused parser and archive tests passed, followed by all 1,007 tests. Preflight, XML comments, formatting verification, `git diff --check`, and the warning-free Release build passed. The archive regression test verified credential and user-path redaction and database exclusion. Standard Release cleanup completed.

## 2026-09-24 — Fix startup rendering and add onboarding diagnostics

- Replaced untitled calls to the titled-divider helper in home and onboarding with Spectre rules. The previous calls threw `ArgumentException` immediately after the banner.
- Replaced the unsupported `whitesmoke` color name in the command banner with its hexadecimal color.
- Added startup session IDs, route and terminal metadata, exit codes, method-only failure traces, and onboarding step and connection-check events without keys, nicknames, or conversation content. Interactive no-argument startup failures wait for Enter before closing.
- Added welcome rendering coverage in all six languages and assertions that onboarding diagnostics omit credentials and nicknames. Documented the owner's exclusive control over all version components.
- Installed the corrected Debug MSIX as the owner-requested 1.0.0.0 through `Add-AppxPackage -ForceUpdateFromAnyVersion -ForceTargetApplicationShutdown`. An initially chosen 1.0.0.1 revision was superseded after the owner's correction; the final installation is 1.0.0.0 and matches the corrected build hash.

Validation: all 1,005 tests passed, including home keyboard navigation and actual welcome rendering. Interactive smoke checks with isolated data reached the key screen without provider calls, and the final installed alias stayed open at language selection and exited cleanly on Esc. Preflight, XML comments, formatting, warning-free Release build and Debug publish, package signing and installed-file verification passed. Standard .NET cleanup completed for both configurations; temporary packaging cleanup remains tracked separately.

## 2026-09-24 — Install Debug MSIX 1.0.0.0 checkpoint

- Published the current working source for Windows x64 in Debug with application version 1.0.0 and MSIX version 1.0.0.0.
- Exported redistribution notices, built the MSIX under `artifacts/msix/debug/1.0.0.0-oobe-20260924-021718/`, and signed it with the existing trusted current-user publisher certificate.
- Attempted an in-place update with `-ForceUpdateFromAnyVersion -ForceTargetApplicationShutdown`. Windows rejected the changed content under the same package identity and version with `0x80073CFB`; removed and reinstalled the current-user package to complete replacement. No application-data files were present in the package's user-data folders.
- Verified the installed application hash against the new build, version 1.0.0.0, the execution alias, product icon, and Start entry without help arguments. Saved the verification record beside the package.

Validation: Debug publish with warnings treated as errors, MSIX manifest validation, signing and signature verification passed. Standard .NET cleanup succeeded after limiting it to the published project because test assets had no Windows runtime target. Automatic approval review blocked recursive cleanup of publish and payload directories; follow-up remains in the task list. Automated tests and CLI smoke tests were not run. No Store upload or Git push was performed.

## 2026-09-24 — Guided welcome, home menu, and desktop launch

- Added four localized welcome steps: system language and ASCII flag, masked OpenAI key verification, optional name, and separate memory and learning consent. Completion does not open general settings.
- Store verified Windows keys in Credential Manager. Explain learning collection, redaction, provider analysis, and the separate local activity history. Leave skills disabled until explicitly enabled.
- Added a shared colored pixel banner for welcome and home. Running `hm` opens a numbered menu with single-key choices for existing chat, script, diagnostic, memory, skills, settings, and help workflows.
- Added an unchecked desktop shortcut option at the end of onboarding. The native link uses the existing product icon and Windows execution alias; Start launches the same no-argument home flow.
- Added regression cases for verification before credential persistence, failed verification, memory choices, desktop consent, and immediate numeric navigation.

Validation: preflight, PowerShell syntax, project XML, XML method comments, formatting verification, and Release build with warnings treated as errors passed. Build output was cleaned. Automated tests and CLI smoke tests were not run; no MSIX was built or installed.

## 2026-09-23 — Generate Store MSIX packages and unify artifact roots

- Generated unsigned x64 and ARM64 Store MSIX packages for version 1.0.0.0 under `artifacts/store/1.0.0.0/`.
- Changed package defaults to use `artifacts/store/<version>/<architecture>/` for Store and `artifacts/debug/<version>/<architecture>/` for Debug.
- Kept temporary Store publish output under the Store artifact root and removed the obsolete `artifacts/msix/store-work` directory.
- Updated the packaging guide and left Partner Center submission for follow-up.

Validation: preflight passed; Release packaging succeeded for both architectures; package manifests and SHA-256 hashes were checked; build output was cleaned. Automated tests were not run.

## 2026-09-23 — Remove hidden headless app entry from Store MSIX manifest

- Moved the `hm.exe` execution alias extension onto the visible `PromptMeUp` application entry and removed the second application entry with `AppListEntry="none"`.
- The generated Store manifest now has one visible app entry; fresh x64 and ARM64 packages still need Store validation.

Validation: PowerShell parser and repository preflight checks passed. Store upload was not repeated.

## 2026-09-23 — Consolidate repository scripts

- Added `scripts/PromptMeUp.ps1` as the single repository entry point, with an interactive menu when no command is supplied and direct `-Command` routing for automation.
- Moved the previous PowerShell entry points into private command modules under `scripts/Common/commands` and kept the published PATH helpers working from their new source location.
- Updated repository guidance, packaging documentation, project links, and GitHub workflows to use the unified launcher.

Validation: all PowerShell files passed parser checks, both changed GitHub workflows passed YAML parsing, every private command is referenced by the launcher, preflight, formatting verification, XML method comments, and the warning-free Release build passed. Automated tests, CLI smoke tests, and packaging were not run because they were not requested.

## 2026-09-23 — Refine the first-run attribution line

- Added a blank line before the PromptMeUp GitHub link.
- Moved the author credit onto the GitHub line and added two accessible icons with text fallbacks.

Validation: preflight, repository-wide formatting verification, XML method comments, and Release build with warnings treated as errors passed. Automated tests were not run because they were not requested.

## 2026-09-23 — Normalize C# formatting and exclude Debug symbols from MSIX

- Normalized the 37 affected C# files to UTF-8 with BOM and CRLF, without changing source text.
- Excluded root-level PDB files from Debug MSIX payloads while preserving them in the publish directory.
- Updated the Windows packaging guide to explain how to reuse a Debug publish containing symbols.

Validation: PowerShell syntax, preflight, repository-wide `dotnet format --verify-no-changes`, XML method comments, and warning-free Release build passed. A signed MSIX was packaged from an existing Debug publish containing `hm.pdb`; the package contained no PDB files. Release build output was cleaned. Automated tests were not run.

## 2026-09-22 — Require setup on first run

- Added an `IsFirstRun` settings flag derived from the successfully saved setup state, without redundant persisted state or legacy migration code.
- Routed every valid command through the common startup path and added a localized first-run prompt that opens Setup on OK or returns to the shell on Cancel.
- Removed the redundant build time-zone field and render the extended ISO 8601 build timestamp, whose value already contains the local UTC offset.

Validation: preflight, scoped formatting verification, XML method-comment check, warning-free Release build, the complete 982-test suite, and the non-interactive first-run CLI smoke passed. The repository-wide formatting check remains blocked by pre-existing CRLF/BOM violations in unrelated files. Release build output was cleaned after validation.

## 2026-09-22 — Fix global activation in the installed app

- Corrected the SQLite schema so skills, learning evidence, proposals, revisions, reminders, and saved memories accept only the global scope used by the application.
- Removed the obsolete project-to-global memory migration and project-scoped regression fixtures.
- Built, signed, installed, and interactively verified Debug MSIX `0.0.1.2222`; activation now succeeds from both `hm --skills` and `hm --learning` without terminating the app.

Validation: preflight, XML method-comment check, warning-free Release build, 108 focused storage and settings tests, signed-package verification, and installed interactive smoke checks passed.

## 2026-09-22 — Install global Learning Debug MSIX

- Built, signed, verified, and installed local x64 Debug MSIX `0.0.1.2221` containing the global Skills and Memory changes.
- Preserved the signed package and SHA-256 record under ignored `artifacts/msix/learning-global-20260922-debug-v2`.

Validation: package signature verification and installed package status passed. Automated tests were not run.

## 2026-09-22 — Make Learning and Skills global

- Rebuilt `hm --learning` on the shared fullscreen workspace used by Skills: action descriptions stay in the footer and confirmation remains in the same visual flow.
- Replaced the Dream JSON dump with a bounded evidence count and an explicit fullscreen confirmation.
- Made skills, memory collection, proposals, and reminders use one application-wide scope; updated the six runtime translations and Setup labels to say global.

Validation: preflight, restore, XML method-comment check, diff inspection, and warning-free Release build passed; the repository-wide formatting check remains blocked by pre-existing CRLF/BOM violations in unrelated files. Automated tests were not run.

## 2026-09-22 — Refine the Skills command list

- Removed the redundant command metadata column from `hm --skills`; package details remain available below the selected command.
- Added green and red status indicators. Toggling a skill now refreshes the active fullscreen menu in place instead of leaving and reopening the page.
- Moved the selected command description into the footer notice, replacing the fixed navigation sentence.

Validation: preflight, restore, XML method-comment check, diff inspection, and warning-free Release build passed; Release output was cleaned. The repository-wide formatting check remains blocked by pre-existing CRLF/BOM violations in unrelated files. Automated tests were not run.

## 2026-09-22 — Build and install global Skills Debug MSIX

- Built, signed, verified, and installed local x64 Debug MSIX `0.0.1.2220` for `UmbertoGiacobbiDotBiz.PromptMeUp`.
- Preserved the signed package and SHA-256 record under ignored `artifacts/msix/global-skills-20260922-debug`.

Validation: package signature verification and installed package status passed. The application Debug output was cleaned. Solution cleanup reported pre-existing test restore assets without a `win-x64` target; the package and its generated intermediates remain under ignored artifacts.

## 2026-09-22 — Keep the Skills screen global

- Replaced the Project group in `hm --skills` with General, including its six localized descriptions and activation message.
- Renamed the menu actions and added a repository rule that prevents project-scoped labels, groups, availability, or configuration from returning to `hm --skills`.

Validation: preflight, restore, XML method-comment check, scoped source search, diff inspection, and warning-free Release build passed; Release output was cleaned. The repository-wide formatting check remains blocked by pre-existing CRLF/BOM violations in unrelated files. Automated tests were not run.

## 2026-09-25 — Install the chat refinements Debug build

- Built a self-contained x64 Debug publish with embedded symbols and version `0.1.8.5` from source commit `41e9171`.
- Exported notices for 27 packages, then created, signed, and verified the local MSIX and its SHA-256 checksum.
- Installed `UmbertoGiacobbiDotBiz.PromptMeUp` `0.1.8.5` over `0.1.8.4`. Verified healthy current-user registration, the WindowsApps execution alias, matching published and installed `hm.dll` hashes, and the installed executable's version and commit.
- Preserved the signed MSIX, checksum, package metadata, and installation record under ignored `artifacts/msix/debug/0.1.8.5/x64`. Debug application-runtime and solution cleanup passed.
- Automated tests, CLI smoke tests, and interactive UI checks were not run for this installation. Recursive deletion of this build's temporary publish and payload directories was blocked by policy and remains in the task list.

## 2026-09-25 — Test and install the fullscreen refinements Debug build

- Passed all 986 automated tests in Debug from source commit `1f6180d` on `codex/refinements-to-version-1.0-beta`.
- Built a self-contained x64 Debug publish with embedded symbols and a build-time `0.1.8.4` version override. Exported notices for 27 packages, then created, signed, and verified the local MSIX.
- Installed `UmbertoGiacobbiDotBiz.PromptMeUp` `0.1.8.4` directly over `0.1.8.3`. Verified healthy registration, the package checksum, matching published and installed `hm.dll` hashes, and a successful `hm --version` call through the execution alias outside the read-only sandbox.
- Preserved the signed MSIX, checksum, metadata, and installation record under ignored `artifacts/msix/debug/0.1.8.4/x64`. Cleaned the application and test Debug outputs and removed this build's temporary publish and payload directories.
- The initial solution-wide runtime clean failed because the test project's restored assets lack `net10.0/win-x64`; separate app-runtime and test-default cleans succeeded. No interactive fullscreen UI check was performed.

## 2026-09-25 — Refine shared fullscreen screens for the 1.0 beta

- Create checkpoint `72b9403` for the prior banner and installation details, then work on the owner-requested `codex/refinements-to-version-1.0-beta` branch.
- Align F6, Shift+Tab, and Ctrl+Left navigation across fullscreen settings, help, command menus, and memory editing. Add sidebar paging and Home/End navigation to command menus.
- Simplify the shared header with an optional repository link, select compact shortcut rows through the shared footer, and show the current position in overflowing sidebars.
- Share a content heading with a focus marker and optional divider across fullscreen sections. Reserve warning color for the destructive consent choice while showing retention details in the primary text color.
- Make a separate local checkpoint commit for each refinement: `d2000ba`, `ee9c36f`, `cb8057e`, `9f24a6e`, `aa389ff`, `7214250`, and `be72dab`.
- Validation: reviewed scoped diffs and Git whitespace checks. Builds, automated tests, formatters, linters, runtime checks, and installation were not run; the installed Debug package remains the earlier version.

## 2026-09-25 — Share themed product and installation information

- Reuse a banner and localized slogan in About and the first setup page, with artwork colors taken from the active terminal theme and a compact fallback for narrow or non-Unicode terminals.
- Extract the existing installation details and project links into a shared component with an optional full-width card. Enable the card for first-run setup and show the same details without a frame in About.
- Keep aligned label-value rows on wider terminals and stack them when space is limited. Add the slogan and installation title in all six supported languages and adapt the existing setup test fixture to the shared content provider.
- Validation: reviewed the scoped source and diff. No build, tests, formatters, linters, installation, or runtime and visual checks were performed. Changes remain local; the installed Debug package has not been updated.

## 2026-09-24 — Install the settings UI Debug build

- Built and installed signed local Debug x64 MSIX `0.1.8.3` from commit `cfcfce2`, using build-time version overrides and embedded debug symbols.
- After the owner removed the earlier installations, verified healthy package registration, matching published and installed application DLL hashes, and the `hm.exe` alias pointing to this Debug package.
- Preserved the signed MSIX, package metadata, checksum, and installation record under ignored `artifacts/msix/debug/0.1.8.3/x64`.
- Completed Debug solution cleanup and removed this build's temporary publish and payload directories. No automated tests, CLI smoke tests, application launch, or application UI validation were performed.

## 2026-09-24 — Make settings information easier to read

- Split message-collection consent into four titled sections with aligned bullet points, preserving the collection, confirmation, retention, and deletion facts in all six supported languages.
- Add one context-specific emoji to each Privacy heading through the shared icon helper, including its no-emoji fallback.
- Embed the shared About artwork and project details in the settings content pane. Reuse the form's scrolling, adapt the content to the pane width, and preserve the selected About section after saving.
- Update the existing consent-view assertion for the newly separated OpenAI confirmation text.
- Validation: reviewed the scoped source and diff. Builds, tests, formatters, linters, and runtime or visual checks were not run. A local Debug build and focused settings navigation and layout checks remain useful follow-up work. Changes remain local on the owner-authorized branch.

## 2026-09-22 — Unify skills and memory editing

- Reworked Skills into a numbered category sidebar with inline commands, package review, textboxes, and textareas in the central panel.
- Reworked saved and suggested memories into one central editor, removed the separate proposal review screen, and routed proposal commands to that workspace.
- Renamed active Experimental workflow, store, model, view, localization, schema, prompt, and documentation terms to Skills and Memory.
- Limited the system information skill's network output to physical adapters while preserving its JSON contract.
- Passed all 986 automated tests. Built, signed, verified, and installed local Debug x64 MSIX `0.0.1.2211`, updating `0.0.1.2210` in place with healthy registration, a working `hm.exe` alias, and matching packaged/installed DLL hashes.
- Validation: PowerShell syntax, preflight, scoped formatting, XML comments, Release compilation, Release cleanup, and application Debug cleanup passed. Execution policy blocked recursive deletion of this package's temporary publish and payload folders, now tracked in the task list.

## 2026-09-21 — Review private design documentation boundary

- Reviewed tracked design/planning candidates; retained real screenshots and current technical guides. Clarified that mockups, roadmaps and internal product decisions belong outside Git. Documentation checks only; no build or tests.

## 2026-09-21 — Repair direct-mode CI regressions and refresh README branding

- Repair the cross-platform test regressions reported by PR #45: make the countdown testable without a physical terminal, establish the audit session before persisting command evidence, update schema-version expectations, and use strict structured-response fixtures with command text visible in the answer.
- Run the 11 originally failing focused tests successfully. A full local test run did not produce a final result through the local process host; CI remains the authoritative complete-suite check.
- Replace the dense MeUp footer with a compact visual mark and linked product summary. Clarify that `hm` means Help Me and is the entry point to PromptMeUp's features. Add the transparent, generated mark and its provenance.
- Validation: preflight, scoped whitespace normalization, full formatting verification, and XML method comment checks passed. Release compilation passed with zero warnings and errors before test execution; standard Release cleanup completed.

## 2026-09-21 — Refactor reviewed command and conversation workflows

- Create owner-requested checkpoint `4186c22` before changes on `codex/direct-mode`.
- Remove the unused setup wizard and forwarding adapter. Keep one `ISetupView` implementation with shared form pages for fullscreen and scrolling presentation; align its focused view tests.
- Replace duplicate in-memory cost bookkeeping and separate usage/cost reads with one typed session accounting query. Attach command-risk review calls to the owning flow without closing it, and retain provider-confirmed usage independently of execution cancellation.
- Build summaries from current retained context and recorded calls without mutating memory during rendering. Refresh final totals with an independent local-read cancellation budget; warn and mark unavailable totals if refresh fails while preserving the original flow error.
- Prepare bounded redacted command evidence once after the exact local preview and result display. Reuse it for audit and downstream consumers; move result-analysis instructions into the six-language `command-result` YAML prompt.
- Add regressions for interrupted turns, failed final reads, parent review accounting, cancellation during accounting/persistence, and shared sanitized evidence. Automated tests and CLI smoke tests were not executed because execution was not authorized.
- Validation: preflight, full formatting verification, XML method comments, YAML locale/placeholder checks, and Release compilation passed with zero warnings or errors. Complete standard Release cleanup. No post-refactor package build or installation.
- Prepare the owner-requested refactoring commit and branch delivery through a pull request to `main`, including the earlier direct-mode, documentation, artwork, and terminal-polish checkpoints. Keep local packages and installation records excluded from Git.

## 2026-09-21 — Review direct execution and connected workflows

- Review the direct countdown, command workflow, conversation accounting, and setup call sites through bounded source inspection. No automated tests or CLI smoke tests were executed for this review.
- Identify an exit-snapshot cache that can omit later interrupted or failed calls, command-risk reviews recorded outside the conversation totals, an unused legacy setup wizard, and duplicated output preparation in command audit and AI follow-up.
- Report these findings and targeted simplifications without applying refactors. Preserve the requested direct-mode checkpoint and capture the remaining documentation, artwork, and terminal polish in a separate owner-authorized checkpoint. No push or publication.

## 2026-09-21 — Install the direct-mode Debug update

- Build and install signed local Windows x64 Debug MSIX `0.1.5.28`, using the existing current-user certificate. Keep the package identity, application data, certificate trust, and `hm.exe` execution alias unchanged.
- Verify the signature, SHA-256 hash, installed version, and registration status. Preserve the signed package and installation record under ignored `artifacts/msix/direct-20260921-debug`.
- Complete standard Debug cleanup for the solution and the application's `win-x64` output. Record blocked publish/payload cleanup in the active task list.
- No automated tests, CLI smoke tests, application launch, upload, release, push, or tag were performed.

## 2026-09-21 — Add direct execution and quieter conversation views

- Enable direct execution by default for questions and chat, with a saved toggle in setup and a per-session `--direct "request"` override. Schema version 5 stores the preference while preserving local settings and history.
- Share command preview, deterministic and AI risk review, execution, audit, redaction, and output analysis. Direct mode blocks high, critical, unknown, or incomplete reviews and uses a five-second progress countdown: Enter runs now, Escape cancels the chain, and Ctrl+C cancels the application. Multiple alternatives still require selection; automatic chains stop after eight commands.
- Keep session summaries hidden during ordinary work, force them at 80% of the effective context budget, and always show a final snapshot at conversation exit. Keep explicit status requests and visibility preferences available.
- Compact chat input spacing and key hints, replace the textual end marker with a separator, and update the English reference guides and six-language runtime guidance. Record the pre-production/no-compatibility-wrapper policy in AGENTS.md.
- Validation: preflight, formatting verification, XML comment checks, six-language YAML syntax checks, and Release compilation passed with zero warnings or errors. Added focused regression coverage but did not execute automated tests or CLI smoke tests. Cleaned generated Release output. No installation, branch, commit, push, or publishing was performed.

## 2026-09-21 — Separate public documentation from internal planning

- Archive the historical fullscreen UX proposal in the owner's private MeUp notes and repair its reference in this work record.
- Document where internal planning belongs; retain current CLI, architecture, privacy and provider-cost guides publicly.
- Inspect the documentation diff and moved-note links. No builds, tests, commits, pushes or branches were created.


## 2026-09-20 — Normalize the real documentation screenshots

- Use the four owner-supplied screenshots with deterministic Pillow crops, opaque privacy masks, and a shared 2944 × 1792 canvas. Keep retained app pixels at their original size; do not resample, regenerate, or rewrite the UI.
- Put the real command-response example first and add help, chat, and About previews. Remove the superseded raw help asset and four earlier mockup/banner assets; retain the shared MeUp family artwork.
- Visually inspect all four outputs. No application builds, tests, commits, pushes, or branches were created.

## 2026-09-20 — Install the terminal-spacing Debug update

- Built and installed signed local Windows x64 Debug MSIX `0.1.5.27`, including the command-review and farewell-link spacing changes. Package identity, application data, certificate trust, and the `hm.exe` execution alias remain unchanged.
- Verified the package signature and registration status. Preserved the MSIX and installation record under ignored `artifacts/msix/spacing-20260920r2-debug`.
- No automated tests, CLI smoke tests, application launch, public upload, release, commit, or tag were performed.

## 2026-09-20 — Space the farewell project link

- Add one blank row above and below the GitHub link in the farewell banner.
- Validation: preflight, formatting verification, XML comments, and Release build passed with zero warnings or errors. Cleaned Release output. No automated tests or CLI smoke tests were run. Changes remain local.

## 2026-09-20 — Space command risk and output icons

- Add one blank line below the command risk score, before the AI or local review label.
- Use explicit emoji presentation for warning, stopwatch, and scissors icons in the shared helper, retaining one trailing space so command-output labels remain separated.
- Validation: preflight, formatting verification, XML comments, and Release build passed with zero warnings or errors. Cleaned Release output. Automated tests and CLI smoke tests were not run; terminal appearance has not been verified in the installed app. Changes remain local.

## 2026-09-20 — Replace README mockups with a real screenshot

- Use the owner's original Italian help-screen capture from PromptMeUp 0.1.5 as the opening product image, with a full-resolution link and an accurate caption. Copy the PNG unchanged and record its provenance.
- Remove the generated command-review mockup from the README; retain earlier artwork files and credits. Close the pending real-screenshot task. Other supplied captures containing local machine or user context remain outside the repository.
- Documentation and image copy only; no builds, tests, commits, pushes, or branches were created.

## 2026-09-20 — Put setup before detailed feature guidance

- Reorder the README around the product description, labeled visual example, setup, and command/privacy limits. Keep feature details and project history later.
- A real screenshot remains pending in todo.md; existing generated illustrations stay explicitly labeled. No builds, tests, commits, pushes, or branches were created for this documentation update.

## 2026-09-20 — Apply the shared MeUp presentation

- Shorten the README headline, keep the product purpose first, and add the shared product links and author signature.
- Add the coordinated concept illustration and its exact generation prompt, provenance, and visual style guide.
- Changes remain local. No build, test, formatter, linter, commit, push, or branch creation was performed for this update.

## 2026-09-20 — Clarify product documentation and author voice

- Put the app's purpose in the first README headline. Simplify contributor guidance and use the solo maintainer's voice in project, support, and security documentation.
- Save the product writing preferences in AGENTS.md, including plain English, concrete benefits, and first-person singular author wording.
- Documentation changes only. No builds, tests, formatters, linters, or CI were run.

This archive tracks completed development tasks for reference and review.

## 2026-09-20 — Install the emoji-spacing and banner Debug update

- Published source commit `ef06355` for Windows x64 in Debug, including the emoji-spacing fixes and localized welcome/farewell copy. Signed local MSIX `0.1.5.25` with the existing current-user certificate and updated the installed app from `0.1.5.24`, preserving package identity, application data, settings, certificate trust, and PATH.
- Verified Debug assembly metadata and source revision, a valid package signature, healthy registration, all 320 installed payload file hashes, and the `hm.exe` execution alias pointing to the new version. No running app needed to be closed.
- Preserved the signed package and installation verification record under ignored `artifacts/msix/banner-ef06355-debug`. Cleaned matching Debug/runtime output; recursive deletion of temporary publish and payload directories was blocked by automatic approval review and remains tracked separately.

Validation: Debug publication, package signing, installation, and payload verification passed. No automated tests, CLI smoke tests, application launch, live AI calls, public uploads, or release tags were requested or performed.

## 2026-09-19 — Refine terminal emoji spacing and welcome/farewell banners

- Explicitly select emoji presentation for the information, settings, and safety symbols in the shared icon helper, keeping the existing single trailing space and ASCII fallbacks. This covers notices, active-skill messages, and help navigation without changing translations.
- Move the farewell wave from the product divider to the localized thank-you line.
- Rewrite the opening headline and tagline in all six languages to introduce hm as help when a Git, Bash, or PowerShell command does not come to mind.

Validation: repository preflight, formatting verification, XML summaries, and Release compilation passed with zero warnings or errors. Cleaned Release output. Local automated tests and CLI smoke tests were not run; no package was rebuilt or installed.

## 2026-09-18 — Install the chat layout and skill identity Debug update

- Built source commit `68142c7` for Windows x64 in Debug, packaged local MSIX `0.1.5.24` with the existing current-user certificate, and updated the installed app from `0.1.5.23`. Preserved application data, settings, certificate trust, and PATH.
- Verified Debug assembly metadata and source revision, the package signature, healthy registration, all 321 installed payload file hashes, and the `hm.exe` execution alias pointing to the new version. No running app needed to be closed.
- Preserved the signed package and installation verification record under ignored `artifacts/msix/chat-68142c7-debug`. Cleaned Debug/runtime output after restoring the test project's missing runtime assets. Recursive deletion of the temporary publish and payload directories was blocked by automatic approval review and remains tracked separately.

Validation: Debug publication and signature verification succeeded; the installed payload matches the signed package. After installation, the user requested automated tests: all 933 tests passed against the same source in Release, with zero failures or skipped tests. The warning-free Release build and subsequent cleanup succeeded; all GitHub checks for source commit `68142c7` also passed. No application launch, live AI calls, public uploads, or release tags were made.

## 2026-09-18 — Refine chat layout and identify active skills

- Removed opening navigation, invocation, and startup snapshot headings. Aligned the current directory and startup settings with the tagline, with one blank line above and below the directory. Replaced the chat introduction with an indented, six-language command heading.
- Added WhiteSmoke keyboard guidance using key symbols, localized text for `--no-emoji`, and a blank line before input. Kept the sender's icon and name beside the submitted message, and ended assistant responses with a localized white marker separated by blank lines.
- Removed redundant INFO text. Added individual icons and readable colors to all ten bundled skill packages and their versioned prompt YAML metadata; active skill names use those identities. Older imports keep defaults, invalid display metadata is rejected, and theme contrast is preserved. Package changes still require fresh approval.
- Extended package validation coverage for malformed display metadata, approval invalidation, and matching bundled prompt metadata. Documented the metadata contract.

Validation: repository preflight, formatting verification, XML summaries, YAML syntax and metadata consistency for all ten skills, and Release compilation passed with zero warnings or errors. Automated tests and CLI smoke tests were not run. Cleaned Release build output; no package was rebuilt or installed.

## 2026-09-18 — Install the Settings and chat UI Debug update

- Clarified the repository workflow: public release artifacts remain GitHub Actions-only, while an explicitly requested signed Debug MSIX may be built and installed locally from ignored `artifacts/msix` without publishing, tagging, or uploading it.
- Published source commit `fdd6de7` for Windows x64, created the requested signed local Debug MSIX `0.1.5.23`, and updated the current-user package from `0.1.5.22` without uninstalling or changing application data, certificate trust, or `PATH`.
- Verified the signature, package registration, `hm.exe` app-execution alias, and matching SHA-256 hashes for the packaged and installed application DLL. Preserved the package and metadata under ignored `artifacts/msix/chat-fdd6de7-debug`.

Validation: preflight, formatting verification, XML summaries, and a warning-free Release build passed. Automated tests were not run. The Debug build output was cleaned after installation; no application launch or live OpenAI call was made.

## 2026-09-18 — Remove repeated skill descriptions in Settings

- Omit the metadata summary only when it exactly matches the opening instruction paragraph, allowing an initial heading and blank lines. Keep distinct descriptions, complete literal instructions, scripts, package metadata, and approval fingerprints unchanged.
- Added regressions for bundled and imported skills, newline variants, longer or differently cased introductions, later examples, and quoted text. Reused the existing Settings layout without editing skill packages or translations.

Validation: preflight, formatting verification, XML summaries, and Release build passed with zero warnings or errors. All 57 targeted Settings view, viewport, and save tests passed. Cleaned Release output. The installed Debug MSIX remains unchanged.

## 2026-09-18 — Install the global-memory Debug update

- Published source commit `ff6f968` in Debug for Windows x64 and packaged local test MSIX `0.1.5.22` with the existing trusted certificate. Updated the current-user installation from `0.1.5.21` without uninstalling or changing app data, settings, certificate trust, PATH, or fonts.
- Verified Debug assembly metadata, the source revision, the package signature, all 320 installed payload hashes, and the `hm` execution alias pointing to the new version. Staged safely while the app was open, then completed registration after the user closed it; no process was terminated.
- Preserved the signed MSIX and verification record under ignored `artifacts/msix/memory-ff6f968-debug`. Cleaned matching Debug/runtime build output. No application launch or live OpenAI calls; the 915 source tests had passed before packaging.

## 2026-09-18 — Use one saved-memory collection and explain feature commands

- Made saved memories global across conversations using the same local data folder. SQLite schema 4 preserves every old note, identifier, timestamp, duplicate, and provenance record; collections above the new-note limit remain accessible and editable. Old scope prefixes are accepted only as compatibility aliases.
- Removed scope controls and labels from saved-note views, command help, and proposal review. New notes and approved suggestions always save globally. Existing project-scoped collection consent, skill approvals, and reminders are unchanged.
- Replaced generic Skills and memory help labels with short purpose descriptions in all six languages. Updated the built-in guide, runtime memory prompts, README, CLI reference, architecture, and privacy documentation without changing the existing UI style.
- Added migration, rollback, global command, view, bounded reflection, and duplicate-cleanup regressions. Deleting a saved note still requires the existing confirmation and clears collected evidence and suggestions; stale pre-migration proposals require fresh review.

Validation: preflight, formatting verification, XML summaries, and the final Release build passed with zero warnings or errors. All 915 tests passed against the final source and packaged prompts. Cleaned Release build output. No real user data was migrated during validation, no live OpenAI calls were made, and no MSIX was rebuilt or installed.

## 2026-09-18 — Install the inline Settings Debug update

- Published source commit `3d1b2c5` locally in Debug for Windows x64 and packaged the requested test MSIX `0.1.5.21` with the existing trusted certificate. Updated the current-user installation from `0.1.5.20` without uninstalling, changing certificate trust, or modifying app data.
- Verified the package signature, healthy installed status, hashes of all 320 payload files, and the `hm` execution alias pointing to the new version. No app process needed to be stopped; the app was left closed for the user's test.
- Preserved the signed MSIX and verification record under ignored `artifacts/msix/settings-3d1b2c5-debug`. Cleaned matching Debug/runtime build output. No source changes or live OpenAI calls; the 875 source tests and 33 final guide checks had already passed before packaging.

## 2026-09-18 — Make Skills and Memory real Settings tabs

- Replaced the separate preference menus with fields in the existing Settings form. Skills, Memory, and ordinary preferences share Save and Cancel; saved-note editing remains explicitly separate. Reused the existing layout, navigation, action bar, palette, and emoji helper.
- Renamed Learning to Memory and the note manager to Saved memories in all six languages. Added readable bundled skill names and shorter privacy explanations; removed experiment terminology from user-facing copy and the chat guide.
- Kept full skill instructions and scripts available in a scrollable preview. Collection and deletion require separate consent. Project preferences and skill approvals save together, reject stale drafts and changed packages, and validate the submitted language before activation. Reactivation cannot skip unreadable or omitted approved packages.
- Updated the quick start, CLI and privacy documentation, and nine versioned chat-guide chapters. Added draft, consent, activation, and viewport regressions without provider calls or real desktop actions.

Validation: preflight, formatting verification, and XML summaries passed. Release builds completed with zero warnings or errors; all 875 tests passed. After the final guide wording edit, all 33 guide/catalog tests passed again. Reviewed captured 60×20 and 100×32 terminal frames and cleaned Release output. No MSIX was rebuilt or installed for this change; main and release versions remain unchanged.

## 2026-09-18 — Install the Settings integration as a local Debug MSIX

- Built current experimental commit `b2f654a` locally in Debug for Windows x64 and created the requested self-contained MSIX test package `0.1.5.20`. Kept the installed package identity and existing trusted signing certificate; no release workflow or public distribution was used.
- Installed the current-user update from `0.1.5.19`. Verified the signature, healthy Windows package status, hashes for all 320 staged files, and the `hm` alias target. Did not change certificate trust, PATH, or user settings/data.
- Preserved the signed package and verification material under ignored `artifacts/msix/settings-b2f654a-debug`. Cleaned Debug build output with the matching runtime and publish path. No live OpenAI calls or application actions were run; the 842 source tests had passed before packaging.

## 2026-09-18 — Connect skills and learning to Settings

- Added Skills and Learning navigation beside the existing Memories manager, plus a read-only Privacy section. General and feature pages show refreshed, local project status without activating features or running maintenance.
- Reused existing approval menus with saved AI settings and credentials. Child-menu changes are immediate and independent of the parent draft; six-language notices explain this distinction. Returning, cancelling a child menu, or encountering a recoverable action error preserves unsaved settings.
- Kept Settings accessible when an imported skill is invalid. Unknown counts are marked unavailable, with a visible warning and sanitized diagnostics; actual feature preferences remain visible.
- Updated the short quick start, CLI reference, and four six-language chat-guide chapters. Added 32 regression cases for passive rendering, refresh, cancellation, invalid catalogs/actions, local-only status reads, and privacy copy.

Validation: preflight, XML summaries, formatting verification, a warning-free Release build, and all 842 tests passed. Navigation used simulated terminal input and disposable local data, without live OpenAI requests or desktop actions. A separate CLI smoke invocation was not run because its combined temporary-directory cleanup command was rejected. Cleaned Release output. Changes remain on the experimental branch and PR #43; main and release versions are unchanged.

## 2026-09-18 — Add an optional preferred name

- Added an optional name or nickname to Personalization and the setup wizard, with six-language help explaining local storage and sharing with OpenAI. Existing preferences remain unchanged; the field can be edited or cleared.
- Added bounded Unicode normalization, credential/control rejection, and an idempotent SQLite upgrade with an empty default. Names stay out of ordinary logs and session metadata.
- Included the name as escaped literal data through a versioned, six-language prompt only for questions and chat. Empty names are omitted; command review, classifiers, Dream, heartbeat, and connection tests do not receive the preference. Context estimates and cache keys account for the same generated instructions.
- Updated the settings and privacy guide chapters and short CLI/privacy documentation. Added 50 regression cases for UI edits and clearing, migration, validation, provider requests, localization, caching, and context budgets.

Validation: preflight, formatting verification, XML summaries, a warning-free Release build, and all 810 tests passed. Tests used simulated terminal input and provider responses; no live OpenAI call, real credential, or desktop action was used. Cleaned Release output. Changes remain on the experimental branch and PR #43.

## 2026-09-18 — Fix experimental consent and skill review findings

- Reject stale settings snapshots inside the save transaction. Changing a reminder or automatic selection in an older window cannot restore revoked capture consent or erase newer evidence.
- Budget the complete serialized, localized skill context. Reject oversized activation and manual selection with a six-language explanation; warn about incompatible older selections without blocking answers.
- Enforce the 64-package local import limit before moving a reviewed package. Serialize concurrent imports with a persistent file lock and preserve rejected staging content.
- Added 32 regression cases covering consent conflicts, escaping, localization, query envelopes, preserved selection, capacity, and competing imports. Kept the simple quick start current.

Validation: preflight, formatting verification, XML summaries, a warning-free Release build, and all 760 tests passed. Synthetic provider replies and disposable local data only; no live API calls, desktop actions, or local release packaging. Cleaned Release output. Changes remain on the existing experimental branch and draft PR #43.

## 2026-09-18 — Explain experimental features through the chat guide

- Added five concise, six-language guide chapters: skills, skill-actions, learning, reflection, and reminders. Documented exact activation steps, examples, privacy limits, and the distinction between manual memories, learning capture, heartbeat hints, and dated reminders.
- Extended the packaged topic allowlist and the versioned chat/query guidance. Natural-language questions can request the new chapters through the existing schema and reader; the two-chapter, 3,000-token limit remains unchanged.
- Clarified that activation means enabling features in the app, not publishing online. Guide answers do not change settings, approve memories, run skills, or create reminders.
- Added short example questions to the CLI reference and quick start. Added synthetic query/chat retrieval coverage for all six languages and checks that guide requests leave experiment settings unchanged.

Validation: preflight, XML summaries, formatting, a warning-free Release build, and all 728 tests passed. The production YAML reader validated the new chapters, and every chapter pair fits the guide allowance in all six languages. Provider responses were simulated; no live OpenAI request or feature action ran. Cleaned Release output before handoff. Changes stay on the existing experimental branch and draft PR #43.

## 2026-09-18 — Complete the skill catalog and simplify the user guide

- Added the seven remaining supported CLI-Intelligence skills: clipboard, screenshot, system_info, http_request, web_search, timezone_convert, and set_reminder. The catalog now has ten skills; metals-dev-monitor remains excluded. Kept PromptMeUp's interface and OpenAI integration.
- Routed snapshot scripts through existing risk scoring and explicit approval. Limited Windows captures to new local PNGs; kept clipboard reads bounded, system summaries privacy-conscious, and HTTPS requests public-only with checked connections and no redirects or proxy. Web search supplies DuckDuckGo instant answers, not a full crawler.
- Added bounded, project-local reminders with reviewed times and notes, transactional activation checks, delivery at chat prompts, and rollback on display failure. No background process or automatic AI call.
- Added six-language runtime guidance and a shared exact release-file allowlist. Replaced the long experimental report with a short how-to for skills, memories, Dream, and heartbeat, including optional AI command-review disclosure.

Validation: preflight, formatting, XML summaries, all twelve relevant PowerShell syntax checks, exact seventeen-file payload validation, and a warning-free Release build passed. All 719 tests passed, including offline embedded-C# compilation, public-address guards, time zones, catalog/localization, reminder persistence and failed-display rollback. No real network skill requests, clipboard access, screen captures, or OpenAI calls were made. Cleaned Release output; no local release artifacts were produced. Changes remain on the experimental branch and draft PR #43.

## 2026-09-18 — Experimental skills and reviewed memory learning

- Implemented the eight-step roadmap on `codex/experimental-memory-skills`; opened [draft PR #43](https://github.com/umbertotechnopreneur/PromptMeUp/pull/43), assigned to the repository owner with enhancement and documentation labels. Kept `main` unchanged.
- Added opt-in skill activation/import, contextual selection, and approved Git/filesystem/concat-files actions. Explicitly excluded metals-dev-monitor and kept the existing command safety boundary.
- Added opt-in observations, provenance, transactional proposal review, Dream reflection across sessions, and manually invoked heartbeat with an optional local reminder. No background agent or automatic memory approval.
- Preserved OpenAI, PromptMeUp views, all six interface/prompt languages, credential filtering, retention bounds, and revision guards against stale evidence or in-flight resurrection.
- Recorded checkpoints in commits `1a0ae82`, `50ed970`, and `6a54bf7`; documented consent, permanent evidence purges, existing audit retention, supported import constraints, and the intentionally limited initial catalog.

Validation: preflight, format verification, XML summaries, Release build (zero warnings/errors), YAML syntax/six-language fields, prompt context allowances, and PowerShell syntax passed. The initial checkpoints compiled tests without executing them; after the user explicitly authorized testing, all 660 local tests passed. No live AI requests or skill scripts ran. Cleaned build output; no local release artifacts were produced.

CI follow-up: corrected macOS catalog and concat-files handling for only the verified `/var`, `/tmp`, and `/etc` system aliases, retaining arbitrary-link rejection. Added alias and linked-ancestor regression cases. Updated portable, installer, MSIX, and legacy staging scripts to include only the exact four bundled skill files. The first CI run had passed all 657 then-current tests on Windows and Linux; macOS alias handling and Windows installer payload validation identified these follow-up changes. Final packaging remains validated through GitHub Actions, not local release builds.

## 2026-09-18 — Reduce agent context and redundant verification

- Consolidated repository rules in `AGENTS.md` and replaced duplicate Copilot instructions with a reference, preserving unique safety and product requirements.
- Added scoped context reading, bounded output, concise reporting, and explicit-only delegation rules.
- Made non-test checks proportional to the changed files and cleanup conditional on builds performed during the task. Documentation-only changes do not trigger build or runtime verification.

Validation: no builds, automated tests, formatters, or smoke checks were run for these instruction-only changes.

## 2026-09-17 — Restyle command-menu numbers

- Render command-menu numbers in the shared warm ochre warning color, without bold, followed immediately by `>`.
- Preserve existing numbering, keyboard shortcuts, and action labels.

Validation: preflight, restore, formatting verification, XML comments, and a Release build passed with zero warnings or errors. Cleaned build output. No automated tests or CLI smoke checks were run.

## 2026-09-18 — Prepare memory and chat changes for protected merges

- Integrated current main into the memory and chat branches, preserving both task histories and the updated dependencies. Moved the derived-JSON-exception assertion correction into the memory branch so it validates independently.
- Made wrapping assertions ignore terminal ANSI decoration, matching the existing rendering assertions and the CI environment while retaining content and width checks.
- Included the requested local README and MSIX script changes: maintainers can create local Windows test packages with an existing trusted certificate; distributed releases still use GitHub Actions.

Validation: repository preflight, restore, formatting verification, XML comments, Release builds with warnings as errors, and PowerShell syntax validation passed. Build output was cleaned. Protected merges remain conditional on all required GitHub checks and resolved conversations.

## 2026-09-18 — Support Shift+Enter in chat

- Preserved Shift+Enter through Windows virtual-terminal input by enabling Windows keyboard records during editing and decoding modifiers, repeats, and key releases before paste handling. Also recognized CSI-u and xterm modified Enter sequences.
- Kept plain Enter as submission and Shift+Enter as a bounded newline at the current caret. Raw and Windows-encoded paste boundaries remain separate from keyboard actions.
- Added the shortcut to the full and compact input hints in all six languages. The compact guide stays above subsequent prompts; the footer is reserved for multiline position, limits, or errors.
- Added regression coverage for modifier preservation, encoded and raw pastes, Escape, repeated keys, and newline insertion at the character limit.

Validation: preflight, restore, formatting verification, XML comments, Release build with warnings as errors, and all 586 automated tests passed. Build artifacts were cleaned. Physical keyboard validation and a new MSIX installation were not performed for this follow-up.

## 2026-09-18 — Test and install the chat readability update

- Ran the complete automated suite and corrected two test assumptions: malformed JSON can throw a derived JSON exception, and the scripted shell must provide rendering options for local success confirmations.
- Added narrow- and wide-terminal coverage to verify that wrapping preserves words and Unicode graphemes, with identical static and animated text. All 578 tests passed.
- Built and installed the user-requested local x64 MSIX `0.1.5.18` from UI commit `108dd19`, preserving the installed menu-number style from `8df509a`. Verified the trusted package signature, Windows package status, the `hm` execution alias, and hashes for all 279 published files.

Validation: preflight, restore, formatting verification, XML comments, Release build with warnings as errors, all 578 tests, Windows publish, dependency notice export, signature validation, and installed-file integrity passed. Standard .NET cleanup succeeded. Automatic approval review blocked recursive removal of temporary package-preparation directories; cleanup remains in the task list. The signed MSIX and verification records are preserved under ignored artifacts.

## 2026-09-18 — Improve terminal chat readability

- Wrapped conversation prose in an indented column capped at 108 cells, using Spectre's styled text rendering for both static and animated output. Kept fenced code literal and preserved terminal scrollback.
- Removed filled backgrounds from inline commands, tightened speaker spacing, and collapsed repeated blank paragraphs. Local display-setting confirmations now use short emphasized states and a shared success icon.
- Made the input viewport grow with its content, up to six rows. Show the full editing guide only at the first prompt and the full character counter only from 80% of the limit.
- Updated all six languages and versioned chat/query prompts to favor concise, outcome-first answers with selective emphasis. Adjusted existing expectations for prompt versions and inline-code spacing.

Validation: preflight, restore, formatting verification, XML comments, and a Release build with warnings as errors passed. Build artifacts were cleaned. Automated tests, CLI smoke checks, and interactive terminal validation were not run.

## 2026-09-17 — Install the memory commands in a local MSIX update

- Built the user-requested local x64 MSIX `0.1.5.17` from memory-command commit `1de3d17`, including the command-menu number style from `8df509a` in an isolated source snapshot. Kept the existing package identity and trusted current-user signing certificate.
- Installed the update and verified Windows package status, the `hm` execution alias, and matching hashes for the application assembly and updated memory/chat prompts. Preserved the signed MSIX and installation evidence under ignored artifacts.

Validation: preflight, restore, formatting, XML comments, Release build, self-contained Windows publish, exported dependency notices, package signature, and installed-file integrity passed. Standard runtime and solution cleanup succeeded. Automated tests, CLI smoke checks, and live AI calls were not run. Automatic approval review blocked removal of the temporary package-preparation directories; their cleanup remains in the task list.

## 2026-09-17 — Add direct memory commands and confirmed numbered deletion

- Added `hm --remember [global|project] <text>` for local persistence and `hm --forget <id or description>` for deleting an accessible saved note. Direct `/remember` and `/forget` invocations route to these workflows instead of AI chat.
- Resolve IDs and exact note text locally. For descriptions, use the existing audited AI service and a six-language structured prompt to search batches of up to eight global and current-project notes; validate every returned identifier before offering matches.
- Show normal-weight warm ochre `1>` choices with immediate digit selection, paging, cancellation, and a separate deletion confirmation. Require an interactive terminal and reject deletion when the reviewed note changed during lookup or confirmation.
- Updated help and versioned prompts in all six languages, documented provider sharing and costs, and added regression cases for parsing, persistence, batch matching, invalid responses, cancellation, concurrent changes, and numbered selection.

Validation: preflight, restore, formatting verification, XML comments, and a Release build with warnings as errors passed. Build output was cleaned. Regression tests were added and compiled but not executed; no CLI smoke checks or live AI calls were run.

## 2026-09-16 — Publish prerelease 0.1.8 with portable archives and Windows installers

- Merged PR #35 after all required checks, including Windows installer compilation, passed. Verified the resulting main checks and pushed annotated tag `v0.1.8` at `142b519`.
- Release workflow run `35083274379` completed successfully. The published prerelease includes Windows ZIPs and Linux/macOS tar.gz archives for x64 and ARM64, unsigned Windows EXE installers for x64 and ARM64, and `SHA256SUMS.txt`.
- Verified all eight package checksums against GitHub asset digests, verified the downloaded checksum manifest digest, and confirmed successful provenance attestation. Published the generated draft as a prerelease, without marking it as the latest stable release.
- Confirmed the public release at https://github.com/umbertotechnopreneur/PromptMeUp/releases/tag/v0.1.8 with all nine assets uploaded, `draft=false`, and `prerelease=true`. The original `v0.1.7` tag remains unchanged and has no published release.

Validation: required local non-test checks passed; GitHub quality checks, native CLI smoke checks, all six packaging jobs, installer compilation, checksum generation, and attestations passed. Cross-published packages were not executed on unmatched runners, and neither EXE installer was installed or run. Standard local cleanup completed after commits and pushes. Updated this task record locally after publication.

## 2026-09-16 — Run the first tagged release and correct the installer compiler check

- Merged PR #34 after all required checks passed, verified the resulting main checks, and pushed annotated tag `v0.1.7` at `d73fc50`.
- Release run `35081567930` passed the quality checks and built the four Linux and macOS archives. Both Windows jobs rejected `ISCC.exe` metadata version `0.0.0.0` before installer compilation, so no release draft was created.
- Moved the supported Inno Setup version check into the installer preprocessor, where `Ver` identifies the loaded compiler. Added native Windows installer compilation to the quality workflow and cleanup after its builds.
- Increased the product version to `0.1.8` for the corrected release while preserving the published `v0.1.7` tag.

Validation: the first release's quality checks and native CLI smoke checks passed on GitHub. The correction passed local preflight, restore, formatting verification, XML comments, a Release build with zero warnings or errors, cleanup, PowerShell syntax parsing, actionlint, and whitespace checks. The corrected installer compilation and new release remain pending their workflow runs. No local automated tests, CLI smoke checks, or installer executions were run.

## 2026-09-16 — Prepare manual release drafts with x64 and ARM64 installers

- Added a manual `draft` mode to the release workflow and retained `rehearsal` for artifact-only runs. The draft uses the verified main commit and product version; tag-triggered drafts remain supported. Existing tags and releases are never overwritten, and public publication stays a separate maintainer action.
- Added unsigned, per-user Windows EXE installers for x64 and ARM64 using Inno Setup 6. The builder validates native architecture, product version, notices, and allowed payload files. The installer protects existing unmanaged folders and newer versions, adds only a user PATH entry, and removes only its owned entry on uninstall.
- Included both installers with the six portable archives, complete checksums, and build attestations. Added matching runtime cleanup to packaging jobs and documented the exact GitHub CLI command, browser trigger, unsigned status, and final publishing step.

Validation: preflight, restore, formatting verification, XML method comments, Release build with zero warnings or errors, standard .NET cleanup, actionlint, PowerShell syntax parsing, whitespace checks, and independent static review passed. No automated tests, CLI smoke checks, workflow runs, or installer executions were performed. Automatic approval review rejected the command to prepare the portable Inno compiler with "blocked by policy", so EXE compilation remains unverified locally. Prepared on an authorized branch; pull-request review and merge remain pending.

## 2026-09-16 — Pull main and install version 0.1.7

- Fast-forwarded `main` to `ac43fae`, restored the existing repository-rule changes, and resolved the task-archive conflict by retaining both incoming and local entries.
- Increased the product version from `0.1.5` to `0.1.7`, above the locally installed `0.1.6`. Built, signed, and installed Windows x64 MSIX `0.1.7.0` with the existing package identity and trusted local certificate.
- Verified healthy package registration, the executable version, SHA-256 equality for all 283 installed payload files, and the execution alias targeting the new package.
- Confirmed that the GitHub release workflow currently builds portable archives only. The separate MSIX builder supports x64 and ARM64; the MSI builder supports x64 only. Public MSIX distribution still needs a signing identity trusted by recipients and release assets to be published.

Validation: preflight, restore, formatting verification, XML method comments, Release build and Windows publish with zero warnings or errors, package signature and installation integrity, standard .NET cleanup, and whitespace checks passed. Runtime cleanup was scoped to the application project because the test project has no runtime-specific restore target. Automated tests, CLI smoke checks, and live AI calls were not run; installation verification did not launch the application. Kept the installer and verification evidence under ignored artifacts. Automatic approval review blocked recursive removal of the remaining temporary directories, which are still present. No changes were committed or published.

## 2026-09-16 — Preserve multiline paste, number command choices, and install the update

- Added a bounded multiline editor for chat and interactive diagnostics. Bracketed paste keeps normalized line breaks, blank lines, and trailing whitespace as editable text until a separate Enter submits it. Oversized insertions preserve the existing draft, and cursor movement and deletion respect Unicode text elements.
- Enabled and restored Windows virtual-terminal input around the prompt, retained cancellation, and limited redraws to owned input rows without clearing terminal history. Documented the bracketed-paste terminal requirement and the diagnostic file-input alternative.
- Numbered command-menu choices from zero. Up to ten entries accept a digit immediately; larger menus require Enter. Empty AI suggestions return to an ongoing chat prompt or offer Finish/Continue after a single question. Command selection still opens the existing preview and authorization flow.
- Added decoder and editing regression sources, and prevented CI environment enrichment from overriding explicit ANSI settings in context-bar test fixtures. Preserved unrelated local source comments.
- Built, signed, and installed MSIX revision `0.1.5.15`. Verified healthy registration, all 282 installed payload hashes, and the `hm` execution alias. Prepared the changes for the existing pull request.

Validation: preflight, restore, formatting verification, XML method comment checks, Release build and Windows publish, package signature verification, installed-file integrity, independent code review, and whitespace checks passed. The build reported zero warnings and errors. Automated tests, CLI smoke checks, and live AI calls were not run locally; installation verification did not launch the application.

## 2026-09-16 — Control chat visibility through natural language

- Added an isolated, strictly parsed LLM intent call for directly typed chat messages, with six-language instructions and local confirmations for independent session-summary and command-suggestion visibility changes.
- Kept preferences within the current chat, retained them across `/clear`, and allowed `/status` and `/context` to display the summary once. Hidden suggestions never bypass the exact preview, risk assessment, and authorization required by `/run`.
- Counted the classification call in session usage and turn costs, including guide-assisted answers. Recalled notes, previous messages, and command output cannot enter the display classifier. Mixed requests apply changes after the answer succeeds.
- Updated chat guidance and documentation, added parser and workflow regression sources, and adapted existing accounting checks for the extra call. Preserved the two unrelated local code comments.

Validation: repository preflight, restore, formatting verification, XML method comment checks, Release build with zero warnings or errors, and whitespace review passed. Automated tests, CLI smoke checks, and live AI calls were not run.

## 2026-09-15 — Record pull-request ownership and labels

- Aligned `AGENTS.md` and `.github/copilot-instructions.md`: every newly created pull request must be assigned to `umbertotechnopreneur` and carry existing repository labels appropriate to its final scope.
- Prepared the app-guide, context-bar, accounting, and documentation changes for a dedicated pull request after confirming that the open dependency-update pull requests are unrelated. Kept the two pre-existing local code comments outside the commit scope.

Validation: repository preflight, restore, formatting verification, XML method comments, Release build with warnings as errors, whitespace checks, and an independent scope review passed. Automated tests and CLI smoke checks were not run.

## 2026-09-15 — Load the app guide through conversation and show context by role

- Added eight versioned guide chapters in all six languages. Chat version 8 and query version 7 can request up to two relevant chapters through a strict response contract when a natural-language question needs app details. Each turn permits one additional AI call, with local topic validation and budget checks before sending the guide.
- Retained loaded chapters as system context for follow-up questions, replacing them when needed and clearing them with `/clear`. Detailed settings procedures remain in the guide. Each provider call records its own usage and cost; turn totals include both calls, and incomplete pricing makes aggregate costs unavailable.
- Split estimated active context into system, user messages, and retained AI answers. Added a colored budget bar, numerical legend, free capacity, and an explicit guide count already included in system tokens, with readable symbols for terminals without color.
- Added focused regression sources for guide loading, response contracts, request payloads, workflow reuse and clearing, call accounting, session costs, and bar rendering. Preserved the unrelated local comments.

Validation: preflight, restore, formatting verification, XML method comment checks, and Release build passed with zero warnings or errors. Static YAML inspection confirmed 48 localized guide sections and matching built copies. Automated tests, CLI smoke checks, and live AI calls were not run. The installed MSIX remains unchanged.

## 2026-09-15 — Keep base application context focused on capabilities

- Replaced detailed context-budget settings procedures in all six chat and query translations with an overview of diagnostics, scripts, plans, previews, recipes, saved notes, usage, and customization.
- Preserved the host-app identity and existing safety rules, and clarified that the model only receives supplied material and cannot independently inspect local files, settings, or full history. Increased the chat prompt to version 7 and the query prompt to version 6, with matching existing version assertions and documentation.
- Assessed a local, versioned guide split by topic for future on-demand assistance. Guide retrieval remains a design proposal and was not implemented.

Validation: preflight, restore, formatting verification, XML comment checks, Release build with zero warnings or errors, YAML parsing and six-language checks, independent content review, and whitespace checks passed. Automated tests and CLI smoke checks were not run. The installed MSIX remains unchanged.

## 2026-09-15 — Give chat and query prompts application context

- Added two aligned paragraphs in all six languages identifying PromptMeUp as the host application and explaining its settings, operating context budget, environment override, and chat usage controls. References to "this app" now have product context unless the conversation identifies another program.
- Kept interactive settings navigation in the answer with no commands offered for chat execution. Preserved the existing console scope, clarification, safety, preference, and JSON instructions, and avoided treating default settings as current user values.
- Increased the chat prompt to version 6 and the query prompt to version 5, updated existing version assertions, and documented the added context. Preserved unrelated local comments and the earlier installation record.

Validation: preflight, restore, formatting verification, XML comment checks, Release build with zero warnings or errors, YAML parsing and six-language checks, independent content review, and whitespace checks passed. Automated tests and CLI smoke checks were not run. The installed MSIX remains unchanged.

## 2026-09-15 — Build and install the current main revision

- Published `main` revision `a3a3ce7` with the existing uncommitted source comments as a self-contained Windows x64 application, including redistribution notices and source-build metadata.
- Created, signed, and installed MSIX revision `0.1.5.14` with the existing package identity and trusted certificate. Verified healthy registration, matching hashes for all 274 packaged and installed payload files, and the `hm` execution alias targeting the updated installation.

Validation: preflight, restore, formatting verification, XML comment checks, Release build and Windows publish with zero warnings or errors, package manifest and signature verification, checksum and installed-file integrity, and whitespace checks passed. Automated tests and CLI smoke checks were not run; installation verification did not launch the application.

## 2026-09-14 — Prepare memory management and build details for review

- Prepared the local memory manager, chat reminders, compile-time About metadata, regression coverage, and product documentation for a focused pull request. Preserved unrelated local source comments and excluded generated installer artifacts.
- Reviewed memory scope isolation, transactional updates, credential rejection, deletion confirmation, and draft preservation with no blocking findings.

Validation: preflight, restore, formatting verification, XML comment checks, Release build with zero warnings or errors, and whitespace checks passed. The same implementation previously passed all 424 automated tests and signed-MSIX installation verification; tests were not repeated for this review preparation.

## 2026-09-14 — Show compile-time build details and install the updated MSIX

- Added the build date and time in UTC and the build machine to About in all six languages. A shared MSBuild target embeds the values during compilation; a metadata reader supplies the passive view without reading the runtime machine or clock.
- Documented repeatable-build overrides and rejected malformed dates, non-UTC offsets, impossible calendar dates, and disabled assembly metadata generation before compilation.
- Published the current local changes, created and signed MSIX revision `0.1.5.13`, and installed it with the existing identity and trusted certificate. Verified healthy registration, matching hashes for all 274 payload files, the installed assembly metadata, and the updated `hm` execution-alias target.

Validation: preflight, restore, formatting verification, XML comment checks, Release build and Windows publish with zero warnings or errors, all 424 automated tests, six focused MSBuild metadata checks, manifest and signature verification, installed-file integrity, and whitespace checks passed. Installation verification read the installed files without launching the application.

## 2026-09-14 — Test and build the current local changes

- Ran the full automated test suite at the user's request. All 424 tests passed, including the new memory-management coverage, in about 28 seconds.
- Completed the final Release build with warnings treated as errors and preserved the existing local changes.

Validation: preflight, restore, formatting verification, XML comment checks, all 424 automated tests, Release build with zero warnings or errors, and whitespace checks passed. CLI smoke checks were not run.

## 2026-09-14 — Pull the latest documentation

- Fast-forwarded `main` to `fa8109c` and restored the existing local memory-management work. Resolved README and task-archive conflicts by keeping the incoming documentation and the local additions.
- Verified that all 26 local source and test files remained byte-for-byte unchanged, including the six new files, and reviewed the three merged documentation files.

Validation: preflight, restore, formatting verification, XML comment checks, Release build with zero warnings or errors, and whitespace checks passed. Automated tests and CLI smoke checks were not run.

## 2026-09-15 — Clean artifacts after every build

- Updated both repository instruction files to require cleanup after every build attempt, including failed builds, with options matching the build configuration, runtime, and output path.
- Added cleanup to the default validation sequence. When requested tests, packaging, or installation need the output, cleanup follows those steps; requested deliverables are preserved.

Validation: preflight, restore, formatting verification, XML method summaries, Release build with zero warnings/errors, post-build cleanup, and whitespace checks passed. Automated tests and CLI smoke checks were not run.

## 2026-09-15 — Install the memory manager MSIX from PR #29

- Fetched PR #29 and packaged its exact source commit `5780a6d973df1c60334aa3891e5620cedfe94142` from an ignored source snapshot, leaving the main checkout in place.
- Published product version `0.1.6` and signed Windows x64 MSIX version `0.1.6.0` with the existing trusted certificate and package identity. Installed the update and verified healthy registration, matching SHA-256 hashes for all 273 payload files, and an execution alias targeting the new package.
- Removed the single legacy PromptMeUp directory entry from the user PATH so `hm` resolves to the MSIX alias in refreshed terminal environments. Saved the prior PATH under ignored artifacts and preserved the older executable and application data.

Validation: preflight, restore, full-solution formatting verification, XML method summaries, Release build and Windows publish with zero warnings/errors, package identity and signature verification, installed-file integrity, and command resolution passed. Automated tests and CLI smoke checks were not run; installation verification did not launch the application.

## 2026-09-14 — Friendlier documentation

- Rewrote the README and current user guides with shorter sentences, direct explanations, and everyday words. Explained chat memory, token usage, command approval, file previews, and privacy without changing the command examples.
- Simplified contributor, support, security, project, release, theme, prompt, and artwork guidance. Kept technical limits, credits, and license information, and corrected two outdated settings checklist entries to match the current Save action and About navigation.
- Added the same plain-English writing rule to both repository instruction files.

Validation: preflight, restore, formatting verification, XML comment checks, and the Release build passed with zero warnings or errors. Static documentation checks found no new broken local links or heading references, unchanged executable and configuration examples, balanced code fences, and no whitespace errors. Automated tests and CLI smoke checks were not run.

## 2026-09-14 — Install the memory management update

- Published the current working tree, including the dedicated memory manager and compact chat/query reminders, as a self-contained Windows x64 application with redistribution notices.
- Created, signed, and installed MSIX revision `0.1.5.12` using the existing package identity and trusted certificate. Included build metadata identifying the base revision and uncommitted source state.
- Verified healthy package registration, matching SHA-256 hashes for all 274 packaged and installed payload files, and an `hm` execution alias targeting the updated package.

Validation: preflight, restore, full-solution formatting verification, XML method summaries, Release build and Windows publish with zero warnings/errors, manifest and signature validation, package checksum, installed-file integrity, execution-alias verification, and whitespace checks passed. Automated tests and CLI smoke checks were not run; installation verification did not launch the application.

## 2026-09-14 — Discover and manage saved memories

- Added compact six-language reminders for `/remember`, `/memories`, and `/forget` when starting a question or chat, without repeating the reminder when a question continues into chat.
- Added a dedicated Memories sidebar entry in Help and Settings, plus `hm --memories`, for local browsing, creation, editing, and confirmed deletion of global and current-project notes. Opening the manager preserves the parent menu, settings draft, and main terminal scrollback.
- Added a responsive alternate-buffer browser with full scrollable note details, bounded compact-menu previews, scope and text editing, cancellation-first deletion, and draft preservation after validation errors. The compact terminal flow exposes the same operations.
- Added atomic exact-identifier updates with existing credential validation, scope isolation, duplicate rejection, and capacity checks. Authored regression coverage for update boundaries, scope moves, rejected inputs, atomic failure, cancellation, and CLI routing; updated the query rendering assertions and product guidance.

Validation: preflight, restore, full-solution formatting verification, XML method summaries, Release build with zero warnings/errors, and whitespace checks passed. Independent source reviews covered navigation, dependency injection, local-only operation, draft preservation, keyboard focus, empty states, layout bounds, cancellation, and six-language completeness. Automated tests and CLI smoke checks were not run.

## 2026-09-14 — Prepare the latest About navigation installer

- Published commit `ec96ae7` as a self-contained Windows x64 application with redistribution notices and created signed MSIX revision `0.1.5.11` using the existing package identity and signing certificate.
- Verified the package signature, version, publisher, execution alias, checksum, source revision, and SHA-256 integrity of all 273 packaged payload files. The installer is ready for installation; the installed application was not updated.

Validation: preflight, restore, full-solution formatting verification, XML method summaries, Release build and Windows publish with zero warnings/errors, manifest validation, signature verification, payload integrity, and whitespace checks passed. Automated tests and CLI smoke checks were not run.

## 2026-09-14 — About navigation in Help and Settings

- Added an About sidebar entry to Help and the settings workspace, opening the existing project information screen with Enter or Right. The compact settings menu opens the same screen and returns to the previous editable section; scrolling help retains the direct `hm about` command.
- Preserved the settings draft, language and theme preview, and menu selection when returning from About. Each fullscreen view releases its alternate buffer before opening the next screen, preserving the main terminal and scrollback.
- Added labels and focus-aware keyboard guidance in all six supported languages, kept help notices within narrow terminal widths, and updated the product guides and existing help-view construction.

Validation: preflight, restore, full-solution formatting verification, XML method summaries, Release build with zero warnings/errors, and whitespace checks passed. Independent source reviews covered keyboard navigation, empty action pages, dependency injection, compact rendering, draft preservation, and alternate-buffer ownership. Automated tests and CLI smoke checks were not run.

## 2026-09-14 — Consistent help and settings workspaces

- Unified help and settings through the same responsive workspace and action-button renderers. Help retains its sidebar at every supported width and matches settings navigation spacing, section emphasis, margins, and footer actions.
- Kept `hm` explicitly white in commands, descriptions, usage hints, and the shared header. Descriptions and examples use two-space insets, with four-space argument notes and preserved wrapped-line indentation. Static help uses the same entry renderer.
- Distinguished optional square-bracket groups with the theme's yellow/amber color, preserving spaces, nested groups, and quoted values.
- Built, signed, and installed MSIX revision `0.1.5.9`; verified healthy registration, all 273 installed payload hashes, and the WindowsApps alias targeting that revision.
- Included the subsequent optional-parameter coloring in MSIX revision `0.1.5.10`, repeated the non-test validation gate, and verified its signature, healthy registration, all 273 payload hashes, and updated alias target before preparing the complete source changes for main.

Validation: preflight, restore, solution formatting verification, XML method summaries, Release build and Windows publish with zero warnings/errors, whitespace checks, signature validation, and installation integrity checks passed. Automated tests and CLI smoke checks were not run.

## 2026-09-14 — About artwork and richer settings overviews

- Added `hm about` and `hm --about`, with a six-language fullscreen information page, the original HELP ME gradient artwork and white H/M initials, project links, version, and attribution. The standalone command opens before settings or AI services are resolved; compact and redirected terminals receive scrolling output, and fullscreen exit restores the main buffer.
- Added the selected theme's complete source path, author, website hyperlink, and description to Theme settings. Metadata follows the live draft and uses the existing scrollable overview. Schema version 3 adds a validated website to all 13 bundled palettes; versions 1 and 2 remain readable, and the computed local source path is excluded from serialization.
- Shared the existing model-price comparison between Costs and AI settings, highlighting the selected model and retaining supported models with unavailable rates. The comparison uses cached standard short-context prices and adapts its columns to the available width. Short settings viewports reserve space for both focused fields and scrollable details.
- Updated six-language UI labels, theme and CLI guidance, and existing theme assertions while preserving unrelated work in the shared checkout.

Validation: preflight, restore, full-solution formatting verification, XML method summaries, Release build with zero warnings/errors, and whitespace checks passed. Independent source reviews covered routing, alternate-buffer restoration, compact layout, metadata privacy, backward compatibility, and price semantics. Automated tests and CLI smoke checks were not run.

## 2026-09-14 — General overview, help entry point, and expanded themes

- Replaced the command center with help for plain `hm`, including first launch and redirected output. General settings now combines draft application status with cached usage and costs; short terminals can scroll the overview with Ctrl+Up/Down, and the scrolling compatibility view shows it too.
- Aligned the operating-budget label with the session overview's first label column while retaining the separate Spectre progress bar and localized values.
- Added semantic explanations beside palette samples, whitesmoke settings values, and clear Save/Cancel action emojis. Added ten bundled themes, backfilled version-two author/description metadata for the original three, and retained explicit support for legacy version-one theme files.
- Built, signed, and installed MSIX revision `0.1.5.8`. Verified healthy registration, all 273 installed payload hashes, and the WindowsApps `hm` alias target.

Validation: preflight, restore, solution formatting verification, XML method summaries, Release build and Windows publish with zero warnings/errors, whitespace checks, package identity/signature verification, and installed payload integrity checks passed. Automated tests and CLI smoke checks were not run. Concurrent work in the shared checkout was preserved.

## 2026-09-14 — Fix the budget bar and align leading emoji

- Removed redundant leading padding from the shared icon-prefix helper while retaining trailing separation, intentional layout margins, and spacing after fullscreen navigation markers. Aligned both contributor instruction files and updated the existing spacing assertions.
- Fixed the operating-budget crash by providing Spectre's progress task with the localized budget label instead of an empty description. Extremely narrow terminals retain the budget text without constructing a bar below its minimum width.
- Published, signed, and installed MSIX revision `0.1.5.7` with the latest fixes. Confirmed healthy registration, matching SHA-256 hashes for all 263 installed payload files, and a WindowsApps execution alias targeting that version.

Validation: preflight, restore, full-solution formatting verification, XML method summaries, Release build and Windows publish with zero warnings/errors, manifest and signature verification, and installed-file integrity checks passed. Independent source review confirmed emoji call-site spacing and the exact Spectre package's progress contracts. Automated tests and CLI smoke checks were not run.

## 2026-09-14 — Install the unified settings MSIX

- Published the current working tree as a self-contained Windows x64 application, exported redistribution notices, and signed MSIX revision `0.1.5.5` with the existing trusted certificate.
- Installed the update and verified healthy registration, matching SHA-256 hashes for all 263 payload files, and an AppExecLink targeting the new version through the WindowsApps `hm.exe` alias.

Validation: preflight, restore, full-solution formatting verification, XML method summaries, Release build, Windows publish, manifest validation, and package signature verification passed. Automated tests and CLI smoke checks were not run; installation verification did not launch the application.

## 2026-09-14 — Unified settings workspace and clearer terminal feedback

- Consolidated General, AI, Credentials, Conversation, Commands, Personalization, and Theme into one settings draft with an emoji sidebar. Setup, AI setup (including the existing AI settings alias), and theme switches only choose the opening section; the main menu has one Settings entry.
- Removed the fullscreen summary and F10 step. Save validates and persists directly; Cancel restores the draft language and palette. Tab reaches Save/Cancel and Left/Right moves between their buttons. Shortcuts distinguish muted key labels from brighter actions.
- Preserved section navigation in compact terminals and added a shared section-menu compatibility view. Exposed complete focused labels, hid credential replacements, previewed theme colors, and kept connection checks opt-in after initial setup.
- Moved the operating budget onto its own row with a Spectre progress bar and the existing localized estimate, capacity, and percentage. Added a blank line immediately after the closing thank-you message.
- Updated all six UI languages and the product guides while preserving concurrent command-clarification work separately.

Validation: preflight, restore, full-solution formatting verification, XML method summaries, Release build with zero warnings/errors, and whitespace checks passed. Independent source reviews covered routing, keyboard focus, compact geometry, cancellation, and privacy. Automated tests and CLI smoke checks were not run.

## 2026-09-14 — Clarify ambiguous requests without closing the session

- Fixed the suggestion menu page size for answers with no suggested commands and chat answers with only one suggestion.
- Added an explicit clarification/chat continuation choice and neutral finish guidance when no commands are suggested, using the existing conversation and authorization workflow. Updated all six UI languages and the CLI reference.
- Versioned the query and chat prompts to ask a focused clarification question and withhold commands until necessary details are available, with guidance in all six languages and matching existing version assertions.

Validation: preflight, restore, XML method summaries, Release build with zero warnings/errors, scoped formatting verification, and `git diff --check` passed. Full-solution formatting verification reported line-ending and encoding issues in unrelated concurrent settings/UI changes; those files were preserved. Independent source review covered small menus, defaults, conversation continuity, privacy, authorization, and localization. Automated tests and CLI smoke checks were not run; the installed executable was not replaced.

## 2026-09-14 — Consistent setup and help navigation

- Added a shared fullscreen header with a divider, app version, and project link aligned to the right. Reopened setup starts in the section list; Up/Down selects sections and Enter/Right moves to fields while preserving the draft and review/save step.
- Removed sidebar numbers, added meaningful section icons, and kept country flags, native names, and language codes consistent in choices and summaries. Emoji-free rendering remains available.
- Matched help to the setup layout and focus model. Help entries show complete examples with lowercase white `hm`, colored options and values, descriptions using the same colors, and explanations of useful arguments. All new text is available in the six supported languages.
- Added a blank line after the closing project separator and removed the redundant application-status heading. Updated the product guides to describe the new navigation and examples.
- Published, signed, and installed MSIX revision `0.1.5.4`, including the coordinated fullscreen footer and conditional range improvements.

Validation: the shared preflight, restore, formatting verification, XML method summaries, and Release build passed with zero warnings or errors. Independent source reviews covered keyboard focus, draft preservation, narrow layouts, and example rendering. Windows publish and package signature verification passed; installed registration reports `Ok`, all 263 payload files match their source hashes, and the WindowsApps execution alias targets the updated package. Automated tests and application smoke tests were not run.

## 2026-09-14 — Conditional help range and consistent fullscreen footers

- Show the localized displayed-line range only when the visible help content overflows. Remove trailing empty spacing rows from the count, retain scroll clamping on resize, and render primary white text with bold numbers on one bounded row.
- Share the notice, separator, action, and shortcut layout between fullscreen help and forms, preserving two-column margins, variable form notices, semantic action colors, and compact localized hints. Both surfaces use the shared fullscreen header.
- Updated the displayed-line wording in all six supported languages, including "Righe visualizzate" in Italian, and prepared an illustrative layout preview outside the repository.

Validation: preflight, restore, full formatting verification, XML method summaries, and the Release build passed with zero warnings or errors. Independent source review covered footer geometry, range visibility, clipping, focus, and resize behavior. Automated tests and CLI smoke tests were not run.

## 2026-09-14 — Release checkpoint and main-branch policy

- Prepared the completed fullscreen forms, themes, navigable help, local image display, artwork, and MSIX packaging changes for the requested commit and push on `main`.
- Reviewed the complete file inventory and confirmed that only intended source, documentation, redistribution notices, themes, and curated artwork are included. Build output, signing material, logs, and local data remain excluded.
- Verified that GitHub already requires pull requests and the existing quality checks on `main`, with administrator bypass enabled. Force pushes and branch deletion remain disabled; no protection settings needed changing.

Validation: preflight, restore, formatting verification, XML method summaries, and the Release build passed with zero warnings or errors. The remote branch matched the local starting commit. Automated tests and application smoke tests were not run.

## 2026-09-13 — Open terminal layouts, fullscreen help, and Matrix artwork

- Removed cards and enclosing frames from runtime views and recorded the no-card rule in both repository instruction files. Forms align labels to the right and values to the left in two columns, with one blank row between fields, semantic navigation colors, and a single action separator.
- Kept long values on one terminal row with Unicode-aware clipping and sized the editor from the actual value column. Reviewed focus visibility, review scrolling, narrow layouts, and resize behavior.
- Added fullscreen help with five navigable sections, scrollable descriptions, keyboard hints in all six languages, and Escape/Q exit. Supported terminals use a disposable alternate buffer; redirected output and CLI errors retain the scrolling reference.
- Recovered the Lenna ANSI resource from the author's archived YAi! project and preserved its 78-by-78 RGB pixels in a compact embedded image. `hm lenna` and `hm --lenna` render it with Spectre.Console, centered in both directions, without AI configuration, a database connection, or a download. Preserved the legacy attribution and source license in release notices.
- Gave the 128- and 256-pixel icon frames green phosphor, glow, and scanlines while preserving smaller frames byte-for-byte. Removed the banner's horizontal underscore and retained its separate block cursor.
- Updated product guides, artwork provenance, and the displayed dependency versions. Produced, signed, and installed MSIX revision `0.1.5.3` after the user closed the earlier running session.

Validation: preflight, restore, formatting verification, XML method summaries, Release build, and self-contained Windows publish passed with no warnings or errors. Independent source reviews covered both fullscreen layouts and standalone command routing. Checked icon frame invariance, visual artwork, exact RGB dimensions, the published embedded resource, exported notices, package manifest, and signing verification. Installed registration reports `Ok`, all 263 payload files match their source hashes, and `hm` resolves through the WindowsApps execution alias. Automated tests and application smoke tests were not run.

## 2026-09-13 — Install the current build and verify command resolution

- Published the current working tree as a self-contained Windows x64 build and installed signed MSIX revision `0.1.5.2` using the existing package identity and trusted signing certificate.
- Verified healthy installed registration and SHA-256 equality for all 261 packaged files, including the application assembly, themes, prompts, and manifest.
- Confirmed both current-process and persisted machine/user PATH resolve `hm` exclusively through the WindowsApps execution alias; inspected its AppExecLink data to verify that it targets the newly installed revision. No persistent PATH change was necessary.

Validation: preflight, restore, formatting verification, XML method summaries, Release build, self-contained publish, license export, manifest validation, package signing, signature verification, installation integrity, and command-resolution checks passed. No automated tests or application smoke tests were run; verification did not launch the application.

## 2026-09-13 — Signed MSIX installation and project icon

- Added a repeatable MSIX packaging script for prepared self-contained Windows builds, including prompts, themes, redistribution notices, architecture checks, manifest validation, certificate-store signing, and signature verification.
- Installed local MSIX revision `0.1.5.1` with the `hm.exe` execution alias and normal Win32 application-data behavior, using an already trusted certificate without exporting its private key or changing trust settings.
- Removed the previous per-user MSI with its standard uninstaller and verified that its executable and user PATH entry were removed while the existing database remained unchanged.
- Added `assets/PromptMeUp.ico`, a reproducible vector-based generator, and provenance notes. The seven icon resolutions are embedded in the executable and reused for the MSIX tiles.
- Documented local MSIX packaging, signing, installation, updates, aliases, and removal alongside the existing MSI instructions.

Validation: preflight, restore, formatting verification, XML method-comment checks, Release build, and self-contained publish passed with zero warnings or errors. Validated the ICO frame directory, visually inspected the icon and executable resource, and verified the SDK package signature, healthy installed registration, matching executable/tile hashes, and WindowsApps command resolution. No automated tests or CLI smoke tests were run; application processes were not launched during validation. Existing unrelated source changes were preserved.

## 2026-09-13 — Command notice spacing and STDERR emphasis

- Added a blank line before and after the combined command-risk advisory and output-sharing notice.
- Confirmed both output warning icons use the shared helper's surrounding spaces; isolated slow blinking to the STDERR label, respecting disabled animations and keeping the icon and output content steady.

Validation: preflight, restore, formatting verification, XML method summaries, and Release build passed with zero warnings or errors. No automated tests or CLI smoke tests were run. Blink appearance depends on terminal support; the installed executable was not replaced.

## 2026-09-13 — Fullscreen setup and JSON terminal themes

- Implemented revisitable fullscreen setup sections, the focused AI settings form, and a standalone `hm --theme` chooser with live palette preview and explicit review/save.
- Kept draft settings and non-echoing credential inputs local to the views. Added keyboard focus, bounded text editing, Unicode-aware cursor windows, internal scrolling, resize handling, six-language hints, and a sequential compatibility path.
- Preserved main terminal history through a disposable alternate buffer and aligned both repository instruction files with that behavior.
- Added validated Cyan, AS/400 Green, and Amber JSON palettes, applied semantic colors throughout shared views, and persisted the selected identifier with a transactional schema-v3 upgrade.
- Included theme resources in build/publish output and Windows ZIP/MSI staging, documented customization, and added regression sources for validation, migration, and command routing.
- Reviewed cancellation, secret rendering, startup and menu integration; corrected focused menu saves so a temporary display-language override cannot change the saved language.

Validation: preflight, restore, formatting verification, XML method summaries, Release build with 0 warnings/errors, whitespace review, and inspection of copied theme JSON resources passed. Automated tests and CLI smoke tests were not run; new regression sources compiled only. Interactive terminal behavior still requires an explicitly requested smoke/manual check. The installed executable was not replaced; no branch or commit was created by this task.

## 2026-09-13 — GitHub Actions failure fixes

- Traced the main quality-gate failures to ANSI styling in semantic output assertions and a second SQLite connection pool left open during Windows fixture cleanup.
- Preserved all visible-text assertions and narrow-terminal coverage while removing ANSI control sequences from comparisons; released both fixture-specific database pools without clearing other fixtures' pools.
- Fixed the attribution failure on dependency PR #8 by recognizing the native SQLite package and checking its declared license file against the reviewed upstream text. Unknown or changed license declarations still stop packaging.

Validation: preflight, restore, formatting verification, XML method-comment checks, and Release build passed with zero warnings or errors in a temporary source snapshot, preserving concurrent fullscreen/theme work. Notice export passed for the current main dependency graph (26 packages) and the exact PR #8 graph with SQLite 3.53.4 (27 packages). No local automated tests or CLI smoke tests were run; the existing GitHub Actions workflows provide those checks after push.

## 2026-09-13 — Product documentation refresh

- Reworked the README around everyday terminal tasks, with direct examples for saved notes, context counters, and the shorter AI settings flow.
- Updated the CLI, memory/cost, privacy, architecture, validation, and prompt guides to match the implemented behavior and limits.
- Simplified the documentation index and support copy; identified the printable manual as an earlier edition and directed readers to the current web guides.
- Checked 51 local links and heading targets and reviewed the guides against the implementation.

Validation: preflight, restore, formatting verification, XML method-comment checks, and Release build passed with zero warnings or errors on a temporary source snapshot of `a9f0532` plus this documentation update. The shared workspace's formatting and build checks encountered concurrent, unfinished fullscreen/theme edits, which were preserved outside this change. Automated tests and CLI smoke tests were not run because they were not requested.

## 2026-09-13 — Commit the remaining local work

- Included the remaining credential-redaction fixes, synthetic regression sources, full terminal/IDE restart guidance, repository validation rules, review resolution, and fullscreen design analysis in one user-requested checkpoint.
- Reviewed the pending file inventory and privacy changes before staging all non-ignored work.

Validation: preflight, restore, formatting verification, XML method-comment checks, and Release build passed with zero warnings or errors. Automated tests and CLI smoke tests were not run because they were not requested.

## 2026-09-13 — Shared workflow, storage, and localization behavior

- Centralized audit session lifecycles with typed outcomes and cancellation-independent cleanup; script validation now records its actual result.
- Shared chat turn submission and action handling while retaining completed-turn cost across recoverable input-limit failures.
- Reused settings-summary formatting and atomic artifact writes, preserving row order, encoding, validation, and explicit overwrite policies.
- Consolidated 396 UI keys into nine functional catalogs with six named translations per key; static source comparison preserved all 2,376 effective translations.
- Extracted setup and installation workflows from the invocation orchestrator and shared non-session activity recording.
- Preserved unrelated local privacy fixes and documentation changes outside the refactoring commit, including their relocated translation entries.

Validation: preflight, restore, formatting verification, XML method-comment checks, and Release build passed with zero warnings or errors. Existing localization test source was adapted and compiled; automated tests and CLI smoke tests were not run because they were not requested.

## 2026-09-13 — Bounded memories, context accounting, and AI settings

- Added explicit project/global memories with local relevance selection, credential protection, and an 800-estimated-token request envelope.
- Separated retained active context, operational input budget, last-call provider counters, and cumulative conversation usage; `/status`, `/context`, and `/clear` preserve accounting.
- Added a persisted 16,000-token default input budget, an optional invocation override, and an additive SQLite version-two migration.
- Added `hm --ai-settings` to change only model behavior and context limits without repeating setup or changing credentials.
- Kept oversized chat requests recoverable without discarding earlier turns, and documented all commands and limits.

Validation: preflight, restore, formatting verification, XML method-comment checks, and Release build passed with zero warnings or errors. Regression test sources were added and compiled; automated tests and CLI smoke tests were not run because they were not requested.

## 2026-09-13 — Fullscreen terminal design census

- Added a fullscreen UX analysis with stable selection numbers for 12 functional pages and one optional navigation hub, source-backed interaction counts, and flows that should retain waterfall output. The historical proposal is now archived in the owner's private MeUp notes.
- Reviewed Spectre.Console feasibility, alternate-buffer history preservation, keyboard behavior, shared forms and individual command authorization.
- Prepared and visually inspected three ImageGen concepts: cyan setup, green setup, and an amber plan workspace. Kept generated previews and prompts in the ignored local artifacts directory.
- Completed analysis only; implementation scope remains for the user to select. Preserved unrelated work and created no branch or commit.

Validation: preflight, restore, formatting verification, XML method summaries, and Release build passed with 0 warnings/errors. Whitespace review passed. No automated tests or CLI smoke tests were run.

## 2026-09-13 — Privacy and key-guidance review fixes

- Resolved all four findings from [the September 13 review](review-2026-09-13.md).
- Shared credential-field classification between text and structural audit redaction, covering common token/key names and preserving typed usage metadata.
- Added complete JSON and PowerShell value handling, including duplicate properties, escaped names, credential-valued containers, doubled quotes, backticks, here-strings, truncated values, and placeholder suffixes.
- Added the terminal-restart hint to organization-cost authentication warnings and clarified the full terminal/IDE restart in all six languages, the README, and the privacy guide.
- Verified that local command/output previews remain exact while synthetic credentials are removed from actual SQLite records and captured provider requests.

Validation: preflight, restore, formatting verification, XML method comments, and Release build with 0 warnings/errors passed. The automated checks launched under the instructions read at task start completed with 320/320 tests and eight isolated CLI smoke checks passing. No further tests were run after the concurrent explicit-request-only testing rule was discovered. No real provider calls, credential changes, PATH changes, or font changes were made; the installed executable was not replaced.

## 2026-09-13 — Tests only on explicit user request

- Aligned `AGENTS.md`, Copilot instructions, contributor guidance, and the validation guide so agent-run automated tests and CLI smoke tests require an explicit user request.
- Kept preflight, restore, formatting, XML-comment checks, and Release builds in the default non-test validation gate. Requests to implement, review, commit, or push changes do not authorize test execution.

Validation: reviewed the documentation diff and checked whitespace. No tests or smoke tests were run for this documentation change, as requested by the user.

## 2026-09-13 — Repository checkpoint validation

- Reviewed the accumulated console presentation, language flags, Windows key guidance, shared project banner, and MSI directory-cleanup changes before publishing the repository checkpoint.
- Confirmed that the four findings in the September 13 review remain tracked separately.

Validation: preflight, restore, formatting verification, XML method comments, Release build with 0 warnings/errors, 225/225 tests, and 15 isolated CLI smoke checks passed. Smoke checks covered help/status in all six languages, version output, invalid arguments, and missing setup; they verified one project banner per invocation and preserved terminal scrollback. No real credentials, provider requests, PATH changes, or font changes were used.

## 2026-09-13 — Current directory and shared project banner

- Added a dedicated current-directory row to the invocation header, supplied by the application and escaped for terminal display.
- Added `RenderProjectBanner` with localized thanks for using hm (help me), a GitHub star/suggestion invitation, the repository link, and linked copyright attribution.
- Reused the banner at the end of help and on process exit without duplicate output; normal, application-error, and initialization-error exits retain their expected exit codes.
- Reviewed the new UI and existing credential, authorization, execution, and redaction paths. Four open findings are documented in [the September 13 code review](review-2026-09-13.md) and tracked separately in the active task list.

Validation: preflight, restore, formatting verification, XML method comments, Release build with 0 warnings/errors, and 225/225 tests passed. Fifteen isolated CLI smoke checks covered help/status in all six languages, invalid arguments, missing setup, and invalid startup configuration. Narrow-terminal, literal bracketed-path, emoji-option, and banner-deduplication rendering tests passed. No real provider requests, credential changes, PATH changes, or font changes were made. The installed application was not replaced.

## 2026-09-13 — API key session-restart guidance

- Added localized restart guidance after API or admin key changes on Windows, where an existing terminal can retain a stale process environment value.
- Added the same guidance to HTTP 401 authentication failures and locally detected missing or invalid API keys without changing unrelated provider errors.
- Covered authentication, non-authentication, and non-Windows error rendering with focused regression tests.

Validation: preflight, restore, formatting verification, XML method comments, Release build with 0 warnings/errors, and 221/221 tests passed.

## 2026-09-13 — Updated per-user MSI installation

- Downloaded the official WiX Toolset 3.14.1 binaries for an isolated, non-administrator build after the system installer required elevation and .NET Framework 3.5.
- Repaired MSI harvesting for container-only payload directories by attaching uninstall cleanup to a descendant component.
- Built and WinGet-validated the current x64 portable ZIP and per-user MSI, then verified the MSI against `SHA256SUMS.txt`.
- Installed the MSI successfully, removed the obsolete portable PATH entry and moved its versioned directory to the Recycle Bin.
- Verified MSI registration, the stable `%LOCALAPPDATA%\Programs\PromptMeUp` executable, exactly one user PATH entry, no machine PATH entry, and the installed six-flag setup menu.

Validation: MSI SHA-256 is `EF4BB8C0EB908CFB3265156C31225CD361C4FDFA1EBADA75AC2284A3F314A151`; Windows Installer returned exit code `0`; preflight, restore, formatting verification, XML comments, Release build with 0 warnings/errors, 218/218 tests, packaged smoke tests, WinGet validation, and installed interactive setup smoke passed.

## 2026-09-13 — Emoji spacing, language flags, and light accent

- Standardized user-facing emoji rendering with one visible space before and after every icon while preserving compact ASCII fallbacks for `--no-emoji`.
- Added country-flag emoji to all six language choices in setup.
- Replaced the purple accent and selection highlight with the shared very-light near-cyan `lightskyblue1` color.
- Added the emoji-spacing rule to both repository-wide instruction files and regression coverage for spacing and flags.

Validation: preflight, restore, formatting verification, XML method comments, Release build with 0 warnings/errors, 218/218 tests, isolated CLI `--version`/`--help`/`--status` smoke checks, and emoji-enabled rendering passed.

## 2026-09-06 — Automatic formatting fixes before the CI gate

- Added `scripts/format.ps1` with applying and read-only verification modes.
- Updated the quality workflow to apply supported `dotnet format` fixes before the final formatting check and summarize runner-only changes.
- Documented the local workflow in `CONTRIBUTING.md` and `docs/VALIDATION.md`.

Validation: preflight, restore, automatic formatting, formatting verification, XML method comments, Release build with 0 warnings/errors, 216/216 tests, CLI `--version`/`--help`/`--third-party` smoke checks, and `git diff --check` passed.

## 2026-09-05 — PR #20 Windows Unicode output

- Fixed the large-source PowerShell bootstrap to emit UTF-8 explicitly. The Windows CI runner otherwise replaced Vietnamese characters with question marks; parent-side UTF-8 decoding cannot recover them.
- Preserved the existing Unicode assertions, command preview, and exit behavior. Local tests and builds were deferred at the user's request; the PR's existing CI covers the correction.

## 2026-09-05 — Current executable, PATH, and concise documentation

- Uninstalled the previous MSI and installed the verified current portable build in the user's stable application directory. PATH selects this copy, which survives repository build cleanup.
- Verified all 39 installed payload files, four installed CLI smoke checks, and an unchanged application database. Repeated the full gate: 216/216 tests and a Release build without warnings or errors.
- Shortened artifact, privacy, and cost guidance. Cleaned .NET Release, runtime-specific, and Debug outputs; ignored directory deletion remains pending because the execution policy rejected it.

## 2026-09-05 — Prominent README early-access notice

- Added a GitHub Note alert immediately after the introduction, using PromptMeUp as the product name and linking directly to the source-build instructions.
- Retained the existing installation-section guidance and confirmed that the public GitHub Releases page has no published releases.

Validation: checked the alert placement, section link, and whitespace with `git diff --check`. Documentation-only follow-up; the preceding full validation gate remains applicable to the unchanged application code.

## 2026-09-05 — Review improvements and configurable artifact limits

- Rejected credential-bearing query arguments consistently and redacted every conversation message before provider transmission while preserving exact local command previews.
- Preserved completed provider responses when optional local pricing or session cleanup fails; recorded actual usage and estimated costs for incomplete or invalid content responses and included billable failures in local totals.
- Shared bounded process execution between approved commands and font helpers, including cancellation cleanup, output caps, and helper deadlines. Large approved source travels over standard input to avoid platform command-line limits.
- Replaced small fixed script and plan limits with configurable UTF-8 budgets: 1 MiB scripts and 8 MiB serialized plans/recipes by default, each configurable from 1 to 64 MiB. Unified read/write checks and exposed effective limits in localized status output.
- Separated artifact payload size from ordinary chat limits and reserved model context for the configurable artifact output budget (16,384 tokens by default). Updated both versioned artifact prompts in all six languages.
- Completed six-language local risk explanations and documented configuration, privacy, accounting, and process boundaries.

Validation: the required local gate passed with 216/216 tests and zero Release-build warnings/errors. Eleven isolated CLI smoke checks covered help/version, status in all six languages, overrides, invalid configuration, and credential-argument rejection. No real provider requests or PATH/font changes were performed. Disposable smoke data remains under ignored `artifacts/`. PATH resolves the existing August 16 installation, independently of the new repository Release build; the installation was inspected and left unchanged.

## 2026-09-02 — Repair PR #10 CI fixture failures

- Investigated the first PR quality run: Lint and macOS passed, while Windows exposed global SQLite pool cleanup racing other fixtures and Linux timed out before the inherited-pipe fixture emitted its expected marker.
- Replaced both global pool clears with cleanup scoped to the fixture's exact database connection pool, retaining parallel test execution.
- Removed PowerShell module discovery from the process fixture, wrote and flushed markers directly, and allowed CI startup headroom. The deadline remains 10 seconds, the return assertion remains below 20 seconds, and the independent child lifetime is 30 seconds, so waiting for the child still fails the regression.
- Preserved production code, timeout semantics, and all output/cancellation assertions; prepared the fix for the existing PR and its native CI matrix.

Validation: the full local gate passed with 148/148 tests and zero build warnings/errors. The parallel SQLite regression subset passed ten consecutive runs; isolated help/version CLI smoke checks passed. Native CI outcomes are recorded in PR #10 checks.

## 2026-09-02 — Resolve ten verified code-review findings

- Completed the privacy, deadline, chat/risk/shell, pricing, and summary plan; every finding in [the review](review-2026-09-02.md) now maps to tracked regression coverage.
- Protected quoted and serialized JSON credentials, rejected new credential-bearing preambles, sanitized legacy settings before use, and removed raw exception details from persistent diagnostics.
- Bounded complete HTTP/process operations, preserved completed assistant answers, corrected diagnostic risk classification and Unix shell instructions, and selected model-specific pricing bands from actual input usage.
- Restricted summary reads to the indexed month interval and preserved successful-request totals at period boundaries.
- Added 58 regression cases and updated privacy, architecture, costs, and validation guidance. Preserved the repository presentation and release work already merged through PR #9.

Validation: full required gate passed with 148/148 tests and zero build warnings/errors; isolated help, version, Italian/Vietnamese status, and French attribution smoke checks passed. No real credentials or AI requests were used. The OS-temporary smoke directory was retained because automatic approval review blocked its deletion; it is outside the repository and excluded from publication.

## 2026-09-02 — MIT repository, product presentation, and portable CI/CD

- Preserved the MIT license and upstream attribution, including transitive dependency notices and the exact self-contained runtime's notices in release packages.
- Added maintainer governance, CODEOWNERS, reproducible main/tag protection settings, dependency review, pinned Actions, and a six-target portable release workflow with checksums, provenance attestations, and a draft publication step.
- Enabled private vulnerability reporting and applied main protection to administrators, required checks, pull requests, linear history, and resolved conversations; blocked changes and deletion of version tags.
- Rebuilt the English README around the weekend-project story and UmbertoGiacobbiDotBiz team, with a retro banner, three explicitly illustrative screen renderings, and links to TrackMeUp, viewsapp.ai, and the creator's website.
- Aligned the English repository-writing rule in AGENTS.md and GitHub Copilot instructions. Preserved the other agent's application-code work.

Validation: the full required gate passed on an isolated baseline plus these changes, with 90/90 tests and zero Release-build warnings/errors. The Windows x64 portable archive passed credential-free startup/help/attribution smoke checks; all inventory notice references resolved inside the ZIP. GitHub-flavored Markdown rendering, local links, PowerShell parsing, and actionlint checks passed. The shared worktree's initial format check detected in-progress C# edits from the concurrent code task; those edits were excluded from this change.

## 2026-09-02 — Guided error diagnosis

- Added bounded text, UTF-8 log file, and stdin diagnosis with credential redaction and strict source validation.
- Added an evidence-led prompt and UI text in all six languages; reused exact per-command approval for follow-up checks.
- Validated parser boundaries, cancellation, redaction, Release build/tests, and isolated CLI smoke checks.
- Delivered as the first feature commit and dedicated PR in the terminal assistance series.

## 2026-09-03 — Reconcile diagnosis PR with main

- Moved diagnosis history away from the archive append point changed by main, and separated planned feature work from the shared baseline task-status entry.
- Preserved every diagnosis and planned-feature entry and rebased the PR onto current main to satisfy its required up-to-date checks without a merge commit.
- Limited manual conflict corrections to the two task ledgers; production code and runtime prompts match the validated integration of the original feature with main.

Validation: the original feature branch passed its full required gate with 98/98 tests; the integrated source passed with 156/156 tests and zero build warnings/errors. Diagnosis help and unconfigured-input smoke checks passed with isolated data. Native CI results for the updated branch are recorded in PR #11.

## 2026-08-16 — PromptMeUp 0.1.5 protected-preamble installer

- Bumped the product and current packaging examples from `0.1.4` to `0.1.5` for the multilingual protected-preamble release.
- Built and validated the self-contained x64/ARM64 ZIPs, x64 MSI, release manifest, checksums, and WinGet manifests.
- Upgraded the current-user installation from `0.1.4` to `0.1.5` silently without elevation or restart.
- Verified all seven installed payload files byte-for-byte against the x64 ZIP, MSI registration `0.1.5`, exactly one user PATH entry, zero machine PATH entries, and `hm --version` / `hm -where` output.

Validation: full repository gate passed with 90/90 tests and zero build warnings/errors; MSI SHA-256 is `1E7E19B2976897ADD724B47DE07F1B195657F94C80FBA71096D4449F6469671C`.

## 2026-08-16 — Multilingual AI preamble protection

- Reframed the optional setup instruction as a preamble appended to every user-facing chat and one-off query, with a hard 500-word limit and used/maximum/remaining word feedback.
- Added deterministic Unicode normalization and multilingual prompt-injection screening for Italian, English, French, German, Spanish, and Vietnamese, including instruction overrides, role forgery, prompt extraction, and delimiter breakout attempts.
- Added provider-bound defense in depth through explicit untrusted-data delimiters and versioned localized YAML rules that prevent the preamble from weakening system instructions or authorizing commands.
- Preserved existing SQLite compatibility, sanitized preambles before persistence and again before provider-bound prompt assembly, and documented the feature and its non-infallible security boundary.

Validation: full repository gate passed with 90/90 tests and zero build warnings/errors; isolated `--help` and `--status` smoke tests passed with a disposable data directory that was removed afterward.

## 2026-08-16 — PromptMeUp 0.1.4 premium UI audit installer

- Bumped the product and current packaging examples from `0.1.3` to `0.1.4` for the completed cross-screen terminal UI audit.
- Built and independently checksum-verified the self-contained x64/ARM64 ZIPs, x64 MSI, release manifest, and validated WinGet manifests.
- Upgraded the local per-user installation to `0.1.4` without closing the existing `hm` session or requiring a reboot.
- Verified the installed binary and Windows Installer registration, exactly one user PATH entry, zero machine PATH entries, and an unchanged machine PATH.

Validation: full repository gate passed with 74/74 tests and zero build warnings/errors; installed version is `0.1.4.0`; MSI SHA-256 is `6B6A857CC458D37100E024111BF7D7DFC67C49D8434A853A4C0D39126FFAB62C`.

## 2026-08-16 — Cross-screen terminal UI audit

- Audited every terminal view for decorative cards, icon spacing, separator width, alignment, semantic colors, completed progress residue, and hard-coded user-facing copy.
- Centralized frameless right-label/left-value grids and applied them to status, session, setup, About, PATH, executable location, costs, and command-result metadata.
- Standardized emoji spacing and ASCII fallbacks, 80%-width section rules, semantic success/warning states, and clean Nerd Font dry-run/unsupported results.
- Localized setup validation, help placeholders, PATH previews, font states, and all command-line parser errors in Italian, English, French, German, Spanish, and Vietnamese.
- Added regression coverage for frameless status and command-result grids, localized font dry runs, localized parser errors, chat guidance, and Markdown-safe teletype output.

Validation: full repository gate passed with 74/74 tests, zero build warnings/errors, complete XML summaries, static view scan, and isolated help, status, PATH, font dry-run, and localized-error smoke tests.

## 2026-08-16 — Premium terminal hierarchy and chat feedback

- Removed the remaining decorative cards, standardized 80%-width separators with intentional leading whitespace, and guaranteed one visible space after every icon.
- Rebuilt status and session summaries as frameless label-value grids with right-aligned labels, left-aligned values, responsive status rows, and at most two session rows on standard wide terminals.
- Stacked active progress beneath its status text and made the complete progress surface disappear when work ends.
- Added a localized five-command chat guide, bounded prompt validation, used/maximum/remaining character feedback, consistent automatic user turns, and a Markdown-safe teletype response effect.
- Switched cost bands to semantic foreground colors, localized command risk and execution metadata, improved the command-center hierarchy, and added a default `Non fare nulla` executable-location action.

Validation: full repository gate passed with 70/70 tests, zero build warnings/errors, XML summaries complete, and isolated status plus executable-location smoke tests passed.

## 2026-08-16 — PromptMeUp 0.1.3 frameless UX installer

- Bumped the product and documented packaging examples from `0.1.2` to `0.1.3` for the frameless command-review refinement.
- Built and checksummed the self-contained x64/ARM64 ZIPs, x64 MSI, release manifest, and validated WinGet manifests.
- Upgraded the local installation from `0.1.2` to `0.1.3`; verified the installed binary, WinGet registration, one user PATH entry, and unchanged machine PATH.

Validation: full repository gate passed with 68/68 tests; MSI SHA-256 is `99674769B454C354E8B4CEA793792135FF88FE429900F86B9DF65D75BC78092E`; installer exit code was `0`.

## 2026-08-16 — Frameless command-review refinement

- Replaced cards in the command suggestion, command authorization, command result, shell header, session snapshot, and chat introduction with spacious emoji-led sections.
- Standardized every decorative section divider to end at 80% of the current terminal width and compacted session data into two metric rows.
- Added semantic low/medium/high/critical risk indicators, a structured command-result view, single-line cancellation notices, and a one-line assistant-plus-response-heading treatment.

## 2026-08-16 — PromptMeUp 0.1.2 installer refresh

- Bumped the product, assembly, package, and documented release version from `0.1.1` to `0.1.2` for the completed terminal UX and scoped-AI release.
- Built the portable `win-x64`/`win-arm64` archives, per-user `win-x64` MSI, checksums, release manifest, and validated WinGet manifests.
- Verified the MSI SHA-256 before installation, upgraded the local per-user installation from `0.1.1` to `0.1.2`, and confirmed the installed `hm.exe`, About metadata, uninstall registration, and fresh PowerShell command resolution.
- Confirmed the installer keeps exactly one user PATH entry and does not alter the machine PATH.

Validation: release build and `winget validate` passed; MSI upgrade returned exit code `0`; installed file version is `0.1.2.0`; `hm --version --no-animation --no-emoji` reports the version, GitHub repository, and creator site.

## 2026-08-16 — Premium Spectre Console and scoped AI assistance

- Preserved terminal scrollback across every application flow, with deliberate whitespace around invocations and an interrupted prompt's cancellation message rendered on its own line.
- Rebuilt the shared shell, chat, Markdown renderer, help, version/About page, setup, status, and costs screens around accessible Spectre panels, grids, semantic chips, progress, emoji/ASCII fallbacks, and a high-contrast shared palette.
- Added a branded `--version` About panel with the GitHub repository and creator site; grouped help by task category; and turned `--costs` into a semantic cost dashboard.
- Added strict structured responses for chat and one-shot queries, a Markdown command-candidate menu with safe default **Do not execute commands**, an `Avvia chat` continuation, and continued use of the existing exact-preview/risk/explicit-authorization gate.
- Added localized console-only chat/query system instructions and privacy-filtered runtime context (working directory, platform/shell, CPU, RAM, GPU), including network-path withholding and no generic-writing/image-generation behavior.
- Aligned GPT-5.6 prompt caching to reuse shape: stable explicit prefixes, implicit append-only chat checkpoints, explicit-only one-shot query caching, provider cache read/write metrics, and a post-response snapshot containing total context plus input/output tokens.

Validation: preflight, restore, formatter apply/verification, XML summary check, Release build with 0 warnings/errors, 68/68 integrated tests, `git diff --check`, and non-interactive `--version`, `--help`, and `--status` smoke checks passed.

## 2026-08-16 — Readability, resilience, and terminal UX refactor

- Corrected ordered multi-token query parsing and made SQLite initialization version-safe, transactional, repairable, and WAL-consistent.
- Extracted AI conversation, authorized-command, provider request/response, and SQLite schema responsibilities from oversized services without weakening command authorization or audit boundaries.
- Fixed one-shot query visibility and prevented scalar or malformed optional Costs API errors from aborting the requested AI operation.
- Rebuilt the terminal experience around compact frameless headings, whitespace, color, responsive status lines, staged setup, optional advanced settings, and secret input that reveals neither value nor length.
- Added `Esc` current-flow cancellation, fail-closed authorization cancellation, and `Ctrl+C` application shutdown; exact `/run` without a command now fails locally instead of reaching the model.
- Removed confirmed-unused PowerShell helpers and redundant contract members, automated the multi-platform quality gate, and documented post-commit/push cleanup.

Validation: preflight, restore, format verification, XML comments, Release build with 0 warnings/errors, 55/55 integrated tests, read-only CLI commands in disposable data directories, and live `Esc` / `Ctrl+C` prompt smokes passed.

## 2026-08-16 — Windows release artifact builder

- Added one PowerShell entry point with a read-only plan mode, bounded release output, and fail-fast prerequisite checks.
- Added deterministic self-contained `win-x64` and `win-arm64` ZIP archives for WinGet, multi-file schema 1.12 manifests, package hashes, and a machine-readable release summary.
- Added an optional per-user x64 MSI built and ICE-validated with WiX Toolset 3.14, including upgrade metadata, `%LOCALAPPDATA%\Programs\PromptMeUp` installation, and installer-owned user PATH registration.
- Added `hm --where` / `hm -where` with exact executable reporting, a copyable change-directory command, and an explicitly previewed and confirmed native file-manager action in all six UI languages.
- Added current-architecture version and executable-location smoke testing and excluded helper scripts, secrets, local data, logs, symbols, and build intermediates from distributable payloads.
- Added successful-build cleanup so `artifacts/release/<version>` retains only packages, checksums, WinGet manifests, and the release summary needed for user testing.
- Documented local WinGet installation, direct MSI testing, prerequisites, output layout, and the portable-first product boundary.

Validation: release `0.1.1` generation and `winget validate` passed; MSI tables confirmed `LocalAppDataFolder`, installer-owned child-directory cleanup, and user PATH registration; a silent upgrade from `0.1.0` returned exit code `0`, removed the old ProductCode, installed `0.1.1`, and left no machine PATH entry; packaged and installed `hm -where` smokes passed; the integrated quality gate passed 55/55 tests.

## 2026-08-12 — Initial PromptMeUp product foundation

- Created the .NET 10 `hm` console product with Spectre.Console and Serilog through `ILogger<T>`.
- Added six-language setup, query/chat flows, short memory, OpenAI Responses integration, prompt caching, token/context/cost status, and YAML runtime prompts.
- Added mandatory command preview/authorization, local and optional AI risk review, bounded PowerShell execution, and credential redaction.
- Added SQLite settings, normalized usage/pricing/costs, flexible session events, and activity audit storage.
- Added portable PATH management, an opt-in Nerd Font helper, MIT/public-repository documentation, tests, and a manual build/lint workflow.

Validation is recorded in `docs/VALIDATION.md` and the publication commit history.

## 2026-09-01 — Public open-source readiness

- Confirmed the public repository's canonical MIT license, package metadata, third-party notices, security policy, contribution guide, and cross-platform quality gate.
- Added Dependabot for NuGet and GitHub Actions, CodeQL analysis for C#, structured bug and feature forms, a pull-request template, and a public support path through GitHub Discussions.
- Enabled GitHub Discussions; disabled the duplicate Wiki; enabled Dependabot alerts and automatic security updates; and added public discovery topics and standard labels.
- Protected `main` with up-to-date required quality checks, pull requests, resolved conversations, linear history, and blocked force-pushes and deletions. Administrative bypass remains available for repository recovery.
- Ignored Windows `desktop.ini` noise and kept the optional MSI release path explicitly dependent on WiX Toolset 3.14 rather than treating it as an open-source prerequisite.

Validation: required repository gate passed with zero build warnings/errors and 90/90 tests; isolated `--version` and `--third-party` smoke tests passed with a disposable data directory; `git diff --check` passed. GitHub settings were read back after application.

## 2026-09-01 — Product motto

- Added the public motto `Yet another CLI AI assistant :-)` beneath the README title and in the terminal About surface for every language.

Validation: full repository gate passed with zero build warnings/errors and 90/90 tests; the non-interactive `--version` smoke test rendered the exact motto; `git diff --check` passed.

## 2026-09-01 — Product-led public documentation

- Reframed the README, contributor, support, security, CLI, architecture, privacy, cost, validation, packaging, and runtime-resource documentation around user outcomes and durable product promises.
- Kept technical, licensing, privacy, and command-authorization details explicit while moving implementation mechanics behind the experience they support.
- Corrected the README navigation anchor, described `hm` as the cross-platform public command, and aligned contributor wording with portable archives as the canonical distribution plus optional platform installers.

Validation: full repository gate passed with zero build warnings/errors and 90/90 tests; isolated non-interactive `--help` and `--version` smoke tests passed; `git diff --check` passed.

## 2026-09-01 — Cross-platform quality-gate repair

- Declared CRLF checkout behavior for C# files in `.gitattributes`, matching the existing repository-wide `.editorconfig` contract on Linux as well as Windows.
- Made the animated Markdown test strip host-emitted ANSI control sequences before asserting semantic text, while continuing to reject raw Markdown markers.
- Kept production rendering unchanged; the failure was isolated to checkout policy and test-output normalization.

Validation: full repository gate passed locally with zero build warnings/errors and 90/90 tests; the previously failing animation test passed in isolation; Git resolved `TerminalViewTests.cs` as `text eol=crlf`; `git diff --check` passed.

## 2026-09-02 — Synchronize local main without merge commits

- Backed up the 24 locally changed or added files and confirmed that their changes were already included in the remote tree; three-way comparisons of the nine differing documentation files matched the remote exactly.
- Fast-forwarded `main` from `6da473d` to `89ec9f0`, preserving the local work without creating commits, branches, or worktrees.

Validation: full repository gate passed with zero build warnings/errors and 90/90 tests; isolated `--help`, `--version`, Vietnamese `--status`, and French `--third-party` smoke tests passed with a disposable data directory. `main` matched `origin/main`, and the fetched range contained no merge commits.

## 2026-09-02 — Ten actionable code-review findings

- Reviewed the current application at `89ec9f0` and recorded ten distinct findings in [the review report](review-2026-09-02.md), including source references, reproduction evidence, impact, and recommended corrections.
- Confirmed three credential-handling failures, two timeout failures, misleading local risk classification, lost assistant answers, a shell mismatch, ignored long-context prices, and an unnecessary historical table scan.
- Reproduced all ten cases in an ignored local probe project with isolated SQLite data and simulated HTTP responses; no real AI calls, credentials, PATH changes, or font changes were used.
- Preserved concurrent repository work and left application code, existing tests, and runtime prompts unchanged.

Validation: ten dedicated defect reproductions confirmed; the full repository gate passed with zero build warnings/errors and 90/90 existing tests. Isolated `--help` and `--version` smoke tests passed. The existing suite does not cover the reproduced failure cases.

## 2026-09-02 — PowerShell script workshop

- Added complete script generation, revision diffs, named-parameter guidance, and explicit saving to new files.
- Added authorized parser-only validation and optional installed PSScriptAnalyzer support; source is never evaluated.
- Added tests for parser isolation, invalid syntax, artifact shape, option scope, and overwrite prevention.
- Validated through the full repository gate and isolated CLI smoke checks; delivered in the second stacked PR.
## 2026-09-02 — Resumable guided plans

- Added bounded 1–8-step console plans with durable local progress and an explicit resume identifier.
- Kept action and verification commands behind separate risk review, exact preview, and approval gates.
- Marked intent before execution; interrupted actions are verified on resume and never replayed automatically.
- Added exclusive plan leases, working-directory checks, user-confirmed outcomes, tests, and six-language guidance.
- Delivered after the full gate as the third stacked feature PR.
## 2026-09-02 — Concrete file-effect previews

- Added bounded local snapshots for copy, move, prefix rename, and delete operations without AI or shell simulation.
- Displayed exact mappings, bytes, and collisions; blocked links, unsupported operations, recursion, and overwrites.
- Rechecked snapshot metadata after review, preserved per-command approval, and stopped batches on failure or denial.
- Added tests for no-mutation previews, collisions, stale source, late destinations, literal quoting, links, and CLI scope.
- Delivered after the full gate as the fourth stacked feature PR.
## 2026-09-03 — Personal parameterized recipes

- Added local recipe listing, inspection, import/export, and saving from completed plans.
- Added schema validation, declared parameters, prerequisite review, and quoted data binding without source substitution.
- Every reuse creates a new resumable plan with fresh approvals; imports cannot inherit progress or authorization.
- Added tests for parameter quoting, credential rejection, definition isolation, overwrite prevention, schema/status handling, and CLI scope.
- Completed the five-feature series with one commit and assigned stacked PR per feature after the full validation gate.

## 2026-09-03 — Resolve SQLite dependency PR conflicts

- Integrated current `main` into PR #3, preserving Microsoft.Data.Sqlite 10.0.11 and the three Microsoft.Extensions 10.0.11 updates already merged into `main`.
- Aligned the affected runtime dependency versions in the CLI attribution table and third-party notices.

Validation: the full repository gate passed with zero build warnings/errors and 182/182 tests. Isolated `--help`, `--version`, `--third-party`, and `--status` smoke checks passed with disposable data directories; the attribution output shows the four resolved 10.0.11 versions. `git diff --check` passed.

## 2026-09-03 — Printable CLI manual and command discovery

- Reviewed the latest feature commit and added README guidance for the five new workflows: diagnosis, PowerShell script drafts, resumable plans, file-effect previews, and reusable command recipes.
- Clarified that the portable archive needs no installation: add the extracted `hm` folder to the user PATH, with `hm --path install`, `status`, and `remove` scoped to that entry.
- Added a retro-styled two-page PDF manual: a printable all-command card on page one and workflow details, examples, and safety notes on page two.
- Marked PDF files as binary so Git preserves the validated document bytes instead of applying text line-ending conversion.

Validation: preflight, restore, format verification, XML comment check, Release build with zero warnings/errors, and 182/182 tests passed. The isolated `--help --no-animation --no-emoji` smoke test passed with a disposable `PROMPTMEUP_DATA_DIR`; `git diff --check` passed. The final two-page PDF was rendered to PNG and visually inspected.

## 2026-09-04 — GitHub documentation discovery and GA4 attribution

- Reframed the README title and opening copy around PromptMeUp's searchable product identity: a safety-first, open-source AI command-line assistant for Windows, Linux, and macOS.
- Made the primary user, contributor, privacy, security, architecture, packaging, release, and validation document titles self-describing when surfaced as individual GitHub pages.
- Added a consistent GA4 campaign taxonomy only to the three README links that lead to owned, analytics-enabled websites: `github` / `referral` / `promptmeup`, with placement-specific `utm_content` values.
- Kept internal navigation, GitHub actions and community links, downloads, security reporting, third-party sources, and template URLs untagged so canonical destinations and analytics attribution remain clean.

Validation: all three tagged destinations returned HTTP 200 and retained their complete UTM query strings. Repository preflight, restore, format verification, XML comment check, Release build with zero warnings/errors, and 182/182 tests passed. The isolated `--help --no-animation --no-emoji` smoke test passed with a disposable `PROMPTMEUP_DATA_DIR`; `git diff --check` passed.

## 2026-09-04 — Live GitHub discovery metadata

- Replaced the live GitHub repository description with concise product language identifying PromptMeUp as a lightweight, open-source, safety-first AI CLI for Windows, Linux, and macOS.
- Preserved the existing relevant topics and added `ai-assistant`, `command-line`, `cli-tool`, `developer-tools`, `dotnet-10`, and `openai-api` for more precise GitHub discovery.
- Left the repository homepage unset because there is not yet a dedicated PromptMeUp landing page; a generic personal or unrelated product URL would weaken the repository's identity.

Validation: GitHub's repository API returned the exact new description and the expected 14-topic set after the remote update. No release was created; the related README and documentation improvements are published through the dedicated documentation branch and pull request.

## 2026-09-04 — Dedicated product-page attribution

- Added a prominent README link to the dedicated PromptMeUp product page on `umbertogiacobbi.biz`.
- Routed the origin-story link to the same product page and normalized all PromptMeUp `utm_content` values to snake_case.
- Kept internal, community, release, and GitHub-native links untagged so campaign attribution remains limited to owned analytics-enabled destinations.

Validation: repository preflight, restore, format verification, XML comment check, Release build with zero warnings/errors, and 182/182 tests passed; `git diff --check` passed for the README change.

## 2026-09-18 — Keep the settings editor open after saving

- Saving settings now persists the validated draft and immediately reopens the editor on its active section, with a visible saved confirmation.
- Cancelling still exits the editor without persisting an unsaved draft.
- Added workflow coverage for the save, reopen, and cancel sequence.

Validation: preflight, restore, formatting verification, XML comment check, and Release build with warnings treated as errors passed. Automated tests were not run because they were not requested. Release build output was cleaned after validation.

## 2026-09-25 — Show build timestamp and Git commit in version details

- Stamp the full source commit into assembly metadata when building, alongside the existing UTC compilation timestamp.
- Show the ISO 8601 timestamp with its embedded `+00:00` offset and the full Git commit in the shared About/OOBE installation details and `hm --version`.
- Keep labels translated in all six languages and use a single-column layout for the new CLI details.

Validation: inspected the scoped diff, checked whitespace, and parsed the MSBuild targets XML. Automated tests and builds were not run because they were not requested for this change.

## 2026-09-25 — Place chat input near the terminal bottom

- Added a reusable opt-out prompt placement helper that adds normal scrollback lines rather than replacing the terminal buffer.
- Positioned chat input near the bottom when the editor opens and after terminal resize; non-interactive output retains its existing flow.

Validation: inspected the scoped diff and checked whitespace. Automated tests and builds were not run because they were not requested for this change.

## 2026-09-25 — Place plan and script choices near the terminal bottom

- Applied the shared prompt placement to plan start and outcome confirmations, script action and save choices, and the script revision prompt.
- Kept command authorization beside its risk and command preview; query, direct, and diagnose continue into the shared chat input when the user chooses more turns.

Validation: inspected the scoped diff and checked whitespace. Automated tests and builds were not run because they were not requested for this change.

## 2026-09-25 — Brighten all built-in theme accents

- Increased accent and information color saturation across all 13 themes, brightened dividers, and matched each selection background to its accent.
- Kept background, primary text, and status meanings unchanged; updated the built-in default cyan palette and two affected theme descriptions.

Validation: parsed all 13 JSON palettes, reviewed the scoped diff, and calculated text, divider, and selection contrast ratios; all exceed the catalog thresholds. Automated tests and builds were not run because they were not requested for this change.

## 2026-09-25 — Add a bottom prompt bar to AI conversation input

- Added a reusable, theme-aware prompt bar with optional separators, current model, context usage meter, session cost, and measured system/user/assistant/free context categories when the terminal has room. The guide is labeled as part of system tokens; no tool-token count is fabricated.
- The multi-line editor redraws the bar while typing and removes it on submit, leaving accepted turns in normal scrollback.
- Chat and its query/diagnostic continuations use the current local session snapshot; diagnostic evidence input begins with the selected model.

Validation: inspected the scoped diff and checked whitespace. Automated tests and builds were not run because they were not requested for this change.

## 2026-09-25 — Show command-result tokens in the chat context bar

- Mark command-result follow-ups locally while preserving their user role in OpenAI requests. Estimate their text tokens separately from human prompts and carry the breakdown into both the active chat bar and the session summary.
- Give tool output its own colored share and six-language label. At narrow terminal widths, prioritize full labels for system, user, tool, and assistant over abbreviated extra metrics.
- Built the Debug solution without warnings or errors and launched the updated Italian chat at 80 columns. The bottom bar displayed the four named context categories; no AI request or command was submitted.

Validation: preflight, XML comment check, Debug build, and interactive UI preview passed. Automated tests were not requested. The auto-review rejected `dotnet format --verify-no-changes` because the owner had not explicitly authorized a formatter.

## 2026-09-25 — Keep chat startup compact with on-demand help

- Reduced the initial chat command list to the essential `/run`, `/status`, `/exit`, and `/help` shortcuts so the conversation and bottom prompt bar have more room in an 80-column terminal.
- Added `/help` as a local command that shows the complete localized chat and memory guide without an AI request. Removed the obsolete intro parameter while keeping the full guide available on demand.

Validation: XML comment check and Debug build passed. Launched the Italian chat, confirmed the compact introduction and full `/help` output, then exited without contacting the AI provider. Automated tests were not requested; Debug outputs were cleaned after the preview.

## 2026-09-18 — Allow two-digit settings shortcuts

- Extended the first-digit waiting window to one second so section shortcuts such as `12` are reliable without pressing Enter.

Validation: formatting verification, XML comment check, and Release build with warnings treated as errors passed. Automated tests were not run because they were not requested. Release build output was cleaned after validation.

## 2026-09-22 — Remove command recipes

- Removed the `--recipes` command and its parser state, workflow, storage, view, model, service registrations, and dedicated tests.
- Removed recipe-specific UI text, navigation, runtime prompt guidance, and public documentation while retaining the shared plan artifact limit.

Validation: preflight, formatting verification, XML comment check, and Release build with warnings treated as errors passed. Automated tests and CLI smoke tests were not run because they were not requested. Release build output was cleaned after validation.

## 2026-09-22 — Consolidate console workflow controls

- Made execution confirmation and the in-work session summary persisted global preferences, editable in setup and through natural-language conversation; `--direct` remains a temporary override.
- Added the operating system, effective PowerShell execution shell, selected script runtime, and current working directory to provider-bound operational prompts.
- Reused the fullscreen viewport, navigation, and menu components across Help, Setup, and Skills; menus now use plain numbered choices.
- Added a persisted script-language preference with first-run detection, reviewable save/temporary execution choices, and language-specific validation.
- Aligned the separate PromptMeUp, MailMeUp, and WorkTrail MSIX scripts around Debug and Store channels, canonical artifact paths, and certificate-to-publisher validation.
- Added a visible PromptMeUp Start-menu entry that launches the packaged console application with `hm --help`, while retaining a separate hidden `hm.exe` alias entry for normal commands.
- Produced and installed the signed local Debug MSIX `0.0.1.2210` for owner testing; no Store upload or publication was performed.

Validation: PromptMeUp preflight, scoped formatting verification, XML comment check, Release build with warnings treated as errors, 988/988 automated tests, PowerShell package-script syntax check, `git diff --check`, MSIX packaging/signature verification, and current-user installation passed. CLI smoke tests and publication were not run. Debug and Release build outputs were cleaned after validation.

## 2026-09-22 — Refine guided plan steps

- Deferred the session summary produced while generating a plan and render it after every completed guided step instead.
- Numbered plan rows from zero with keycap emoji, while retaining text-only numbers when emoji are disabled.
- Raised the plan limit to ten steps and added a localized message for plans that exceed that depth.
- Added a spaced, localized resume instruction above the full plan command and its identifier.

Validation: preflight, formatting verification, XML comment check, and Release build with warnings treated as errors passed. Automated tests and CLI smoke tests were not run because they were not requested. Release build output was cleaned after validation.

## 2026-09-18 — Improve settings and chat guidance

- Reframed Privacy as a full-width guide with content scrolling and clear section headings.
- Numbered settings sections and added direct no-Enter navigation for sections 1–12.
- Rebuilt the opening header, moved navigation and the current directory before the session summary, and corrected the reasoning icon spacing.
- Updated chat to use the configured preferred name and begin typing directly on the prompt line.

Validation: preflight, restore, formatting verification, XML comment check, and Release build with warnings treated as errors passed. Automated tests were not run because they were not requested. Release build output was cleaned after validation.

- [x] Keep direct mode limited to one application session and clear its persisted setting on `--reset` and when the session ends.

- [x] Render command-line prompts beside the You > label and let them wrap across the available terminal width.
- [x] Repair the local test database scope constraints with direct SQL, preserve the affected rows, and retain a pre-change backup.

- [x] Move legacy skill settings into the global settings table, remove the obsolete table from the local database, and add an idempotent startup migration for older databases.

- [x] Make --reset clear setup and direct-mode flags only, and make --reset all delete/recreate the full SQLite database.

## 2026-09-25 — Share terminal action bars and choice menus

- Extracted reusable terminal action bars with theme-aware focus and optional button brackets, then applied them to fullscreen forms, menus, help, memories, and About.
- Added shared single-choice, multiple-choice, and numbered menu rendering. First-run setup, compact settings setup, and the home menu now use the same choice presentation while preserving their keyboard flows.
- Recorded focused checkpoint commits `5abc25a`, `77636b1`, `8a54361`, and `c5a2966`.
- Validation: scoped source review and `git diff --check`. No build, automated tests, or interactive UI check was run because they were not requested.
