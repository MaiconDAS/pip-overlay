![PiP Overlay](docs/assets/banner.svg)

**Português** · [English](README.en.md)

Vídeo do Edge ou Chrome sobre seu jogo, com posição e tamanho ajustáveis. A interação fica no editor: o vídeo dentro do jogo não recebe cliques nem foco do teclado.

[**Baixar v0.19.0**](https://github.com/MaiconDAS/pip-overlay/releases/latest) · [Guia de uso](docs/pt-BR/USAGE.md) · [Compatibilidade](docs/COMPATIBILITY.md) · [Relatar problema](https://github.com/MaiconDAS/pip-overlay/issues/new/choose)

Windows x64 · Código aberto / MIT · Sem RTSS ou extensão

## Versão atual: 0.19.0

- **Abra o PiP e jogue:** saída desktop como padrão, sem selecionar ou reiniciar o jogo para uso em janela/borderless.
- **Agendamento eficiente:** avisos de quadros novos, menos consultas ociosas e reutilização de recursos ao redimensionar.
- **Recuperação de apresentação:** novas tentativas quando a GPU está ocupada; FPS automático considera pressão sustentada sem alterar a resolução.
- **Controles claros:** modo de saída visível, posicionamento pelo teclado e gerenciador de perfis para jogos e monitores locais.
- **Preferências portáveis:** backup/importação, associação de monitores e download verificado de instaladores.
- **Dez idiomas na interface:** português, inglês, espanhol, alemão, italiano, francês, japonês, coreano, chinês simplificado e russo.

Em 11 de novembro de 2026, o usuário aprovou o funcionamento e o desempenho em **Black Desert Online e Digimon Masters Online**. É um relato de uso, não um benchmark controlado nem comprovação de fullscreen exclusivo. A validação adicional permanece incompleta.

[Histórico](CHANGELOG.md) · [Evidências e limites pendentes](docs/VALIDATION-0.19.md) · [Política de versões](docs/RELEASES.md)

## Como usar

1. Baixe e execute o instalador. Ele inclui o runtime, cria atalhos e registra a desinstalação no Windows. Atualizações preservam preferências.
2. Abra o PiP Overlay e coloque o vídeo no **Picture-in-Picture nativo do Edge ou Chrome**. Mantenha a janela aberta, sem minimizar.
3. A overlay independente aparece sobre janelas e jogos borderless, sem selecionar o processo do jogo. A conexão direta fica como opção avançada; fullscreen exclusivo exige validação de compatibilidade separada.
4. Arraste o vídeo no editor e use suas bordas ou cantos para redimensioná-lo. A conexão ocorre quando a fonte e um renderizador compatível estão disponíveis.

Resolução e FPS são independentes. Escolher 30 fps mantém o limite de resolução escolhido; o tamanho real da captura depende do navegador e da fonte. **Pausar** impede a reconexão automática. Fechar a janela mantém o app na bandeja; use **Sair** para encerrá-lo.

[Guia completo de instalação, ajustes e desinstalação](docs/pt-BR/USAGE.md)

## Editor visual

*Imagem da 0.18; a versão atual também inclui identificação do modo de saída e gerenciador de perfis.*

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

**Detectar um jogo não garante que ele aceite a overlay.** Proteções, launchers e configurações gráficas podem impedir a integração. Há aprovação de uso em Black Desert Online e Digimon Masters Online; isso não valida todas as integrações ou fullscreen exclusivo. Fullscreen das APIs mais novas requer validação específica; testes sintéticos não certificam todos os jogos nem garantem 60 fps.

[Limites e detecção de jogos](docs/COMPATIBILITY.md) · [Matriz de testes](docs/TEST-MATRIX.md)

## Privacidade e instalação

O vídeo é processado localmente, sem gravação ou envio pelo app. Consultas opcionais de atualização acessam o GitHub. O diagnóstico é exportado somente por escolha do usuário, sem envio automático. [Leia a política de privacidade](docs/pt-BR/PRIVACY.md).

O instalador é por usuário, inclui .NET e renderizadores e não instala serviços ou drivers. A desinstalação preserva preferências e arquivos pessoais. A assinatura pública permanece pendente; o SHA-256 verifica integridade, não identidade do editor. [Detalhes da assinatura](docs/SIGNING.md).

## Documentação e contribuição

[Documentação em português](docs/README.md) · [English documentation](docs/en/README.md) · [Desenvolvimento](docs/DEVELOPMENT.md) · [Como contribuir](CONTRIBUTING.md) · [Segurança](docs/pt-BR/SECURITY.md)

C# / WinForms / .NET 8 e C++ / Direct3D. Testes de instalação são feitos primeiro em Windows descartável. Código próprio sob [MIT](LICENSE); dependências mantêm suas [licenças originais](THIRD_PARTY_NOTICES.md). Projeto independente de [MaiconDAS](https://github.com/MaiconDAS), sem afiliação com fabricantes de navegadores ou jogos.
