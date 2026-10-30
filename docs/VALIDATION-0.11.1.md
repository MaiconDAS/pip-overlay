[English](en/VALIDATION-0.11.1.md)

> Registro histórico da versão indicada. Para a build atual, consulte [VALIDATION-0.16.md](VALIDATION-0.16.md).

# Validação 0.11.1

- Testes de conexão, atalhos e geometria aprovados.
- 19 verificações de resolução/FPS, migração, persistência e restauração da fonte aprovadas.
- As 12 combinações entre quatro limites de resolução e três taxas de quadros mantêm os dois parâmetros independentes.
- 1080p a 30 fps preservado após salvar e carregar as preferências.
- Preferências antigas de resolução adaptativa e FPS preservadas.
- Interface inspecionada com os controles separados e sem botão Abrir jogo na tela principal.
- Renderizadores nativos não foram modificados nesta atualização. Os testes gráficos da versão 0.11 continuam documentados em VALIDATION-0.11.md; não foram repetidos como se fossem novos testes de jogos.

As piscadas relatadas a 60 fps ainda não receberam uma correção específica nesta versão. Selecionar 30 fps agora não reduz automaticamente a resolução. O tamanho efetivamente capturado continua limitado pelo navegador e pela fonte.

Instalador: 493 arquivos extraídos em pasta isolada e comparados por SHA-256 com o pacote; destino existente recusado. Licença MIT e avisos de terceiros incluídos.
