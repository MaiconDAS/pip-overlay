**Português** · [English](en/COMPATIBILITY.md)

# Compatibilidade com jogos

Estado da versão **0.19.0**. O modo desktop é o padrão e independe da API do jogo enquanto o Windows consegue compor a janela por cima dele.

| Componente | Situação |
| --- | --- |
| Windows | Windows 10 2004 ou superior / Windows 11, x64 |
| Edge e Chrome | Fonte pelo PiP nativo, aberto e sem minimizar |
| Janela e borderless | Saída desktop por DirectComposition; sem carregar bibliotecas no jogo |
| Fullscreen exclusivo | A saída desktop não oferece cobertura; requer integração específica e validação |
| DirectX 9 direto | Host próprio validado, incluindo fullscreen |
| DirectX 11 direto | Conexão com processo já aberto validada em hosts próprios x86/x64; exclusivo não validado |
| DirectX 12 / OpenGL | Integração preparada na inicialização, validada em hosts próprios x86/x64 |
| Vulkan | Experimental: SDR RGBA/BGRA UNORM 8 bits, fila gráfica de apresentação, uma swapchain |
| Vídeo | Até 1920×1080; 15/30/60 fps independentes, sujeitos à origem e ao hardware |
| Instalação | Runtime e renderizadores incluídos; sem RTSS ou extensão; assinatura pública pendente |

## Uso recomendado

Abra o PiP Overlay e coloque o vídeo em PiP no navegador. Não selecione processo: o modo desktop aparece sobre janelas e jogos borderless, deixa os cliques atravessarem e não toma foco. Não troca automaticamente para integração direta ao detectar um jogo.

## Relatos de uso em jogos

| Jogo | Evidência |
| --- | --- |
| Black Desert Online | Funcionamento e desempenho aprovados pelo usuário em 11 de novembro de 2026; relato anterior identificou sucesso no modo desktop e falha na seleção direta. |
| Digimon Masters Online | Funcionamento e desempenho aprovados pelo usuário em 11 de novembro de 2026. |

O relato mais recente não informou a versão exata do executável, GPU/driver, resolução, FPS ou modo de apresentação de cada jogo. Não se deve inferir fullscreen exclusivo, integração direta funcionando ou garantia para qualquer hardware. Não foi fornecida comparação controlada do FPS dos jogos.

## Conexão direta opcional

Em **Ajustes → Mais → Conectar diretamente ao jogo**, a lista passa a consultar janelas e módulos gráficos. Aplicativos comuns podem aparecer porque também usam APIs gráficas; detectar um módulo não confirma que o processo seja um jogo compatível.

DirectX 9/11 podem conectar a processos abertos quando permitido. DirectX 12, OpenGL e Vulkan podem precisar de **Iniciar jogo configurado** no menu da bandeja. Configure o executável real em **Trocar jogo**. Launchers, permissões e proteções podem impedir a integração. Não há elevação de privilégios nem tentativa de contornar proteção.

## Limites

- Não há promessa de cobertura de todos os jogos ou de custo zero de performance.
- Não foi medido o impacto no FPS de jogos reais nesta versão; a mudança evita a descoberta de processos no modo desktop, mas captura e composição continuam consumindo recursos.
- HDR e GPUs AMD/NVIDIA não foram validados pelo projeto nesta rodada. O relato doméstico é evidência de uso, não benchmark controlado.
- Vulkan não compõe em múltiplas swapchains, cadeias de extensão na apresentação, device groups ou recursos protegidos.
- PiP nativo continua obrigatório; uma aba comum não é extraída automaticamente. Vídeo protegido pode não ser capturável.

[Validação atual](VALIDATION-0.19.md) · [Validação dos renderizadores na 0.17](VALIDATION-0.17.md) · [Matriz de testes](TEST-MATRIX.md) · [Arquitetura](ARCHITECTURE.md)
