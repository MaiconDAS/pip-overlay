<p align="center">
  <img src="docs/assets/banner.svg" alt="PiP Overlay — Seu vídeo. Seu jogo. Seu espaço." width="100%" />
</p>

<p align="center">
  <a href="https://github.com/MaiconDAS/pip-overlay/releases"><img alt="Versão" src="https://img.shields.io/github/v/release/MaiconDAS/pip-overlay?include_prereleases&amp;style=flat-square&amp;color=6ce7c4&amp;label=vers%C3%A3o" /></a>
  <img alt="Windows 10 e 11, x64" src="https://img.shields.io/badge/Windows-10%20%2F%2011%20%C2%B7%20x64-233448?style=flat-square" />
  <img alt="DirectX, OpenGL e Vulkan" src="https://img.shields.io/badge/renderiza%C3%A7%C3%A3o-DirectX%20%2F%20OpenGL%20%2F%20Vulkan-233448?style=flat-square" />
  <a href="LICENSE"><img alt="Licença MIT" src="https://img.shields.io/badge/licen%C3%A7a-MIT-6ce7c4?style=flat-square" /></a>
  <img alt="Sem RTSS ou extensão" src="https://img.shields.io/badge/sem-RTSS%20ou%20extens%C3%A3o-233448?style=flat-square" />
</p>

<p align="center"><strong>Assista ao seu vídeo enquanto joga. Os cliques continuam no jogo.</strong></p>
<p align="center">
  <a href="https://github.com/MaiconDAS/pip-overlay/releases"><strong>Baixar instalador</strong></a> ·
  <a href="#como-usar">Como usar</a> ·
  <a href="docs/DEVELOPMENT.md">Compilar</a> ·
  <a href="https://github.com/MaiconDAS/pip-overlay/issues/new/choose">Relatar problema</a>
</p>

## O que é

PiP Overlay é um aplicativo para Windows que leva o Picture-in-Picture do Edge ou Chrome para dentro de jogos DirectX, OpenGL e Vulkan. Use o PiP sobre **seu jogo em fullscreen**, quando compatível com o renderizador. A imagem é desenhada pelo renderizador nativo, sem uma janela interativa sobre o jogo.

Abra o PiP do navegador. Para iniciar Jogos nas novas APIs, use **Iniciar jogo configurado** no menu do ícone da ferramenta perto do relógio; escolha o executável na primeira vez. A ferramenta conecta o vídeo automaticamente e mantém os controles fora da partida.

## Mudanças na versão 0.14

- **Instalador completo:** atalho na área de trabalho (opcional e marcado por padrão), menu Iniciar e entrada em Aplicativos instalados / Programas e Recursos.
- **Desinstalação:** pelo Windows, pelo menu Iniciar ou pelo atalho na pasta do app. Preferências e arquivos pessoais são preservados.
- **Atualização na mesma pasta:** mantém os ajustes. Instalação por usuário em AppData, sem administrador, serviço ou inicialização automática.
- **Ícone novo:** símbolo arredondado em azul profundo e verde-menta, com nove tamanhos de 16 a 256 pixels para o executável, atalhos e bandeja.

![Novo ícone do PiP Overlay](docs/assets/app-icon.png)

## Mudanças na versão 0.13

- **Escolha visual do vídeo:** quando há vários PiPs, o botão Vídeos abre miniaturas para selecionar a origem com um clique.
- **Posicionamento mais fácil:** encaixe nas bordas e no centro, margem ajustável, centralização e desfazer a última posição. Segure Alt para arrastar livremente; Ctrl+Z funciona somente no editor.
- **Perfil visível:** nome do jogo, restauração dos padrões e cópia dos ajustes para outro executável.
- **Status mais claros:** diferencia espera por vídeo, espera por jogo, captura interrompida e conexão não confirmada.
- **Assinatura preparada:** fluxo de assinatura e verificação disponível; o instalador continua sem assinatura pública enquanto não houver certificado.

Veja a [validação da 0.13](docs/VALIDATION-0.13.md) e o [fluxo de assinatura](docs/SIGNING.md).

## Mudanças na versão 0.12

- **Imagem estável sob contenção:** DirectX 9 e Vulkan preservam o último quadro enquanto aguardam o próximo.
- **Menos memória na captura:** elimina duas texturas intermediárias no caminho BGRA em tamanho nativo.
- **FPS automático opcional:** ajusta a frequência entre os limites escolhidos, preservando a resolução.
- **Perfis por jogo:** posição, tamanho, monitor, resolução e FPS são lembrados por executável.
- **Atualizações e diagnóstico:** aviso de nova versão, importação de preferências no instalador e relatório técnico sem imagens, URLs ou caminhos pessoais.
- **Tela principal enxuta:** configurações adicionais ficam em Ajustes.

