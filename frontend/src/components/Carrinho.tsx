'use client';

import { FormEvent, useState } from 'react';
import { AcaoCarrinho, Carrinho as TipoCarrinho, moeda, totalCarrinho } from '@/lib/carrinho';
import { produto } from '@/lib/catalogo';
import { CARTOES_TESTE, LIMITE_POR_PEDIDO } from '@/lib/regras-pagamento';

interface Props {
  carrinho: TipoCarrinho;
  despachar: (acao: AcaoCarrinho) => void;
  aoFinalizar: (cliente: string, cartao: string) => void;
  enviando: boolean;
  erro: string | null;
}

export function Carrinho({ carrinho, despachar, aoFinalizar, enviando, erro }: Props) {
  const [cliente, setCliente] = useState('');
  const [cartao, setCartao] = useState(CARTOES_TESTE[0].numero);
  const itens = Object.entries(carrinho);
  const total = totalCarrinho(carrinho);

  function enviar(e: FormEvent) {
    e.preventDefault();
    aoFinalizar(cliente, cartao);
  }

  return (
    <section className="bloco" aria-labelledby="titulo-carrinho">
      <h2 id="titulo-carrinho">Carrinho</h2>
      {itens.length === 0 ? (
        <p className="muted">Adicione produtos do catálogo.</p>
      ) : (
        <ul className="itens-carrinho">
          {itens.map(([id, qtd]) => (
            <li key={id}>
              <span>{produto(id)?.nome}</span>
              <input
                type="number"
                min={0}
                max={10}
                value={qtd}
                aria-label={`Quantidade de ${produto(id)?.nome}`}
                onChange={(e) => despachar({ tipo: 'alterar', produtoId: id, quantidade: Number(e.target.value) })}
              />
            </li>
          ))}
        </ul>
      )}
      <p className="total">
        Total <strong>{moeda(total)}</strong>
      </p>
      {total > LIMITE_POR_PEDIDO && (
        <p className="aviso">Acima de {moeda(LIMITE_POR_PEDIDO)}, o serviço de Pagamentos recusa o pedido.</p>
      )}

      <form onSubmit={enviar} className="formulario">
        <label>
          <span>Cliente</span>
          <input value={cliente} onChange={(e) => setCliente(e.target.value)} placeholder="Nome do cliente" required />
        </label>
        <label>
          <span>Cartão (simulado)</span>
          <select value={cartao} onChange={(e) => setCartao(e.target.value)}>
            {CARTOES_TESTE.map((c) => (
              <option key={c.numero} value={c.numero}>
                {c.numero} · {c.rotulo}
              </option>
            ))}
          </select>
        </label>
        {erro && (
          <p className="erro" role="alert">
            {erro}
          </p>
        )}
        <button type="submit" className="botao" disabled={enviando || itens.length === 0}>
          {enviando ? 'Enviando…' : 'Finalizar pedido'}
        </button>
      </form>
    </section>
  );
}
