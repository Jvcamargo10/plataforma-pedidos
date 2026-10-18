using Compartilhado;

namespace Pagamentos.Worker;

// Classe nomeada (sem top-level statements) para não colidir com o Program dos outros serviços nos testes.
public static class Programa
{
    public static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.Services.Configure<OpcoesPagamentos>(builder.Configuration.GetSection("Pagamentos"));
        builder.Services.AddMensageria(builder.Configuration, fila: "pagamentos.pedidos-criados");
        builder.Services.AddScoped<IManipulador, PedidoCriadoManipulador>();
        builder.Services.AddTelemetria("pagamentos");

        builder.Build().Run();
    }
}
