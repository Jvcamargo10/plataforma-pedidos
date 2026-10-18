using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Compartilhado;

public sealed class OpcoesMensageria
{
    public const string Secao = "Mensageria";
    public const string Exchange = "pedidos";
    public const string ExchangeMortas = "pedidos.dlx";

    /// <summary>Sem RabbitMQ (false), o serviço usa o barramento em memória.</summary>
    public bool Habilitada { get; set; } = true;
    public string Host { get; set; } = "localhost";
    public int Porta { get; set; } = 5672;
    public string Usuario { get; set; } = "guest";
    public string Senha { get; set; } = "guest";
}

/// <summary>Abre (e reabre) a conexão com o RabbitMQ, com novas tentativas enquanto o broker sobe.</summary>
public sealed class ConexaoRabbitMq(IOptions<OpcoesMensageria> opcoes, ILogger<ConexaoRabbitMq> log) : IAsyncDisposable
{
    private readonly SemaphoreSlim _trava = new(1, 1);
    private IConnection? _conexao;

    public async Task<IChannel> NovoCanalAsync(CancellationToken ct)
    {
        await _trava.WaitAsync(ct);
        try
        {
            if (_conexao is not { IsOpen: true })
            {
                var o = opcoes.Value;
                var fabrica = new ConnectionFactory { HostName = o.Host, Port = o.Porta, UserName = o.Usuario, Password = o.Senha };
                for (var tentativa = 1; ; tentativa++)
                {
                    try
                    {
                        _conexao = await fabrica.CreateConnectionAsync(ct);
                        break;
                    }
                    catch (Exception e) when (tentativa < 15 && e is not OperationCanceledException)
                    {
                        log.LogWarning("RabbitMQ indisponível (tentativa {Tentativa}): {Erro}", tentativa, e.Message);
                        await Task.Delay(TimeSpan.FromSeconds(2), ct);
                    }
                }
            }

            var canal = await _conexao.CreateChannelAsync(cancellationToken: ct);
            await canal.ExchangeDeclareAsync(OpcoesMensageria.Exchange, ExchangeType.Topic, durable: true, autoDelete: false, cancellationToken: ct);
            await canal.ExchangeDeclareAsync(OpcoesMensageria.ExchangeMortas, ExchangeType.Fanout, durable: true, autoDelete: false, cancellationToken: ct);
            return canal;
        }
        finally
        {
            _trava.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_conexao is not null) await _conexao.DisposeAsync();
    }
}

public sealed class BarramentoRabbitMq(ConexaoRabbitMq conexao) : IBarramento, IAsyncDisposable
{
    private readonly SemaphoreSlim _trava = new(1, 1);
    private IChannel? _canal;

    public async Task PublicarAsync(Mensagem mensagem, CancellationToken ct = default)
    {
        using var atividade = Telemetria.Fonte.StartActivity($"publicar {mensagem.Tipo}", ActivityKind.Producer);
        var props = new BasicProperties
        {
            MessageId = mensagem.Id.ToString(),
            Type = mensagem.Tipo,
            ContentType = "application/json",
            Persistent = true,
        };
        var corpo = JsonSerializer.SerializeToUtf8Bytes(mensagem with { TraceParent = atividade?.Id ?? mensagem.TraceParent }, Mensagem.Json);

        await _trava.WaitAsync(ct);
        try
        {
            if (_canal is not { IsOpen: true }) _canal = await conexao.NovoCanalAsync(ct);
            await _canal.BasicPublishAsync(OpcoesMensageria.Exchange, mensagem.Tipo, mandatory: false, basicProperties: props, body: corpo, cancellationToken: ct);
        }
        finally
        {
            _trava.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_canal is not null) await _canal.DisposeAsync();
    }
}

