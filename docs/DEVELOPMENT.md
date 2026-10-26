# Desenvolvimento

## Requisitos

- Windows 10 2004 ou superior, x64.
- .NET SDK 8 instalado e disponível como `dotnet`.
- PowerShell 7 (`pwsh`).
- Acesso ao NuGet oficial para o primeiro restore.
- Zig 0.14.1 portátil para os componentes nativos.

Os scripts não instalam dependências globalmente nem alteram Registro, serviços, drivers ou inicialização. `Initialize-Tools.ps1` baixa o Zig oficial em `.tools` e valida SHA-256 antes da extração. MinHook 1.3.4 está em `vendor/minhook`.

```powershell
pwsh ./scripts/Initialize-Tools.ps1
pwsh ./scripts/Build.ps1
pwsh ./scripts/Build-Native.ps1
```

As duas compilações usam `artifacts/app` por padrão. Para um Zig já disponível, informe `-ZigPath C:\caminho\zig.exe`. O cache NuGet fica em `.cache/packages`, salvo se `NUGET_PACKAGES` estiver definido no processo.

## Testes sem jogo

```powershell
pwsh ./scripts/Test.ps1
```

O comando executa testes de conexão automática, atalhos simulados e geometria. Não registra atalhos globais nem conecta ao Digimon. O workflow de CI roda esse conjunto em Windows.

Para teste de renderização, compile primeiro os componentes nativos e execute explicitamente:

```powershell
./artifacts/app/PipOverlay.exe --gpu-test ./artifacts/gpu.json
./artifacts/app/PipOverlay.exe --gpu-test ./artifacts/fullscreen.json --fullscreen
```

Esses testes abrem janelas próprias e o segundo ocupa a tela em fullscreen temporariamente. Não os execute durante uma partida. Os comandos de teste são excluídos da build de produção.

## Distribuição

```powershell
pwsh ./scripts/Package.ps1
```

Saídas: `dist/installer/Instalar-PiP-Overlay.exe`, `dist/SHA256SUMS.txt` e o ZIP intermediário. O instalador inclui .NET, componentes nativos e os avisos de licença. Não inclui ferramentas de desenvolvimento, hosts de teste, extensão ou preferências do usuário.

A instalação é uma extração para uma pasta nova escolhida pelo usuário. Não sobrescreve uma pasta existente. A opção de abrir o aplicativo só é executada ao concluir a interface. A extração automatizada pode ser validada em pasta temporária:

```powershell
./dist/installer/Instalar-PiP-Overlay.exe --extract C:\caminho\de\teste\pasta-nova
```

## Estrutura

| Pasta | Conteúdo |
| --- | --- |
| `src/` | Aplicativo WinForms e testes internos de desenvolvimento |
| `native/` | Captura D3D11, transporte, renderização D3D9 e auxiliares |
| `installer/` | Instalador independente com pacote embutido |
| `scripts/` | Preparação local, compilação, testes e distribuição |
| `vendor/minhook/` | Biblioteca de hooks e licença original |
| `docs/assets/` | Identidade visual e imagem do editor |

Arquivos `Rtss*` e validações V4 em `src/` são referências históricas de desenvolvimento. O `.csproj` os exclui da build de produção. O produto atual não depende de RTSS.

Não versionar executáveis, dados de uso, tokens, caches, resultados com caminhos pessoais ou arquivos `PipD3D9-*.bin` gerados em uma máquina específica.
