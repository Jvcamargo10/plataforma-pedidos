import { act, render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import Pagina from '@/app/page';

jest.mock('@/lib/config', () => {
  const { ApiDemo } = jest.requireActual('@/lib/api-demo');
  const api = new ApiDemo((fn: () => void, ms: number) => setTimeout(fn, ms));
  return { MODO_DEMO: true, api: () => api };
});

describe('página da plataforma', () => {
  beforeEach(() => jest.useFakeTimers());
  afterEach(() => jest.useRealTimers());

  it('cria o pedido e mostra a confirmação vinda dos eventos', async () => {
    const usuario = userEvent.setup({ advanceTimers: jest.advanceTimersByTime });
    render(<Pagina />);

    await usuario.click(screen.getByRole('button', { name: 'Adicionar Mouse sem fio' }));
    await usuario.type(screen.getByLabelText('Cliente'), 'Ana Souza');
    await usuario.click(screen.getByRole('button', { name: 'Finalizar pedido' }));

    const lista = screen.getByRole('region', { name: 'Pedidos' });
    expect(await within(lista).findByText('Aguardando pagamento')).toBeInTheDocument();

    await act(async () => {
      jest.advanceTimersByTime(2500);
    });

    expect(within(lista).getByText('Confirmado')).toBeInTheDocument();
    expect(screen.getByText('PagamentoAprovado')).toBeInTheDocument();
  });
});
