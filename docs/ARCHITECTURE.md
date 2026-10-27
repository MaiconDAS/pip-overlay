# Arquitetura e validação

A captura usa Windows Graphics Capture e D3D11, com três buffers de leitura e memória compartilhada. Não há recodificação nem gravação. Ainda existe cópia pela CPU; não é zero-copy.

## Qualidade

Resolução e FPS são independentes. Os limites de captura são 1920 × 1080, 1280 × 720, 854 × 480 ou adaptação ao tamanho do overlay. A frequência pode ser 60, 30 ou 15 fps. Quando tamanho e formato BGRA coincidem, uma cópia direta substitui a filtragem. O PiP do navegador é ampliado temporariamente, sem ativação, e restaurado ao parar. O navegador pode limitar esse tamanho; o status mostra a resolução efetiva. Selecionar 1080p no site não garante uma janela PiP com 1920 × 1080 pixels.

Alterar apenas o FPS mantém o limite de resolução e o tamanho solicitado à janela de origem. A opção Adaptar ao tamanho quantiza a resolução para reduzir realocações durante o arraste. Resoluções maiores podem consumir mais memória e largura de banda.

## Renderizadores

- D3D9 mantém o renderizador legado para os processos reconhecidos.
- D3D11, D3D12 e OpenGL usam ReShade 6.8.0 e um add-on próprio. O efeito desenha somente o retângulo do vídeo. A interface interna do motor é bloqueada; hooks de entrada e navegação por controle são desativados.
- Vulkan usa uma camada própria de transferência/blit, sem shaders de storage image. Cada imagem da swapchain possui comandos, textura, upload, fence e semáforo próprios. A submissão espera os semáforos originais e a apresentação espera a composição. O semáforo só é reutilizado quando sua imagem retorna ao aplicativo. A GPU não é bloqueada a cada quadro; espera completa só ocorre na destruição dos recursos.

## Inicialização

**Iniciar jogo configurado**, no menu do ícone perto do relógio, inicia o executável escolhido. O auxiliar da mesma arquitetura prepara esse processo antes do ponto de entrada. Arquivos e configurações ficam em `data/games` na instalação. Não são escritos arquivos na pasta do jogo, Registro, serviços ou variáveis globais. A camada Vulkan é habilitada somente no ambiente do processo filho.

Pausar torna os renderizadores passivos; bibliotecas permanecem carregadas até o jogo terminar. A descoberta do PiP ocorre a cada 1,5 segundo quando não há sessão. Uma falha não gera tentativas infinitas.

## Limites

- Se um launcher abre outro processo, selecione o executável que renderiza o jogo.
- Vulkan inicial: SDR RGBA/BGRA 8 bits, fila gráfica com apresentação, uma swapchain por apresentação e sem extensões na cadeia de apresentação. Outros casos passam sem composição. HDR e device groups não são suportados.
- Não há tentativa de contornar proteção de jogos ou conteúdo de vídeo.
- Suporte à API não garante compatibilidade com todos os jogos e drivers.
- Fonte, resolução disponibilizada pelo navegador e capacidade da máquina continuam limitando qualidade e fps.

Veja [VALIDATION-0.11.md](VALIDATION-0.11.md).
