using System.Collections.Concurrent;
using RestauranteConcorrente.Infra;
using RestauranteConcorrente.Modelos;

namespace RestauranteConcorrente.Atores;

public sealed class Atendente
{
    private readonly string _nome;
    private readonly BlockingCollection<Pedido> _fila;
    private readonly Func<int> _proximoNumero;
    private readonly int _pedidosParaGerar;
    private readonly Random _rng;

    public Atendente(
        string nome,
        BlockingCollection<Pedido> fila,
        Func<int> proximoNumero,
        int pedidosParaGerar,
        int seed)
    {
        _nome = nome;
        _fila = fila;
        _proximoNumero = proximoNumero;
        _pedidosParaGerar = pedidosParaGerar;
        _rng = new Random(seed);
    }

    public Task ExecutarAsync() => Task.Run(Executar);

    private void Executar()
    {
        Log.Escrever(_nome, "entrou em serviço");

        for (int i = 0; i < _pedidosParaGerar; i++)
        {
            var intervaloMs = _rng.Next(80, 350);
            Thread.Sleep(intervaloMs);

            var prato = Cardapio.Aleatorio(_rng);
            var numero = _proximoNumero();
            var pedido = new Pedido(numero, prato, _nome);

            Log.Escrever(_nome, $"anotou {pedido} (fila: {_fila.Count}/{_fila.BoundedCapacity})");

            // Add bloqueia quando a fila atinge a capacidade — comportamento esperado da BlockingCollection.
            _fila.Add(pedido);
        }

        Log.Escrever(_nome, "saiu de serviço");
    }
}
