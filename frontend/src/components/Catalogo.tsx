import { CATALOGO } from '@/lib/catalogo';
import { moeda } from '@/lib/carrinho';

export function Catalogo({ aoAdicionar }: { aoAdicionar: (produtoId: string) => void }) {
  return (
    <section className="bloco" aria-labelledby="titulo-catalogo">
      <h2 id="titulo-catalogo">Catálogo</h2>
      <ul className="grade-produtos">
        {CATALOGO.map((p) => (
          <li key={p.id} className="produto">
            <div>
              <h3>{p.nome}</h3>
              <p className="muted">{p.descricao}</p>
            </div>
            <div className="produto-rodape">
              <strong>{moeda(p.preco)}</strong>
              <button type="button" className="botao botao-leve" onClick={() => aoAdicionar(p.id)} aria-label={`Adicionar ${p.nome}`}>
                Adicionar
              </button>
            </div>
          </li>
        ))}
      </ul>
    </section>
  );
}
