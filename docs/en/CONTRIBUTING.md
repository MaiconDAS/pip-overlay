# Contributing

[Português](../../CONTRIBUTING.md) · [English index](README.md)

For bugs, include the app version, Windows version, browser, game display mode and reproduction steps. Remove usernames, private paths and credentials from logs and screenshots. Use [private reporting guidance](../../SECURITY.md) for security issues.

1. Branch from `main` and describe the concrete problem.
2. Keep the change focused and preserve existing user preferences.
3. Run `pwsh ./scripts/Test.ps1`.
4. For capture/rendering changes, validate the dedicated graphics hosts and the affected APIs. Document measured results and limits.
5. Submit a pull request explaining before/after behavior and tests.

Do not modify Windows settings, browser policies, drivers or services to make a test pass. Do not bypass game protections. Prefer project-local dependencies and preserve third-party license notices. Installer lifecycle tests belong on disposable hosted Windows runners. Never commit binaries, credentials, personal settings or machine-specific Direct3D catalogs.

Original contributions are offered under the project's [MIT license](../../LICENSE); dependencies retain their own licenses. Update both Portuguese and English user-facing documentation when behavior changes.
