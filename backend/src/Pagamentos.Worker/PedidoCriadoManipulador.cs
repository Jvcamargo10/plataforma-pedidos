using Compartilhado;
using Microsoft.Extensions.Options;

namespace Pagamentos.Worker;

public sealed class OpcoesPagamentos
{
    /// <summary>Atraso artificial, para dar para ver o fluxo acontecendo na tela.</summary>
    public int LatenciaMs { get; set; } = 800;
}

public sealed class PedidoCriadoManipulador(IBarramento barramento, IOptions<OpcoesPagamentos> opcoes, ILogger<PedidoCriadoManipulador> log) : IManipulador
{
    public IReadOnlyCollection<string> Tipos { get; } = [TiposEvento.PedidoCriado];

    public async Task ManipularAsync(Mensagem mensagem, CancellationToken ct)
    {
        var pedido = mensagem.Ler<PedidoCriado>();
        if (opcoes.Value.LatenciaMs > 0) await Task.Delay(opcoes.Value.LatenciaMs, ct);

        var (aprovado, motivo) = RegrasPagamento.Avaliar(pedido.Total, pedido.FinalCartao);
        // Id determinístico: se este pedido for processado de novo, Pedidos reconhece a resposta repetida.
        var id = Mensagem.IdDeterministico($"pagamento:{pedido.PedidoId}");
        var resposta = aprovado
            ? Mensagem.Criar(new PagamentoAprovado(pedido.PedidoId, $"TX-{id.ToString("N")[..10].ToUpperInvariant()}"), "Pagamentos", id)
            : Mensagem.Criar(new PagamentoRecusado(pedido.PedidoId, motivo!), "Pagamentos", id);

        await barramento.PublicarAsync(resposta, ct);
        log.LogInformation("Pedido {Pedido}: {Resultado}", pedido.PedidoId, resposta.Tipo);
    }
}
