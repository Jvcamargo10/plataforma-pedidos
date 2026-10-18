import { produto } from './catalogo';

export type Carrinho = Record<string, number>;

export type AcaoCarrinho =
  | { tipo: 'adicionar'; produtoId: string }
  | { tipo: 'alterar'; produtoId: string; quantidade: number }
  | { tipo: 'limpar' };

export const QUANTIDADE_MAXIMA = 10;

export function carrinhoReducer(estado: Carrinho, acao: AcaoCarrinho): Carrinho {
  switch (acao.tipo) {
    case 'adicionar': {
      const atual = estado[acao.produtoId] ?? 0;
      return { ...estado, [acao.produtoId]: Math.min(atual + 1, QUANTIDADE_MAXIMA) };
    }
    case 'alterar': {
      const { [acao.produtoId]: _removido, ...resto } = estado;
      const quantidade = Math.min(Math.max(Math.trunc(acao.quantidade), 0), QUANTIDADE_MAXIMA);
      return quantidade === 0 ? resto : { ...resto, [acao.produtoId]: quantidade };
    }
    case 'limpar':
      return {};
  }
}

export function totalCarrinho(carrinho: Carrinho): number {
  const centavos = Object.entries(carrinho).reduce(
    (acc, [id, qtd]) => acc + Math.round((produto(id)?.preco ?? 0) * 100) * qtd,
    0,
  );
  return centavos / 100;
}

export function itensCarrinho(carrinho: Carrinho) {
  return Object.entries(carrinho).map(([produtoId, quantidade]) => ({ produtoId, quantidade }));
}

export const moeda = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
