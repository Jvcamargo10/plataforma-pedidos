import { ErroApi, PlataformaApi } from './api';
import { produto } from './catalogo';
import { avaliarPagamento, cartaoValido } from './regras-pagamento';
import { EventoPedido, NovoPedido, Pedido, Servico, TipoEvento } from './tipos';

type Agendar = (fn: () => void, ms: number) => void;

/**
 * Simula no navegador o que os três serviços fazem através do RabbitMQ:
 * Pedidos publica PedidoCriado → Pagamentos avalia e publica o resultado → Pedidos confirma ou cancela
 * → Notificações repassa cada evento ao navegador.
 */
export class ApiDemo implements PlataformaApi {
  private pedidos: Pedido[] = [];
  private ouvintes = new Set<(e: EventoPedido) => void>();
  private seq = 0;

  constructor(private readonly agendar: Agendar = (fn, ms) => setTimeout(fn, ms)) {}

  async listarPedidos(): Promise<Pedido[]> {
    return [...this.pedidos];
  }

  async criarPedido(novo: NovoPedido): Promise<Pedido> {
    if (!novo.cliente.trim()) throw new ErroApi('Informe o nome do cliente.');
    if (!novo.itens.length) throw new ErroApi('O carrinho está vazio.');
    if (!cartaoValido(novo.cartao)) throw new ErroApi('Número de cartão inválido.');

    const itens = novo.itens.map((i) => {
      const p = produto(i.produtoId);
      if (!p) throw new ErroApi(`Produto ${i.produtoId} não existe.`);
      return { produtoId: p.id, nome: p.nome, quantidade: i.quantidade, precoUnitario: p.preco };
    });
    const total = Math.round(itens.reduce((acc, i) => acc + i.precoUnitario * 100 * i.quantidade, 0)) / 100;
    const pedido: Pedido = {
      id: `PED-${String(++this.seq).padStart(4, '0')}`,
      cliente: novo.cliente.trim(),
      itens,
      total,
      status: 'AguardandoPagamento',
      criadoEm: new Date().toISOString(),
    };
    this.pedidos = [pedido, ...this.pedidos];

    const pagamento = avaliarPagamento(total, novo.cartao.replace(/\D/g, '').slice(-4));
    this.agendar(() => this.emitir(pedido.id, 'PedidoCriado', 'Pedidos', `Total ${total.toFixed(2)} publicado no RabbitMQ`), 150);
    this.agendar(() => {
      if (pagamento.aprovado) this.emitir(pedido.id, 'PagamentoAprovado', 'Pagamentos', 'Transação autorizada');
      else this.emitir(pedido.id, 'PagamentoRecusado', 'Pagamentos', pagamento.motivo!);
    }, 1400);
    this.agendar(() => {
      this.atualizar(pedido.id, pagamento.aprovado);
      if (pagamento.aprovado) this.emitir(pedido.id, 'PedidoConfirmado', 'Pedidos', 'Pedido confirmado');
      else this.emitir(pedido.id, 'PedidoCancelado', 'Pedidos', pagamento.motivo!);
    }, 1900);

    return pedido;
  }

  assinar(aoReceber: (evento: EventoPedido) => void): () => void {
    this.ouvintes.add(aoReceber);
    return () => this.ouvintes.delete(aoReceber);
  }

  private atualizar(id: string, aprovado: boolean) {
    this.pedidos = this.pedidos.map((p) =>
      p.id === id ? { ...p, status: aprovado ? 'Confirmado' : 'Cancelado' } : p,
    );
  }

  private emitir(pedidoId: string, tipo: TipoEvento, servico: Servico, detalhe: string) {
    const evento: EventoPedido = {
      id: `${pedidoId}-${tipo}`,
      pedidoId,
      tipo,
      servico,
      ocorridoEm: new Date().toISOString(),
      detalhe,
    };
    this.ouvintes.forEach((o) => o(evento));
  }
}
