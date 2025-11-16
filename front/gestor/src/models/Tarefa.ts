export default interface Tarefa {
  id: number;
  titulo: string;
  descricao: string;
  status: string;
  categoriaId: number;
  usuarioId: number;
  criadoEm: string;
}