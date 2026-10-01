namespace RestauranteConcorrente.Infra;

public sealed class Cozinha
{
    public const int TotalFornos = 2;

    private readonly SemaphoreSlim _fornos = new(TotalFornos, TotalFornos);
    private int _fornosOcupados;

    // Objetos de lock separados para cada utensílio. O Cozinheiro deve pegar sempre
    // na ordem Tabua → Faca (requisito 5): inverter essa ordem causa o deadlock que
    // vamos demonstrar na Entrega 3.
    public object Tabua { get; } = new();
    public object Faca { get; } = new();

    public FornoEmUso UsarForno()
    {
        _fornos.Wait();
        var ocupadosAgora = Interlocked.Increment(ref _fornosOcupados);
        return new FornoEmUso(this, ocupadosAgora);
    }

    private void LiberarForno()
    {
        Interlocked.Decrement(ref _fornosOcupados);
        _fornos.Release();
    }

    public readonly struct FornoEmUso : IDisposable
    {
        private readonly Cozinha _cozinha;
        public int Ocupacao { get; }

        internal FornoEmUso(Cozinha cozinha, int ocupacao)
        {
            _cozinha = cozinha;
            Ocupacao = ocupacao;
        }

        public void Dispose() => _cozinha.LiberarForno();
    }
}
