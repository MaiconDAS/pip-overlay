**Português** · [English](en/ARCHITECTURE.md)

# Arquitetura e validação

A captura usa Windows Graphics Capture e D3D11, com três buffers de leitura e memória compartilhada. Não há recodificação nem gravação. O caminho de compatibilidade usa cópia pela CPU. O DirectX 11 pode usar transporte por textura compartilhada na GPU; ainda há cópias na GPU, portanto não é zero-copy.

## Qualidade

Resolução e FPS são independentes. Os limites de captura são 1920 × 1080, 1280 × 720, 854 × 480 ou adaptação ao tamanho do overlay. A frequência pode ser 60, 30 ou 15 fps. Quando tamanho e formato BGRA coincidem, uma cópia direta substitui a filtragem. O PiP do navegador é ampliado temporariamente, sem ativação, e restaurado ao parar. O navegador pode limitar esse tamanho; o status mostra a resolução efetiva. Selecionar 1080p no site não garante uma janela PiP com 1920 × 1080 pixels.

Alterar apenas o FPS mantém o limite de resolução e o tamanho solicitado à janela de origem. A opção Adaptar ao tamanho quantiza a resolução para reduzir realocações durante o arraste. Resoluções maiores podem consumir mais memória e largura de banda.

## Renderizadores

- D3D9 usa o renderizador por processo, detectado por janela e módulo gráfico, sem filtro por nome de jogo.
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

Veja [VALIDATION-0.16.md](VALIDATION-0.16.md).

## Transporte compartilhado no DirectX 11

Uma memória de controle separada e versionada negocia três texturas RGBA com mutexes por chave. O produtor escreve numa textura livre; o consumidor copia a imagem concluída mais recente para a textura do efeito e libera quadros antigos. A aquisição não espera pelo outro processo. Falhas ou incompatibilidade retornam ao caminho CPU; uma origem retida permite esse retorno mesmo com vídeo parado. Buffers de leitura da CPU são liberados quando o caminho GPU entra em uso. O diagnóstico identifica o transporte ativo. DirectX 9/12, OpenGL e Vulkan mantêm o caminho de compatibilidade.

O vídeo tem cantos discretos de seis pixels: geometria no D3D9, máscara nas APIs ReShade e um conjunto limitado de regiões de blit no Vulkan. O editor permite coordenadas exatas de borda mesmo com margem de encaixe ativa.

A captura prioriza quadros recentes e usa manutenção periódica para manter a origem ativa. Fechar a interface mantém o app na bandeja; quando pausado e oculto, a descoberta evita trabalho. Recursos D3D11 compartilhados podem ser incompatíveis entre adaptadores, por isso o fallback é necessário. A instalação, separadamente, gerencia atalhos e registro de desinstalação por usuário.
