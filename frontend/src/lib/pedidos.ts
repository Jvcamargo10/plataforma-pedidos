import { EventoPedido, Pedido } from './tipos';

/** Aplica um evento vindo do barramento à lista de pedidos exibida no navegador. */
export function aplicarEvento(pedidos: Pedido[], evento: EventoPedido): Pedido[] {
  return pedidos.map((p) => {
    if (p.id !== evento.pedidoId) return p;
    if (evento.tipo === 'PedidoConfirmado') return { ...p, status: 'Confirmado', motivo: null };
    if (evento.tipo === 'PedidoCancelado') return { ...p, status: 'Cancelado', motivo: evento.detalhe };
    return p;
  });
}

/** Junta eventos sem duplicar (o SignalR pode reenviar após reconexão). */
export function registrarEvento(eventos: EventoPedido[], evento: EventoPedido, limite = 50): EventoPedido[] {
  if (eventos.some((e) => e.id === evento.id)) return eventos;
  return [evento, ...eventos].slice(0, limite);
}
