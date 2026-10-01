# Restaurante Concorrente

Projeto prático da disciplina **Desenvolvimento para Concorrência** (7º período, UniRV).
Simula um restaurante onde atendentes geram pedidos, cozinheiros preparam em paralelo e,
nas próximas entregas, um garçom entrega e um gerente fecha o estabelecimento.

## Integrantes

- Rogger Martins Guimarães

## Como executar

Pré-requisito: [.NET 10 SDK](https://dotnet.microsoft.com/download).

```powershell
dotnet run                 # usa 4 cozinheiros
dotnet run -- 1            # 1 cozinheiro
dotnet run -- 8            # 8 cozinheiros
```

## Estado atual — Entrega 2 (01/10)

Além do esqueleto da Entrega 1, agora os cozinheiros disputam recursos
compartilhados sem corromper dados e sem travar.

| Requisito | Ferramenta | Onde está |
|-----------|------------|-----------|
| 2 atendentes geram 60 pedidos aleatórios | Tasks produtoras | `Atores/Atendente.cs` |
| Fila com capacidade 10 (atendente espera quando cheia) | `BlockingCollection<Pedido>` | `Restaurante.cs` |
| N cozinheiros consumindo em paralelo | Tasks consumidoras | `Atores/Cozinheiro.cs` |
| No máximo 2 pratos no forno ao mesmo tempo | `SemaphoreSlim(2)` | `Infra/Cozinha.cs` |
| Tábua e faca pegas sempre na ordem Tábua → Faca | `lock` | `Atores/Cozinheiro.cs` |
| Estoque atômico, sem valores negativos | `ConcurrentDictionary` + lock | `Infra/Estoque.cs` |
| Balcão + sino acordando o garçom | `ConcurrentQueue` + `AutoResetEvent` | `Infra/Balcao.cs`, `Atores/Garcom.cs` |
| Log de cada evento | `Console.WriteLine` thread-safe | `Infra/Log.cs` |

**Ainda não implementado** (Entrega 3): caixa (`lock` + `AddOrUpdate`), gerente
(`CancellationTokenSource`), relatório final com verificações automáticas, menu
com modo seguro + 3 demonstrações de bug + comparação 1 × N cozinheiros.

## Ferramentas de IA

- Claude Code (geração do esqueleto a partir do PDF do projeto).
