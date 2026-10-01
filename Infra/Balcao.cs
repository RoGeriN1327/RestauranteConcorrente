using System.Collections.Concurrent;
using RestauranteConcorrente.Modelos;

namespace RestauranteConcorrente.Infra;

public sealed class Balcao : IDisposable
{
    private readonly ConcurrentQueue<PedidoPronto> _fila = new();
    private readonly AutoResetEvent _sino = new(initialState: false);

    public void Depositar(PedidoPronto pronto)
    {
        _fila.Enqueue(pronto);
        _sino.Set();
    }

    public void ChamarGarcom() => _sino.Set();

    public void AguardarSino() => _sino.WaitOne();

    // AutoResetEvent agrupa vários Set em um único Reset — por isso, cada vez que o garçom
    // acorda ele precisa drenar a fila inteira; caso contrário, pratos ficariam esquecidos
    // (pegadinha descrita na seção 5 do PDF).
    public IEnumerable<PedidoPronto> DrenarTudo()
    {
        while (_fila.TryDequeue(out var pronto))
        {
            yield return pronto;
        }
    }

    public void Dispose() => _sino.Dispose();
}
