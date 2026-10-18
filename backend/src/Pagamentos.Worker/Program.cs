using Compartilhado;
using Pagamentos.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<OpcoesPagamentos>(builder.Configuration.GetSection("Pagamentos"));
builder.Services.AddMensageria(builder.Configuration, fila: "pagamentos.pedidos-criados");
builder.Services.AddScoped<IManipulador, PedidoCriadoManipulador>();
builder.Services.AddTelemetria("pagamentos");

builder.Build().Run();
