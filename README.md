![PiP Overlay](docs/assets/banner.svg)

**Português** · [English](README.en.md)

Vídeo do Edge ou Chrome sobre seu jogo, com posição e tamanho ajustáveis. A interação fica no editor: o vídeo dentro do jogo não recebe cliques nem foco do teclado.

[**Baixar v0.18.0**](https://github.com/MaiconDAS/pip-overlay/releases/latest) · [Guia de uso](docs/pt-BR/USAGE.md) · [Compatibilidade](docs/COMPATIBILITY.md) · [Relatar problema](https://github.com/MaiconDAS/pip-overlay/issues/new/choose)

Windows x64 · Código aberto / MIT · Sem RTSS ou extensão

> **Candidata 0.19.0:** o código contém melhorias de agendamento desktop, recuperação e perfis. Os testes adicionais foram interrompidos a pedido do usuário; o download estável continua na 0.18.0. [Notas da candidata](CHANGELOG.md).

## Novidades da versão atual

A **0.18.0** torna a saída independente na área de trabalho o padrão:

- **Abra o PiP e jogue:** sem selecionar processo ou reiniciar o jogo no modo desktop. Não há troca automática para integração no jogo.
- **Controles simples:** conexão direta é opcional e avançada; o botão Vídeos permanece visível. Setas movem, Ctrl+setas redimensiona e Shift aumenta o passo.
- **Layouts por monitor:** restauração proporcional ao mudar a resolução, com layout salvo por monitor e preferência de tela preservada.
- **Recuperação:** exportação/importação com backup e download verificado de instaladores, incluindo versões estáveis anteriores.
- **Dez idiomas e diagnóstico:** novos controles traduzidos; relatórios locais distinguem saída desktop de renderizador conectado.

[Histórico](CHANGELOG.md) · [Validação e limites da 0.18](docs/VALIDATION-0.18.md)

## Como usar

1. Baixe e execute o instalador. Ele inclui o runtime, cria atalhos e registra a desinstalação no Windows. Atualizações preservam preferências.
2. Abra o PiP Overlay e coloque o vídeo no **Picture-in-Picture nativo do Edge ou Chrome**. Mantenha a janela aberta, sem minimizar.
3. A overlay independente aparece sobre janelas e jogos borderless, sem selecionar o processo do jogo. A conexão direta fica como opção avançada; fullscreen exclusivo exige validação de compatibilidade separada.
4. Arraste o vídeo no editor e use suas bordas ou cantos para redimensioná-lo. A conexão ocorre quando a fonte e um renderizador compatível estão disponíveis.

Resolução e FPS são independentes. Escolher 30 fps mantém o limite de resolução escolhido; o tamanho real da captura depende do navegador e da fonte. **Pausar** impede a reconexão automática. Fechar a janela mantém o app na bandeja; use **Sair** para encerrá-lo.

[Guia completo de instalação, ajustes e desinstalação](docs/pt-BR/USAGE.md)

## Editor visual

![Editor do PiP Overlay com ajustes laterais](docs/assets/workspace-018.png)

O editor representa o monitor proporcionalmente. Os ajustes ficam na lateral, preservando a área principal do PiP.

| Quero ajustar | Onde encontrar |
| --- | --- |
| Resolução e FPS | Ajustes → Imagem |
| Monitor, posição e encaixe | Ajustes → Posição |
| Jogos, fontes, atalhos e atualizações | Ajustes → Mais |
| Centralizar ou desfazer | Barra do editor; Ctrl+Z funciona no editor |
| Mover sem encaixe | Segure Alt ao arrastar |

## Compatibilidade

| Componente | Situação |
| --- | --- |
| Windows | Windows 10 2004 ou superior / Windows 11, x64 |
| Navegadores | PiP nativo do Edge e Chrome |
| DirectX 9 | Conexão por processo com D3D9; testes em host próprio, incluindo fullscreen |
| DirectX 11 | Conexão com processo aberto; hosts próprios x86/x64 |
| DirectX 12 e OpenGL | Integração preparada na inicialização; hosts próprios x86/x64 |
| Janela/borderless | Saída independente via DirectComposition, sem integração no processo |
| Vulkan | Experimental; SDR de 8 bits, com limites de apresentação |
| Transporte GPU | Otimizado em DirectX 11; outras APIs mantêm transporte CPU |

**Detectar um jogo não garante que ele aceite a overlay.** Proteções, launchers e configurações gráficas podem impedir a integração. Há relato do usuário de funcionamento no Black Desert Online pela saída desktop; isso não valida integração direta ou fullscreen exclusivo. Fullscreen das APIs mais novas requer validação específica; testes sintéticos não certificam todos os jogos nem garantem 60 fps.

[Limites e detecção de jogos](docs/COMPATIBILITY.md) · [Matriz de testes](docs/TEST-MATRIX.md)

## Privacidade e instalação

O vídeo é processado localmente, sem gravação ou envio pelo app. Consultas opcionais de atualização acessam o GitHub. O diagnóstico é exportado somente por escolha do usuário, sem envio automático. [Leia a política de privacidade](docs/pt-BR/PRIVACY.md).

O instalador é por usuário, inclui .NET e renderizadores e não instala serviços ou drivers. A desinstalação preserva preferências e arquivos pessoais. A assinatura pública permanece pendente; o SHA-256 verifica integridade, não identidade do editor. [Detalhes da assinatura](docs/SIGNING.md).

## Documentação e contribuição

[Documentação em português](docs/README.md) · [English documentation](docs/en/README.md) · [Desenvolvimento](docs/DEVELOPMENT.md) · [Como contribuir](CONTRIBUTING.md) · [Segurança](docs/pt-BR/SECURITY.md)

C# / WinForms / .NET 8 e C++ / Direct3D. Testes de instalação são feitos primeiro em Windows descartável. Código próprio sob [MIT](LICENSE); dependências mantêm suas [licenças originais](THIRD_PARTY_NOTICES.md). Projeto independente de [MaiconDAS](https://github.com/MaiconDAS), sem afiliação com fabricantes de navegadores ou jogos.
