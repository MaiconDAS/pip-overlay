![PiP Overlay](docs/assets/banner.svg)

**Português** · [English](README.en.md) · [Documentação](docs/README.md) · [Privacidade](docs/pt-BR/PRIVACY.md)

**Seu vídeo por perto. Seu jogo em foco.**

PiP Overlay leva o Picture-in-Picture do Edge ou Chrome para jogos compatíveis no Windows. Posicione e redimensione o vídeo no editor visual; dentro do jogo, o overlay não recebe cliques nem foco do teclado. Sem RTSS, extensão de navegador, conta ou instalação separada de runtime.

[**Baixar instalador**](https://github.com/MaiconDAS/pip-overlay/releases) · [Relatar problema](https://github.com/MaiconDAS/pip-overlay/issues/new/choose) · [Histórico](CHANGELOG.md)

## Novidades da 0.16

- Transporte automático por texturas compartilhadas na GPU para **DirectX 11**, com três buffers e retorno ao caminho CPU quando necessário.
- PiP pode encostar nas quatro bordas da tela, mesmo com margem de encaixe ativa.
- Cantos do vídeo levemente arredondados, mantendo a interação somente no editor.
- Política de privacidade e documentação principal em português e inglês.

A otimização de transporte é específica de DirectX 11. DirectX 9/12, OpenGL e Vulkan mantêm o transporte de compatibilidade. Medições e limites estão na [validação da versão](docs/VALIDATION-0.16.md); não há promessa de ganho de FPS em todos os jogos.

## Como usar

1. Execute `Instalar-PiP-Overlay.exe`. O instalador cria atalhos no menu Iniciar, uma entrada de desinstalação no Windows e, se selecionado, um atalho na área de trabalho. Atualizações preservam preferências.
2. Abra o aplicativo e coloque o vídeo no PiP nativo do Edge ou Chrome. Mantenha essa janela aberta, sem minimizar.
3. Para as APIs mais novas, use **Iniciar jogo configurado** no menu do ícone da ferramenta perto do relógio. Escolha o executável real do jogo na primeira vez. A conexão com fontes compatíveis é automática.
4. Arraste o PiP no editor e use bordas ou cantos para redimensionar proporcionalmente.

| Ação | Onde encontrar |
| --- | --- |
| Resolução, FPS e FPS automático | Ajustes → Imagem |
| Monitor, encaixe e margem | Ajustes → Posição |
| Atalhos, fontes, atualizações e diagnóstico | Ajustes → Mais |
| Centralizar ou desfazer | Barra do editor; Ctrl+Z também funciona no editor |
| Pausar e impedir reconexão | Pausar |
| Voltar a conectar | Retomar ou o atalho configurado |
| Manter na bandeja | Fechar a janela principal |
| Encerrar completamente | Sair no menu da bandeja |

Resolução e FPS são independentes: até 1080p a 30 fps mantém a resolução solicitada com menos atualizações que 60 fps. A qualidade selecionada no site não garante as dimensões reais da janela PiP. A ferramenta pode ampliá-la temporariamente e restaura seu tamanho ao parar. O navegador e a máquina podem impor limites menores.

[Guia completo de instalação e uso](docs/USO.txt)

## Editor visual

![Editor com ajustes laterais](docs/assets/workspace-016.png)

A área representa o monitor proporcionalmente. Os ajustes ficam na lateral, em Imagem, Posição e Mais, sem reduzir a altura do editor. O encaixe ajuda a alinhar, mas permite coordenadas exatas nas bordas. Segure Alt para mover sem encaixe.

## Compatibilidade e limites

Windows 10 2004 ou superior e Windows 11, sistema x64. PiP nativo do Edge e Chrome. Caminho legado para processos D3D9 reconhecidos; renderizadores DirectX 11/12, OpenGL e Vulkan experimental, com testes em hosts próprios de 32/64 bits.

Suporte a uma API não certifica todos os jogos. Proteções de jogos/vídeos, launchers que criam outro processo e certas configurações gráficas podem impedir a conexão. O aplicativo não contorna essas proteções. D3D9 fullscreen tem validação em host próprio; fullscreen nas APIs mais novas ainda exige validação específica. Resultados em janela sintética não garantem desempenho em jogos reais.

Vulkan atualmente compõe SDR RGBA/BGRA UNORM de 8 bits, usando fila gráfica e uma swapchain por apresentação. HDR, device groups e outros modos avançados permanecem fora do suporte. Alvos de 15/30/60 fps não são garantias de entrega.

[Compatibilidade detalhada](docs/COMPATIBILITY.md) · [Matriz de testes](docs/TEST-MATRIX.md) · [Arquitetura](docs/ARCHITECTURE.md)

## Privacidade e instalação

O vídeo é processado localmente; o aplicativo não grava nem envia seu vídeo. A consulta de atualizações acessa o GitHub e pode ser desativada. Preferências e logs técnicos ficam no computador. O diagnóstico JSON é exportado por escolha do usuário, sem envio automático. Consulte a [política de privacidade](docs/pt-BR/PRIVACY.md).

O instalador é por usuário e inclui .NET e renderizadores. Não instala drivers/serviços nem configura inicialização com o Windows. Ele cria os atalhos e o registro de desinstalação do usuário. A assinatura digital pública ainda está pendente; SHA-256 confere integridade, mas não identifica o editor como uma assinatura. Veja [assinatura](docs/SIGNING.md).

Atualizações reutilizam a pasta registrada e preservam preferências. Ao escolher outro destino, a importação automática não sobrescreve configurações já existentes. Desinstalar pelo Windows ou pelos atalhos preserva dados pessoais; a política explica como removê-los quando desejado.

## Desenvolvimento e licença

C# / WinForms / .NET 8 e C++ / Direct3D, com ferramentas locais ao projeto. Veja [desenvolvimento](docs/DEVELOPMENT.md), [contribuição](CONTRIBUTING.md) e [segurança](docs/pt-BR/SECURITY.md). Mudanças de instalação são testadas primeiro em Windows descartável. Ferramentas de teste são excluídas da build final.

Código próprio sob a [licença MIT](LICENSE). Dependências mantêm suas [licenças e atribuições](THIRD_PARTY_NOTICES.md). Projeto independente de [MaiconDAS](https://github.com/MaiconDAS), sem afiliação com fabricantes de navegadores, APIs ou jogos. Relatórios de versões anteriores são registros históricos, disponíveis no [histórico](CHANGELOG.md).

### Detecção de jogos (0.16)

A lista consulta janelas abertas e módulos gráficos, sem restringir o nome do executável a um jogo. A consulta é somente leitura, tem cache de cinco segundos e deixa de ser repetida enquanto a sessão está conectada e o editor está oculto. Aplicativos gráficos também podem aparecer: detectar um renderizador não prova que seja um jogo compatível.

DirectX 9 usa a conexão existente por processo. DirectX 11/12, OpenGL e Vulkan encontrados sem uma integração preparada aparecem como **não conectado**. Em **Ajustes → Mais**, selecione o executável real em **Trocar jogo**; a inicialização preparada continua no menu do ícone perto do relógio. A seleção do executável também permite incluir uma janela cujo renderizador não foi identificado. Launchers que abrem outro processo e jogos protegidos podem impedir essa integração. Não há tentativa de contornar a proteção ou elevar privilégios.

Black Desert Online não foi testado nesta versão. Sua presença na lista e a autorização da integração pelo jogo são verificações diferentes; não é declarado compatível só porque a detecção foi ampliada.
