**Português** · [English](en/COMPATIBILITY.md)

# Compatibilidade com Jogos

Estado da versão **0.16.0**, experimental. Suporte ao renderizador não significa que todos os jogos que usam essa API sejam compatíveis.

| Componente | Suporte e validação |
| --- | --- |
| Windows | Windows 10 versão 2004 ou superior e Windows 11, sistema x64 |
| Microsoft Edge | Fonte pelo Picture-in-Picture nativo; precisa permanecer aberto e sem minimizar |
| Google Chrome | Fonte pelo Picture-in-Picture nativo; precisa permanecer aberto e sem minimizar |
| Jogos DirectX 9 | Caminho legado para processos reconhecidos; não há detecção genérica de todos os jogos D3D9. Host próprio testado, incluindo fullscreen |
| Jogos DirectX 11 | Implementado com motor incluído; testes de pixels em hosts próprios de 32 e 64 bits |
| Jogos DirectX 12 | Implementado com motor incluído; testes de pixels em hosts próprios de 32 e 64 bits |
| Jogos OpenGL | Implementado com motor incluído; testes de pixels em hosts próprios de 32 e 64 bits |
| Jogos Vulkan | Camada própria; testes de pixels em hosts próprios de 32 e 64 bits. Inicialmente SDR RGBA/BGRA UNORM de 8 bits, fila gráfica com apresentação |
| Fullscreen | Composição dentro do processo do jogo. D3D9 testado em fullscreen em host próprio; fullscreen das novas APIs ainda requer validação específica |
| Janela e borderless | Novas APIs exercitadas em hosts próprios com janela; borderless em jogos reais ainda requer validação |
| Resolução do vídeo | Captura até 1920 × 1080, limitada pelo tamanho efetivo do PiP e pelo navegador |
| Taxa de quadros | Alvos de 60, 30 e 15 fps; não são garantias de desempenho |
| Instalação | Um instalador com runtime e renderizadores incluídos; sem RTSS ou extensão |

## Como iniciar Jogos

Abra o PiP no navegador e use **Iniciar jogo configurado** no menu do ícone da ferramenta perto do relógio. Selecione o executável na primeira vez. A seleção fica salva. A conexão com o vídeo é automática quando a fonte e o renderizador estão disponíveis.

Launchers que criam outro processo e proteções do jogo podem impedir a conexão. Não há tentativa de contornar essas proteções. Vídeos protegidos também podem não ser capturáveis.

## Limites ainda não validados

- Não há lista de jogos certificados para as novas APIs.
- Não há comparação controlada do impacto no FPS de jogos reais nesta versão.
- HDR não foi validado; a camada Vulkan não compõe em HDR.
- Vulkan não compõe em apresentações com múltiplas swapchains, cadeias de extensão de apresentação, device groups ou recursos protegidos.
- O PiP do navegador continua necessário; a ferramenta não extrai automaticamente o vídeo de uma aba comum.

Veja os [resultados de teste](VALIDATION-0.11.md) e a [arquitetura](ARCHITECTURE.md).

Consulte a [matriz de testes](TEST-MATRIX.md) e a [validação 0.12](VALIDATION-0.12.md) para os novos recursos. As limitações por API continuam aplicáveis.

A versão 0.13 adiciona seleção visual de PiPs e melhorias de edição; não amplia a lista de GPUs ou jogos validados. Consulte [validação 0.13](VALIDATION-0.13.md).

Na 0.16, o transporte compartilhado pela GPU é específico de DirectX 11, com retorno automático à CPU. Isso não amplia a lista de jogos certificados nem acelera automaticamente outras APIs. Cantos arredondados e posicionamento na borda são recursos de desenho/edição separados do transporte.

### Detecção de jogos (0.16)

A lista consulta janelas abertas e módulos gráficos, sem restringir o nome do executável a um jogo. A consulta é somente leitura, tem cache de cinco segundos e deixa de ser repetida enquanto a sessão está conectada e o editor está oculto. Aplicativos gráficos também podem aparecer: detectar um renderizador não prova que seja um jogo compatível.

DirectX 9 usa a conexão existente por processo. DirectX 11/12, OpenGL e Vulkan encontrados sem uma integração preparada aparecem como **não conectado**. Em **Ajustes → Mais**, selecione o executável real em **Trocar jogo**; a inicialização preparada continua no menu do ícone perto do relógio. A seleção do executável também permite incluir uma janela cujo renderizador não foi identificado. Launchers que abrem outro processo e jogos protegidos podem impedir essa integração. Não há tentativa de contornar a proteção ou elevar privilégios.

Black Desert Online não foi testado nesta versão. Sua presença na lista e a autorização da integração pelo jogo são verificações diferentes; não é declarado compatível só porque a detecção foi ampliada.
