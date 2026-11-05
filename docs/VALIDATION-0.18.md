# Validação da 0.18.0

**Português** · [English](en/VALIDATION-0.18.md) · [Documentação](README.md)

Data: 5 de novembro de 2026. Escopo: saída desktop como padrão, UX, layouts por monitor, recuperação e atualizações verificadas. Os renderizadores nativos não foram alterados nesta versão.

## Resultados locais

- Compilação gerenciada e testes de conexão, atalhos, geometria, melhorias, recuperação e localização aprovados. O teste de recuperação tem 30 verificações: arquivos inválidos/grandes, chave de perfil com tentativa de sair da pasta, backup, restauração após falha de escrita, migração desktop, preferências explícitas, proporção em telas horizontais/verticais, teclas, integridade do download, cancelamento e controles de modo.
- Dez idiomas completos para os novos textos e troca ao vivo preservando resolução. Capturas de interface em todas as línguas; revisão visual em português e inglês. Isso não equivale à revisão linguística por falantes nativos.
- Editor preservou 490 px de altura ao abrir as abas; teste na janela mínima aprovado, sem rolagem horizontal. Seleção de FPS e alternância de FPS automático aprovadas. Testes de miniaturas, escolha da fonte, bordas e desfazer aprovados.
- Saída desktop em host sintético: pixel esperado, compartilhamento GPU ativo, 58 quadros publicados e 57 enviados ao consumidor na amostra inicial, cliques atravessando para a janela abaixo, nenhuma ativação da overlay, reposicionamento no topo após ativar outra janela e continuidade após redimensionamento.
- Download testado com resposta HTTP sintética: arquivo correto aceito; hash incorreto rejeitado sem substituir arquivo verificado; cancelamento limpa arquivo parcial; URLs externas e pré-lançamentos rejeitados. Nenhum instalador foi executado no Windows do usuário.

## Limites da validação

A matemática de resolução e orientação foi testada sem alterar a configuração de telas do Windows. Desconexão/reconexão física e múltiplos monitores com DPI diferente ainda precisam de teste em equipamento adequado. Não houve benchmark controlado de jogo, novo teste de fullscreen exclusivo nem teste de HDR nesta rodada.

O teste com origem estática observou 17 despertares e nenhum novo quadro processado em cinco segundos; é um teste de regressão, não evidência de ganho percentual em jogos. O modo desktop deixou de consultar módulos de jogos, mas captura/composição ainda consomem CPU/GPU. Não se promete custo zero ou 60 fps em qualquer máquina.

O usuário relatou sucesso com desktop sobre Black Desert Online no computador doméstico e falha na seleção direta. Não foi possível confirmar o modo de apresentação nem medir aquele computador. O teste não certifica integração direta com o jogo.

A assinatura pública permanece pendente. SHA-256 verifica correspondência com o arquivo informado pelo GitHub, não identidade do editor. Arquivos de preferências transportam perfis por hash de caminho: caminhos ou identificadores de monitor diferentes em outro PC exigem nova associação.

## Pacote e instalação

O pacote de produção exclui testes e fontes sintéticas. A verificação local extrai o instalador e compara seus arquivos com o pacote portátil. Instalação, atualização, atalhos e desinstalação são verificados exclusivamente pelo workflow em Windows descartável do GitHub antes da publicação. Os resultados da execução são associados à release.

[Compatibilidade](COMPATIBILITY.md) · [Renderizadores e medições da 0.17](VALIDATION-0.17.md)
