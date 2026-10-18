using Compartilhado;

namespace Notificacoes.Api;

// Classe nomeada (sem top-level statements) para não colidir com o Program dos outros serviços nos testes.
public static class Programa
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddSignalR();
        builder.Services.AddMensageria(builder.Configuration, fila: "notificacoes.todos-eventos");
        builder.Services.AddScoped<IManipulador, RepassadorManipulador>();
        builder.Services.AddTelemetria("notificacoes");
        builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
            p.WithOrigins(builder.Configuration.GetSection("Cors:Origens").Get<string[]>() ?? ["http://localhost:3000"])
             .AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

        var app = builder.Build();
        app.UseCors();
        app.MapGet("/api/saude", () => Results.Ok(new { status = "ok", servico = "notificacoes" }));
        app.MapHub<PedidosHub>("/hubs/pedidos");
        app.Run();
    }
}
