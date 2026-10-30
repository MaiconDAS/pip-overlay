# Política de privacidade

[English](../../PRIVACY.md) · [Documentação](../README.md)

Atualizada em 29 de outubro de 2026. Aplica-se ao aplicativo oficial PiP Overlay mantido em [MaiconDAS/pip-overlay](https://github.com/MaiconDAS/pip-overlay). Forks e sites externos podem funcionar de outra forma.

## Processamento local do vídeo

O aplicativo identifica janelas PiP de navegadores e processos de jogos para conectar o overlay. Pode consultar localmente títulos de janelas, nomes e identificadores de processos e o caminho do executável selecionado. Captura pixels da janela PiP escolhida e os entrega ao renderizador no mesmo computador, por RAM ou recursos da GPU. Não implementa gravação de vídeo, captura de áudio, transmissão para a nuvem, anúncios ou análise de uso. Não lê senhas, cookies, histórico do navegador ou o conteúdo DOM de abas comuns.

Os quadros são recursos temporários; o aplicativo não os salva intencionalmente como imagens ou vídeos. O Windows gerencia memória, paginação e despejos de falha independentemente. A janela PiP pode ser ampliada temporariamente e restaurada ao encerrar a captura.

## Dados salvos no computador

A pasta `data` da instalação guarda preferências, monitor, posição/tamanho, resolução/FPS, atalhos, datas de consulta de atualizações e perfis por jogo. Seleção e registros de execução de jogos podem conter caminhos de executáveis, identificadores de processos e horários. Configurações, caches e logs técnicos dos renderizadores de terceiros podem conter caminhos e informações do sistema/gráficos. Arquivos de descoberta do Direct3D são locais a este computador. Servem à configuração, compatibilidade e diagnóstico; não são enviados pelo PiP Overlay.

Preferências e arquivos pessoais são preservados na desinstalação. Para remover dados locais, feche o aplicativo e os jogos iniciados por ele e remova a pasta `data` remanescente e os diagnósticos que você salvou, após guardar o que quiser preservar. Não existe servidor do projeto com uma cópia dessas configurações locais.

## Rede e GitHub

Avisos de atualização vêm ativados por padrão. O aplicativo consulta a API pública `api.github.com/repos/MaiconDAS/pip-overlay/releases`; o User-Agent informa a versão do app. O GitHub recebe informações normais da conexão, incluindo seu IP. A consulta não inclui quadros capturados, títulos do navegador, URLs de vídeos, caminhos de executáveis ou preferências. Consultas automáticas bem-sucedidas são limitadas a uma por dia, na abertura; também há consulta manual. Desative **Ajustes > Mais > Avisar atualizações** para interromper consultas automáticas. O overlay local não depende dessa consulta.

Abrir a página da versão/download usa seu navegador. Páginas do repositório, issues, downloads e infraestrutura de atualização são operados pelo GitHub conforme sua [política de privacidade](https://docs.github.com/en/site-policy/privacy-policies/github-general-privacy-statement). Navegador, serviço de vídeo, Windows e drivers têm políticas próprias. Downloads das ferramentas de desenvolvimento são separados do funcionamento normal do app instalado.

## Diagnóstico e suporte

**Diagnóstico** salva um JSON somente após você escolher o destino. Inclui versões do app/Windows, arquitetura do processo, resolução/FPS escolhidos, dimensões de captura, transporte e contadores de desempenho/erro. A lista explícita de campos exclui quadros, áudio, URLs, títulos de janela, caminhos pessoais e texto bruto de exceções. Não há envio automático.

Ao anexar um relatório, imagem ou log a uma issue, você decide compartilhar esse conteúdo; issues públicas são públicas. Confira logs de terceiros e imagens antes de compartilhar, pois podem identificar você. O JSON de diagnóstico filtrado e os logs dos renderizadores são arquivos diferentes.

## Contato e alterações

Para dúvidas gerais, contate o mantenedor por uma [issue no GitHub](https://github.com/MaiconDAS/pip-overlay/issues), sem dados privados. Para vulnerabilidades, siga [SECURITY.md](SECURITY.md). Pedidos sobre dados mantidos pelo GitHub devem usar os canais de privacidade do GitHub. Esta política acompanha o histórico do código; alterações relevantes devem constar nas notas de versão. Ela descreve o funcionamento atual e não declara certificação ou conformidade jurídica universal.
