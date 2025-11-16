import React from "react";
import { BrowserRouter, Routes, Route, Link } from "react-router-dom";
import CadastrarCategoria from "./componentes/CadastrarCategoria";
import CadastrarTarefa from "./componentes/CadastrarTarefa";
import CadastrarUsuario from "./componentes/CadastrarUsuario";

function App() {
  return (
    <div className="App">
      <BrowserRouter>
        <nav>
          <ul>
            <li>
              <Link to="/categoria/cadastrar">Cadastrar Categoria</Link>
            </li>
             <li>
              <Link to="/usuario/cadastrar">Cadastrar usuario</Link>
            </li>
            <li>
              <Link to="/tarefa/cadastrar">Cadastrar Tarefa</Link>
            </li>
           
          </ul>
        </nav>

        <Routes>
           <Route path="/usuario/cadastrar" element={<CadastrarUsuario />} />
          <Route path="/categoria/cadastrar" element={<CadastrarCategoria />} />
          <Route path="/tarefa/cadastrar" element={<CadastrarTarefa />} />
        </Routes>
      </BrowserRouter>
    </div>
  );
}

export default App;