# Security policy

[Português](docs/pt-BR/SECURITY.md) · [Privacy](PRIVACY.md)

Security fixes target the latest published release. Older versions are not maintained as separate security branches. Experimental API support is not a guarantee of compatibility with protected games.

Report vulnerabilities privately using [Security > Report a vulnerability](https://github.com/MaiconDAS/pip-overlay/security/advisories/new). Private vulnerability reporting is enabled for this repository. If it is unavailable, open an issue requesting a private contact channel, without exploit details, credentials, personal logs or affected-user data. Do not publish working exploits or sensitive reports in a normal issue. Include the version, affected component, reproduction conditions and impact in the private report. No guaranteed response time or bug-bounty program is offered.

The app does not attempt to bypass game or video protections. Do not disable Windows security, install drivers or change system permissions to work around a failure. Local development should use project-local tools. Installer changes must be tested on disposable Windows runners before they reach users.

Use downloads from this repository's releases and compare the published SHA-256. A checksum verifies integrity, not publisher identity. Public code signing remains pending; see [signing](docs/en/SIGNING.md). Never commit signing keys, tokens or personal configuration. See [third-party notices](docs/en/THIRD_PARTY_NOTICES.md) for bundled dependencies.