/// <summary>
/// Consome a fila do serviço e entrega cada mensagem ao manipulador do tipo, num escopo de DI próprio.
/// Sucesso → ack. Falha → nack sem reenfileirar, e a mensagem vai para a fila de mortas (pedidos.dlx).
/// </summary>
public sealed class ConsumidorRabbitMq(
    string fila,
    ConexaoRabbitMq conexao,
    IServiceScopeFactory escopos,
    ILogger<ConsumidorRabbitMq> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        IReadOnlyCollection<string> tipos;
        using (var escopo = escopos.CreateScope())
            tipos = escopo.ServiceProvider.GetServices<IManipulador>().SelectMany(m => m.Tipos).Distinct().ToList();

        var canal = await conexao.NovoCanalAsync(ct);
        await canal.QueueDeclareAsync(fila, durable: true, exclusive: false, autoDelete: false,
            arguments: new Dictionary<string, object?> { ["x-dead-letter-exchange"] = OpcoesMensageria.ExchangeMortas },
            cancellationToken: ct);
        await canal.QueueDeclareAsync($"{fila}.mortas", durable: true, exclusive: false, autoDelete: false, cancellationToken: ct);
        await canal.QueueBindAsync($"{fila}.mortas", OpcoesMensageria.ExchangeMortas, "", cancellationToken: ct);
        foreach (var tipo in tipos)
            await canal.QueueBindAsync(fila, OpcoesMensageria.Exchange, tipo, cancellationToken: ct);
        await canal.BasicQosAsync(0, 10, false, ct);

        var consumidor = new AsyncEventingBasicConsumer(canal);
        consumidor.ReceivedAsync += async (_, entrega) =>
        {
            try
            {
                var mensagem = JsonSerializer.Deserialize<Mensagem>(Encoding.UTF8.GetString(entrega.Body.Span), Mensagem.Json)!;
                await Despachar(escopos, mensagem, ct);
                await canal.BasicAckAsync(entrega.DeliveryTag, false, ct);
            }
            catch (Exception e)
            {
                log.LogError(e, "Falha ao processar mensagem {Id}; enviada para a fila de mortas.", entrega.BasicProperties.MessageId);
                await canal.BasicNackAsync(entrega.DeliveryTag, false, requeue: false, ct);
            }
        };
        await canal.BasicConsumeAsync(fila, autoAck: false, consumer: consumidor, cancellationToken: ct);
        log.LogInformation("Consumindo a fila {Fila} ({Tipos}).", fila, string.Join(", ", tipos));

        await Task.Delay(Timeout.Infinite, ct).ContinueWith(_ => { }, CancellationToken.None);
        await canal.DisposeAsync();
    }

    /// <summary>Entrega a mensagem aos manipuladores do tipo. Público para os testes usarem sem RabbitMQ.</summary>
    public static async Task Despachar(IServiceScopeFactory escopos, Mensagem mensagem, CancellationToken ct)
    {
        using var escopo = escopos.CreateScope();
        foreach (var manipulador in escopo.ServiceProvider.GetServices<IManipulador>().Where(m => m.Tipos.Contains(mensagem.Tipo)))
        {
            using var atividade = Telemetria.IniciarConsumo(mensagem, manipulador.GetType().Name);
            await manipulador.ManipularAsync(mensagem, ct);
        }
    }
}

public static class RegistroMensageria
{
    /// <summary>Registra o barramento (RabbitMQ ou memória) e, se houver fila, o consumidor do serviço.</summary>
    public static IServiceCollection AddMensageria(this IServiceCollection services, IConfiguration config, string? fila)
    {
        services.Configure<OpcoesMensageria>(config.GetSection(OpcoesMensageria.Secao));
        var habilitada = config.GetSection(OpcoesMensageria.Secao).GetValue("Habilitada", true);
        if (!habilitada)
        {
            services.AddSingleton<BarramentoEmMemoria>();
            services.AddSingleton<IBarramento>(sp => sp.GetRequiredService<BarramentoEmMemoria>());
            return services;
        }

        services.AddSingleton<ConexaoRabbitMq>();
        services.AddSingleton<IBarramento, BarramentoRabbitMq>();
        if (fila is not null)
            services.AddHostedService(sp => new ConsumidorRabbitMq(
                fila,
                sp.GetRequiredService<ConexaoRabbitMq>(),
                sp.GetRequiredService<IServiceScopeFactory>(),
                sp.GetRequiredService<ILogger<ConsumidorRabbitMq>>()));
        return services;
    }
}