Veja a [validação da 0.12](docs/VALIDATION-0.12.md) e a [matriz de testes](docs/TEST-MATRIX.md). O transporte compartilhado por GPU foi prototipado, mas não está ativado na distribuição: ainda precisa demonstrar ganho e compatibilidade.
## Mudanças na versão 0.11.1

- **Resolução e FPS independentes:** escolha até 1080p, 720p, 480p ou adaptação ao tamanho, junto de 60, 30 ou 15 fps. É possível usar 1080p a 30 fps.
- **Tela principal mais simples:** botão Abrir jogo removido. A conexão automática continua; a inicialização necessária às novas APIs fica no menu do ícone perto do relógio.
- **Open source:** código próprio publicado sob licença MIT, mantendo as licenças das dependências.

## Mudanças na versão 0.11

- **Mais nitidez:** captura em resolução nativa até 1080p no modo Até 1080p, evitando redução intermediária quando a fonte permite.
- **Mais renderizadores:** DirectX 11, DirectX 12 e OpenGL com motor incluído; Vulkan com camada própria, em 32 e 64 bits.
- **Abertura de Jogos:** botão **Abrir jogo**, seleção salva e reconexão ao mesmo processo após reabrir a ferramenta.
- **Controle de consumo:** opções de até 60, 30 e 15 fps; resolução e taxa efetiva no status.
- **Distribuição:** um instalador com dependências incluídas e interface sem ferramentas internas de teste.

Consulte o [histórico completo](CHANGELOG.md), a [compatibilidade com Jogos](docs/COMPATIBILITY.md) e os [resultados dos testes](docs/VALIDATION-0.11.md).

## Como usar

