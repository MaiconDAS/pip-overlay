[English](en/VALIDATION-0.15.md)

> Registro histórico da versão indicada. Para a build atual, consulte [VALIDATION-0.16.md](VALIDATION-0.16.md).

# Validação 0.15.0 — interface e captura

Testes locais em 29/10/2026, com janelas e renderizadores sintéticos do próprio projeto. Nenhuma configuração global do Windows foi modificada.

## Interface

- Ajustes divididos em Imagem, Posição e Mais, em uma lateral de 316 px lógicos.
- Editor mantém 506 px de altura e 778 px de largura com a lateral aberta na janela padrão.
- As três seções foram verificadas também no tamanho mínimo (1040 × 720), sem rolagem horizontal.
- Seleção de FPS, abertura da lista e ativação do mínimo adaptativo verificadas no teste de workspace.
- Seletores, interruptores, botões e margem têm pintura própria; a navegação por teclado usa os controles WinForms.
- Resolução e FPS independentes, atalhos e geometria continuam cobertos pelos testes existentes.

## Captura e desempenho

A captura agora espera o evento de novo quadro e mantém apenas os quadros mais recentes disponíveis. Uma manutenção a cada 250 ms mantém a imagem parada ativa e publica readbacks pendentes. O transporte continua usando memória compartilhada e readback assíncrono; não é zero-copy. O Flush necessário ao envio permanece.

Comparação sintética de uma passagem antes/depois, vídeo limitado a 30 fps:

| Medida | Antes | Depois |
| --- | ---: | ---: |
| Quadros enviados/s | 29,66 | 29,57 |
| FPS do renderizador com overlay | 60,14 | 59,78 |
| p95 do tempo de quadro | 17,69 ms | 18,70 ms |
| CPU do produtor, % de um núcleo | 21,00% | 13,31% |
| Tempo médio de envio, incluindo readback | 0,229 ms | 0,447 ms |

As fases sem overlay também oscilaram bastante: CPU antes de 17,24% para 5,04%; depois de 3,77% para 14,51%. Portanto, **esta comparação não demonstra redução consistente de CPU, latência ou impacto em jogos**. O custo médio de envio medido aumentou nesta amostra. O resultado útil confirmado é manter a cadência esperada e evitar processamento repetido quando não há nova imagem. Arquivos brutos em [before.json](benchmarks/0.15/before.json) e [after.json](benchmarks/0.15/after.json).

Não foram feitos testes nesta versão com um jogo real ou com a carga do navegador do usuário. O experimento de transporte por textura GPU compartilhada não apresentou benefício comprovado e não foi ativado no produto.

## Renderizadores e imagem

- DirectX 11, DirectX 12, OpenGL e Vulkan, 32 e 64 bits: verificações de pixels sem divergências.
- Captura em resolução nativa de 1280×720 e 1920×1080: pixels exatos no teste de captura.
- Captura contínua D3D9: teste aprovado.
- Os resultados sintéticos não ampliam a compatibilidade declarada para todos os jogos ou sistemas anticheat.

## Distribuição

As ferramentas de teste são excluídas da build de produção. O instalador permanece sem assinatura pública, conforme a decisão do proprietário de preparar o processo e aguardar um certificado. Instalação, atalhos, Registro e desinstalação só são exercitados em um Windows descartável do GitHub Actions.

Imagem parada: em 5,01 s, 17 despertares de manutenção, nenhum novo quadro processado e 274 apresentações da imagem preservada. A resolução do contador de CPU não permite concluir consumo zero. [Relatório](benchmarks/0.15/static.json).
