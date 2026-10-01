using System.Collections.Concurrent;
using RestauranteConcorrente.Atores;
using RestauranteConcorrente.Infra;
using RestauranteConcorrente.Modelos;

namespace RestauranteConcorrente;

public sealed class Restaurante
{
    private const int CapacidadeFila = 10;
    private const int TotalPedidos = 60;

    private readonly int _quantidadeCozinheiros;

    public Restaurante(int quantidadeCozinheiros)
    {
        _quantidadeCozinheiros = quantidadeCozinheiros;
    }

    public async Task AbrirAsync()
    {
        Log.Escrever("Gerente", $"abrindo o restaurante com {_quantidadeCozinheiros} cozinheiro(s)");

        using var fila = new BlockingCollection<Pedido>(boundedCapacity: CapacidadeFila);

        var contadorPedidos = 0;
        Func<int> proximoNumero = () => Interlocked.Increment(ref contadorPedidos);

        // Divide os 60 pedidos entre os dois atendentes. Se o total for ímpar, o primeiro gera a mais.
        var pedidosA = (TotalPedidos + 1) / 2;
        var pedidosB = TotalPedidos / 2;

        var atendenteA = new Atendente("Atendente A", fila, proximoNumero, pedidosA, seed: 1);
        var atendenteB = new Atendente("Atendente B", fila, proximoNumero, pedidosB, seed: 2);

        var tarefasAtendentes = new[]
        {
            atendenteA.ExecutarAsync(),
            atendenteB.ExecutarAsync()
        };

        var cozinheiros = Enumerable.Range(1, _quantidadeCozinheiros)
            .Select(i => new Cozinheiro($"Cozinheiro {i}", fila))
            .ToArray();

        var tarefasCozinheiros = cozinheiros.Select(c => c.ExecutarAsync()).ToArray();

        // CompleteAdding só pode ser chamado depois que AMBOS os atendentes terminarem,
        // senão o segundo tenta Add em uma coleção já marcada e lança InvalidOperationException.
        await Task.WhenAll(tarefasAtendentes);
        fila.CompleteAdding();
        Log.Escrever("Gerente", "atendentes encerraram; fila fechada para novas entradas");

        await Task.WhenAll(tarefasCozinheiros);
        Log.Escrever("Gerente", $"serviço encerrado — {contadorPedidos} pedidos processados");
    }
}
