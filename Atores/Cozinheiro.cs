using System.Collections.Concurrent;
using RestauranteConcorrente.Infra;
using RestauranteConcorrente.Modelos;

namespace RestauranteConcorrente.Atores;

public sealed class Cozinheiro
{
    private readonly string _nome;
    private readonly BlockingCollection<Pedido> _fila;

    public Cozinheiro(string nome, BlockingCollection<Pedido> fila)
    {
        _nome = nome;
        _fila = fila;
    }

    public Task ExecutarAsync() => Task.Run(Executar);

    private void Executar()
    {
        Log.Escrever(_nome, "entrou na cozinha");

        // GetConsumingEnumerable só termina quando CompleteAdding é chamado e a fila esvazia.
        foreach (var pedido in _fila.GetConsumingEnumerable())
        {
            Log.Escrever(_nome, $"começou {pedido} ({pedido.Prato.PreparoMs} ms)");
            Thread.Sleep(pedido.Prato.PreparoMs);
            Log.Escrever(_nome, $"finalizou {pedido}");
        }

        Log.Escrever(_nome, "saiu da cozinha");
    }
}
