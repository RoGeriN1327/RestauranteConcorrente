using RestauranteConcorrente.Infra;
using RestauranteConcorrente.Modelos;

namespace RestauranteConcorrente.Atores;

public sealed class Garcom
{
    private readonly string _nome;
    private readonly Balcao _balcao;
    private readonly List<PedidoPronto> _entregues = new();
    private volatile bool _fechando;

    public Garcom(string nome, Balcao balcao)
    {
        _nome = nome;
        _balcao = balcao;
    }

    // Só o garçom escreve na lista, e o Restaurante só lê depois que a Task dele termina.
    public IReadOnlyList<PedidoPronto> Entregues => _entregues;

    public Task ExecutarAsync() => Task.Run(Executar);

    // Chamado pelo Restaurante quando os cozinheiros encerram. O volatile garante
    // visibilidade imediata da flag na thread do garçom; o Set acorda a espera.
    public void Encerrar()
    {
        _fechando = true;
        _balcao.ChamarGarcom();
    }

    private void Executar()
    {
        Log.Escrever(_nome, "entrou em serviço");

        while (true)
        {
            _balcao.AguardarSino();
            EntregarTudo();

            if (_fechando)
            {
                // Segunda drenagem defensiva: cobre o caso raro de um prato ter sido depositado
                // entre a última drenagem e a verificação da flag.
                EntregarTudo();
                break;
            }
        }

        Log.Escrever(_nome, "saiu de serviço");
    }

    private void EntregarTudo()
    {
        foreach (var pronto in _balcao.DrenarTudo())
        {
            _entregues.Add(pronto);
            Log.Escrever(_nome, $"entregou {pronto.Pedido} (preparado por {pronto.Cozinheiro})");
        }
    }
}
