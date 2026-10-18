using Compartilhado;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Pedidos.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<PedidosDb>(o => o.UseSqlite(builder.Configuration["Banco:Conexao"] ?? "Data Source=pedidos.db"));
builder.Services.AddMensageria(builder.Configuration, fila: "pedidos.resultados-pagamento");
builder.Services.AddScoped<IManipulador, ResultadoPagamentoManipulador>();
if (builder.Configuration.GetValue("Outbox:Habilitado", true))
    builder.Services.AddHostedService<DespachanteOutbox>();
builder.Services.AddTelemetria("pedidos");
builder.Services.AddProblemDetails();
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins(builder.Configuration.GetSection("Cors:Origens").Get<string[]>() ?? ["http://localhost:3000"])
     .AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
app.UseCors();

using (var escopo = app.Services.CreateScope())
    escopo.ServiceProvider.GetRequiredService<PedidosDb>().Database.EnsureCreated();

var api = app.MapGroup("/api");

api.MapGet("/saude", () => Results.Ok(new { status = "ok", servico = "pedidos" }));

api.MapGet("/pedidos", async (PedidosDb db, CancellationToken ct) =>
    (await db.Pedidos.AsNoTracking().ToListAsync(ct)).OrderByDescending(p => p.CriadoEm).Take(50));

api.MapGet("/pedidos/{id}", async Task<Results<Ok<Pedido>, NotFound>> (string id, PedidosDb db, CancellationToken ct) =>
    await db.Pedidos.FindAsync([id], ct) is { } p ? TypedResults.Ok(p) : TypedResults.NotFound());

api.MapPost("/pedidos", async Task<Results<Created<Pedido>, ProblemHttpResult>> (NovoPedido novo, PedidosDb db, CancellationToken ct) =>
{
    var erros = Validacao.Validar(novo);
    if (erros.Count > 0) return TypedResults.Problem(string.Join(" ", erros), statusCode: 400);

    var itens = novo.Itens
        .Select(i => (Produto: Catalogo.Produtos[i.ProdutoId], i.Quantidade))
        .Select(x => new ItemPedido(x.Produto.Id, x.Produto.Nome, x.Quantidade, x.Produto.Preco))
        .ToList();
    var pedido = new Pedido
    {
        Id = $"PED-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
        Cliente = novo.Cliente.Trim(),
        Itens = itens,
        Total = itens.Sum(i => i.PrecoUnitario * i.Quantidade),
    };
    var digitos = new string(novo.Cartao.Where(char.IsDigit).ToArray());

    db.Pedidos.Add(pedido);
    db.Enfileirar(new PedidoCriado(pedido.Id, pedido.Cliente, pedido.Total, digitos[^4..]));
    await db.SaveChangesAsync(ct);
    return TypedResults.Created($"/api/pedidos/{pedido.Id}", pedido);
});

app.Run();

public partial class Program { }
