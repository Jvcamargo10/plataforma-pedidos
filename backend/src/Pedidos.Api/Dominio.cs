namespace Pedidos.Api;

public sealed record Produto(string Id, string Nome, decimal Preco);

/// <summary>Catálogo fixo (fictício), o mesmo exibido pelo front-end.</summary>
public static class Catalogo
{
    public static readonly IReadOnlyDictionary<string, Produto> Produtos = new[]
    {
        new Produto("notebook-14", "Notebook 14\"", 4299.90m),
        new Produto("monitor-27", "Monitor 27\"", 1599.00m),
        new Produto("teclado-mec", "Teclado mecânico", 389.90m),
        new Produto("mouse-sem-fio", "Mouse sem fio", 149.90m),
        new Produto("headset", "Headset com microfone", 459.00m),
        new Produto("webcam-hd", "Webcam Full HD", 279.90m),
    }.ToDictionary(p => p.Id);
}

public static class StatusPedido
{
    public const string AguardandoPagamento = nameof(AguardandoPagamento);
    public const string Confirmado = nameof(Confirmado);
    public const string Cancelado = nameof(Cancelado);
}

public sealed record ItemPedido(string ProdutoId, string Nome, int Quantidade, decimal PrecoUnitario);

public sealed class Pedido
{
    public string Id { get; set; } = "";
    public string Cliente { get; set; } = "";
    public List<ItemPedido> Itens { get; set; } = [];
    public decimal Total { get; set; }
    public string Status { get; set; } = StatusPedido.AguardandoPagamento;
    public string? Motivo { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public void Confirmar()
    {
        if (Status != StatusPedido.AguardandoPagamento) return;
        Status = StatusPedido.Confirmado;
    }

    public void Cancelar(string motivo)
    {
        if (Status != StatusPedido.AguardandoPagamento) return;
        Status = StatusPedido.Cancelado;
        Motivo = motivo;
    }
}

/// <summary>Mensagem gravada na mesma transação do pedido e publicada depois (padrão Outbox).</summary>
public sealed class MensagemOutbox
{
    public Guid Id { get; set; }
    public string Tipo { get; set; } = "";
    public string Conteudo { get; set; } = "";
    public string? TraceParent { get; set; }
    public DateTime OcorridoEm { get; set; }
    public DateTime? PublicadaEm { get; set; }
    public int Tentativas { get; set; }
}

/// <summary>Mensagens já consumidas, para não processar a mesma duas vezes (consumidor idempotente).</summary>
public sealed class MensagemProcessada
{
    public Guid Id { get; set; }
    public DateTime ProcessadaEm { get; set; } = DateTime.UtcNow;
}

public sealed record NovoItem(string ProdutoId, int Quantidade);

public sealed record NovoPedido(string Cliente, string Cartao, List<NovoItem> Itens);

public static class Validacao
{
    public const int QuantidadeMaxima = 10;

    public static List<string> Validar(NovoPedido novo)
    {
        var erros = new List<string>();
        if (string.IsNullOrWhiteSpace(novo.Cliente)) erros.Add("Informe o nome do cliente.");
        if (novo.Itens is not { Count: > 0 }) erros.Add("O carrinho está vazio.");
        foreach (var item in novo.Itens ?? [])
        {
            if (!Catalogo.Produtos.ContainsKey(item.ProdutoId)) erros.Add($"Produto {item.ProdutoId} não existe.");
            if (item.Quantidade is < 1 or > QuantidadeMaxima) erros.Add($"Quantidade inválida para {item.ProdutoId}.");
        }
        if (!CartaoValido(novo.Cartao)) erros.Add("Número de cartão inválido.");
        return erros;
    }

    /// <summary>Luhn. O número completo fica só aqui; para o barramento vão os 4 últimos dígitos.</summary>
    public static bool CartaoValido(string? cartao)
    {
        var d = new string((cartao ?? "").Where(char.IsDigit).ToArray());
        if (d.Length is < 13 or > 19) return false;
        var soma = 0;
        for (var i = 0; i < d.Length; i++)
        {
            var n = d[d.Length - 1 - i] - '0';
            if (i % 2 == 1)
            {
                n *= 2;
                if (n > 9) n -= 9;
            }
            soma += n;
        }
        return soma % 10 == 0;
    }
}
