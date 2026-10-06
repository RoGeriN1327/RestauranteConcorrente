using System.Globalization;
using RestauranteConcorrente.Infra;
using RestauranteConcorrente.Modelos;

namespace RestauranteConcorrente;

public static class Relatorio
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    // Retorna true se as três verificações deram OK.
    public static bool Imprimir(Configuracao config, Resultado r)
    {
        var fechamento = config.Fechamento is { } f ? $"{f.TotalSeconds:0} s" : "sem";
        var vendas = string.Join(" | ", Cardapio.Todos.Select(p =>
            $"{p.Nome} {r.Vendas.GetValueOrDefault(p.Nome)}"));
        var estoque = string.Join(" | ", r.EstoqueFinal.Select(kv => $"{kv.Key} {kv.Value}"));

        Log.Linha();
        Log.Linha("===== RELATÓRIO · Restaurante Concorrente =====");
        Log.Linha($"Modo: {Configuracao.Descrever(config.Modo)} | Cozinheiros: {config.Cozinheiros} | " +
                  $"Fornos: {Cozinha.TotalFornos} | Fechamento: {fechamento}");
        Log.Linha();
        Log.Linha($"Pedidos recebidos ............ {r.Recebidos}");
        Log.Linha($"Entregues .................... {r.Entregues.Count}");
        Log.Linha($"Recusados (sem ingrediente) .. {r.Recusados}");
        Log.Linha($"Não atendidos (fechamento) ... {r.NaoAtendidos}");
        Log.Linha();
        Log.Linha($"Vendas: {vendas}");
        Log.Linha($"Faturamento esperado ......... {Dinheiro(r.FaturamentoEsperado)}");
        Log.Linha($"Faturamento registrado ....... {Dinheiro(r.FaturamentoRegistrado)}");
        Log.Linha();
        Log.Linha($"Estoque final: {estoque}");
        Log.Linha();

        var contagemOk = r.Recebidos == r.Entregues.Count + r.Recusados + r.NaoAtendidos;
        var faturamentoOk = r.FaturamentoRegistrado == r.FaturamentoEsperado;
        var estoqueOk = r.EstoqueFinal.Values.All(v => v >= 0);

        Verificacao(contagemOk, "recebidos = entregues + recusados + não atendidos");
        Verificacao(faturamentoOk, "faturamento registrado = faturamento esperado",
            $"diferença de {Dinheiro(r.FaturamentoEsperado - r.FaturamentoRegistrado)} — vendas perdidas na race condition");
        Verificacao(estoqueOk, "nenhum ingrediente com estoque negativo",
            "negativos: " + string.Join(", ", r.EstoqueFinal.Where(kv => kv.Value < 0).Select(kv => $"{kv.Key} {kv.Value}")));
        Log.Linha();
        Log.Linha($"Tempo total: {r.Tempo.TotalSeconds.ToString("0.0", PtBr)} s");

        return contagemOk && faturamentoOk && estoqueOk;
    }

    public static string Dinheiro(decimal valor) => $"R$ {valor.ToString("N2", PtBr)}";

    private static void Verificacao(bool ok, string descricao, string? detalheErro = null)
    {
        Log.Linha(ok
            ? $"[OK] {descricao}"
            : $"[ERRO] {descricao}" + (detalheErro is null ? "" : $" ({detalheErro})"));
    }
}
