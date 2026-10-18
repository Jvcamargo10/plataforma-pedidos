namespace Pagamentos.Worker;

/// <summary>Gateway de pagamento simulado. As mesmas regras aparecem no front-end (regras-pagamento.ts) para a demo.</summary>
public static class RegrasPagamento
{
    public const decimal LimitePorPedido = 5000m;

    public static (bool Aprovado, string? Motivo) Avaliar(decimal total, string finalCartao) =>
        finalCartao == "0002" ? (false, "Recusado pelo emissor")
        : total > LimitePorPedido ? (false, "Valor acima do limite por pedido")
        : (true, null);
}
