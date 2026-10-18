import { Produto } from './tipos';

/** Catálogo fictício, igual ao semeado no serviço de Pedidos. */
export const CATALOGO: Produto[] = [
  { id: 'notebook-14', nome: 'Notebook 14"', descricao: '16 GB de RAM, SSD de 512 GB', preco: 4299.9 },
  { id: 'monitor-27', nome: 'Monitor 27"', descricao: 'QHD, 75 Hz, ajuste de altura', preco: 1599 },
  { id: 'teclado-mec', nome: 'Teclado mecânico', descricao: 'ABNT2, switches marrons', preco: 389.9 },
  { id: 'mouse-sem-fio', nome: 'Mouse sem fio', descricao: 'Bluetooth e receptor USB', preco: 149.9 },
  { id: 'headset', nome: 'Headset com microfone', descricao: 'Cancelamento de ruído', preco: 459 },
  { id: 'webcam-hd', nome: 'Webcam Full HD', descricao: '1080p com microfone embutido', preco: 279.9 },
];

export function produto(id: string): Produto | undefined {
  return CATALOGO.find((p) => p.id === id);
}
