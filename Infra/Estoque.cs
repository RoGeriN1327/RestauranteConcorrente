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

    public Estoque()
    {
        _niveis = new ConcurrentDictionary<string, int>(
            _ingredientesConhecidos.Select(nome =>
                new KeyValuePair<string, int>(nome, QuantidadeInicial)));
    }

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

    public IReadOnlyDictionary<string, int> Snapshot()
    {
        lock (_trava)
        {
            return _niveis.ToDictionary(kv => kv.Key, kv => kv.Value);
        }
    }
}
