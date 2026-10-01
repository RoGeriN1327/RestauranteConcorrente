namespace RestauranteConcorrente.Modelos;

public static class Cardapio
{
    public static readonly Prato Pizza = new(
        nome: "Pizza",
        preco: 45.00m,
        preparoMs: 800,
        usaForno: true,
        usaTabuaFaca: false,
        ingredientes: new[] { "massa", "queijo", "tomate" });

    public static readonly Prato Lasanha = new(
        nome: "Lasanha",
        preco: 38.00m,
        preparoMs: 1000,
        usaForno: true,
        usaTabuaFaca: false,
        ingredientes: new[] { "massa", "queijo", "carne" });

    public static readonly Prato Salada = new(
        nome: "Salada",
        preco: 22.00m,
        preparoMs: 400,
        usaForno: false,
        usaTabuaFaca: true,
        ingredientes: new[] { "alface", "tomate" });

    public static readonly Prato Hamburguer = new(
        nome: "Hamburguer",
        preco: 30.00m,
        preparoMs: 500,
        usaForno: false,
        usaTabuaFaca: true,
        ingredientes: new[] { "pao", "carne", "queijo" });

    public static readonly IReadOnlyList<Prato> Todos = new[]
    {
        Pizza, Lasanha, Salada, Hamburguer
    };

    public static Prato Aleatorio(Random rng) => Todos[rng.Next(Todos.Count)];
}
