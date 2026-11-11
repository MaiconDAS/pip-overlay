# Releases e distribuição

**Português** · [English](en/RELEASES.md)

A release atual é a **0.19.0**, promovida de candidata em 11 de novembro de 2026, preservando os artefatos de 6 de novembro. [Baixe a release atual](https://github.com/MaiconDAS/pip-overlay/releases/latest). Consulte as [evidências e validações pendentes](VALIDATION-0.19.md) antes de interpretar o status da release como garantia de compatibilidade.

## Arquivo de versões

Versões anteriores são históricas, sem manutenção em branches separadas. A estável anterior, 0.18.0, continua disponível para retorno manual. Faça backup das preferências antes; não há garantia de compatibilidade de preferências novas com versões antigas. Os nomes originais dos arquivos são preservados para não quebrar links.

| Versão | Instalador | Canal |
| --- | --- | --- |
| [v0.19.0](https://github.com/MaiconDAS/pip-overlay/releases/tag/v0.19.0) | `PiP-Overlay-v0.19.0.exe` | Atual |
| [v0.18.0](https://github.com/MaiconDAS/pip-overlay/releases/tag/v0.18.0) | `PiP-Overlay-v0.18.0.exe` | Estável anterior |
| [v0.17.0](https://github.com/MaiconDAS/pip-overlay/releases/tag/v0.17.0) | `PiP-Overlay-v0.17.0.exe` | Estável anterior |
| [v0.16.1](https://github.com/MaiconDAS/pip-overlay/releases/tag/v0.16.1) | `PiP-Overlay-v0.16.1.exe` | Estável anterior |
| [v0.16.0](https://github.com/MaiconDAS/pip-overlay/releases/tag/v0.16.0) | `Instalar-PiP-Overlay.exe` | Estável anterior |
| [v0.15.0](https://github.com/MaiconDAS/pip-overlay/releases/tag/v0.15.0) | `Instalar-PiP-Overlay.exe` | Pré-release histórica |
| [v0.14.1](https://github.com/MaiconDAS/pip-overlay/releases/tag/v0.14.1) | `Instalar-PiP-Overlay.exe` | Pré-release histórica |
| [v0.14.0](https://github.com/MaiconDAS/pip-overlay/releases/tag/v0.14.0) | `Instalar-PiP-Overlay.exe` | Pré-release histórica |
| [v0.13.0](https://github.com/MaiconDAS/pip-overlay/releases/tag/v0.13.0) | `Instalar-PiP-Overlay.exe` | Pré-release histórica |
| [v0.12.0](https://github.com/MaiconDAS/pip-overlay/releases/tag/v0.12.0) | `Instalar-PiP-Overlay.exe` | Pré-release histórica |
| [v0.11.1](https://github.com/MaiconDAS/pip-overlay/releases/tag/v0.11.1) | `Instalar-PiP-Overlay.exe` | Pré-release histórica |
| [v0.11.0](https://github.com/MaiconDAS/pip-overlay/releases/tag/v0.11.0) | `Instalar-PiP-Overlay.exe` | Pré-release histórica |
| [v0.10.0](https://github.com/MaiconDAS/pip-overlay/releases/tag/v0.10.0) | `Instalar-PiP-Overlay.exe` | Pré-release histórica |

## Regras de publicação

1. Alinhar versão do código, changelog, documentação em português/inglês e nome do instalador (`PiP-Overlay-vX.Y.Z.exe`).
2. Registrar o que foi verificado e o que continua pendente. Compilar e validar alterações de código conforme seu alcance; testar mudanças no instalador/sistema em Windows descartável antes da distribuição. Nunca usar o Windows do usuário como primeiro ambiente de teste.
3. Publicar a partir de commit/tag fixo com instalador e `SHA256SUMS.txt`. Preservar tags e binários publicados; mudanças nos binários exigem nova versão.
4. Usar pré-release para candidatas. A promoção altera somente metadados; registrar sua data e decisão separadamente da publicação original. O atualizador do app utiliza releases estáveis.
5. Manter notas bilíngues e links para compatibilidade, privacidade, evidências e changelog. O status da release não implica suporte a todo jogo, GPU ou fullscreen exclusivo.

SHA-256 verifica integridade, não identidade do editor. A assinatura pública continua pendente. Downloads não são executados automaticamente pelo app. Relate problemas em [Issues](https://github.com/MaiconDAS/pip-overlay/issues/new/choose); comunique vulnerabilidades pelo [canal privado](https://github.com/MaiconDAS/pip-overlay/security/advisories/new).
