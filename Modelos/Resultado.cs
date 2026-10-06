namespace RestauranteConcorrente.Modelos;

public sealed record Resultado(
    int Recebidos,
    IReadOnlyList<PedidoPronto> Entregues,
    int Recusados,
    int NaoAtendidos,
    decimal FaturamentoRegistrado,
    IReadOnlyDictionary<string, int> Vendas,
    IReadOnlyDictionary<string, int> EstoqueFinal,
    TimeSpan Tempo)
{
    // Calculado no fim, sem concorrência: soma o preço de cada prato que o garçom entregou.
    public decimal FaturamentoEsperado => Entregues.Sum(p => p.Pedido.Prato.Preco);
}
