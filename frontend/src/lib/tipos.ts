export interface Produto {
  id: string;
  nome: string;
  descricao: string;
  preco: number;
}

export interface ItemPedido {
  produtoId: string;
  nome: string;
  quantidade: number;
  precoUnitario: number;
}

export type StatusPedido = 'AguardandoPagamento' | 'Confirmado' | 'Cancelado';

export type Servico = 'Pedidos' | 'Pagamentos' | 'Notificacoes';

export type TipoEvento = 'PedidoCriado' | 'PagamentoAprovado' | 'PagamentoRecusado' | 'PedidoConfirmado' | 'PedidoCancelado';

/** Evento publicado no RabbitMQ e repassado ao navegador pelo serviço de Notificações (SignalR). */
export interface EventoPedido {
  id: string;
  pedidoId: string;
  tipo: TipoEvento;
  servico: Servico;
  ocorridoEm: string;
  detalhe: string;
}

export interface Pedido {
  id: string;
  cliente: string;
  itens: ItemPedido[];
  total: number;
  status: StatusPedido;
  criadoEm: string;
  motivo?: string | null;
}

export interface NovoPedido {
  cliente: string;
  cartao: string;
  itens: { produtoId: string; quantidade: number }[];
}
