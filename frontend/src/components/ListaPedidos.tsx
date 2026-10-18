import { moeda } from '@/lib/carrinho';
import { Pedido, StatusPedido } from '@/lib/tipos';

const ROTULO: Record<StatusPedido, string> = {
  AguardandoPagamento: 'Aguardando pagamento',
  Confirmado: 'Confirmado',
  Cancelado: 'Cancelado',
};

export function ListaPedidos({ pedidos }: { pedidos: Pedido[] }) {
  return (
    <section className="bloco" aria-labelledby="titulo-pedidos">
      <h2 id="titulo-pedidos">Pedidos</h2>
      {pedidos.length === 0 ? (
        <p className="muted">Nenhum pedido ainda.</p>
      ) : (
        <ul className="pedidos">
          {pedidos.map((p) => (
            <li key={p.id} className="pedido">
              <div className="pedido-topo">
                <strong>{p.id}</strong>
                <span className={`status status-${p.status}`}>{ROTULO[p.status]}</span>
              </div>
              <p className="muted">
                {p.cliente} · {p.itens.reduce((a, i) => a + i.quantidade, 0)} item(ns) · {moeda(p.total)}
              </p>
              {p.motivo && <p className="motivo">{p.motivo}</p>}
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}
