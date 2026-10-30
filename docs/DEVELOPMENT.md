**Português** · [English](en/DEVELOPMENT.md)

# Desenvolvimento

## Requisitos

- Windows 10 2004 ou superior, x64.
- .NET SDK 8 instalado e disponível como `dotnet`.
- PowerShell 7 (`pwsh`).
- Acesso ao NuGet oficial para o primeiro restore.
- Zig 0.14.1 portátil para os componentes nativos.

Os scripts de compilação não instalam dependências globalmente nem alteram Registro, serviços, drivers ou inicialização. O teste de integração de instalação altera apenas uma máquina descartável do GitHub. `Initialize-Tools.ps1` baixa o Zig oficial em `.tools` e valida SHA-256 antes da extração. MinHook 1.3.4 está em `vendor/minhook`.

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

O comando executa testes de conexão automática, atalhos simulados e geometria. Não registra atalhos globais nem conecta ao seu jogo. O workflow de CI roda esse conjunto em Windows.

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

Saídas: `dist/installer/PiP-Overlay v0.16.1.exe`, `dist/SHA256SUMS.txt` e o ZIP intermediário. O instalador inclui .NET, componentes nativos e os avisos de licença. Não inclui ferramentas de desenvolvimento, hosts de teste, extensão ou preferências do usuário.

O instalador é compilado com Inno Setup 7.1.0. `Initialize-Installer.ps1` baixa arquivos fixados por SHA256 e extrai o compilador em `.tools`, sem executar seu instalador. O motor de distribuição gerencia atalhos, registro de desinstalação por usuário e atualização.

`Test-Package.ps1` extrai e compara o conteúdo sem instalar no Windows. `Test-InstallerIntegration.ps1` é bloqueado fora de um runner Windows temporário do GitHub: ele instala, verifica atalhos e Registro, atualiza preservando preferências e desinstala. O workflow `installer.yml` recebe a tag de uma release em rascunho. Nunca usar o computador do usuário como primeiro teste de integração do instalador.

O ícone é reproduzível com `Render-AppIcon.ps1`; os arquivos finais já estão versionados e não precisam ser regenerados para compilar.

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

## Compilação e validação gráfica

`Build-Native.ps1` também prepara ReShade 6.8.0 (extração, sem executar seu instalador), os cabeçalhos Vulkan e o compilador MSVC portátil. Downloads da Microsoft e do ReShade têm SHA-256 verificado e ficam em `.tools`. O primeiro build pode baixar centenas de megabytes. Os termos das ferramentas Microsoft continuam aplicáveis. O MSVC é necessário para a ABI do SDK ReShade; Zig compila os demais componentes.

```powershell
pwsh ./scripts/Test-Graphics.ps1 -ZigPath C:\caminho\zig.exe
```

Esse teste abre exclusivamente hosts próprios de D3D11, D3D12, OpenGL e Vulkan, em 32 e 64 bits. Verifica cinco posições e tamanhos por API; requer drivers com suporte às APIs. Também valida a preservação dos pixels em 720p/1080p, restauração da janela de origem e captura contínua em um host D3D9. Não é um benchmark de jogos nem do YouTube. Relatórios locais ficam em `artifacts/tests` e não devem ser versionados porque incluem caminhos da máquina. Veja [VALIDATION-0.16.md](VALIDATION-0.16.md) para os resultados atuais.

Para conferir o instalador sem abrir o aplicativo nem alterar o Windows:

```powershell
pwsh ./scripts/Test-Package.ps1
```

O script compara por SHA-256 todos os arquivos extraídos com o ZIP, verifica a recusa de sobrescrita e procura artefatos internos indevidos. A pasta de teste permanece em `artifacts` para inspeção.

## Testes e manutenção

`Test.ps1` inclui o controlador adaptativo, isolamento de perfis, privacidade do diagnóstico e seleção segura de releases. `Test-Graphics.ps1` também exerce contenção nas novas APIs. Para o D3D9, após compilar os componentes em `artifacts/tests`, execute:

```powershell
./artifacts/tests/PipOverlay.exe --contention-test ./artifacts/tests/contention.json
./artifacts/tests/PipOverlay.exe --gpu-benchmark ./artifacts/tests/benchmark.json
pwsh ./scripts/Test-SharedGpu.ps1 -ZigPath C:\caminho\zig.exe
pwsh ./scripts/Test-Upgrade.ps1
```

O script Test-SharedGpu.ps1 é um microbenchmark isolado entre processos próprios. Ele é diferente do transporte integrado D3D11 habilitado desde a 0.16 e não mede sozinho o benefício no aplicativo. O benchmark registra FPS, p95 do tempo de quadro e CPU em fases sem/com/sem overlay. A importação do instalador é validada exclusivamente em pastas temporárias dentro de artifacts.

Perfis ficam em `data/profiles`; nomes usam hash do executável. A verificação de releases não transmite configurações e pode ser desativada em Ajustes. Sua frequência automática máxima é uma consulta bem-sucedida por dia, ao abrir a ferramenta. A atualização é baixada pelo usuário na página da release; o app não executa um instalador automaticamente.

Para testar miniaturas e edição em uma sessão Windows com DWM, execute scripts/Test-UX.ps1. O modo de desenvolvimento --soak-test usa PipTest32.exe e as bibliotecas nativas junto do executável de testes para 30 ciclos de captura. Esses modos são excluídos da distribuição. Assinatura pública: [SIGNING.md](SIGNING.md).


## Transporte integrado e documentação

Após preparar uma build de desenvolvimento com hosts, bibliotecas e efeitos, use --shared-test report.json pixels 64 (ou 32) para pixels, posição, resolução e fallback. Os modos cpu e gpu comparam os transportes; reconnect verifica oito recriações de sessão. São comandos excluídos da produção. Test-Performance.ps1 executa a matriz de configuração e variantes experimentais. Não compare variantes simultaneamente nem generalize microbenchmarks para FPS de jogos.

Execute scripts/Test-Documentation.ps1 para verificar links locais, pares de idiomas e alinhamento da versão. O nome do instalador vem de scripts/Get-InstallerName.ps1, derivado da versão do projeto. Para uma release nova, atualize os manifestos e as duas edições dos guias.
