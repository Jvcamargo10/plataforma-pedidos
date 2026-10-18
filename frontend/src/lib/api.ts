import { EventoPedido, NovoPedido, Pedido } from './tipos';

/** O front fala com o serviço de Pedidos (REST) e escuta o serviço de Notificações (SignalR). */
export interface PlataformaApi {
  listarPedidos(): Promise<Pedido[]>;
  criarPedido(novo: NovoPedido): Promise<Pedido>;
  /** Assina os eventos em tempo real. Devolve a função que cancela a assinatura. */
  assinar(aoReceber: (evento: EventoPedido) => void): () => void;
}

export class ErroApi extends Error {}
