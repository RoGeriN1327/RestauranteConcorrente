using System.Collections.Concurrent;
using RestauranteConcorrente.Atores;
using RestauranteConcorrente.Infra;
using RestauranteConcorrente.Modelos;

namespace RestauranteConcorrente;

public sealed class Restaurante
{
    public const int CapacidadeFila = 10;
    public const int TotalPedidos = 60;

    private readonly Configuracao _config;

    public Restaurante(Configuracao config)
    {
        _config = config;
    }

    public async Task<Resultado> AbrirAsync()
    {
        Log.Reiniciar();
        Log.Silencioso = !_config.LogDetalhado;

        var fechamento = _config.Fechamento is { } f ? $"fecha em {f.TotalSeconds:0} s" : "sem fechamento";
        Log.Escrever("Gerente", $"abrindo o restaurante com {_config.Cozinheiros} cozinheiro(s), {fechamento}");

        using var fila = new BlockingCollection<Pedido>(boundedCapacity: CapacidadeFila);
        var estoque = new Estoque(inseguro: _config.Modo == Modo.BugEstoqueNegativo);
        var caixa = new Caixa(inseguro: _config.Modo == Modo.BugRaceCaixa);
        var cozinha = new Cozinha();
        using var balcao = new Balcao();
        using var gerente = new Gerente();
        var garcom = new Garcom("Garçom", balcao);

        var contadorPedidos = 0;
        Func<int> proximoNumero = () => Interlocked.Increment(ref contadorPedidos);

        var pedidosA = (TotalPedidos + 1) / 2;
        var pedidosB = TotalPedidos / 2;

        // Seeds fixas: toda execução gera a mesma sequência de pratos, então os modos e a
        // comparação 1 x N trabalham sempre com os mesmos 60 pedidos.
        var atendentes = new[]
        {
            new Atendente("Atendente A", fila, proximoNumero, pedidosA, seed: 1, gerente.Token),
            new Atendente("Atendente B", fila, proximoNumero, pedidosB, seed: 2, gerente.Token)
        };

        var cozinheiros = Enumerable.Range(1, _config.Cozinheiros)
            .Select(i => new Cozinheiro(
                $"Cozinheiro {i}", fila, estoque, cozinha, balcao, caixa,
                ordemInvertida: _config.Modo == Modo.BugDeadlock,
                gerente.Token))
            .ToArray();

        gerente.Abrir(_config.Fechamento);

        var tarefasAtendentes = atendentes.Select(a => a.ExecutarAsync()).ToArray();
        var tarefasCozinheiros = cozinheiros.Select(c => c.ExecutarAsync()).ToArray();
        var tarefaGarcom = garcom.ExecutarAsync();

        using var fimVigia = new CancellationTokenSource();
        if (_config.Modo == Modo.BugDeadlock)
        {
            _ = VigiarTravamentoAsync(fimVigia.Token);
        }

        // CompleteAdding só pode ser chamado depois que AMBOS os atendentes terminarem,
        // senão o segundo tenta Add em uma coleção já marcada e lança InvalidOperationException.
        await Task.WhenAll(tarefasAtendentes);
        fila.CompleteAdding();
        Log.Escrever("Gerente", "atendentes encerraram; fila fechada para novas entradas");

        // No modo deadlock este await nunca termina: os cozinheiros presos no lock não
        // enxergam o token e o relatório não aparece.
        await Task.WhenAll(tarefasCozinheiros);
        fimVigia.Cancel();
        Log.Escrever("Gerente", "cozinheiros encerraram; chamando garçom para esvaziar o balcão");

        // Com o fechamento, os cozinheiros param de pegar pedidos; o que sobrou na fila
        // não foi atendido. A fila já está fechada, então essa drenagem termina sozinha.
        var naoAtendidos = 0;
        foreach (var pedido in fila.GetConsumingEnumerable())
        {
            naoAtendidos++;
            Log.Escrever("Gerente", $"{pedido} ficou na fila — não atendido");
        }

        garcom.Encerrar();
        await tarefaGarcom;

        var estoqueFinal = estoque.Snapshot();
        var linha = string.Join(" | ", estoqueFinal.Select(kv => $"{kv.Key} {kv.Value}"));
        Log.Escrever("Gerente", $"estoque final: {linha}");
        Log.Escrever("Gerente", "serviço encerrado");

        var tempo = Log.Decorrido;
        Log.Silencioso = false;

        return new Resultado(
            Recebidos: atendentes.Sum(a => a.Anotados),
            Entregues: garcom.Entregues,
            Recusados: cozinheiros.Sum(c => c.Recusados),
            NaoAtendidos: naoAtendidos,
            FaturamentoRegistrado: caixa.Faturamento,
            Vendas: caixa.Vendas,
            EstoqueFinal: estoqueFinal,
            Tempo: tempo);
    }

    // Só no modo deadlock: avisa quando o log fica parado, para a demonstração ficar clara.
    private static async Task VigiarTravamentoAsync(CancellationToken fim)
    {
        var avisado = false;
        try
        {
            while (!fim.IsCancellationRequested)
            {
                await Task.Delay(500, fim);
                var parado = Log.DesdeUltimoEvento;
                if (parado > TimeSpan.FromSeconds(3) && !avisado)
                {
                    Log.Linha();
                    Log.Linha($">>> Nenhum evento há {parado.TotalSeconds:0} s: os cozinheiros estão em DEADLOCK.");
                    Log.Linha(">>> Um segura a tábua esperando a faca, outro segura a faca esperando a tábua.");
                    Log.Linha(">>> O relatório nunca vai aparecer. Encerre com Ctrl+C.");
                    Log.Linha();
                    avisado = true;
                }
                else if (parado < TimeSpan.FromSeconds(1))
                {
                    avisado = false;
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }
}
