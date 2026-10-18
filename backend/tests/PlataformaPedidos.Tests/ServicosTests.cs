using Compartilhado;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Notificacoes.Api;
using Pagamentos.Worker;

namespace PlataformaPedidos.Tests;

public class PagamentosTests
{
    [Theory]
    [InlineData("100", "1111", true, null)]
    [InlineData("100", "0002", false, "Recusado pelo emissor")]
    [InlineData("5000.01", "1111", false, "Valor acima do limite por pedido")]
    [InlineData("5000", "1111", true, null)]
    public void Regras(string total, string final, bool aprovado, string? motivo) =>
        Assert.Equal((aprovado, motivo), RegrasPagamento.Avaliar(decimal.Parse(total, System.Globalization.CultureInfo.InvariantCulture), final));

    [Fact]
    public async Task Responde_com_id_deterministico_para_o_mesmo_pedido()
    {
        var barramento = new BarramentoEmMemoria();
        var manipulador = new PedidoCriadoManipulador(barramento, Options.Create(new OpcoesPagamentos { LatenciaMs = 0 }),
            NullLogger<PedidoCriadoManipulador>.Instance);
        var pedido = Mensagem.Criar(new PedidoCriado("PED-1", "Ana", 99m, "1111"), "Pedidos");

        await manipulador.ManipularAsync(pedido, CancellationToken.None);
        await manipulador.ManipularAsync(pedido, CancellationToken.None);

        Assert.Equal(2, barramento.Publicadas.Count);
        Assert.Equal(barramento.Publicadas[0].Id, barramento.Publicadas[1].Id);
        Assert.Equal(TiposEvento.PagamentoAprovado, barramento.Publicadas[0].Tipo);
    }
}

public class NotificacoesTests
{
    [Fact]
    public void Converte_evento_para_o_formato_da_tela()
    {
        var m = Mensagem.Criar(new PagamentoRecusado("PED-9", "Recusado pelo emissor"), "Pagamentos");
        var tela = Conversor.ParaTela(m);
        Assert.Equal(new EventoParaTela(m.Id.ToString(), "PED-9", "PagamentoRecusado", "Pagamentos", m.OcorridoEm, "Recusado pelo emissor"), tela);
    }

    [Fact]
    public void Total_usa_ponto_decimal_independente_da_cultura()
    {
        var m = Mensagem.Criar(new PedidoCriado("PED-9", "Ana", 149.9m, "1111"), "Pedidos");
        Assert.Equal("Total 149.90 publicado no RabbitMQ", Conversor.ParaTela(m).Detalhe);
    }
}

public class MensagemTests
{
    [Fact]
    public void Id_deterministico_e_estavel_e_diferente_por_chave()
    {
        Assert.Equal(Mensagem.IdDeterministico("pagamento:PED-1"), Mensagem.IdDeterministico("pagamento:PED-1"));
        Assert.NotEqual(Mensagem.IdDeterministico("pagamento:PED-1"), Mensagem.IdDeterministico("pagamento:PED-2"));
    }

    [Fact]
    public void Envelope_carrega_o_nome_do_tipo_e_le_o_conteudo()
    {
        var m = Mensagem.Criar(new PedidoCancelado("PED-1", "x"), "Pedidos");
        Assert.Equal(TiposEvento.PedidoCancelado, m.Tipo);
        Assert.Equal("x", m.Ler<PedidoCancelado>().Motivo);
    }
}
