import { useState, useEffect } from "react";
import Categoria  from "../models/Categoria";
import Usuario  from "../models/Usuario";

function CadastrarTarefa() {
  const [titulo, setTitulo] = useState("");
  const [descricao, setDescricao] = useState("");
  const [categoriaId, setCategoriaId] = useState("");
  const [usuarioId, setUsuarioId] = useState("");
  const [categorias, setCategorias] = useState<Categoria[]>([]);
  const [usuarios, setUsuarios] = useState<Usuario[]>([]);
  const [mensagem, setMensagem] = useState("");

 useEffect(() => {
  fetch("http://localhost:5172/api/categoria/listar")
    .then((res) => res.json())
    .then((data) => {
      console.log("Categorias retornadas:", data); // <- ADD
      setCategorias(data);
    });

  fetch("http://localhost:5172/api/usuario/listar")
    .then((res) => res.json())
    .then((data) => {
      console.log("Usuarios retornados:", data); // <- ADD
      setUsuarios(data);
    });
}, []);

  async function salvarTarefa(e: React.FormEvent) {
    e.preventDefault();

    const tarefa = {
      titulo,
      descricao,
      categoriaId: Number(categoriaId),
      usuarioId: Number(usuarioId)
    };

    const resposta = await fetch("http://localhost:5172/api/tarefa/cadastrar", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(tarefa),
    });

    if (resposta.ok) {
      setMensagem("Tarefa cadastrada com sucesso!");
      setTitulo("");
      setDescricao("");
      setCategoriaId("");
      setUsuarioId("");
    } else {
      setMensagem("Erro ao cadastrar tarefa.");
    }
  }

  return (
    <div>
      <h2>Cadastrar Tarefa</h2>

      <form onSubmit={salvarTarefa}>
        <input
          type="text"
          placeholder="Título"
          value={titulo}
          onChange={(e) => setTitulo(e.target.value)}
          required
        />

        <textarea
          placeholder="Descrição"
          value={descricao}
          onChange={(e) => setDescricao(e.target.value)}
        />

        <select
          value={categoriaId}
          onChange={(e) => setCategoriaId(e.target.value)}
          required
        >
          <option value="">Selecione a Categoria</option>
          {categorias.map((cat) => (
            <option key={cat.id} value={cat.id}>
              {cat.nome}
            </option>
          ))}
        </select>

        <select
          value={usuarioId}
          onChange={(e) => setUsuarioId(e.target.value)}
          required
        >
          <option value="">Selecione o Usuário</option>
          {usuarios.map((u) => (
            <option key={u.id} value={u.id}>
              {u.nome}
            </option>
          ))}
        </select>

        <button type="submit">Salvar Tarefa</button>
      </form>

      <p>{mensagem}</p>
    </div>
  );
}

export default CadastrarTarefa;