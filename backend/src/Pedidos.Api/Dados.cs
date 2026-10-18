using System.Text.Json;
using Compartilhado;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Pedidos.Api;

public sealed class PedidosDb(DbContextOptions<PedidosDb> options) : DbContext(options)
{
    public DbSet<Pedido> Pedidos => Set<Pedido>();
    public DbSet<MensagemOutbox> Outbox => Set<MensagemOutbox>();
    public DbSet<MensagemProcessada> Processadas => Set<MensagemProcessada>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Pedido>(e =>
        {
            e.Property(p => p.Total).HasPrecision(18, 2);
            e.Property(p => p.Itens).HasConversion(
                v => JsonSerializer.Serialize(v, Mensagem.Json),
                v => JsonSerializer.Deserialize<List<ItemPedido>>(v, Mensagem.Json) ?? new List<ItemPedido>(),
                new ValueComparer<List<ItemPedido>>(
                    (a, b) => a!.SequenceEqual(b!),
                    v => v.Count,
                    v => v.ToList()));
            e.HasIndex(p => p.CriadoEm);
        });
        model.Entity<MensagemOutbox>().HasIndex(m => m.PublicadaEm);
    }

    /// <summary>Guarda o evento na outbox. Ele é gravado no mesmo SaveChanges do pedido.</summary>
    public void Enfileirar<T>(T evento, Guid? id = null) where T : notnull
    {
        var m = Mensagem.Criar(evento, "Pedidos", id);
        Outbox.Add(new MensagemOutbox
        {
            Id = m.Id, Tipo = m.Tipo, Conteudo = m.Conteudo, TraceParent = m.TraceParent, OcorridoEm = m.OcorridoEm,
        });
    }
}
