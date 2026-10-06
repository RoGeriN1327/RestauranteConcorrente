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
    private readonly CancellationToken _fechamento;

    public Atendente(
        string nome,
        BlockingCollection<Pedido> fila,
        Func<int> proximoNumero,
        int pedidosParaGerar,
        int seed,
        CancellationToken fechamento)
    {
        _nome = nome;
        _fila = fila;
        _proximoNumero = proximoNumero;
        _pedidosParaGerar = pedidosParaGerar;
        _rng = new Random(seed);
        _fechamento = fechamento;
    }

    // Só é lido depois que a Task do atendente termina, então não precisa de sincronização.
    public int Anotados { get; private set; }

    public Task ExecutarAsync() => Task.Run(Executar);

    private void Executar()
    {
        Log.Escrever(_nome, "entrou em serviço");

        for (int i = 0; i < _pedidosParaGerar; i++)
        {
            var intervaloMs = _rng.Next(80, 350);

            // Espera o intervalo, mas acorda na hora se o gerente fechar o restaurante.
            if (_fechamento.WaitHandle.WaitOne(intervaloMs))
            {
                break;
            }

            var prato = Cardapio.Aleatorio(_rng);
            var numero = _proximoNumero();
            var pedido = new Pedido(numero, prato, _nome);

            try
            {
                // Add bloqueia quando a fila atinge a capacidade — comportamento esperado da
                // BlockingCollection. Com o token, a espera é interrompida no fechamento.
                _fila.Add(pedido, _fechamento);
            }
            catch (OperationCanceledException)
            {
                Log.Escrever(_nome, $"restaurante fechou com a fila cheia — {pedido} não foi anotado");
                break;
            }

            Anotados++;
            Log.Escrever(_nome, $"anotou {pedido} (fila: {_fila.Count}/{_fila.BoundedCapacity})");
        }

        Log.Escrever(_nome, "saiu de serviço");
    }
}
