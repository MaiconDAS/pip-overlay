# Histórico

## 0.11.1 — resolução e FPS independentes; licença MIT

- Resolução selecionável: até 1080p, 720p, 480p ou adaptação ao tamanho do overlay.
- FPS selecionável separadamente: 60, 30 ou 15. Alterar o FPS não reduz a resolução.
- Preferências anteriores migradas, preservando resolução adaptativa e FPS quando presentes.
- Botão Abrir jogo removido da tela principal. Inicialização manual disponível somente pelo menu do ícone da bandeja.
- Código próprio licenciado sob MIT; licença e avisos de terceiros incluídos no instalador.
- 19 verificações de combinações, persistência, migração e restauração da janela aprovadas.

Esta versão separa os controles; não inclui uma correção específica para as piscadas relatadas a 60 fps.
## 0.11.0 — qualidade e novos renderizadores

- Captura em resolução nativa até 1080p no modo Máxima, com alvo de 60 fps; modos de 30/15 fps continuam disponíveis.
- Ampliação temporária do PiP de origem e restauração ao parar; resolução efetiva visível no status.
- DirectX 11/12 e OpenGL com motor incluído e add-on próprio; Vulkan com camada própria ativada apenas no processo iniciado.
- Botão Abrir jogo e seleção persistente do executável, sem copiar arquivos para a pasta do jogo.
- Preservação da conexão após reabrir a ferramenta, enquanto o mesmo processo do jogo continua ativo.
- Testes gráficos com posições, tamanhos e conferência de pixels; documentação dos limites por API.

Versão experimental: validação em hosts próprios, sem certificação de jogos, HDR ou garantia de 60 fps. Não altera configurações globais do Windows.
## 0.10.0 — primeira versão publicada neste repositório

- Instalação independente em um único executável, sem extensão ou RTSS.
- Abertura opcional do aplicativo ao concluir a instalação.
- Uso do PiP nativo do Edge e Chrome como fonte de vídeo.
- Interface com ajustes recolhidos e editor proporcional.
- Conexão automática com pausa persistente e atalhos configuráveis.
- Bandeja do sistema para manter o aplicativo disponível com a janela fechada.
- Renderizador Direct3D 9, captura reduzida na GPU e resolução adaptativa até 1080p.

Estado experimental. As melhorias foram desenvolvidas antes da criação do repositório; esta publicação inicia o histórico Git sem simular commits antigos.
