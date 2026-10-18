import { EventoPedido, Servico } from '@/lib/tipos';

const ETAPAS: { id: Servico | 'RabbitMQ' | 'Navegador'; titulo: string; detalhe: string }[] = [
  { id: 'Pedidos', titulo: 'Pedidos', detalhe: 'API REST + outbox' },
  { id: 'RabbitMQ', titulo: 'RabbitMQ', detalhe: 'exchange "pedidos"' },
  { id: 'Pagamentos', titulo: 'Pagamentos', detalhe: 'worker consumidor' },
  { id: 'Notificacoes', titulo: 'Notificações', detalhe: 'SignalR' },
  { id: 'Navegador', titulo: 'Navegador', detalhe: 'esta tela' },
];

const hora = (iso: string) => new Date(iso).toLocaleTimeString('pt-BR');

/** Por onde cada evento passa: quem publica, o RabbitMQ e a entrega ao navegador pelo SignalR. */
function caminho(evento: EventoPedido | undefined): Set<string> {
  if (!evento) return new Set();
  return new Set([evento.servico, 'RabbitMQ', 'Notificacoes', 'Navegador']);
}

export function FluxoEventos({ eventos }: { eventos: EventoPedido[] }) {
  const ultimo = eventos[0];
  const ativos = caminho(ultimo);
  return (
    <section className="bloco" aria-labelledby="titulo-fluxo">
      <h2 id="titulo-fluxo">Eventos em tempo real</h2>
      <ol className="fluxo" aria-label="Caminho das mensagens">
        {ETAPAS.map((e) => (
          <li key={e.id} className={['etapa', ativos.has(e.id) && 'ativa', ultimo?.servico === e.id && 'origem'].filter(Boolean).join(' ')}>
            <strong>{e.titulo}</strong>
            <span>{e.detalhe}</span>
          </li>
        ))}
      </ol>
      {eventos.length === 0 ? (
        <p className="muted">Finalize um pedido para ver as mensagens passando pelos serviços.</p>
      ) : (
        <ul className="log" aria-live="polite">
          {eventos.map((e) => (
            <li key={e.id} data-tipo={e.tipo}>
              <time dateTime={e.ocorridoEm}>{hora(e.ocorridoEm)}</time>
              <span className="servico">{e.servico}</span>
              <span className="tipo">{e.tipo}</span>
              <span className="id-pedido">{e.pedidoId}</span>
              <span className="detalhe">{e.detalhe}</span>
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}
