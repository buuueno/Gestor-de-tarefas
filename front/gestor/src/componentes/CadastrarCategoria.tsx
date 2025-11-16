import { useState } from "react";
import axios from "axios";

function CadastrarCategoria() {

  const [nome, setNome] = useState("");
  const [descricao, setDescricao] = useState("");
  const [tipo, setTipo] = useState("");

  async function enviarCategoria(e: any) {
    e.preventDefault();

    try {
      const categoria = {
        nome: nome,
        descricao: descricao,
        tipo: tipo
      };

      const resposta = await axios.post(
        "http://localhost:5172/api/categoria/cadastrar",
        categoria
      );

      alert("Categoria cadastrada com sucesso!");
      console.log(resposta.data);

      setNome("");
      setDescricao("");
      setTipo("");

    } catch (error) {
      console.error("Erro ao cadastrar categoria:", error);
      alert("Erro ao cadastrar categoria");
    }
  }

  return (
    <div style={{ padding: "20px" }}>
      <h2>Cadastrar Categoria</h2>

      <form onSubmit={enviarCategoria}>

        <div>
          <label>Nome:</label>
          <input
            type="text"
            value={nome}
            onChange={(e) => setNome(e.target.value)}
            required
          />
        </div>

        <div>
          <label>Descrição:</label>
          <input
            type="text"
            value={descricao}
            onChange={(e) => setDescricao(e.target.value)}
          />
        </div>

        <div>
          <label>Tipo:</label>
          <select
            value={tipo}
            onChange={(e) => setTipo(e.target.value)}
            required
          >
            <option value="">Selecione...</option>
            <option value="trabalho">Trabalho</option>
            <option value="faculdade">Faculdade</option>
            <option value="pessoal">Pessoal</option>
            <option value="outro">Outro</option>
          </select>
        </div>

        <button type="submit" style={{ marginTop: "10px" }}>
          Cadastrar
        </button>
      </form>
    </div>
  );
}

export default CadastrarCategoria;