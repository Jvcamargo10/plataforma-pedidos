using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Compartilhado;

public static class Telemetria
{
    public static readonly ActivitySource Fonte = new("PlataformaPedidos.Mensageria");

    public static string? TraceParentAtual() => Activity.Current?.Id;

    /// <summary>Continua o trace iniciado em outro serviço, usando o traceparent que veio na mensagem.</summary>
    public static Activity? IniciarConsumo(Mensagem mensagem, string servico)
    {
        ActivityContext.TryParse(mensagem.TraceParent, null, out var pai);
        var atividade = Fonte.StartActivity($"consumir {mensagem.Tipo}", ActivityKind.Consumer, pai);
        atividade?.SetTag("messaging.system", "rabbitmq");
        atividade?.SetTag("messaging.message.id", mensagem.Id.ToString());
        atividade?.SetTag("app.servico", servico);
        return atividade;
    }

    /// <summary>
    /// Tracing com OpenTelemetry. O exportador OTLP só é ligado quando OTEL_EXPORTER_OTLP_ENDPOINT
    /// está definido (no docker compose, aponta para o Jaeger).
    /// </summary>
    public static IServiceCollection AddTelemetria(this IServiceCollection services, string nomeServico)
    {
        services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(nomeServico))
            .WithTracing(t =>
            {
                t.AddSource(Fonte.Name).AddAspNetCoreInstrumentation().AddHttpClientInstrumentation();
                if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT")))
                    t.AddOtlpExporter();
            });
        return services;
    }
}
