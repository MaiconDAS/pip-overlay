# Repository audit — 0.16

[Português](../REPOSITORY-AUDIT-0.16.md)

The review compared principal documentation, version manifests, usage instructions, build/test/package scripts, issue/PR templates and GitHub metadata with the implementation. Vendored sources retain their original versions and notices.

Current instructions no longer mix in removed buttons, manual installer-import steps or the obsolete statement that GPU transport is disabled. App/Inno/installer-test/workflow versions align at 0.16.0. Compatibility distinguishes rendering APIs, D3D11-only GPU transport, synthetic validation and real games. Newer-API fullscreen is not claimed as certified.

Architecture distinguishes per-user installer integration from normal overlay operation. Privacy documents GitHub update requests and local data instead of claiming no networking. Sanitized diagnostics and third-party logs are separate. Private vulnerability reporting is enabled on GitHub. Principal guides and templates have Portuguese/English entry points.

Older reports are explicitly historical. The current editor image replaces the old README preview. Relative links and version consistency have automated checks. Original MIT and third-party licenses remain intact. Historical RTSS classes stay development-only. Public signing remains pending.

The additional review removed game-name filters from the editor and D3D9 loader. Remaining specific identifiers belong to historical diagnostics excluded from production. Window/renderer discovery does not certify game compatibility.
