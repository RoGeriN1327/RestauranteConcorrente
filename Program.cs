using RestauranteConcorrente;

const int quantidadeCozinheirosPadrao = 4;

var quantidadeCozinheiros = quantidadeCozinheirosPadrao;
if (args.Length > 0 && int.TryParse(args[0], out var parseado) && parseado > 0)
{
    quantidadeCozinheiros = parseado;
}

var restaurante = new Restaurante(quantidadeCozinheiros);
await restaurante.AbrirAsync();
