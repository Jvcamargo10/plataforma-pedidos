using Compartilhado;
using Microsoft.AspNetCore.SignalR;

namespace Notificacoes.Api;

/// <summary>Hub que o navegador assina para receber os eventos em tempo real.</summary>
public sealed class PedidosHub : Hub { }

/// <summary>Formato que o front-end espera (EventoPedido em tipos.ts).</summary>
public sealed record EventoParaTela(string Id, string PedidoId, string Tipo, string Servico, DateTime OcorridoEm, string Detalhe);

public static class Conversor
{
    public static EventoParaTela ParaTela(Mensagem m)
    {
        (string PedidoId, string Detalhe) dados;
        switch (m.Tipo)
        {
            case TiposEvento.PedidoCriado:
                var criado = m.Ler<PedidoCriado>();
                dados = (criado.PedidoId, $"Total {criado.Total.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)} publicado no RabbitMQ");
                break;
            case TiposEvento.PagamentoAprovado:
                var aprovado = m.Ler<PagamentoAprovado>();
                dados = (aprovado.PedidoId, $"Transação {aprovado.TransacaoId} autorizada");
                break;
            case TiposEvento.PagamentoRecusado:
                var recusado = m.Ler<PagamentoRecusado>();
                dados = (recusado.PedidoId, recusado.Motivo);
                break;
            case TiposEvento.PedidoConfirmado:
                dados = (m.Ler<PedidoConfirmado>().PedidoId, "Pedido confirmado");
                break;
            case TiposEvento.PedidoCancelado:
                var cancelado = m.Ler<PedidoCancelado>();
                dados = (cancelado.PedidoId, cancelado.Motivo);
                break;
            default:
                throw new InvalidOperationException($"Tipo desconhecido: {m.Tipo}");
        }
        return new EventoParaTela(m.Id.ToString(), dados.PedidoId, m.Tipo, m.Origem, m.OcorridoEm, dados.Detalhe);
    }
}

public sealed class RepassadorManipulador(IHubContext<PedidosHub> hub) : IManipulador
{
    public IReadOnlyCollection<string> Tipos => TiposEvento.Todos;

    public Task ManipularAsync(Mensagem mensagem, CancellationToken ct) =>
        hub.Clients.All.SendAsync("evento", Conversor.ParaTela(mensagem), ct);
}
