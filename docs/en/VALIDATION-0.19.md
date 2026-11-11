# 0.19 — Evidence and limitations

[Português](../VALIDATION-0.19.md) · **English**

Updated November 11, 2026. This report separates user feedback from technical validation.

| Evidence | Scope |
| --- | --- |
| Existing 0.19 build | Compiled before publication as a candidate on November 6; compilation alone does not validate behavior. |
| User feedback on November 11 | The user approved performance and behavior in Black Desert Online and Digimon Masters Online on their home computer. |
| Configuration details | Exact installed build, GPU/driver, resolution, FPS and presentation mode were not included in this latest report. |
| Preliminary performance observations | Variable workload; no reliable percentage improvement established. |
| November 11 repository audit | Documentation and release metadata only. No new application, graphics, installer or CI tests. |

The existing 0.19.0 release was promoted on November 11 after user approval. Installer bytes, original source tag and SHA-256 remain unchanged. This is a release-channel decision, not a claim of universal game compatibility.

Earlier Black Desert feedback identified successful desktop output while direct connection failed. The latest approval does not establish which integration was used in either game. Historical 0.18 checks remain evidence for 0.18; they are not new 0.19 results.

## Remaining work

- Controlled comparisons of CPU/GPU cost and video delivery, including impact on game FPS.
- Exclusive fullscreen across each graphics integration and physical GPU/driver combinations.
- Physical multi-monitor/DPI changes, actual sleep/resume and prolonged sessions.
- Final installer lifecycle verification for this build and public code signing.

No installation or system configuration changes were made during this audit. Future installer/system changes require isolated verification. See [compatibility](COMPATIBILITY.md), [historical matrix](TEST-MATRIX.md) and [release policy](RELEASES.md).
