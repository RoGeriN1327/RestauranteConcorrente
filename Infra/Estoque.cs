using System.Collections.Concurrent;

namespace RestauranteConcorrente.Infra;

public sealed class Estoque
{
    public const int QuantidadeInicial = 40;

    private static readonly string[] _ingredientesConhecidos =
    {
        "massa", "queijo", "tomate", "carne", "alface", "pao"
    };

    private readonly ConcurrentDictionary<string, int> _niveis;
    private readonly object _trava = new();

    public Estoque(bool inseguro = false)
    {
        Inseguro = inseguro;
        _niveis = new ConcurrentDictionary<string, int>(
            _ingredientesConhecidos.Select(nome =>
                new KeyValuePair<string, int>(nome, QuantidadeInicial)));
    }

    // Modo inseguro: o cozinheiro usa Verificar no início do preparo e Descontar no fim,
    // em vez de TentarReservar.
    public bool Inseguro { get; }

    // ConcurrentDictionary garante acesso seguro a cada entrada, mas verificar e descontar
    // são dois passos: sem o lock externo, dois cozinheiros podem ver "há 1 unidade" ao mesmo
    // tempo e descontar duas vezes (Aula 6 — "Coleção segura ≠ Lógica segura").
    public bool TentarReservar(IReadOnlyList<string> ingredientes)
    {
        lock (_trava)
        {
            foreach (var nome in ingredientes)
            {
                if (_niveis[nome] < 1)
                {
                    return false;
                }
            }

            foreach (var nome in ingredientes)
            {
                _niveis[nome]--;
            }
            return true;
        }
    }

    // BUG proposital (passo 1): só olha o estoque, sem reservar nada.
    public bool Verificar(IReadOnlyList<string> ingredientes) =>
        ingredientes.All(nome => _niveis[nome] >= 1);

    // BUG proposital (passo 2): cada desconto isolado é atômico (AddOrUpdate), mas entre
    // Verificar e Descontar passa o tempo inteiro do preparo. Vários cozinheiros verificam
    // a mesma última unidade de queijo, todos cozinham e todos descontam: estoque negativo.
    public void Descontar(IReadOnlyList<string> ingredientes)
    {
        foreach (var nome in ingredientes)
        {
            _niveis.AddOrUpdate(nome, 0, (_, atual) => atual - 1);
        }
    }

    public IReadOnlyDictionary<string, int> Snapshot()
    {
        lock (_trava)
        {
            return _niveis.ToDictionary(kv => kv.Key, kv => kv.Value);
        }
    }
}
