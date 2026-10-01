using RestauranteConcorrente.Infra;

namespace RestauranteConcorrente.Atores;

public sealed class Garcom
{
    private readonly string _nome;
    private readonly Balcao _balcao;
    private volatile bool _fechando;

    public Garcom(string nome, Balcao balcao)
    {
        _nome = nome;
        _balcao = balcao;
    }

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

            foreach (var pronto in _balcao.DrenarTudo())
            {
                Log.Escrever(_nome, $"entregou {pronto.Pedido} (preparado por {pronto.Cozinheiro})");
            }

            if (_fechando)
            {
                // Segunda drenagem defensiva: cobre o caso raro de um prato ter sido depositado
                // entre a última drenagem e a verificação da flag.
                foreach (var pronto in _balcao.DrenarTudo())
                {
                    Log.Escrever(_nome, $"entregou {pronto.Pedido} (preparado por {pronto.Cozinheiro})");
                }
                break;
            }
        }

        Log.Escrever(_nome, "saiu de serviço");
    }
}
