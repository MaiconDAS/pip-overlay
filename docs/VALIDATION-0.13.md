# Validação 0.13.0

Ambiente local: Windows, Intel UHD Graphics 770. Esta versão altera a interface, os perfis e as mensagens; não modifica os renderizadores de produção.

## Interface e regressão

- 13 verificações em `scripts/Test-UX.ps1`: encaixe, margem, centro, movimento livre, limites da tela, centralização, desfazer, histórico ao trocar perfil, leitura de perfil antigo, cópia de opções, estados de conexão, duas miniaturas DWM, seleção correta e recusa de fonte fechada (algumas verificações agrupam condições).
- Testes existentes de conexão automática, pausa/retomada, atalhos, geometria, controle adaptativo, perfis, diagnóstico e atualizações passaram.
- Prévia da interface normal e expandida inspecionada. Os testes de miniaturas usam duas janelas sintéticas; não constituem validação de múltiplos vídeos reais em cada navegador.
- A interação permanece restrita à ferramenta. Ctrl+Z é local ao editor; não foi criado um novo atalho global.

## Ciclo prolongado de captura

O teste final completou **30 ciclos em 171,37 segundos**, alternando fontes sintéticas de 640×360 e 1280×720, sessões de 720p/1080p a 30 fps e alteração para 480p a 15 fps. Cada ciclo encerrou a captura e fechou a fonte antes de criar outra.

- 30 mensagens de clique recebidas pelo host D3D9 x86 próprio.
- Zero alterações indevidas dos estados gráficos verificados.
- Publicação e renderização confirmadas em todos os ciclos.
- Variação de memória privada do produtor entre o terceiro e o último ciclo: +2.437.120 bytes (~2,32 MiB). Essa amostra curta não prova ausência de vazamentos de longa duração.
- As primeiras rodadas foram interrompidas pelo encerramento do host e não foram consideradas aprovadas. A rodada final usou o host reconstruído pelo pipeline nativo atual.

Este teste exercita descarte/recriação de sessões e fontes. Não mede o FPS de jogos reais, não simula horas de uso e não substitui testes com Edge/Chrome ou cliques físicos dentro de um jogo.

## Distribuição e assinatura

- Compilação de produção e instalador 0.13.0 concluídos.
- 493 arquivos extraídos em uma pasta temporária tiveram seus hashes comparados ao pacote. Destino existente foi recusado.
- Nenhum modo de teste, fonte sintética ou preferência pessoal foi incluído no pacote.
- Importação de preferências/perfis testada em pastas temporárias: origem preservada, arquivos alheios ignorados e importação inválida revertida.
- Instalador verificado como `NotSigned`. Consulta de assinatura e recusa de parâmetros incompletos passaram; assinatura real pendente de certificado público. Consulte [SIGNING.md](SIGNING.md).

## Limites

Não houve teste com jogos reais nesta rodada nem medição nova de impacto no FPS. A matriz AMD/NVIDIA, fullscreen das novas APIs e vídeos reais permanece pendente. Não há promessa de custo zero, 60 fps garantidos ou compatibilidade universal. Veja [TEST-MATRIX.md](TEST-MATRIX.md).

Nenhuma configuração do Windows, driver, serviço, Registro ou política de navegador foi alterada.
