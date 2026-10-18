namespace Compartilhado;

/// <summary>Nomes dos eventos. Também são as routing keys no exchange "pedidos".</summary>
public static class TiposEvento
{
    public const string PedidoCriado = nameof(PedidoCriado);
    public const string PagamentoAprovado = nameof(PagamentoAprovado);
    public const string PagamentoRecusado = nameof(PagamentoRecusado);
    public const string PedidoConfirmado = nameof(PedidoConfirmado);
    public const string PedidoCancelado = nameof(PedidoCancelado);

    public static readonly string[] Todos = [PedidoCriado, PagamentoAprovado, PagamentoRecusado, PedidoConfirmado, PedidoCancelado];
}

/// <summary>O cartão completo nunca trafega no barramento: Pagamentos recebe só os 4 últimos dígitos.</summary>
public sealed record PedidoCriado(string PedidoId, string Cliente, decimal Total, string FinalCartao);

public sealed record PagamentoAprovado(string PedidoId, string TransacaoId);

public sealed record PagamentoRecusado(string PedidoId, string Motivo);

public sealed record PedidoConfirmado(string PedidoId);

public sealed record PedidoCancelado(string PedidoId, string Motivo);
