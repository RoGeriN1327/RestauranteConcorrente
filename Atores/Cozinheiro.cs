using System.Collections.Concurrent;
using RestauranteConcorrente.Infra;
using RestauranteConcorrente.Modelos;

namespace RestauranteConcorrente.Atores;

public sealed class Cozinheiro
{
    // Pausa entre pegar o primeiro e o segundo utensílio no modo deadlock: garante que
    // outro cozinheiro tenha tempo de pegar o utensílio que falta.
    private const int PausaEntreUtensiliosMs = 100;

    private readonly string _nome;
    private readonly BlockingCollection<Pedido> _fila;
    private readonly Estoque _estoque;
    private readonly Cozinha _cozinha;
    private readonly Balcao _balcao;
    private readonly Caixa _caixa;
    private readonly bool _ordemInvertida;
    private readonly CancellationToken _fechamento;

    public Cozinheiro(
        string nome,
        BlockingCollection<Pedido> fila,
        Estoque estoque,
        Cozinha cozinha,
        Balcao balcao,
        Caixa caixa,
        bool ordemInvertida,
        CancellationToken fechamento)
    {
        _nome = nome;
        _fila = fila;
        _estoque = estoque;
        _cozinha = cozinha;
        _balcao = balcao;
        _caixa = caixa;
        _ordemInvertida = ordemInvertida;
        _fechamento = fechamento;
    }

    // Só é lido depois que a Task do cozinheiro termina, então não precisa de sincronização.
    public int Recusados { get; private set; }

    public Task ExecutarAsync() => Task.Run(Executar);

    private void Executar()
    {
        Log.Escrever(_nome, "entrou na cozinha");

        while (true)
        {
            Pedido? pedido;
            try
            {
                // false = fila fechada (CompleteAdding) e vazia: acabou o serviço.
                // O token interrompe a espera quando o gerente fecha o restaurante;
                // o prato que já está em preparo não é interrompido.
                if (!_fila.TryTake(out pedido, Timeout.Infinite, _fechamento))
                {
                    break;
                }
            }
            catch (OperationCanceledException)
            {
                Log.Escrever(_nome, "restaurante fechou — não pega mais pedidos");
                break;
            }

            Preparar(pedido);
        }

        Log.Escrever(_nome, "saiu da cozinha");
    }

    private void Preparar(Pedido pedido)
    {
        // Modo seguro: reserva tudo de uma vez, numa operação atômica.
        // Modo inseguro: só verifica agora e desconta quando o prato fica pronto.
        var temIngredientes = _estoque.Inseguro
            ? _estoque.Verificar(pedido.Prato.Ingredientes)
            : _estoque.TentarReservar(pedido.Prato.Ingredientes);

        if (!temIngredientes)
        {
            Recusados++;
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

        if (_estoque.Inseguro)
        {
            _estoque.Descontar(pedido.Prato.Ingredientes);
        }

        Log.Escrever(_nome, $"finalizou {pedido}");

        // A conta é fechada pelo cozinheiro que terminou o prato: vários cozinheiros
        // registram no caixa ao mesmo tempo, por isso o faturamento precisa de lock.
        _caixa.Registrar(pedido.Prato);
        _balcao.Depositar(new PedidoPronto(pedido, _nome));
    }

    private void PrepararNoForno(Pedido pedido)
    {
        using var forno = _cozinha.UsarForno();
        Log.Escrever(_nome, $"{pedido} entrou no forno ({forno.Ocupacao}/{Cozinha.TotalFornos})");
        Thread.Sleep(pedido.Prato.PreparoMs);
        Log.Escrever(_nome, $"{pedido} saiu do forno");
    }

    private void PrepararNaBancada(Pedido pedido)
    {
        // Modo seguro: ordem fixa Tábua → Faca para todos os pratos. Como ninguém segura a
        // faca esperando a tábua, a espera circular (condição do deadlock) é impossível.
        // Modo deadlock: o Hambúrguer inverte a ordem (Faca → Tábua). Com uma Salada segurando
        // a tábua e um Hambúrguer segurando a faca, cada um espera o outro para sempre.
        var inverter = _ordemInvertida && pedido.Prato == Cardapio.Hamburguer;
        var (primeiro, nomePrimeiro, segundo, nomeSegundo) = inverter
            ? (_cozinha.Faca, "faca", _cozinha.Tabua, "tábua")
            : (_cozinha.Tabua, "tábua", _cozinha.Faca, "faca");

        lock (primeiro)
        {
            Log.Escrever(_nome, $"{pedido} pegou a {nomePrimeiro}");
            if (_ordemInvertida)
            {
                Thread.Sleep(PausaEntreUtensiliosMs);
                Log.Escrever(_nome, $"{pedido} esperando a {nomeSegundo}...");
            }

            lock (segundo)
            {
                Log.Escrever(_nome, $"{pedido} pegou a {nomeSegundo}");
                Thread.Sleep(pedido.Prato.PreparoMs);
            }
        }
    }
}
