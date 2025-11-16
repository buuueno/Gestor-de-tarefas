import React, { useState } from "react";

function CadastrarUsuario() {
    const [nome, setNome] = useState("");
    const [email, setEmail] = useState("");

    async function salvarUsuario(e: React.FormEvent) {
        e.preventDefault();

        const usuario = { nome, email };

        const resposta = await fetch("http://localhost:5172/api/usuario/cadastrar", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(usuario),
        });

        if (resposta.ok) {
            alert("Usuário cadastrado com sucesso!");
            setNome("");
            setEmail("");
        } else {
            alert("Erro ao cadastrar usuário.");
        }
    }

    return (
        <div>
            <h2>Cadastrar Usuário</h2>

            <form onSubmit={salvarUsuario}>
                <input
                    type="text"
                    placeholder="Nome"
                    value={nome}
                    onChange={(e) => setNome(e.target.value)}
                    required
                />

                <input
                    type="email"
                    placeholder="Email"
                    value={email}
                    onChange={(e) => setEmail(e.target.value)}
                    required
                />

                <button type="submit">Salvar</button>
            </form>
        </div>
    );
}

export default CadastrarUsuario;