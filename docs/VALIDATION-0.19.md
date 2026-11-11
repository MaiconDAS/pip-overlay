# 0.19 — Evidências e limitações

**Português** · [English](en/VALIDATION-0.19.md)

Atualizado em 11 de novembro de 2026. Este relatório separa o relato do usuário da validação técnica.

| Evidência | Alcance |
| --- | --- |
| Build 0.19 existente | Compilada antes da publicação como candidata em 6 de novembro; compilar não valida o comportamento. |
| Relato do usuário em 11 de novembro | O usuário aprovou desempenho e funcionamento no Black Desert Online e no Digimon Masters Online em seu computador de casa. |
| Detalhes da configuração | Build exata instalada, GPU/driver, resolução, FPS e modo de apresentação não foram informados nesse último relato. |
| Observações preliminares de desempenho | Carga variável; não foi estabelecido percentual confiável de melhoria. |
| Revisão do repositório em 11 de novembro | Somente documentação e metadados das releases. Sem novos testes do aplicativo, gráficos, instalador ou CI. |

A release 0.19.0 existente foi promovida em 11 de novembro após a aprovação do usuário. Os bytes do instalador, a tag original do código e o SHA-256 foram preservados. É uma decisão de canal de distribuição, não uma declaração de compatibilidade universal.

O relato anterior do Black Desert identificou funcionamento no modo desktop e falha na conexão direta. A aprovação mais recente não estabelece qual integração foi usada em cada jogo. As verificações históricas da 0.18 continuam sendo evidência daquela versão; não são novos resultados da 0.19.

## Trabalho restante

- Comparações controladas de consumo de CPU/GPU e entrega de vídeo, incluindo impacto no FPS do jogo.
- Fullscreen exclusivo nas diferentes integrações gráficas e combinações físicas de GPU/driver.
- Monitores físicos, mudanças de DPI, suspensão real e sessões prolongadas.
- Verificação final do ciclo do instalador desta build e assinatura digital pública.

Nenhuma instalação ou configuração do sistema foi alterada nesta revisão. Futuras mudanças no instalador/sistema exigem verificação isolada. Veja [compatibilidade](COMPATIBILITY.md), [matriz histórica](TEST-MATRIX.md) e [política de releases](RELEASES.md).
