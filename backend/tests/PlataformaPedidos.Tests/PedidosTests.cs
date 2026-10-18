using System.Net;
using System.Net.Http.Json;
using Compartilhado;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Pedidos.Api;

namespace PlataformaPedidos.Tests;

/// <summary>Sobe o serviço de Pedidos com SQLite temporário e barramento em memória (sem RabbitMQ).</summary>
public sealed class PedidosFactory : WebApplicationFactory<Program>
{
    private readonly string _banco = Path.Combine(Path.GetTempPath(), $"pedidos-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Banco:Conexao", $"Data Source={_banco}");
        builder.UseSetting("Mensageria:Habilitada", "false");
        builder.UseSetting("Outbox:Habilitado", "false");
    }

    public BarramentoEmMemoria Barramento => Services.GetRequiredService<BarramentoEmMemoria>();

    public async Task<int> DespacharOutboxAsync()
    {
        using var escopo = Services.CreateScope();
        return await DespachanteOutbox.DespacharPendentesAsync(
            escopo.ServiceProvider.GetRequiredService<PedidosDb>(), Barramento, CancellationToken.None);
    }

    public Task EntregarAsync(Mensagem mensagem) =>
        ConsumidorRabbitMq.Despachar(Services.GetRequiredService<IServiceScopeFactory>(), mensagem, CancellationToken.None);
}

public class PedidosTests(PedidosFactory fabrica) : IClassFixture<PedidosFactory>
{
    private readonly HttpClient _http = fabrica.CreateClient();

    private static NovoPedido Novo(string cartao = "4111 1111 1111 1111", int quantidade = 2) =>
        new("Ana Souza", cartao, [new NovoItem("mouse-sem-fio", quantidade), new NovoItem("teclado-mec", 1)]);

    private async Task<Pedido> CriarAsync(NovoPedido novo)
    {
        var resposta = await _http.PostAsJsonAsync("/api/pedidos", novo);
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        return (await resposta.Content.ReadFromJsonAsync<Pedido>())!;
    }

    [Fact]
    public async Task Cria_pedido_com_preco_do_catalogo_e_publica_pela_outbox_so_o_final_do_cartao()
    {
        var pedido = await CriarAsync(Novo());
        Assert.Equal(689.70m, pedido.Total);
        Assert.Equal(StatusPedido.AguardandoPagamento, pedido.Status);

        await fabrica.DespacharOutboxAsync();
        var publicada = fabrica.Barramento.Publicadas.Single(m => m.Tipo == TiposEvento.PedidoCriado && m.Ler<PedidoCriado>().PedidoId == pedido.Id);
        var evento = publicada.Ler<PedidoCriado>();
        Assert.Equal("1111", evento.FinalCartao);
        Assert.DoesNotContain("4111", publicada.Conteudo);

        Assert.Equal(0, await fabrica.DespacharOutboxAsync());
    }

    [Fact]
    public async Task Confirma_com_pagamento_aprovado_e_ignora_mensagem_repetida()
    {
        var pedido = await CriarAsync(Novo());
        var aprovado = Mensagem.Criar(new PagamentoAprovado(pedido.Id, "TX-1"), "Pagamentos");

        await fabrica.EntregarAsync(aprovado);
        await fabrica.EntregarAsync(aprovado);
        await fabrica.DespacharOutboxAsync();

        var atual = await _http.GetFromJsonAsync<Pedido>($"/api/pedidos/{pedido.Id}");
        Assert.Equal(StatusPedido.Confirmado, atual!.Status);
        Assert.Single(fabrica.Barramento.Publicadas, m => m.Tipo == TiposEvento.PedidoConfirmado && m.Ler<PedidoConfirmado>().PedidoId == pedido.Id);
    }

    [Fact]
    public async Task Cancela_com_pagamento_recusado_e_guarda_o_motivo()
    {
        var pedido = await CriarAsync(Novo());
        await fabrica.EntregarAsync(Mensagem.Criar(new PagamentoRecusado(pedido.Id, "Recusado pelo emissor"), "Pagamentos"));

        var atual = await _http.GetFromJsonAsync<Pedido>($"/api/pedidos/{pedido.Id}");
        Assert.Equal(StatusPedido.Cancelado, atual!.Status);
        Assert.Equal("Recusado pelo emissor", atual.Motivo);
    }

    [Fact]
    public async Task Recusa_pedido_invalido_com_os_motivos()
    {
        var resposta = await _http.PostAsJsonAsync("/api/pedidos", new NovoPedido(" ", "1234", [new NovoItem("nao-existe", 0)]));
        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        var problema = await resposta.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Contains("Informe o nome do cliente.", problema!.Detail);
        Assert.Contains("Produto nao-existe não existe.", problema.Detail);
        Assert.Contains("Número de cartão inválido.", problema.Detail);
    }
}
