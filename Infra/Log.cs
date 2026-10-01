using System.Diagnostics;

namespace RestauranteConcorrente.Infra;

public static class Log
{
    private static readonly Stopwatch _relogio = Stopwatch.StartNew();
    private static readonly object _trava = new();

    public static void Escrever(string ator, string mensagem)
    {
        var t = _relogio.Elapsed;
        var linha = $"[{t.Minutes:D2}:{t.Seconds:D2}.{t.Milliseconds:D3}] {ator} · {mensagem}";
        lock (_trava)
        {
            Console.WriteLine(linha);
        }
    }
}
