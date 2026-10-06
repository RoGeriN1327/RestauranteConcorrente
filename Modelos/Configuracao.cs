namespace RestauranteConcorrente.Modelos;

public enum Modo
{
    Seguro,
    BugRaceCaixa,
    BugDeadlock,
    BugEstoqueNegativo
}

// Fechamento nulo = sem gerente fechando por prazo (usado na comparação 1 x N).
public sealed record Configuracao(
    Modo Modo,
    int Cozinheiros,
    TimeSpan? Fechamento,
    bool LogDetalhado = true)
{
    public static string Descrever(Modo modo) => modo switch
    {
        Modo.Seguro => "SEGURO",
        Modo.BugRaceCaixa => "INSEGURO · race condition no caixa",
        Modo.BugDeadlock => "INSEGURO · deadlock nos utensílios",
        Modo.BugEstoqueNegativo => "INSEGURO · estoque negativo",
        _ => modo.ToString()
    };
}
