**Português** · [English](docs/en/CONTRIBUTING.md)

# Como contribuir

Para bugs, informe a versão, Windows, navegador, modo de exibição do jogo e passos para reproduzir. Remova nomes de usuário, caminhos pessoais e credenciais de logs e imagens.

Para alterações de código:

1. Crie uma branch a partir de `main`.
2. Descreva o problema e mantenha o escopo da alteração pequeno.
3. Execute `pwsh ./scripts/Test.ps1`.
4. Se alterar captura ou renderização, valide também o host Direct3D 9 próprio.
5. Abra um pull request com comportamento antes/depois e testes executados.

Não altere configurações do Windows, políticas do navegador, serviços ou drivers para fazer um teste passar. Não adicione mecanismos para contornar proteções de jogos. Dependências devem manter suas licenças e ficar locais ao projeto sempre que possível.

O código próprio está sob a [licença MIT](LICENSE). Ao enviar uma contribuição, você concorda em disponibilizá-la sob essa mesma licença. Dependências de terceiros mantêm suas licenças originais.

Atualize também a documentação em português e inglês quando o comportamento mudar.
