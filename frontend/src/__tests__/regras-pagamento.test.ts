import { avaliarPagamento, cartaoValido, luhnValido } from '@/lib/regras-pagamento';

describe('regras de pagamento', () => {
  it('valida o cartão pelo algoritmo de Luhn', () => {
    expect(luhnValido('4111111111111111')).toBe(true);
    expect(luhnValido('4111111111111112')).toBe(false);
  });

  it('aprova, recusa pelo emissor e recusa acima do limite', () => {
    expect(avaliarPagamento(100, '1111')).toEqual({ aprovado: true, motivo: null });
    expect(avaliarPagamento(100, '0002').motivo).toBe('Recusado pelo emissor');
    expect(avaliarPagamento(5000.01, '1111').motivo).toBe('Valor acima do limite por pedido');
    expect(cartaoValido('4000 0000 0000 0002')).toBe(true);
    expect(cartaoValido('1234')).toBe(false);
  });
});
