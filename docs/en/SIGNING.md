# Code signing

[Português](../SIGNING.md) · [English index](README.md)

The distributed installer currently has **no public code signature**. The project owner does not yet have a verified signing certificate/service. Published SHA-256 checksums verify integrity, not publisher identity.

The workflow is prepared in `scripts/Sign-Release.ps1`. This command only inspects prerequisites:

```powershell
./scripts/Sign-Release.ps1 -CheckOnly
```

Once a legitimate code-signing certificate with accessible private key exists in `CurrentUser/My`, use an already installed SignTool and the certificate authority's RFC 3161 timestamp endpoint:

```powershell
./scripts/Sign-Release.ps1 -SignTool 'PATH/signtool.exe' -Thumbprint '40_CHARACTER_THUMBPRINT' -TimestampUrl 'https://CA-ENDPOINT'
```

The script signs a copy in a new artifacts folder, verifies signature/identity/timestamp and generates a new hash. It preserves the original installer and does not install certificates or change Windows settings. Never store private keys or credentials in the repository. Real signing and trust-chain validation remain pending; signing is not a guarantee that SmartScreen will show no warning.

The current script signs the outer Inno executable only. Fully signed distribution also requires configuring Inno's SignTool and SignedUninstaller before compilation using a legitimate certificate. This remains pending. Reference: [Microsoft SignTool](https://learn.microsoft.com/en-us/windows/desktop/seccrypto/signtool).

Prerequisite inspection and rejection of incomplete parameters were tested. The script does not request passwords in source code. File and timestamp digests use SHA-256 and verification uses Authenticode policy.
