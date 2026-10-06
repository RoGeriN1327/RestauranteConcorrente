using System.Diagnostics;

namespace RestauranteConcorrente.Infra;

public static class Log
{
    private static readonly object _trava = new();
    private static Stopwatch _relogio = Stopwatch.StartNew();
    private static long _ultimoEventoMs;

    // Desliga o log de eventos (usado na comparação 1 x N, onde só importa o tempo).
    public static bool Silencioso { get; set; }

    public static TimeSpan Decorrido => _relogio.Elapsed;

    public static TimeSpan DesdeUltimoEvento =>
        TimeSpan.FromMilliseconds(_relogio.ElapsedMilliseconds - Interlocked.Read(ref _ultimoEventoMs));

    public static void Reiniciar()
    {
        _relogio = Stopwatch.StartNew();
        Interlocked.Exchange(ref _ultimoEventoMs, 0);
    }

    public static void Escrever(string ator, string mensagem)
    {
        var t = _relogio.Elapsed;
        Interlocked.Exchange(ref _ultimoEventoMs, (long)t.TotalMilliseconds);
        if (Silencioso)
        {
            return;
        }

        var linha = $"[{t.Minutes:D2}:{t.Seconds:D2}.{t.Milliseconds:D3}] {ator} · {mensagem}";
        lock (_trava)
        {
            Console.WriteLine(linha);
        }
    }

    // Texto fora do formato de evento (menu, relatório, avisos), com a mesma trava do log.
    public static void Linha(string texto = "")
    {
        lock (_trava)
        {
            Console.WriteLine(texto);
        }
    }
}
