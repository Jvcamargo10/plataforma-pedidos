using Compartilhado;
using Microsoft.EntityFrameworkCore;

namespace Pedidos.Api;

/// <summary>
/// Publica no RabbitMQ o que foi gravado na outbox. Se o broker cair, o pedido continua salvo e a
/// mensagem sai quando ele voltar: nunca existe pedido gravado sem evento, nem evento sem pedido.
/// </summary>
public sealed class DespachanteOutbox(IServiceScopeFactory escopos, ILogger<DespachanteOutbox> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var escopo = escopos.CreateScope();
                await DespacharPendentesAsync(
                    escopo.ServiceProvider.GetRequiredService<PedidosDb>(),
                    escopo.ServiceProvider.GetRequiredService<IBarramento>(),
                    ct);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                log.LogWarning("Falha ao despachar a outbox: {Erro}", e.Message);
            }
            await Task.Delay(TimeSpan.FromMilliseconds(500), ct).ContinueWith(_ => { }, CancellationToken.None);
        }
    }

    public static async Task<int> DespacharPendentesAsync(PedidosDb db, IBarramento barramento, CancellationToken ct, int lote = 50)
    {
        var pendentes = await db.Outbox
            .Where(m => m.PublicadaEm == null && m.Tentativas < 10)
            .OrderBy(m => m.OcorridoEm)
            .Take(lote)
            .ToListAsync(ct);

        foreach (var m in pendentes)
        {
            try
            {
                await barramento.PublicarAsync(new Mensagem(m.Id, m.Tipo, "Pedidos", m.OcorridoEm, m.Conteudo, m.TraceParent), ct);
                m.PublicadaEm = DateTime.UtcNow;
            }
            catch (Exception) when (!ct.IsCancellationRequested)
            {
                m.Tentativas++;
                await db.SaveChangesAsync(ct);
                throw;
            }
        }
        await db.SaveChangesAsync(ct);
        return pendentes.Count;
    }
}
