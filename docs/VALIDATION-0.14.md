# Validação 0.14.0

## Instalador completo

O extrator anterior foi substituído pelo Inno Setup 7.1.0. A instalação padrão é por usuário em `%LOCALAPPDATA%\Programs\PiP Overlay`, com opção de outra pasta, sem elevação. O registro de desinstalação usa HKCU; não há serviço, driver, tarefa agendada nem inicialização automática.

O workflow `installer.yml` testa o executável da release ainda em rascunho numa máquina Windows descartável do GitHub. O teste é explicitamente bloqueado fora de um runner hospedado pelo GitHub.

Verificações realizadas no runner:

- Instalação silenciosa em caminho com espaços.
- Entrada em Aplicativos instalados, nome, versão, pasta e comando de desinstalação.
- Atalhos na área de trabalho e menu Iniciar, destino, diretório de trabalho e ícone.
- Atalhos de desinstalação no menu Iniciar e na pasta do aplicativo.
- Importação de preferências e perfis de uma instalação portátil anterior.
- Reinstalação/atualização na mesma pasta sem sobrescrever os ajustes existentes.
- Desinstalação recusada quando o mutex da aplicação indica que está aberta.
- Remoção de executável, atalhos e entrada de desinstalação.
- Preservação das preferências e de um arquivo pessoal adicionado à pasta.

## Pacote e ícone

494 arquivos extraídos foram comparados por SHA256 ao conteúdo preparado. Nenhum host de teste ou preferência pessoal foi distribuído. Os testes de regressão de conexão, atalhos, geometria, perfis e atualizações passaram.

O ícone contém imagens de 16, 20, 24, 32, 40, 48, 64, 128 e 256 pixels. O executável e o instalador incorporam esse ICO; a interface carrega o tamanho adequado do recurso incorporado. A prévia da interface foi inspecionada.

## Limites e proteção do Windows

A integração foi exercitada no Windows do runner hospedado, não na instalação real do usuário. Isso não equivale a testar todas as políticas corporativas, todos os perfis com OneDrive ou todas as versões de Windows 10/11. Os renderizadores não foram alterados; não há nova alegação de compatibilidade com jogos.

O instalador permanece sem assinatura pública, conforme a decisão de deixar essa etapa pendente de certificado. Os hashes publicados conferem integridade, não identidade do editor.

O compilador foi obtido por extração do arquivo oficial verificado, sem executar seu instalador. Nenhuma configuração de DNS, Registro, serviço ou driver do computador do usuário foi alterada. A reversão da instalação distribuída é feita pelo desinstalador; arquivos pessoais são preservados.
