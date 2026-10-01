using System.Collections.Concurrent;
using RestauranteConcorrente.Infra;
using RestauranteConcorrente.Modelos;

namespace RestauranteConcorrente.Atores;

public sealed class Cozinheiro
{
    private readonly string _nome;
    private readonly BlockingCollection<Pedido> _fila;
    private readonly Estoque _estoque;
    private readonly Cozinha _cozinha;
    private readonly Balcao _balcao;

    public Cozinheiro(
        string nome,
        BlockingCollection<Pedido> fila,
        Estoque estoque,
        Cozinha cozinha,
        Balcao balcao)
    {
        _nome = nome;
        _fila = fila;
        _estoque = estoque;
        _cozinha = cozinha;
        _balcao = balcao;
    }

    public Task ExecutarAsync() => Task.Run(Executar);

    private void Executar()
    {
        Log.Escrever(_nome, "entrou na cozinha");

        foreach (var pedido in _fila.GetConsumingEnumerable())
        {
            Preparar(pedido);
        }

        Log.Escrever(_nome, "saiu da cozinha");
    }

    private void Preparar(Pedido pedido)
    {
        if (!_estoque.TentarReservar(pedido.Prato.Ingredientes))
        {
            Log.Escrever(_nome, $"recusou {pedido} — sem ingrediente");
            return;
        }

        Log.Escrever(_nome, $"começou {pedido} ({pedido.Prato.PreparoMs} ms)");

        if (pedido.Prato.UsaForno)
        {
            PrepararNoForno(pedido);
        }
        else if (pedido.Prato.UsaTabuaFaca)
        {
            PrepararNaBancada(pedido);
        }
        else
        {
            Thread.Sleep(pedido.Prato.PreparoMs);
        }

        Log.Escrever(_nome, $"finalizou {pedido}");
        _balcao.Depositar(new PedidoPronto(pedido, _nome));
    }

    private void PrepararNoForno(Pedido pedido)
    {
        using var forno = _cozinha.UsarForno();
        Log.Escrever(_nome, $"{pedido} entrou no forno ({forno.Ocupacao}/{Cozinha.TotalFornos})");
        Thread.Sleep(pedido.Prato.PreparoMs);
        Log.Escrever(_nome, $"{pedido} saiu do forno");
    }

    // Ordem fixa Tabua → Faca para evitar deadlock. Em Entrega 3 teremos um modo inseguro
    // que inverte essa ordem para demonstrar o travamento.
    private void PrepararNaBancada(Pedido pedido)
    {
        lock (_cozinha.Tabua)
        {
            Log.Escrever(_nome, $"{pedido} pegou a tábua");
            lock (_cozinha.Faca)
            {
                Log.Escrever(_nome, $"{pedido} pegou a faca");
                Thread.Sleep(pedido.Prato.PreparoMs);
            }
        }
    }
}
