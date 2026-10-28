# Validação 0.12 — estabilidade e desempenho

Ambiente disponível: Intel UHD Graphics 770, driver 32.0.101.7092, Windows x64. Os testes usam aplicativos próprios; não são certificação de jogos.

## Piscadas por contenção

O teste mantém o transporte bloqueado por 1,8 segundo com o produtor vivo. A versão 0.11.1 desenhou 0 quadros nesse intervalo. O renderizador corrigido desenhou 103 quadros, com 4 verificações de pixels aprovadas e nenhuma divergência. A imagem anterior é reutilizada enquanto a próxima não está disponível. Isso corrige o caso reproduzido, sem afirmar que toda possível causa de piscada foi eliminada.

## Memória e transporte

A captura BGRA em tamanho nativo dispensa duas texturas intermediárias. Em 1920 × 1080 são aproximadamente 15,8 MiB de alocações de imagem evitadas, sem contar overhead do driver. Padrões de um pixel em 720p/1080p continuam preservados exatamente.

O protótipo `native/shared_gpu_probe.cpp` transferiu uma textura 1080p entre dois processos D3D11 com keyed mutex e conferiu todos os pixels. O microteste com 240 iterações registrou 18,48 ms por iteração no caminho compartilhado e 13,09 ms no caminho síncrono com CPU. São operações sintéticas com sincronização diferente, não medidas de FPS do aplicativo nem uma comparação definitiva. Não há evidência suficiente para ativar esse transporte na versão final desta máquina; ele fica como experimento reproduzível, fora do instalador.

Referência técnica: https://learn.microsoft.com/en-us/windows/win32/api/d3d11/nf-d3d11-id3d11device-opensharedresource

## FPS automático

Opcional e desligado por padrão. Usa o tempo de processamento da captura, não a frequência natural do vídeo, para reduzir FPS após sobrecarga persistente. Recupera gradualmente após um período estável. Respeita o mínimo escolhido e o FPS máximo configurado; nunca altera a resolução. Não mede diretamente o FPS do jogo.

## Perfis, atualização e privacidade

Perfis identificados por hash do caminho do executável armazenam posição, tamanho, monitor, resolução, FPS e modo automático. Atalhos globais permanecem independentes. A verificação de versão consulta somente releases deste repositório e pode ser desativada. Não instala nem executa arquivos automaticamente. A importação no instalador copia apenas JSON de preferências e perfis; preserva a versão anterior.

O diagnóstico contém versão, sistema, arquitetura, limites de captura e contadores técnicos. Não contém títulos, URLs, imagens, caminhos de executáveis, atalhos ou textos de exceções. O arquivo só é exportado para o destino escolhido pelo usuário; não há envio automático.

## Comparação de renderização

Smoke test sequencial com fonte sintética e host D3D9 em janela, alvo de 30 fps para o PiP. O teste anterior usa o renderizador 0.11.1 com o capturador e harness atuais, isolando a alteração de renderização.

| Fase | FPS do host | p95 do tempo de quadro |
| --- | --- | --- |
| Renderizador anterior, overlay ativo | 59,99 | 17,96 ms |
| Renderizador corrigido, sem overlay | 59,90 | 17,64 ms |
| Renderizador corrigido, overlay ativo | 59,93 | 18,40 ms |
| Renderizador corrigido, sem overlay ao final | 59,91 | 29,99 ms |

O PiP publicou 29,56 fps na fase corrigida. Não foram encontrados erros de restauração de estado do host. A variação do p95 mesmo sem overlay indica ruído de carga/agendamento; estes números não demonstram ganho de FPS nem medem impacto em jogos reais. Dados completos, incluindo CPU: [benchmark](benchmarks/0.12-intel-uhd770.json).

Na rodada final, o teste D3D9 manteve 107 quadros e quatro verificações de pixels durante contenção. D3D11/D3D12/OpenGL passaram em 32/64 bits com 20 verificações por API/arquitetura. Vulkan passou com 511 verificações em x64 e 510 em x86, sem divergências; continuou desenhando 72 quadros durante a contenção em cada arquitetura.

Instalador: 493 arquivos conferidos por SHA-256, sem ferramentas internas ou dados pessoais. Importação de preferências e perfis testada em pasta isolada: origem preservada, arquivos não relacionados excluídos e importação inválida revertida sem criar a instalação final.
