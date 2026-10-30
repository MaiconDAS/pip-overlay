# Validação 0.16 — transporte GPU, bordas e documentação

[English](en/VALIDATION.md) · [Documentação](README.md)

Ambiente: Intel UHD Graphics 770, driver 32.0.101.7092, Windows x64. Foram usadas fontes e renderizadores próprios do projeto; não houve alteração global do Windows.

## Comparação integrada em DirectX 11

Mesma fonte animada Full HD, alvo de 60 fps, renderizador D3D11 x64 e resolução de transferência confirmada em 1920×1080 nos dois caminhos. Três execuções por transporte, em ordem CPU/GPU/GPU/CPU/CPU/GPU, com aquecimento e aproximadamente oito segundos de observação por execução. Medianas da build com cantos arredondados e sem buffers CPU redundantes no caminho GPU:

| Medida | Transporte CPU | Texturas GPU |
| --- | ---: | ---: |
| Fonte sintética, quadros/s | 58,83 | 63,24 |
| Quadros publicados/s | 36,58 | 48,68 |
| Quadros enviados à textura do renderizador/s | 34,83 | 48,68 |
| CPU do renderizador, % de um núcleo | 31,67% | 10,05% |
| CPU do produtor incluindo a fonte sintética, % de um núcleo | 111,40% | 72,28% |
| Tempo amostrado de submissão, incluindo readback no caminho CPU | 3,50 ms | 0,29 ms |

Os resultados sustentam ativar o transporte GPU em D3D11 com fallback. Não medem latência ponta a ponta nem uso percentual da GPU; a fonte e o sistema variaram entre execuções. A CPU do produtor inclui o desenho da fonte sintética. Não são medições de FPS de um jogo real nem uma promessa de ganho igual em outras máquinas/APIs. Dados: [benchmarks/0.16](benchmarks/0.16).

A primeira implementação com textura única reduzia a cadência e foi descartada. A versão final usa três texturas, evita esperar pelo consumidor e recicla quadros antigos. O readback/upload pela CPU é evitado nesse transporte, mas continuam existindo cópias e sincronização na GPU.

## Imagem, compatibilidade e interface

- DirectX 11, DirectX 12 e OpenGL, em 32/64 bits: verificação de pixels centrais e de cantos removidos, em cinco posições/tamanhos, sem divergências.
- Vulkan 32/64 bits: verificação de pixels e cantos arredondados sem divergências. O arredondamento usa um conjunto limitado de regiões de blit; não há novo pipeline de shaders.
- D3D9 x86: verificação dedicada dos cantos, posições e resize com fonte sólida; nenhuma divergência. Captura contínua e testes anteriores de contenção continuam disponíveis.
- Transporte GPU D3D11 em 32/64 bits: ativação, posição, tamanho, mudança de 1080p para 720p e retorno forçado à CPU com fonte parada passaram, sem divergências nos pixels verificados.
- A captura CPU em tamanho nativo continua passando no teste de preservação de pixels em 1280×720 e 1920×1080.
- O encaixe alcança exatamente os quatro cantos, preservando também margem, centro e movimento livre. Testes de editor, seleção de fontes, perfis e desfazer passaram.

## Limites e distribuição

A otimização por GPU é específica de DirectX 11. As outras APIs mantêm o transporte CPU. Fullscreen em novos renderizadores e ganhos em jogos reais permanecem sujeitos a validação específica; resultados de hosts em janela não substituem esses testes. AMD/NVIDIA e cenários com adaptadores diferentes não foram validados neste hardware.

A build de produção exclui modos e classes de teste. O pacote inclui políticas de privacidade em português e inglês e guia de uso em inglês. A publicação depende da verificação do pacote e dos testes de instalação/atualização/desinstalação em Windows descartável. A assinatura pública continua pendente.

Relatórios de versões anteriores são registros históricos. A investigação [PERFORMANCE-2026-10-29.md](PERFORMANCE-2026-10-29.md) precede a integração e não deve ser usada como descrição da implementação final.

Reconexão GPU: oito sessões alternando 720p/1080p no mesmo host passaram; crescimento de memória privada observado de aproximadamente 58 MiB, abaixo do limite de regressão de 96 MiB. Isso não prova ausência de vazamentos em uso prolongado.

Detecção: hosts gráficos com nomes genéricos em 32/64 bits foram encontrados. Processos modernos não preparados permanecem explicitamente não conectados. Não houve teste com Black Desert Online.
