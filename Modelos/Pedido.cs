namespace RestauranteConcorrente.Modelos;

public sealed class Pedido
{
    public int Numero { get; }
    public Prato Prato { get; }
    public string Atendente { get; }
    public DateTime CriadoEm { get; }

    public Pedido(int numero, Prato prato, string atendente)
    {
        Numero = numero;
        Prato = prato;
        Atendente = atendente;
        CriadoEm = DateTime.Now;
    }

    public override string ToString() => $"{Prato.Nome} #{Numero}";
}
