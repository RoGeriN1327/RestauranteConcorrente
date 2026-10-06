using System.Globalization;
using System.Text;
using RestauranteConcorrente;
using RestauranteConcorrente.Infra;
using RestauranteConcorrente.Modelos;

const int cozinheirosPadrao = 4;
const int fechamentoPadraoSegundos = 15;

Console.OutputEncoding = Encoding.UTF8;

// Uso direto, sem menu: dotnet run -- <opção> [cozinheiros] [segundos até o fechamento]
if (args.Length > 0)
{
    var cozinheiros = args.Length > 1 && int.TryParse(args[1], out var c) && c > 0 ? c : cozinheirosPadrao;
    var segundos = args.Length > 2 && int.TryParse(args[2], out var s) && s > 0 ? s : fechamentoPadraoSegundos;
    if (!await ExecutarOpcaoAsync(args[0], cozinheiros, segundos))
    {
        Console.WriteLine($"Opção inválida: {args[0]}. Use 1 a 5.");
        return 1;
    }
    return 0;
}

while (true)
{
    Console.WriteLine();
    Console.WriteLine("===== Restaurante Concorrente =====");
    Console.WriteLine("1 - Modo seguro");
    Console.WriteLine("2 - Bug: race condition no caixa");
    Console.WriteLine("3 - Bug: deadlock nos utensílios");
    Console.WriteLine("4 - Bug: estoque negativo");
    Console.WriteLine("5 - Comparação 1 x N cozinheiros");
    Console.WriteLine("0 - Sair");
    Console.Write("Opção: ");

    var opcao = Console.ReadLine()?.Trim().TrimStart('﻿');
    if (opcao is null or "0")
    {
        return 0;
    }

    if (opcao is not ("1" or "2" or "3" or "4" or "5"))
    {
        Console.WriteLine("Opção inválida.");
        continue;
    }

    var cozinheiros = PerguntarNumero("Quantos cozinheiros", cozinheirosPadrao);
    var segundos = opcao == "5" ? 0 : PerguntarNumero("Fechar o restaurante após quantos segundos", fechamentoPadraoSegundos);
    await ExecutarOpcaoAsync(opcao, cozinheiros, segundos);
}

static async Task<bool> ExecutarOpcaoAsync(string opcao, int cozinheiros, int segundos)
{
    var fechamento = TimeSpan.FromSeconds(segundos);
    switch (opcao)
    {
        case "1": await RodarAsync(new Configuracao(Modo.Seguro, cozinheiros, fechamento)); return true;
        case "2": await RodarAsync(new Configuracao(Modo.BugRaceCaixa, cozinheiros, fechamento)); return true;
        case "3": await RodarAsync(new Configuracao(Modo.BugDeadlock, cozinheiros, fechamento)); return true;
        case "4": await RodarAsync(new Configuracao(Modo.BugEstoqueNegativo, cozinheiros, fechamento)); return true;
        case "5": await CompararAsync(cozinheiros); return true;
        default: return false;
    }
}

static async Task RodarAsync(Configuracao config)
{
    Console.WriteLine();
    Console.WriteLine($">>> {Configuracao.Descrever(config.Modo)}");
    if (config.Modo == Modo.BugDeadlock)
    {
        Console.WriteLine(">>> Salada pega tábua → faca, Hambúrguer pega faca → tábua. Se o log parar, é deadlock (Ctrl+C).");
    }
    Console.WriteLine();

    var resultado = await new Restaurante(config).AbrirAsync();
    Relatorio.Imprimir(config, resultado);
}

// Mesmos 60 pedidos (seeds fixas), modo seguro, sem fechamento, só muda o número de cozinheiros.
static async Task CompararAsync(int n)
{
    var pt = CultureInfo.GetCultureInfo("pt-BR");
    var quantidades = n == 1 ? new[] { 1 } : new[] { 1, n };

    Console.WriteLine();
    Console.WriteLine($"Comparação · {Restaurante.TotalPedidos} pedidos, sem fechamento (log desligado)");

    var tempos = new List<(int Cozinheiros, TimeSpan Tempo)>();
    foreach (var q in quantidades)
    {
        Console.Write($"  rodando com {q} cozinheiro(s)... ");
        var config = new Configuracao(Modo.Seguro, q, Fechamento: null, LogDetalhado: false);
        var r = await new Restaurante(config).AbrirAsync();
        tempos.Add((q, r.Tempo));
        Console.WriteLine($"ok ({r.Entregues.Count} entregues, {r.Recusados} recusados)");
    }

    Console.WriteLine();
    foreach (var (q, tempo) in tempos)
    {
        var rotulo = q == 1 ? "1 cozinheiro " : $"{q} cozinheiros";
        Console.WriteLine($"{rotulo,-15} ...... {tempo.TotalSeconds.ToString("0.0", pt)} s");
    }

    if (tempos.Count == 2)
    {
        var ganho = tempos[0].Tempo.TotalSeconds / tempos[1].Tempo.TotalSeconds;
        Console.WriteLine($"Speedup ............... {ganho.ToString("0.00", pt)}x");
    }
}

static int PerguntarNumero(string pergunta, int padrao)
{
    Console.Write($"{pergunta}? [{padrao}]: ");
    var texto = Console.ReadLine();
    return int.TryParse(texto, out var valor) && valor > 0 ? valor : padrao;
}
