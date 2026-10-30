# Revisão do repositório — 0.16

[English](en/REPOSITORY-AUDIT.md)

A revisão confrontou documentação principal, manifestos de versão, instruções de uso, scripts de build/teste/pacote, templates de issues/PR e metadados do GitHub com a implementação atual. Fontes vendorizadas mantêm seus avisos e versões originais; não foram reescritas como se fossem código próprio.

Correções realizadas:

- README deixou de misturar instruções atuais com listas antigas de recursos. Botões removidos, importação manual e a afirmação de que o transporte GPU estava desativado não aparecem como orientação atual.
- Versões do aplicativo, Inno Setup, teste do instalador e workflow de candidato estão alinhadas em 0.16.0.
- Compatibilidade diferencia API de renderização, transporte GPU D3D11, validação sintética e jogos reais. Fullscreen das novas APIs não é declarado certificado.
- Arquitetura distingue mudanças do instalador (atalhos e registro de desinstalação por usuário) do funcionamento normal do overlay, que não altera configurações globais.
- Política de privacidade descreve consulta ao GitHub e dados locais; não afirma que o app nunca faz conexões de rede. Diagnóstico exportado e logs técnicos de terceiros são diferenciados.
- Segurança ganhou canal privado de vulnerabilidades habilitado no GitHub. Templates e guias principais têm acesso em português e inglês.
- Relatórios anteriores estão marcados como históricos. A licença MIT original e as licenças de terceiros foram preservadas.
- Imagem atual do editor substitui a captura antiga no README. Links relativos e arquivos de documentação são verificados por script.

Arquivos históricos RTSS permanecem no código de desenvolvimento, excluídos da build de produção. A assinatura pública permanece pendente; não foi anunciada como concluída.

A revisão adicional removeu filtros de nome de jogo do editor e do carregador DirectX 9. Identificadores específicos restantes pertencem a diagnósticos históricos excluídos da produção. Detecção por janela/renderizador não é apresentada como certificação de compatibilidade.
