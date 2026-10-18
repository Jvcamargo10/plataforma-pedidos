using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Compartilhado;

/// <summary>Envelope que trafega no RabbitMQ.</summary>
public sealed record Mensagem(Guid Id, string Tipo, string Origem, DateTime OcorridoEm, string Conteudo, string? TraceParent)
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static Mensagem Criar<T>(T evento, string origem, Guid? id = null) where T : notnull =>
        new(id ?? Guid.NewGuid(), typeof(T).Name, origem, DateTime.UtcNow,
            JsonSerializer.Serialize(evento, Json), Telemetria.TraceParentAtual());

    public T Ler<T>() => JsonSerializer.Deserialize<T>(Conteudo, Json)
                         ?? throw new InvalidOperationException($"Mensagem {Id} vazia.");

    /// <summary>
    /// Id estável derivado de uma chave de negócio. Se o mesmo fato for publicado de novo
    /// (reentrega, reinício do serviço), o consumidor reconhece o id e não processa duas vezes.
    /// </summary>
    public static Guid IdDeterministico(string chave) =>
        new(MD5.HashData(Encoding.UTF8.GetBytes(chave)));
}

public interface IBarramento
{
    Task PublicarAsync(Mensagem mensagem, CancellationToken ct = default);
}

/// <summary>Trata um ou mais tipos de mensagem. Cada serviço registra os seus.</summary>
public interface IManipulador
{
    IReadOnlyCollection<string> Tipos { get; }
    Task ManipularAsync(Mensagem mensagem, CancellationToken ct);
}

/// <summary>Barramento em memória, para testes e para rodar um serviço isolado sem RabbitMQ.</summary>
public sealed class BarramentoEmMemoria : IBarramento
{
    private readonly List<Mensagem> _publicadas = [];
    private readonly Lock _trava = new();

    public IReadOnlyList<Mensagem> Publicadas
    {
        get { lock (_trava) return [.. _publicadas]; }
    }

    public Task PublicarAsync(Mensagem mensagem, CancellationToken ct = default)
    {
        lock (_trava) _publicadas.Add(mensagem);
        return Task.CompletedTask;
    }
}
