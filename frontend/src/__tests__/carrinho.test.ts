import { carrinhoReducer, totalCarrinho } from '@/lib/carrinho';

describe('carrinho', () => {
  it('soma itens sem erro de arredondamento', () => {
    let c = carrinhoReducer({}, { tipo: 'adicionar', produtoId: 'mouse-sem-fio' });
    c = carrinhoReducer(c, { tipo: 'adicionar', produtoId: 'mouse-sem-fio' });
    c = carrinhoReducer(c, { tipo: 'adicionar', produtoId: 'teclado-mec' });
    expect(c).toEqual({ 'mouse-sem-fio': 2, 'teclado-mec': 1 });
    expect(totalCarrinho(c)).toBe(689.7);
  });

  it('remove o item quando a quantidade vai a zero e limita o máximo', () => {
    let c = carrinhoReducer({ 'webcam-hd': 3 }, { tipo: 'alterar', produtoId: 'webcam-hd', quantidade: 0 });
    expect(c).toEqual({});
    c = carrinhoReducer(c, { tipo: 'alterar', produtoId: 'webcam-hd', quantidade: 99 });
    expect(c['webcam-hd']).toBe(10);
  });
});
