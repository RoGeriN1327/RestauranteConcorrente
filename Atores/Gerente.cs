using RestauranteConcorrente.Infra;

namespace RestauranteConcorrente.Atores;

public sealed class Gerente : IDisposable
{
    private const string Nome = "Gerente";

    private readonly CancellationTokenSource _fechamento = new();

    // Atendentes e cozinheiros recebem este token: quando o prazo vence, todos veem
    // o pedido de fechamento ao mesmo tempo, sem precisar de flags compartilhadas.
    public CancellationToken Token => _fechamento.Token;

    public void Abrir(TimeSpan? prazo)
    {
        if (prazo is not { } p)
        {
            return;
        }

        _fechamento.Token.Register(() =>
            Log.Escrever(Nome, $"prazo de {p.TotalSeconds:0} s encerrado — fechando o restaurante"));

        // CancelAfter agenda o Cancel em um timer; ninguém precisa ficar esperando o relógio.
        _fechamento.CancelAfter(p);
    }

    public void Dispose() => _fechamento.Dispose();
}
