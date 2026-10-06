using System.Collections.Concurrent;
using RestauranteConcorrente.Modelos;

namespace RestauranteConcorrente.Infra;

public sealed class Caixa
{
    // Janela artificial entre ler e gravar o faturamento no modo inseguro (Aula 2).
    // Sem ela a race existe, mas é rara demais para aparecer em toda execução.
    private const int JanelaInseguraMs = 50;

    private readonly bool _inseguro;
    private readonly object _trava = new();
    private readonly ConcurrentDictionary<string, int> _vendas = new();
    private decimal _faturamento;

    public Caixa(bool inseguro)
    {
        _inseguro = inseguro;
    }

    public void Registrar(Prato prato)
    {
        // AddOrUpdate é atômico: a leitura do valor atual e a gravação do novo acontecem
        // como uma única operação, então dois cozinheiros não perdem uma venda entre si.
        _vendas.AddOrUpdate(prato.Nome, 1, (_, atual) => atual + 1);

        if (_inseguro)
        {
            // BUG proposital: ler → somar → gravar sem lock. Dois cozinheiros leem o mesmo
            // valor, cada um soma o seu prato e o último a gravar apaga a venda do outro.
            var lido = _faturamento;
            Thread.Sleep(JanelaInseguraMs);
            _faturamento = lido + prato.Preco;
            return;
        }

        // Conserto: o lock torna ler-somar-gravar uma seção crítica (um cozinheiro por vez).
        lock (_trava)
        {
            _faturamento += prato.Preco;
        }
    }

    public decimal Faturamento
    {
        get
        {
            lock (_trava)
            {
                return _faturamento;
            }
        }
    }

    public IReadOnlyDictionary<string, int> Vendas => _vendas.ToDictionary(kv => kv.Key, kv => kv.Value);
}
