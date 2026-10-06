# Restaurante Concorrente

Projeto prático da disciplina **Desenvolvimento para Concorrência** (7º período, UniRV).
Simula um restaurante onde atendentes geram pedidos, cozinheiros preparam em paralelo
disputando fornos, utensílios e ingredientes, um garçom entrega os pratos e um gerente
fecha o estabelecimento após um prazo.

## Integrantes

- Rogger Martins Guimarães

## Como executar

Pré-requisito: [.NET 10 SDK](https://dotnet.microsoft.com/download).

```powershell
dotnet run                 # abre o menu
dotnet run -- 1            # roda a opção 1 direto (sem menu)
dotnet run -- 1 8 20       # opção 1 com 8 cozinheiros, fechando em 20 s
```

Ao escolher uma opção no menu, o programa pergunta o número de cozinheiros (padrão 4)
e em quantos segundos o gerente fecha o restaurante (padrão 15).

## Menu

| Opção | O que faz | Resultado esperado |
|-------|-----------|--------------------|
| 1 · Modo seguro | Roda o restaurante com todas as proteções | Relatório com as três verificações `[OK]` |
| 2 · Bug: race condition no caixa | O caixa soma o faturamento sem `lock` (ler → `Sleep` → gravar) | `[ERRO]` faturamento registrado menor que o esperado |
| 3 · Bug: deadlock nos utensílios | Salada pega tábua → faca, Hambúrguer pega faca → tábua, com `Thread.Sleep(100)` entre os dois | O log para, aparece o aviso de deadlock e o relatório nunca sai (encerrar com Ctrl+C) |
| 4 · Bug: estoque negativo | O cozinheiro verifica o estoque ao começar e só desconta ao terminar, sem proteção | `[ERRO]` estoque final com queijo negativo |
| 5 · Comparação 1 x N | Roda os mesmos 60 pedidos sem fechamento com 1 e com N cozinheiros (log desligado) | Os dois tempos e o speedup |
| 0 · Sair | Encerra o programa | |

Os atendentes usam seeds fixas, então todas as opções trabalham com a mesma sequência de 60 pedidos.

## Requisitos e onde estão

| # | Requisito | Ferramenta | Onde está |
|---|-----------|------------|-----------|
| 1 | 2 atendentes geram 60 pedidos aleatórios | Tasks produtoras | `Atores/Atendente.cs` |
| 2 | Fila com capacidade 10 (atendente espera quando cheia) | `BlockingCollection<Pedido>` | `Restaurante.cs` |
| 3 | N cozinheiros consumindo em paralelo | Tasks consumidoras | `Atores/Cozinheiro.cs` |
| 4 | No máximo 2 pratos no forno ao mesmo tempo | `SemaphoreSlim(2)` | `Infra/Cozinha.cs` |
| 5 | Tábua e faca pegas sempre na ordem Tábua → Faca | `lock` | `Atores/Cozinheiro.cs` |
| 6 | Estoque reserva todos os ingredientes de uma vez, nunca negativo | `ConcurrentDictionary` + `lock` | `Infra/Estoque.cs` |
| 7 | Balcão + sino acordando o garçom | `ConcurrentQueue` + `AutoResetEvent` | `Infra/Balcao.cs`, `Atores/Garcom.cs` |
| 8 | Caixa soma o faturamento e conta vendas por prato | `lock` + `AddOrUpdate` | `Infra/Caixa.cs` |
| 9 | Gerente fecha após X segundos; sobras da fila viram "não atendidos" | `CancellationTokenSource` com prazo | `Atores/Gerente.cs`, `Restaurante.cs` |
| 10 | Log de cada evento com horário e autor | `Console.WriteLine` thread-safe | `Infra/Log.cs` |
| 11 | Menu: modo seguro, 3 bugs e comparação 1 x N | — | `Program.cs` |

O relatório final e as três verificações automáticas ficam em `Relatorio.cs`.

## Os três bugs e os consertos

- **Race condition no caixa**: `faturamento += preço` são três passos (ler, somar, gravar).
  Dois cozinheiros leem o mesmo valor e o último a gravar apaga a venda do outro.
  **Conserto:** `lock` em volta dos três passos (`Caixa.Registrar`).
- **Deadlock nos utensílios**: um cozinheiro segura a tábua esperando a faca enquanto outro
  segura a faca esperando a tábua (espera circular). **Conserto:** ordem fixa, tábua
  sempre antes da faca (`Cozinheiro.PrepararNaBancada`).
- **Estoque negativo**: o `ConcurrentDictionary` protege cada operação, mas verificar e
  descontar são operações separadas. Vários cozinheiros veem a mesma última unidade
  e todos descontam. **Conserto:** verificar e descontar todos os ingredientes dentro de
  um único `lock` (`Estoque.TentarReservar`).

## Ferramentas de IA

- Claude Code: geração do esqueleto a partir do PDF do projeto e implementação da Entrega 3.
