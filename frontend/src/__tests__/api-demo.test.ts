import { ApiDemo } from '@/lib/api-demo';
import { aplicarEvento, registrarEvento } from '@/lib/pedidos';
import { EventoPedido, Pedido } from '@/lib/tipos';

function apiImediata() {
  const fila: (() => void)[] = [];
  const api = new ApiDemo((fn) => fila.push(fn));
  return { api, rodar: () => fila.splice(0).forEach((fn) => fn()) };
}

describe('ApiDemo', () => {
  it('publica a sequência de eventos de um pedido aprovado', async () => {
    const { api, rodar } = apiImediata();
    const eventos: EventoPedido[] = [];
    api.assinar((e) => eventos.push(e));

    const pedido = await api.criarPedido({ cliente: 'Ana', cartao: '4111 1111 1111 1111', itens: [{ produtoId: 'mouse-sem-fio', quantidade: 1 }] });
    rodar();

    expect(eventos.map((e) => e.tipo)).toEqual(['PedidoCriado', 'PagamentoAprovado', 'PedidoConfirmado']);
    expect((await api.listarPedidos())[0]).toMatchObject({ id: pedido.id, status: 'Confirmado' });
  });

  it('cancela o pedido quando o pagamento é recusado', async () => {
    const { api, rodar } = apiImediata();
    const eventos: EventoPedido[] = [];
    api.assinar((e) => eventos.push(e));

    await api.criarPedido({ cliente: 'Ana', cartao: '4000 0000 0000 0002', itens: [{ produtoId: 'mouse-sem-fio', quantidade: 1 }] });
    rodar();

    expect(eventos.map((e) => e.tipo)).toEqual(['PedidoCriado', 'PagamentoRecusado', 'PedidoCancelado']);
    expect(eventos[2].detalhe).toBe('Recusado pelo emissor');
  });

  it('recusa pedido sem itens', async () => {
    await expect(new ApiDemo().criarPedido({ cliente: 'Ana', cartao: '', itens: [] })).rejects.toThrow('O carrinho está vazio.');
  });
});

describe('aplicação dos eventos', () => {
  const pedido = { id: 'PED-1', status: 'AguardandoPagamento' } as Pedido;
  const evento = (tipo: EventoPedido['tipo'], detalhe = ''): EventoPedido => ({
    id: `PED-1-${tipo}`, pedidoId: 'PED-1', tipo, servico: 'Pedidos', ocorridoEm: '', detalhe,
  });

  it('atualiza o status com o evento final', () => {
    expect(aplicarEvento([pedido], evento('PedidoConfirmado'))[0].status).toBe('Confirmado');
    expect(aplicarEvento([pedido], evento('PedidoCancelado', 'Recusado'))[0]).toMatchObject({ status: 'Cancelado', motivo: 'Recusado' });
  });

  it('ignora evento repetido', () => {
    const e = evento('PedidoCriado');
    expect(registrarEvento(registrarEvento([], e), e)).toHaveLength(1);
  });
});
