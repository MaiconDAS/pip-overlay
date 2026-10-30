# Instalação e uso

**Português** · [English](../en/USAGE.md) · [Documentação](../README.md)

## Instalar ou levar para outro computador

Copie `PiP-Overlay-v0.16.1.exe` e execute-o. O atalho na área de trabalho é opcional e vem marcado por padrão. Atalhos no menu Iniciar e a entrada em Aplicativos instalados / Programas e Recursos são criados automaticamente. Runtime e renderizadores estão incluídos; não precisa de extensão, RTSS ou download separado de runtime.

A instalação registrada é detectada automaticamente. Atualizar na mesma pasta preserva preferências. Escolher outro destino importa as preferências da instalação registrada sem sobrescrever ajustes existentes no destino. Para uma cópia portátil antiga, selecione a própria pasta dela. Não há etapa manual de importação nem varredura indiscriminada do disco.

## Conectar vídeo e jogo

Abra o PiP Overlay e o PiP nativo do vídeo no Edge ou Chrome. Mantenha-o aberto e sem minimizar. Para APIs modernas, use **Iniciar jogo configurado** no menu do ícone perto do relógio e selecione o executável real na primeira vez. A conexão é automática quando uma fonte e um renderizador compatíveis estão disponíveis. Uma aba comum do navegador, sozinha, não é uma fonte suportada.

A lista identifica janelas e módulos gráficos sem filtrar por nome de jogo. Um processo pode aparecer como **não conectado** quando ainda não foi preparado ou sua consulta foi bloqueada. Use **Ajustes → Mais → Trocar jogo** para configurar outro executável. Detectar um jogo não comprova compatibilidade; Black Desert Online não foi validado. Veja [compatibilidade](../COMPATIBILITY.md).

## Posição, tamanho e qualidade

Arraste o PiP e use suas bordas ou cantos para redimensionar mantendo a proporção. **Centralizar** centraliza; **Desfazer** ou Ctrl+Z dentro do editor restaura a posição anterior. Alt suspende temporariamente o encaixe. Com vários PiPs, **Vídeos** abre miniaturas para escolher a fonte.

| Seção lateral | Opções |
| --- | --- |
| Imagem | Resolução, FPS e ajuste automático |
| Posição | Monitor, posição, encaixe e margem |
| Mais | Jogos, fontes, atalhos, atualizações e diagnóstico |

Resolução é independente de 15/30/60 fps. Até 1080p a 30 fps preserva o limite solicitado. Selecionar 1080p no site não garante captura PiP de 1920×1080. O app amplia temporariamente a origem e restaura seu tamanho ao parar; limites de hardware e navegador continuam aplicáveis. FPS automático reduz a cadência dentro dos limites escolhidos, sem reduzir a resolução selecionada. Perfis guardam posição e qualidade por jogo.

O PiP pode encostar nas bordas mesmo com margem de encaixe e tem cantos discretos de seis pixels. O transporte GPU D3D11 é selecionado automaticamente quando compatível; outras APIs usam transporte CPU. Não há ajuste técnico adicional necessário.

## Pausar, sair e desinstalar

**Pausar** também interrompe a reconexão automática; **Retomar** a reativa. Fechar a janela deixa o app na bandeja. Use **Sair** para encerrá-lo. O aplicativo não configura inicialização automática com o Windows.

Desinstale por Aplicativos instalados, pelo menu Iniciar ou pelo atalho na pasta do app. Feche primeiro o aplicativo e os jogos iniciados por ele. Preferências e arquivos pessoais são preservados; veja a [política de privacidade](PRIVACY.md) para remover dados locais. O pacote contém políticas de privacidade nos dois idiomas. A assinatura pública permanece [pendente](../SIGNING.md).
