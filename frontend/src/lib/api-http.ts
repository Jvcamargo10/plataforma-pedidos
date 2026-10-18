import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { ErroApi, PlataformaApi } from './api';
import { EventoPedido, NovoPedido, Pedido } from './tipos';

export class ApiHttp implements PlataformaApi {
  constructor(
    private readonly urlPedidos: string,
    private readonly urlHub: string,
  ) {}

  async listarPedidos(): Promise<Pedido[]> {
    return this.pedir<Pedido[]>('/api/pedidos');
  }

  async criarPedido(novo: NovoPedido): Promise<Pedido> {
    return this.pedir<Pedido>('/api/pedidos', { method: 'POST', body: JSON.stringify(novo) });
  }

  assinar(aoReceber: (evento: EventoPedido) => void): () => void {
    const conexao = new HubConnectionBuilder()
      .withUrl(this.urlHub)
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();
    conexao.on('evento', aoReceber);
    conexao.start().catch(() => undefined);
    return () => void conexao.stop();
  }

  private async pedir<T>(caminho: string, init?: RequestInit): Promise<T> {
    const resposta = await fetch(this.urlPedidos + caminho, {
      ...init,
      headers: { 'Content-Type': 'application/json' },
    }).catch(() => {
      throw new ErroApi('Não foi possível falar com o serviço de Pedidos.');
    });
    if (!resposta.ok) {
      const problema = await resposta.json().catch(() => null);
      throw new ErroApi(problema?.detail ?? `Erro ${resposta.status}`);
    }
    return resposta.json() as Promise<T>;
  }
}
