@../ControleFinanceiro.Application/IA/CLAUDE.md

# Servidor MCP

As regras da saída para o modelo, importadas acima, valem para toda ferramenta daqui.

- **Servidor local por stdio**, sem porta e fora do AppHost: quem o inicia é o cliente MCP, a partir da cópia que `pwsh scripts/PublicarMcp.ps1` publica em `artifacts/mcp/`. Registro, pré-requisitos e o roteiro de conferência estão no [docs/servidor-mcp.md](../../docs/servidor-mcp.md).
- **Ferramenta que grava (`ReadOnly = false`) tem as duas travas**: a meta `anthropic/requiresUserInteraction` (`[McpMeta(...)]`) e uma regra `ask` no `.claude/settings.json`, sem nenhum `allow` que a cubra. O `FerramentasQueGravamTests` confere; o porquê está no [docs/harness.md](../../docs/harness.md#ferramentas-do-servidor-mcp-que-gravam).
- **A `[Description]` cita outra ferramenta pelo `Tools`** (`{Tools.PreverVinculo}`), e não pelo literal. O atributo exige constante, então é a exceção da regra do SmartEnum de `src/CLAUDE.md`.
