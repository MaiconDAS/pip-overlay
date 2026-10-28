# Matriz de testes

A matriz distingue suporte implementado de validação efetiva. Ambiente Intel UHD Graphics 770, driver 32.0.101.7092. Resultados de hosts próprios não equivalem a jogos certificados.

| Ambiente | D3D9 | D3D11 | D3D12 | OpenGL | Vulkan |
| --- | --- | --- | --- | --- | --- |
| Intel UHD 770, hosts em janela, x64/x86 | Testado x86; x64 compilado | Testado | Testado | Testado | Testado SDR |
| Intel UHD 770, host D3D9 fullscreen | Testado na 0.11 | Pendente | Pendente | Pendente | Pendente |
| Intel UHD 770, jogos reais, fullscreen/borderless | Sem comparação controlada | Pendente | Pendente | Pendente | Pendente |
| AMD, hosts e jogos | Hardware indisponível | Hardware indisponível | Hardware indisponível | Hardware indisponível | Hardware indisponível |
| NVIDIA, hosts e jogos | Hardware indisponível | Hardware indisponível | Hardware indisponível | Hardware indisponível | Hardware indisponível |

## Protocolo reproduzível

1. Registrar versão do app, GPU, driver, API, modo de janela, resolução do jogo/vídeo e FPS do PiP.
2. Usar a mesma cena, limite de FPS e condições de energia; repetir três vezes.
3. Medir 60 segundos sem overlay, 60 com overlay e 60 sem overlay. Registrar FPS, tempo de quadro mediano e percentil 95, CPU do produtor e do jogo, quadros publicados e enviados.
4. Testar quatro cantos, centro, redimensionamento, pausa, retomada, troca de resolução, perda de foco e cliques no jogo.
5. Não registrar imagens, títulos ou URLs no diagnóstico compartilhado. Anexar somente os dados técnicos que o usuário revisar.

Para desenvolvimento local: `scripts/Test-Graphics.ps1`, `--contention-test`, `--gpu-benchmark`, `scripts/Test-SharedGpu.ps1` e `scripts/Test-Upgrade.ps1`. O benchmark de host tem duração reduzida e serve como smoke test; não substitui as três rodadas de jogo real.
