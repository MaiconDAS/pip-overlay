# Histórico

## 0.14.1 — detecção automática da instalação

- Removida a etapa de importação que bloqueava o avanço com o campo vazio.
- O instalador reutiliza automaticamente a pasta da instalação registrada.
- Uma pasta existente pode ser escolhida diretamente, sem aviso intermediário de pasta existente; preferências e perfis nessa pasta são preservados.
- Ao escolher outro destino, as preferências da instalação registrada são copiadas automaticamente, sem substituir ajustes que já existam no destino.
- Instalações portáteis são atualizadas escolhendo sua própria pasta. Não há busca indiscriminada no disco.
- Teste novo percorre o assistente interativo até a confirmação, além dos testes de instalação limpa e atualização.

## 0.14.0 — instalador completo e identidade visual

- **Instalador completo:** atalho na área de trabalho (opcional e marcado por padrão), menu Iniciar e entrada em Aplicativos instalados / Programas e Recursos.
- **Desinstalação:** pelo Windows, pelo menu Iniciar ou pelo atalho na pasta do app. Preferências e arquivos pessoais são preservados.
- **Atualização na mesma pasta:** mantém os ajustes. Instalação por usuário em AppData, sem administrador, serviço ou inicialização automática.
- **Ícone novo:** símbolo arredondado em azul profundo e verde-menta, com nove tamanhos de 16 a 256 pixels para o executável, atalhos e bandeja.

![Novo ícone do PiP Overlay](docs/assets/app-icon.png)

## 0.13.0 — seleção visual e posicionamento

- **Escolha visual do vídeo:** quando há vários PiPs, o botão Vídeos abre miniaturas para selecionar a origem com um clique.
- **Posicionamento mais fácil:** encaixe nas bordas e no centro, margem ajustável, centralização e desfazer a última posição. Segure Alt para arrastar livremente; Ctrl+Z funciona somente no editor.
- **Perfil visível:** nome do jogo, restauração dos padrões e cópia dos ajustes para outro executável.
- **Status mais claros:** diferencia espera por vídeo, espera por jogo, captura interrompida e conexão não confirmada.
- **Assinatura preparada:** fluxo de assinatura e verificação disponível; o instalador continua sem assinatura pública enquanto não houver certificado.

Veja a [validação da 0.13](docs/VALIDATION-0.13.md) e o [fluxo de assinatura](docs/SIGNING.md).

## 0.12.0 — estabilidade, perfis e manutenção

- Preserva o último quadro em contenção no D3D9/Vulkan; teste reproduz o defeito anterior e verifica a correção.
- Evita duas texturas intermediárias na captura BGRA em tamanho nativo.
- FPS automático opcional, com limites e recuperação gradual, sem alteração da resolução.
- Perfis por executável para monitor, posição, tamanho, resolução e FPS.
- Verificação opcional de versões via GitHub, sem instalação automática.
- Exportação de diagnóstico com campos técnicos permitidos, sem imagens ou dados pessoais do vídeo.
- Importação de preferências e perfis pelo instalador, preservando a pasta anterior.
- Matriz de testes e benchmark com FPS e percentil 95 do tempo de quadro.
- Protótipo de compartilhamento D3D11 entre processos validado em pixels; não ativado no produto por falta de ganho comprovado.
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
