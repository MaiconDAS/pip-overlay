# Validação 0.11

Testes locais em Windows com Intel UHD Graphics 770. Hosts próprios; não equivalem a validação em jogos de terceiros.

| Verificação | Resultado |
| --- | --- |
| Conexão, atalhos e geometria | 47 verificações aprovadas |
| Qualidade e restauração da janela | 7 verificações aprovadas |
| Captura nativa 1280 × 720 e 1920 × 1080 | Todos os bytes dos pixels preservados |
| DirectX 11 x64 e x86 | 20 verificações por arquitetura, 5 posições, nenhuma divergência |
| DirectX 12 x64 e x86 | 20 verificações por arquitetura, 5 posições, nenhuma divergência |
| OpenGL x64 e x86 | 20 verificações por arquitetura, 5 posições, nenhuma divergência |
| Vulkan x64 e x86, camada própria | 509 e 504 verificações, respectivamente, 5 posições, nenhuma divergência |

O padrão de captura inclui detalhes de um pixel e gradientes, para detectar filtragem indesejada. A composição usa cores conhecidas e coordenadas proporcionais verificadas após renderização. Isso não mede a qualidade do stream do YouTube nem comprova 60 fps reais de vídeo do navegador.

Vulkan via ReShade falhou nesta GPU por exigir shaderStorageImageReadWithoutFormat. A camada própria usa transferência/blit e passou sem essa exigência. OpenGL foi verificado por leitura do framebuffer porque o formato RGBX do host não é aceito pelo capturador de screenshots do motor.

Ainda não há comparação controlada do impacto no desempenho de jogos reais nesta versão. HDR e casos especiais listados na arquitetura não estão habilitados. Logs com caminhos pessoais ficam fora do Git.

Captura contínua WGC → transporte → host D3D9: 52,4 quadros/s publicados em 12,56 segundos, transferência 1282 × 752 e 25 conferências de pixels sem divergências. O processo de teste consumiu cerca de 21,2% de um núcleo lógico; esse valor inclui a fonte sintética e as leituras de validação, não representa o custo isolado do overlay em um jogo. Não é uma medição de vídeo 1080p60 do navegador.

Instalador: 491 arquivos extraídos em pasta isolada e comparados por SHA-256 com o pacote; destino existente recusado. Nenhum host, comando de teste ou configuração pessoal foi incluído na build de produção.
