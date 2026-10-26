<p align="center">
  <img src="docs/assets/banner.svg" alt="PiP Overlay — Seu vídeo. Seu jogo. Seu espaço." width="100%" />
</p>

<p align="center">
  <a href="https://github.com/MaiconDAS/pip-overlay/releases"><img alt="Versão" src="https://img.shields.io/github/v/release/MaiconDAS/pip-overlay?include_prereleases&amp;style=flat-square&amp;color=6ce7c4&amp;label=vers%C3%A3o" /></a>
  <img alt="Windows 10 e 11, x64" src="https://img.shields.io/badge/Windows-10%20%2F%2011%20%C2%B7%20x64-233448?style=flat-square" />
  <img alt="Direct3D 9" src="https://img.shields.io/badge/renderiza%C3%A7%C3%A3o-Direct3D%209-233448?style=flat-square" />
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

PiP Overlay é um aplicativo para Windows que leva o Picture-in-Picture do Edge ou Chrome para dentro de um jogo Direct3D 9. Use o PiP sobre **seu jogo em fullscreen**, quando compatível com o renderizador. A imagem é desenhada pelo renderizador nativo, sem uma janela interativa sobre o jogo.

Você abre o PiP do navegador. A ferramenta encontra o vídeo e o jogo, inicia a overlay e mantém os controles no editor, fora da partida.

## Como usar

1. Baixe **`Instalar-PiP-Overlay.exe`** em [Releases](https://github.com/MaiconDAS/pip-overlay/releases).
2. Execute o instalador, escolha uma pasta nova e conclua a instalação. O .NET necessário já está incluído.
3. Deixe o aplicativo aberto e abra o vídeo em PiP no **Microsoft Edge** ou **Google Chrome**.
4. Abra seu jogo. A conexão é automática; não é preciso voltar ao aplicativo para clicar em Iniciar.

**Não é necessário instalar extensão, RTSS ou criar uma conta de desenvolvedor.** O PiP normal do navegador continua sendo a fonte de vídeo e precisa permanecer aberto, sem minimizar.

| Você quer… | Faça assim |
| --- | --- |
| Mover ou redimensionar | Arraste o vídeo ou as bordas no editor. |
| Escolher monitor, qualidade ou atalhos | Abra **Ajustes**. |
| Suspender o funcionamento | Use **Pausar**; a reconexão também fica suspensa. |
| Voltar a assistir | Use **Retomar** ou o atalho configurado. |
| Deixar rodando sem a janela | Feche a janela; o aplicativo fica perto do relógio. |
| Encerrar completamente | Clique em **Sair** no menu do ícone perto do relógio. |

O aplicativo não configura inicialização automática do Windows. Depois de reiniciar o computador, abra `PipOverlay.exe` novamente.

## Um editor que representa sua tela

<p align="center"><img src="docs/assets/editor.png" alt="Editor do PiP Overlay com área proporcional ao monitor, controles de redimensionamento e botão Pausar" width="960" /></p>

A posição e o tamanho são proporcionais ao monitor selecionado. A interação com mouse e teclado fica no aplicativo; o renderizador não cria controles clicáveis dentro do jogo.

## Recursos

- **Conexão automática** quando o PiP e o jogo estão disponíveis.
- **Pausa persistente**, sem reiniciar a overlay contra a escolha do usuário.
- **Editor visual** com arraste e redimensionamento proporcional.
- **Atalhos configuráveis** para retomar e pausar.
- **Resolução adaptativa até 1920 × 1080**, limitada pela imagem disponível na fonte.
- **30 fps ou modo econômico de 15 fps**.
- **Um instalador independente**, sem downloads extras durante a instalação.

## Compatibilidade e estado do projeto

| Item | Estado |
| --- | --- |
| Sistema | Windows 10 a partir da versão 2004 / Windows 11, x64 |
| Navegadores de origem | Edge e Chrome, usando o PiP nativo |
| Renderizador | Direct3D 9; carregamento direcionado ao processo permitido |
| Jogos | A detecção automática ainda é limitada aos processos suportados; compatibilidade com outros jogos não foi implementada nem validada |
| Outros renderizadores | Direct3D 11/12, Vulkan e OpenGL não implementados |
| Distribuição | Projeto experimental, versão 0.10.0 |

Os testes automatizados cobrem lógica de conexão, atalhos e geometria. A renderização também foi exercitada em um aplicativo Direct3D 9 próprio, incluindo fullscreen. Isso **não equivale a uma certificação de compatibilidade com todas as versões do jogo ou drivers**. A integração com navegador e jogo não foi repetida para cada alteração de interface e empacotamento.

Vídeos protegidos podem não ser capturáveis. O projeto não altera proteções do jogo nem tenta contorná-las.

## Desempenho, sem promessas de custo zero

O navegador continua decodificando o vídeo. A captura é reduzida na GPU antes de uma leitura assíncrona e do envio ao renderizador Direct3D 9. Não há codificação ou gravação de imagens por quadro.

Ainda existe transferência pela CPU e consumo de GPU. Um overlay maior custa mais. As medições sintéticas disponíveis variaram e **não demonstram uma redução conclusiva do impacto no jogo**. Em repouso, a busca ocorre a cada 1,5 segundo; seu consumo ainda não foi medido separadamente.

Para uma máquina mais limitada, comece com um vídeo pequeno e o modo **Econômico**. Saiba mais em [arquitetura e validação](docs/ARCHITECTURE.md).

## Desenvolvimento

O projeto usa **C# / WinForms / .NET 8** na interface e **C++ / Direct3D** na captura e renderização. O compilador nativo portátil é Zig 0.14.1; MinHook 1.3.4 está incluído com sua licença.

```powershell
pwsh ./scripts/Initialize-Tools.ps1
pwsh ./scripts/Package.ps1
```

O instalador é gerado em `dist/installer/`. Consulte [DEVELOPMENT.md](docs/DEVELOPMENT.md) para requisitos, testes e compilação individual.

## Contribuir

Encontrou um problema? Abra uma [issue](https://github.com/MaiconDAS/pip-overlay/issues/new/choose) com a versão do aplicativo, navegador, Windows e passos para reproduzir. As orientações estão em [CONTRIBUTING.md](CONTRIBUTING.md).

O projeto ainda não definiu uma licença de código aberto para o código próprio. As licenças e atribuições das dependências estão em [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).

---

<p align="center">Criado por <a href="https://github.com/MaiconDAS">MaiconDAS</a> · Projeto independente, sem afiliação com Microsoft, Google, MSI ou desenvolvedores de jogos.</p>
