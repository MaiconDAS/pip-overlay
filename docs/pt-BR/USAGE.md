# Instalação e uso

**Português** · [English](../en/USAGE.md) · [Documentação](../README.md)

## Instalar ou levar para outro computador

Copie `PiP-Overlay-v0.18.0.exe` e execute-o. O atalho na área de trabalho é opcional e vem marcado por padrão. Atalhos no menu Iniciar e a entrada em Aplicativos instalados / Programas e Recursos são criados automaticamente. Runtime e renderizadores estão incluídos; não precisa de extensão, RTSS ou download separado de runtime.

A instalação registrada é detectada automaticamente. Atualizar na mesma pasta preserva preferências. Escolher outro destino importa as preferências da instalação registrada sem sobrescrever ajustes existentes no destino. Para uma cópia portátil antiga, selecione a própria pasta dela. Não há etapa manual de importação nem varredura indiscriminada do disco.

## Abra o PiP e jogue

Abra o PiP Overlay e o PiP nativo do vídeo no Edge ou Chrome. Mantenha a origem aberta e sem minimizar. A overlay desktop padrão aparece sobre janelas e jogos borderless, deixa os cliques atravessarem e não toma o foco do teclado. Não é preciso selecionar um jogo nem reiniciá-lo. Uma aba comum do navegador, sozinha, não é uma fonte suportada.

**Ajustes → Mais → Conectar diretamente ao jogo** revela a seleção avançada de jogos. Esse modo pode carregar uma integração no processo compatível; não é necessário para a saída desktop. D3D9/11 podem conectar a processos abertos quando permitido; outras APIs podem exigir inicialização preparada pelo menu da bandeja. Um processo listado não comprova compatibilidade. A saída desktop não cobre fullscreen exclusivo. Veja [compatibilidade](../COMPATIBILITY.md).

## Idioma

Em **Ajustes → Mais → Idioma**, escolha uma das dez línguas. A alteração é aplicada sem reiniciar a captura e fica salva globalmente, independentemente do perfil do jogo. Mensagens técnicas do Windows podem seguir o idioma do sistema.

## Posição, tamanho e qualidade

Arraste o PiP e use suas bordas ou cantos para redimensionar mantendo a proporção. **Centralizar** centraliza; **Desfazer** ou Ctrl+Z dentro do editor restaura a posição anterior. Alt suspende temporariamente o encaixe. Com vários PiPs, **Vídeos** abre miniaturas para escolher a fonte.

| Seção lateral | Opções |
| --- | --- |
| Imagem | Resolução, FPS e ajuste automático |
| Posição | Monitor, posição, encaixe e margem |
| Mais | Idioma, jogos, fontes, atalhos, atualizações e diagnóstico |

Resolução é independente de 15/30/60 fps. Até 1080p a 30 fps preserva o limite solicitado. Selecionar 1080p no site não garante captura PiP de 1920×1080. O app amplia temporariamente a origem e restaura seu tamanho ao parar; limites de hardware e navegador continuam aplicáveis. FPS automático reduz a cadência dentro dos limites escolhidos, sem reduzir a resolução selecionada. Perfis guardam posição e qualidade por jogo.

O PiP pode encostar nas bordas mesmo com margem de encaixe e tem cantos discretos de seis pixels. O transporte GPU D3D11 é selecionado automaticamente quando compatível; outras APIs usam transporte CPU. Não há ajuste técnico adicional necessário.

## Pausar, sair e desinstalar

**Pausar** também interrompe a reconexão automática; **Retomar** a reativa. Fechar a janela deixa o app na bandeja. Use **Sair** para encerrá-lo. O aplicativo não configura inicialização automática com o Windows.

Desinstale por Aplicativos instalados, pelo menu Iniciar ou pelo atalho na pasta do app. Feche primeiro o aplicativo e os jogos iniciados por ele. Preferências e arquivos pessoais são preservados; veja a [política de privacidade](PRIVACY.md) para remover dados locais. O pacote contém políticas de privacidade nos dois idiomas. A assinatura pública permanece [pendente](../SIGNING.md).

## Layouts e teclado

As setas movem o PiP em um pixel físico; Shift aumenta o passo para dez. Ctrl+setas redimensiona preservando a proporção. Ctrl+Z desfaz o último gesto. Essas teclas só funcionam quando o editor tem foco. A overlay continua sem interação.

Layouts de monitores são salvos separadamente dentro do perfil atual. Mudanças de resolução ajustam o tamanho proporcionalmente e preservam a posição relativa, incluindo as bordas. Se o monitor preferido desconectar, o aplicativo usa a tela principal sem esquecer a preferência. Ao reconectar, restaura seu layout. As telas são identificadas pelo nome atribuído pelo Windows, que pode mudar em outro PC. Perfis por jogo são aplicados no modo direto; o modo desktop usa o layout atual sem procurar jogos.

## Preferências e recuperação

Em **Ajustes → Mais**, exporte um JSON e importe no outro computador. A importação valida o arquivo, mescla os perfis de jogos, salva os ajustes anteriores em `data/backups`, mantém o caminho local do executável e pausa o PiP. O aplicativo fecha; abra novamente para aplicar e escolha Retomar. Backups podem ser importados pela mesma ação. As chaves dos perfis dependem do caminho do executável; caminhos diferentes em outro PC exigem selecionar/copiar o perfil novamente. O monitor também pode precisar de nova seleção.

**Atualizações** lista instaladores estáveis com SHA-256 informado pela release oficial do GitHub. Verifica tamanho e hash antes de aceitar o arquivo, salva um backup e permite escolher uma versão estável anterior. Feche o app e execute o instalador baixado. Versões antigas podem não entender ajustes novos; os backups continuam disponíveis. O hash não substitui assinatura do editor; a assinatura pública está pendente. Fechar a janela de download cancela a operação. Nenhum instalador é executado automaticamente. Downloads e backups permanecem locais até serem removidos pelo usuário.
