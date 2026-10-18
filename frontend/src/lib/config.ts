import { PlataformaApi } from './api';
import { ApiDemo } from './api-demo';
import { ApiHttp } from './api-http';

export const MODO_DEMO = process.env.NEXT_PUBLIC_DEMO === 'true';

let instancia: PlataformaApi | null = null;

export function api(): PlataformaApi {
  instancia ??= MODO_DEMO
    ? new ApiDemo()
    : new ApiHttp(
        process.env.NEXT_PUBLIC_URL_PEDIDOS ?? 'http://localhost:5101',
        process.env.NEXT_PUBLIC_URL_HUB ?? 'http://localhost:5103/hubs/pedidos',
      );
  return instancia;
}
