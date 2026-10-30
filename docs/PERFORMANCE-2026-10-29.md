> Investigação exploratória anterior à integração da 0.16. Resultados integrados atuais: [VALIDATION-0.16.md](VALIDATION-0.16.md).

# Investigação de desempenho — 29/10/2026

## Ambiente e escopo

Intel UHD Graphics 770, driver 32.0.101.7092. Testes locais com fonte WinForms animada de 1920×1080 e renderizador D3D9 próprio em janela. Nenhum jogo real, navegador, instalação ou ajuste global do Windows foi usado. A CPU do produtor inclui a fonte sintética; percentuais são relativos a um núcleo, não ao computador inteiro.

## Matriz de configuração

Três repetições por configuração em ordem alternada, cada uma com aproximadamente seis segundos sem overlay, seis com overlay e seis sem overlay novamente. Medianas:

| Configuração | Quadros enviados/s | Custo amostrado de envio | CPU adicional do renderizador (pontos de um núcleo) |
| --- | ---: | ---: | ---: |
| 1080p / 30 fps | 28,76 | 2,86 ms | 14,23 |
| 1080p / 60 fps | 34,37 | 2,09 ms | 15,41 |
| 720p / 60 fps | 40,99 | 1,30 ms | 10,48 |

O renderizador manteve aproximadamente 60 apresentações/s nesta matriz. Isso não representa o desempenho de um jogo exigente. O custo de envio é uma média de amostras do último envio, incluindo readback, e não latência ponta a ponta. O custo por envio não mede custo total por segundo.

720p reduz o custo observado, mas altera a resolução. Não é uma otimização transparente de qualidade. Em Full HD, pedir 60 fps não garantiu 60 quadros enviados. A instrumentação adicional confirmou fonte perto de 60 fps em algumas execuções com saída muito inferior, justificando investigar transporte e sincronização.

## Alternativas sem reduzir resolução

Testes A/B finais, na ordem atual, cópia em bloco, sem espera, sem espera, cópia em bloco, atual. O executável de teste aguarda o encerramento do seu renderizador antes da próxima execução. Uma rodada exploratória anterior teve falha ao trocar uma DLL ainda em uso e foi descartada integralmente; não está nos dados publicados deste relatório.

| Execução | Fonte /s | Enviados /s | p95 do quadro do renderizador |
| --- | ---: | ---: | ---: |
| Atual 1 | 60,4 | 30,9 | 32,83 ms |
| Cópia em bloco 1 | 39,3 | 19,1 | 72,35 ms |
| Sem espera 1 | 27,0 | 14,4 | 66,70 ms |
| Sem espera 2 | 43,9 | 24,7 | 42,87 ms |
| Cópia em bloco 2 | 63,4 | 36,4 | 18,87 ms |
| Atual 2 | 52,8 | 25,3 | 33,30 ms |

A variação da fonte e das fases sem overlay indica condições instáveis. Esses resultados não sustentam atribuir ganho ou regressão exclusivamente a uma alternativa. Não promover cópia em bloco ou remoção da espera como otimizações comprovadas. Nenhuma dessas variantes foi aplicada à versão distribuída. O modo sem espera é apenas uma opção de diagnóstico excluída da compilação de produção.

## Textura compartilhada pela GPU

Três repetições adicionais de 240 transferências Full HD, executadas sequencialmente após o teste do aplicativo:

| Repetição | Compartilhamento GPU | Readback síncrono + upload |
| --- | ---: | ---: |
| 1 | 0,8350 ms | 3,0581 ms |
| 2 | 0,7643 ms | 3,3062 ms |
| 3 | 0,8149 ms | 3,4633 ms |

A mediana foi 0,8149 ms versus 3,3062 ms: aproximadamente 75% menos tempo **neste microbenchmark**. Validação de pixels entre processos passou nas três repetições. O loop cronometrado usa dois dispositivos D3D11 no mesmo processo; o teste entre processos valida os pixels separadamente. O caminho CPU de referência é síncrono e não reproduz o transporte assíncrono da versão 0.15, nem integra o renderizador D3D9 do aplicativo.

Portanto, o resultado confirma viabilidade e justifica um protótipo integrado, inicialmente em DirectX 11. Não demonstra uma redução de 75% do custo do app ou compatibilidade equivalente em D3D9, D3D12, OpenGL ou Vulkan. Uma primeira passagem exploratória foi muito mais lenta (10,66/20,77 ms), reforçando a necessidade de aquecimento, repetição e controle de carga na próxima etapa.

## Conclusão e próximo experimento

Há uma alternativa promissora: transporte direto por textura compartilhada na GPU. A próxima prova deve comparar esse transporte com o atual, usando o mesmo renderizador, resolução, fonte e cadência, com aquecimento e várias repetições, medindo CPU, tempo de quadro e estabilidade dos pixels. Manter fallback até comprovar benefício. A matriz de configuração também sustenta manter 1080p/30 como opção de nitidez com cadência mais previsível nesta máquina.

Dados brutos em [benchmarks/performance-2026-10-29](benchmarks/performance-2026-10-29). Reproduzir a matriz e as alternativas com scripts/Test-Performance.ps1, informando um Zig existente e a pasta dos binários nativos de teste. O teste separado da textura compartilhada está em scripts/Test-SharedGpu.ps1. Nenhum instalador novo foi gerado.
