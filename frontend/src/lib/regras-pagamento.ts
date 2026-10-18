/**
 * Regras do serviço de Pagamentos (RegrasPagamento.cs), repetidas aqui para a demonstração.
 * O serviço real é um gateway simulado: não processa cartões de verdade.
 */
export const LIMITE_POR_PEDIDO = 5000;

export const CARTOES_TESTE = [
  { numero: '4111 1111 1111 1111', rotulo: 'Cartão de teste: aprova' },
  { numero: '4000 0000 0000 0002', rotulo: 'Cartão de teste: recusado pelo emissor' },
];

/** Validado pelo serviço de Pedidos antes de aceitar o pedido. */
export function cartaoValido(cartao: string): boolean {
  const digitos = cartao.replace(/\D/g, '');
  return digitos.length >= 13 && digitos.length <= 19 && luhnValido(digitos);
}

/** Avaliado pelo serviço de Pagamentos, que só recebe os 4 últimos dígitos do cartão. */
export function avaliarPagamento(total: number, finalCartao: string): { aprovado: boolean; motivo: string | null } {
  if (finalCartao === '0002') return { aprovado: false, motivo: 'Recusado pelo emissor' };
  if (total > LIMITE_POR_PEDIDO) return { aprovado: false, motivo: 'Valor acima do limite por pedido' };
  return { aprovado: true, motivo: null };
}

export function luhnValido(digitos: string): boolean {
  let soma = 0;
  for (let i = 0; i < digitos.length; i++) {
    let d = Number(digitos[digitos.length - 1 - i]);
    if (i % 2 === 1) {
      d *= 2;
      if (d > 9) d -= 9;
    }
    soma += d;
  }
  return soma % 10 === 0;
}
