namespace RestauranteConcorrente.Modelos;

public sealed class Prato
{
    public string Nome { get; }
    public decimal Preco { get; }
    public int PreparoMs { get; }
    public bool UsaForno { get; }
    public bool UsaTabuaFaca { get; }
    public IReadOnlyList<string> Ingredientes { get; }

    public Prato(
        string nome,
        decimal preco,
        int preparoMs,
        bool usaForno,
        bool usaTabuaFaca,
        IReadOnlyList<string> ingredientes)
    {
        Nome = nome;
        Preco = preco;
        PreparoMs = preparoMs;
        UsaForno = usaForno;
        UsaTabuaFaca = usaTabuaFaca;
        Ingredientes = ingredientes;
    }
}
