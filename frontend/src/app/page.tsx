'use client';

import { useEffect, useReducer, useState } from 'react';
import { Carrinho } from '@/components/Carrinho';
import { Catalogo } from '@/components/Catalogo';
import { FluxoEventos } from '@/components/FluxoEventos';
import { ListaPedidos } from '@/components/ListaPedidos';
import { carrinhoReducer, itensCarrinho } from '@/lib/carrinho';
import { MODO_DEMO, api } from '@/lib/config';
import { aplicarEvento, registrarEvento } from '@/lib/pedidos';
import { EventoPedido, Pedido } from '@/lib/tipos';

export default function Pagina() {
  const [carrinho, despachar] = useReducer(carrinhoReducer, {});
  const [pedidos, setPedidos] = useState<Pedido[]>([]);
  const [eventos, setEventos] = useState<EventoPedido[]>([]);
  const [enviando, setEnviando] = useState(false);
  const [erro, setErro] = useState<string | null>(null);

  useEffect(() => {
    const cliente = api();
    cliente.listarPedidos().then(setPedidos).catch((e: Error) => setErro(e.message));
    return cliente.assinar((evento) => {
      setEventos((atual) => registrarEvento(atual, evento));
      setPedidos((atual) => aplicarEvento(atual, evento));
    });
  }, []);

  async function finalizar(cliente: string, cartao: string) {
    setEnviando(true);
    setErro(null);
    try {
      const pedido = await api().criarPedido({ cliente, cartao, itens: itensCarrinho(carrinho) });
      setPedidos((atual) => [pedido, ...atual.filter((p) => p.id !== pedido.id)]);
      despachar({ tipo: 'limpar' });
    } catch (e) {
      setErro(e instanceof Error ? e.message : 'Não foi possível criar o pedido.');
    } finally {
      setEnviando(false);
    }
  }

  return (
    <>
      {MODO_DEMO && (
        <div className="faixa-demo" role="note">
          Modo demonstração: os três serviços e o RabbitMQ são simulados no navegador, com as mesmas regras do back-end.
        </div>
      )}
      <header className="topo">
        <h1>Plataforma de Pedidos</h1>
        <p className="lead">
          Microsserviços .NET que conversam por mensageria. Faça um pedido e acompanhe o evento passando por cada serviço.
        </p>
      </header>
      <main className="layout">
        <div className="coluna">
          <Catalogo aoAdicionar={(id) => despachar({ tipo: 'adicionar', produtoId: id })} />
        </div>
        <div className="coluna">
          <Carrinho carrinho={carrinho} despachar={despachar} aoFinalizar={finalizar} enviando={enviando} erro={erro} />
          <ListaPedidos pedidos={pedidos} />
        </div>
        <div className="largura-total">
          <FluxoEventos eventos={eventos} />
        </div>
      </main>
      <footer className="rodape">
        Projeto de portfólio de <a href="https://jvcamargo10.github.io/portfolio/">João Camargo</a> ·{' '}
        <a href="https://github.com/Jvcamargo10/plataforma-pedidos">código no GitHub</a>
      </footer>
    </>
  );
}
