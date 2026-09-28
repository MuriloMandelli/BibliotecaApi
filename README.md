# Biblioteca API: REST + gRPC sobre o mesmo Domínio

Trabalho N1 de Arquitetura de Software. API de uma biblioteca (livros e empréstimos) que expõe
**os mesmos casos de uso por REST e por gRPC**, organizada em camadas
**Apresentação → Domínio → Repositório**.

## Domínio

| Entidade | Campos principais |
|---|---|
| **Livro** | título, autor, ISBN (único), total de exemplares, exemplares disponíveis (calculado) |
| **Empréstimo** | livro, nome do leitor, data do empréstimo, data prevista, data de devolução |

Relação: um Livro tem N Empréstimos.

### Regras de negócio (camada de Domínio, em `Services/`)

A regra principal **cruza os dois agregados** (equivalente a "a marca precisa estar ativa" da VeiculosApi):

1. **Só empresta se o livro tiver exemplar disponível.** O `EmprestimoService` consulta o
   `ILivroRepository` e compara o total de exemplares com os empréstimos ativos daquele livro.
2. Leitor pode ter no máximo **3 empréstimos ativos**.
3. **Não exclui livro com empréstimo ativo** (`LivroService` olha os empréstimos do livro).
4. **Não reduz o total de exemplares** abaixo da quantidade emprestada no momento.
5. ISBN único; empréstimo não pode ser devolvido duas vezes; empréstimo ativo não pode ser excluído.

### Exceções de Domínio → status

| Exceção (Domínio) | REST (`Middleware/ExceptionHandlingMiddleware`) | gRPC (`Grpc/DomainExceptionInterceptor`) |
|---|---|---|
| `NotFoundException` | 404 Not Found | `NotFound` |
| `ConflictException` (ISBN duplicado) | 409 Conflict | `AlreadyExists` |
| `RegraNegocioException` | 400 Bad Request | `FailedPrecondition` |
| qualquer outra | 500 | `Internal` |

Nenhum Controller ou GrpcService tem `if`/`try-catch` decidindo status: o Domínio lança a
exceção e cada lado da Apresentação traduz num lugar só.

## Como rodar

### Docker (recomendado)

```bash
docker compose up --build
```

(ou `docker build -t biblioteca-api .` + `docker run -p 8080:8080 -p 8081:8081 biblioteca-api`)

### Sem Docker (.NET 8 SDK)

```bash
cd BibliotecaApi
dotnet run
```

O banco SQLite é criado sozinho e já vem com 3 livros de exemplo. "O Hobbit" (id 3) já nasce
com o único exemplar emprestado, para demonstrar a regra de disponibilidade.

## Portas

| Porta | Protocolo | Uso |
|---|---|---|
| **8080** | HTTP/1.1 | REST + Swagger em http://localhost:8080/swagger |
| **8081** | HTTP/2 (h2c) | gRPC (com Server Reflection ligado) |

São portas separadas porque, sem TLS, o Kestrel não consegue negociar HTTP/1.1 e HTTP/2 na mesma porta.

## Exemplos REST (curl)

```bash
# listar livros
curl http://localhost:8080/api/livros

# cadastrar livro -> 201
curl -X POST http://localhost:8080/api/livros -H "Content-Type: application/json" \
  -d '{"titulo":"Refactoring","autor":"Martin Fowler","isbn":"9780134757599","totalExemplares":2}'

# ISBN duplicado -> 409
curl -X POST http://localhost:8080/api/livros -H "Content-Type: application/json" \
  -d '{"titulo":"X","autor":"Y","isbn":"9780132350884","totalExemplares":1}'

# emprestar livro com exemplar -> 201
curl -X POST http://localhost:8080/api/emprestimos -H "Content-Type: application/json" \
  -d '{"livroId":1,"nomeLeitor":"Joao","prazoDias":7}'

# emprestar livro SEM exemplar (regra cruzada) -> 400
curl -X POST http://localhost:8080/api/emprestimos -H "Content-Type: application/json" \
  -d '{"livroId":3,"nomeLeitor":"Joao","prazoDias":7}'

# devolver -> 200
curl -X POST http://localhost:8080/api/emprestimos/1/devolucao

# livro inexistente -> 404
curl http://localhost:8080/api/livros/999
```

Coleção pronta para importar no Postman: `postman/BibliotecaApi-REST.postman_collection.json`.

## Exemplos gRPC

### Postman
New → **gRPC** → URL `localhost:8081` → aba *Service definition* → **Use server reflection**
(ou importe os arquivos de `BibliotecaApi/Protos/`). Escolha o método e mande a mensagem, por exemplo
`biblioteca.EmprestimosService/Criar` com `{"livro_id": 3, "nome_leitor": "Joao", "prazo_dias": 7}`
→ volta `FAILED_PRECONDITION`.

### grpcurl

```bash
grpcurl -plaintext localhost:8081 list
grpcurl -plaintext -d '{}' localhost:8081 biblioteca.LivrosService/Listar
grpcurl -plaintext -d '{"titulo":"Refactoring","autor":"Martin Fowler","isbn":"9780134757599","total_exemplares":1}' localhost:8081 biblioteca.LivrosService/Criar
grpcurl -plaintext -d '{"id":999}' localhost:8081 biblioteca.LivrosService/ObterPorId          # NotFound
grpcurl -plaintext -d '{"livro_id":3,"nome_leitor":"Joao","prazo_dias":7}' localhost:8081 biblioteca.EmprestimosService/Criar   # FailedPrecondition
grpcurl -plaintext -d '{"id":1}' localhost:8081 biblioteca.EmprestimosService/Devolver
```

## Onde cada camada vive

```
BibliotecaApi/
├── Controllers/     Apresentação REST   (LivrosController, EmprestimosController)
├── Grpc/            Apresentação gRPC   (LivroGrpcService, EmprestimoGrpcService, DomainExceptionInterceptor)
├── Middleware/      Apresentação REST   (ExceptionHandlingMiddleware: exceção -> HTTP)
├── Protos/          Contrato gRPC (contract-first: livros.proto, emprestimos.proto)
├── DTOs/            Objetos de entrada/saída do Domínio
├── Services/        DOMÍNIO: regras de negócio (ILivroService, IEmprestimoService + implementações)
├── Exceptions/      Exceções de Domínio (NotFound, Conflict, RegraNegocio)
├── Repositories/    REPOSITÓRIO: acesso a dados com EF Core (ILivroRepository, IEmprestimoRepository)
├── Models/          Entidades (Livro, Emprestimo)
└── Data/            DbContext (SQLite) + seed
```

**Ponto central (baixo acoplamento):** `LivrosController` e `LivroGrpcService` recebem a **mesma**
interface `ILivroService` por injeção de dependência (idem para empréstimos). A regra de negócio
existe uma única vez, no Domínio. Nenhuma classe de Apresentação conhece o `AppDbContext`.

Fluxo de uma chamada (dá pra ver no console, cada log marca a camada):

```
[Apresentação-REST] POST /api/emprestimos   ou   [Apresentação-gRPC] Criar empréstimo
        -> [Domínio] Validando regras para emprestar livro 3...
            -> [Repositório] SELECT Livro 3
        -> [Domínio] Regra violada: livro 'O Hobbit' sem exemplares disponíveis
[Apresentação-*] Exceção de domínio traduzida para HTTP 400 / FailedPrecondition
```
