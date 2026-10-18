using Compartilhado;

namespace Pedidos.Api;

/// <summary>Recebe o resultado do pagamento e confirma ou cancela o pedido.</summary>
public sealed class ResultadoPagamentoManipulador(PedidosDb db, ILogger<ResultadoPagamentoManipulador> log) : IManipulador
{
    public IReadOnlyCollection<string> Tipos { get; } = [TiposEvento.PagamentoAprovado, TiposEvento.PagamentoRecusado];

    public async Task ManipularAsync(Mensagem mensagem, CancellationToken ct)
    {
        if (await db.Processadas.FindAsync([mensagem.Id], ct) is not null)
        {
            log.LogInformation("Mensagem {Id} repetida; ignorada.", mensagem.Id);
            return;
        }

        var (pedidoId, aprovado, motivo) = mensagem.Tipo == TiposEvento.PagamentoAprovado
            ? (mensagem.Ler<PagamentoAprovado>().PedidoId, true, (string?)null)
            : (mensagem.Ler<PagamentoRecusado>().PedidoId, false, mensagem.Ler<PagamentoRecusado>().Motivo);

        var pedido = await db.Pedidos.FindAsync([pedidoId], ct);
        if (pedido is not null && pedido.Status == StatusPedido.AguardandoPagamento)
        {
            if (aprovado)
            {
                pedido.Confirmar();
                db.Enfileirar(new PedidoConfirmado(pedido.Id));
            }
            else
            {
                pedido.Cancelar(motivo!);
                db.Enfileirar(new PedidoCancelado(pedido.Id, motivo!));
            }
        }

        db.Processadas.Add(new MensagemProcessada { Id = mensagem.Id });
        await db.SaveChangesAsync(ct);
    }
}
