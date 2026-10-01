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

## Estado atual — Entrega 1 (28/09)

Esta etapa cobre o esqueleto:

| Requisito | Onde está |
|-----------|-----------|
| 2 atendentes geram 60 pedidos aleatórios (Tasks produtoras) | `Atores/Atendente.cs` |
| Fila `BlockingCollection<Pedido>` capacidade 10 | `Restaurante.cs` |
| N cozinheiros consumindo em paralelo (Tasks consumidoras) | `Atores/Cozinheiro.cs` |
| Log com horário e autor do evento | `Infra/Log.cs` |

**Ainda não implementado** (vem nas Entregas 2 e 3): fornos (`SemaphoreSlim`),
tábua e faca (`lock`), estoque (`ConcurrentDictionary`), balcão + sino (`ConcurrentQueue` +
`AutoResetEvent`), garçom, caixa, gerente (`CancellationTokenSource`), relatório
final e menu com demonstrações de bug.

## Ferramentas de IA

- Claude Code (geração do esqueleto a partir do PDF do projeto).