1. Baixe **`Instalar-PiP-Overlay.exe`** em [Releases](https://github.com/MaiconDAS/pip-overlay/releases).
2. Execute o instalador e conclua a instalação. O atalho na área de trabalho vem marcado por padrão; o menu Iniciar e a opção de desinstalar são criados automaticamente. O .NET já está incluído.
3. Deixe o aplicativo aberto e abra o vídeo em PiP no **Microsoft Edge** ou **Google Chrome**.
4. A conexão com jogos reconhecidos é automática. Para as novas APIs, clique com o botão direito no ícone PiP Overlay perto do relógio e escolha **Iniciar jogo configurado**. Na primeira vez, selecione o executável.

**Não é necessário instalar extensão, RTSS ou criar uma conta de desenvolvedor.** O PiP normal do navegador continua sendo a fonte de vídeo e precisa permanecer aberto, sem minimizar.

| Você quer… | Faça assim |
| --- | --- |
| Mover ou redimensionar | Arraste o vídeo ou as bordas no editor. |
| Escolher monitor, qualidade ou atalhos | Abra **Ajustes**. |
| Suspender o funcionamento | Use **Pausar**; a reconexão também fica suspensa. |
| Voltar a assistir | Use **Retomar** ou o atalho configurado. |
| Deixar rodando sem a janela | Feche a janela; o aplicativo fica perto do relógio. |
| Ajustar consumo automaticamente | Em **Ajustes**, habilite **FPS automático** e escolha o FPS mínimo. O campo FPS define o máximo. |
| Exportar informações para suporte | Use **Ajustes → Diagnóstico**; nenhum dado é enviado automaticamente. |
| Verificar versão nova | Use **Ajustes → Atualizações**. O aviso automático pode ser desativado. |
| Encerrar completamente | Clique em **Sair** no menu do ícone perto do relógio. |

O aplicativo não configura inicialização automática do Windows. Depois de reiniciar o computador, abra `PipOverlay.exe` novamente.

## Um editor que representa sua tela

<p align="center"><img src="docs/assets/editor.png" alt="Editor do PiP Overlay com área proporcional ao monitor, controles de redimensionamento e botão Pausar" width="960" /></p>

Em **Ajustes**, escolha **Resolução: Até 1080p** e **FPS: 30 fps** para priorizar nitidez com menos atualizações. Os controles são independentes: alterar o FPS não muda a resolução. A ferramenta amplia temporariamente o PiP de origem e restaura seu tamanho ao parar. O navegador pode impor um limite menor; a resolução real aparece no status.

A posição e o tamanho são proporcionais ao monitor selecionado. A interação com mouse e teclado fica no aplicativo; o renderizador não cria controles clicáveis dentro do jogo.

## Recursos

- **Conexão automática** quando o PiP e o jogo estão disponíveis.
- **Pausa persistente**, sem reiniciar a overlay contra a escolha do usuário.
- **Editor visual** com arraste e redimensionamento proporcional.
- **Atalhos configuráveis** para retomar e pausar.
- **Captura até 1920 × 1080**, sem redução intermediária no modo Até 1080p, limitada pela fonte.
- **Até 60 fps**, com alternativas de 30 e 15 fps.
- **Um instalador independente**, sem downloads extras durante a instalação.

## Compatibilidade com Jogos

| Item | Estado |
| --- | --- |
| Sistema | Windows 10 a partir da versão 2004 / Windows 11, x64 |
| Navegadores de origem | Edge e Chrome, usando o PiP nativo |
| DirectX 9 | Legado para processos reconhecidos; não é suporte genérico a todos os jogos D3D9 |
| DirectX 11 / DirectX 12 / OpenGL | Implementados e testados em hosts próprios, em 32 e 64 bits |
| Jogos | Inicialização do executável escolhido; launchers que criam outro processo e proteções podem impedir a conexão |
| Vulkan | Camada própria, testada em 32 e 64 bits; inicialmente SDR RGBA/BGRA UNORM de 8 bits |
| Distribuição | Projeto experimental, versão 0.14.0 |

Os testes automatizados cobrem lógica de conexão, atalhos e geometria. A renderização também foi exercitada em um aplicativo Direct3D 9 próprio, incluindo fullscreen. Isso **não equivale a uma certificação de compatibilidade com todas as versões do jogo ou drivers**. D3D11, D3D12, OpenGL e Vulkan passaram por verificações de pixels em hosts próprios; a captura nativa preservou os pixels de padrões em 720p e 1080p. Veja [os resultados e limites](docs/VALIDATION-0.11.md).

A tabela detalhada de [compatibilidade com Jogos](docs/COMPATIBILITY.md) diferencia implementação, testes em janela/fullscreen e casos ainda não validados. Não há lista de jogos certificados para as novas APIs.

Vídeos protegidos podem não ser capturáveis. O projeto não altera proteções do jogo nem tenta contorná-las.

## Desempenho, sem promessas de custo zero

O navegador continua decodificando o vídeo. O limite de resolução controla o tamanho capturado; o FPS controla a frequência das atualizações. Resoluções menores são reduzidas na GPU. Os quadros seguem por leitura assíncrona e memória compartilhada. Não há codificação nem gravação de imagens por quadro.

Ainda existe transferência pela CPU e consumo de GPU. Um overlay maior custa mais. O modo Até 1080p custa mais que os modos reduzidos. **Ainda não há comparação controlada do impacto desta versão em jogos reais**. Em repouso, a busca ocorre a cada 1,5 segundo; seu consumo ainda não foi medido separadamente.

Para uma máquina mais limitada, comece com um vídeo pequeno e **30 fps**, ajustando a resolução separadamente. Saiba mais em [arquitetura e validação](docs/ARCHITECTURE.md).

## Desenvolvimento

O projeto usa **C# / WinForms / .NET 8** na interface e **C++ / Direct3D** na captura e renderização. O compilador nativo portátil é Zig 0.14.1; MinHook 1.3.4 está incluído com sua licença. O add-on usa MSVC portátil, e o pacote inclui ReShade 6.8.0; nada disso exige instalação manual pelo usuário.

```powershell
pwsh ./scripts/Initialize-Tools.ps1
pwsh ./scripts/Package.ps1
```

O instalador é gerado em `dist/installer/`. Consulte [DEVELOPMENT.md](docs/DEVELOPMENT.md) para requisitos, testes e compilação individual.

## Contribuir

Encontrou um problema? Abra uma [issue](https://github.com/MaiconDAS/pip-overlay/issues/new/choose) com a versão do aplicativo, navegador, Windows e passos para reproduzir. As orientações estão em [CONTRIBUTING.md](CONTRIBUTING.md).

O código próprio do projeto é **open source sob a [licença MIT](LICENSE)**. Você pode usar, modificar e redistribuir, inclusive comercialmente, preservando o aviso de copyright e a licença. As licenças e atribuições das dependências estão em [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).

---

<p align="center">Criado por <a href="https://github.com/MaiconDAS">MaiconDAS</a> · Projeto independente, sem afiliação com Microsoft, Google, MSI ou desenvolvedores de jogos.</p>
