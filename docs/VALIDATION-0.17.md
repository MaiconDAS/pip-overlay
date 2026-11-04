# Validação 0.17

**Português** · [English](en/VALIDATION-0.17.md) · [Documentação](README.md)

Testes em 4 de novembro de 2026, Windows x64 com Intel UHD Graphics 770. Foram usados aplicativos e fontes sintéticas na pasta do projeto. Nenhuma configuração global do Windows foi alterada.

## Funcionalidade

- Dez idiomas: catálogos completos para as chaves cadastradas, parâmetros de texto, persistência, troca ao vivo e renderização do editor. A tradução não reinicia a captura.
- DirectX 11 já aberto, x86/x64: três posições, tamanho de captura 1080p/720p, cantos arredondados, redimensionamento da swapchain, restauração de estado gráfico e retorno à CPU. Dezesseis verificações de pixels sem divergências por teste.
- Integrações preparadas DirectX 11/12, OpenGL e Vulkan em x86/x64: verificações de pixels aprovadas. Captura em tamanho nativo 720p/1080p preserva os pixels da fonte sintética.
- Saída independente: imagem correta, hit testing atravessando a janela, nenhuma ativação ou evento de foco da overlay e redimensionamento. O teste monitora eventos de foreground do Windows.
- Reconexão automática limitada; pausa cancela as novas tentativas. Perfis, atalhos, geometria e qualidade mantêm os testes existentes.

A saída independente atende janela/borderless. Fullscreen exclusivo depende da integração no renderizador. O teste inicial de fullscreen D3D11 foi recusado pelo Windows antes da conexão (0x887A0022); portanto resultados de janela não certificam fullscreen. Os resultados são de hosts próprios, não de Black Desert Online ou de todos os jogos.

## Desempenho

Comparação em fonte Full HD animada e alvo de 60 fps, três execuções por backend, ordem preparado/direto/direto/preparado/preparado/direto. Ambos usam transporte compartilhado pela GPU. Medianas:

| Medida | Integração preparada | Conexão direta D3D11 |
| --- | ---: | ---: |
| Quadros enviados ao renderizador/s | 42,90 | 42,66 |
| CPU do host, equivalente a um núcleo | 12,20% | 11,12% |
| CPU do produtor, incluindo fonte sintética | 88,53% | 86,68% |
| Tempo de envio | 0,835 ms | 0,894 ms |

Não houve ganho demonstrado de FPS. A dispersão entre execuções impede prometer redução geral de custo. A vantagem confirmada é conectar sem reiniciar o aplicativo. A saída independente evita integração no jogo e só apresenta quadros novos; o consumo em jogos reais continua dependente de hardware, navegador e cena.

Em 24 reconexões alternando 720p e 1080p, o transporte GPU permaneceu ativo. Depois de normalizar a coleta de lixo antes da linha de base, a memória privada cresceu 24,74 MiB; as amostras a cada quatro ciclos foram aproximadamente 15,1 / 16,4 / 16,6 / 22,1 / 24,0 / 24,7 MiB. Isso não prova ausência de vazamento em sessões longas. As medições anteriores de oito ciclos usavam outra linha de base e não constituem uma comparação antes/depois válida.

[Resumo numérico](benchmarks/0.17/summary.json) · [Reconexões](benchmarks/0.17/reconnect.json)

## Reprodução e limites

Execute `scripts/Test.ps1`, `scripts/Test-Graphics.ps1` e `scripts/Test-UX.ps1` com as ferramentas locais do projeto. Testes de integração do instalador que alteram atalhos e Registro são permitidos somente no runner Windows descartável. AMD/NVIDIA, múltiplos adaptadores e jogos protegidos ainda exigem validação específica. Assinatura pública permanece pendente.
