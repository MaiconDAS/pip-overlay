# Dependências e atribuições

- **MinHook 1.3.4**, de Tsuda Kageyu e contribuidores. Fontes em `vendor/minhook`, com os avisos originais em [LICENSE.txt](vendor/minhook/LICENSE.txt). Origem: https://github.com/TsudaKageyu/minhook/tree/v1.3.4
- **.NET / Windows Desktop Runtime**, Microsoft e contribuidores. O empacotamento inclui `LICENSE-DOTNET.txt`, `LICENSE-WINDOWSDESKTOP.txt` e `THIRD-PARTY-NOTICES-DOTNET.txt` dos pacotes oficiais usados na compilação.
- **Zig 0.14.1**, ferramenta de compilação portátil obtida de https://ziglang.org/download/0.14.1/. Não é distribuída dentro do instalador do aplicativo.

Os nomes de navegadores, jogos e tecnologias identificam compatibilidade; não indicam parceria ou aprovação de seus titulares. O código original do PiP Overlay está sob a [licença MIT](LICENSE). Essa licença não substitui as licenças dos componentes de terceiros listados aqui.

## Renderizadores da versão 0.11

- **ReShade 6.8.0**, crosire e contribuidores: motor oficial e SDK no commit `18deaa52de0c425a78b329e9cb3c497281cd00ec`. Licença BSD de 3 cláusulas e avisos em [vendor/reshade/LICENSE.md](vendor/reshade/LICENSE.md). O instalador contém os binários oficiais extraídos do pacote Add-on e `LICENSE-RESHADE.md`; não executa o instalador do ReShade. Origem: https://github.com/crosire/reshade/tree/v6.8.0
- **Vulkan-Headers 1.3.290**, Khronos Group e contribuidores: commit `b379292b2ab6df5771ba9870d53cf8b2c9295daf`, licenças e avisos em [vendor/vulkan/LICENSE.md](vendor/vulkan/LICENSE.md) e nos cabeçalhos. Usados para compilar nossa camada Vulkan. Origem: https://github.com/KhronosGroup/Vulkan-Headers/tree/v1.3.290
- **Microsoft Visual C++ Build Tools e Windows SDK**: ferramentas oficiais obtidas e extraídas somente na pasta local de desenvolvimento, com hashes registrados em `scripts/msvc-packages.json`. Sujeitas aos termos da Microsoft. As ferramentas não são distribuídas no instalador; o add-on usa o runtime de C++ vinculado estaticamente. Não é necessário instalar Visual Studio ou o redistribuível separadamente para usar o aplicativo.

## Inno Setup 7.1.0
O instalador utiliza Inno Setup, de Jordan Russell e Martijn Laan, sem modificações no motor. Licença incluída como LICENSE-INNO-SETUP.txt. Fonte: https://github.com/jrsoftware/issrc. O extrator Innounp é ferramenta de desenvolvimento e não é distribuído com o app.
