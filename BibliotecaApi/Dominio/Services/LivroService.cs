using Microsoft.EntityFrameworkCore;
using BibliotecaApi.DTOs;
using BibliotecaApi.Exceptions;
using BibliotecaApi.Models;
using BibliotecaApi.Repositories;

namespace BibliotecaApi.Services;

/// <summary>
/// Camada de Domínio de Livro. Regras que cruzam com Empréstimo:
///  - não dá pra reduzir o total de exemplares abaixo da quantidade emprestada agora;
///  - não dá pra excluir um livro que ainda tem empréstimo ativo.
/// </summary>
public class LivroService : ILivroService
{
    private readonly ILivroRepository _livroRepository;
    private readonly ILogger<LivroService> _logger;

    public LivroService(ILivroRepository livroRepository, ILogger<LivroService> logger)
    {
        _livroRepository = livroRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<LivroResponseDto>> ListarAsync(string? busca)
    {
        _logger.LogInformation("[Domínio] Listando livros (busca={Busca})", busca);
        var livros = await _livroRepository.ListarAsync(busca);
        return livros.Select(ToDto);
    }

    public async Task<LivroResponseDto> ObterPorIdAsync(int id)
    {
        _logger.LogInformation("[Domínio] Buscando livro {Id}", id);
        return ToDto(await ObterOuFalharAsync(id));
    }

    public async Task<LivroResponseDto> CriarAsync(LivroCreateDto dto)
    {
        _logger.LogInformation("[Domínio] Validando regras para cadastrar livro '{Titulo}'", dto.Titulo);

        ValidarCampos(dto.Titulo, dto.Autor, dto.Isbn, dto.TotalExemplares);
        var isbn = NormalizarIsbn(dto.Isbn);

        if (await _livroRepository.ExisteIsbnAsync(isbn))
        {
            _logger.LogWarning("[Domínio] Regra violada: ISBN '{Isbn}' duplicado", isbn);
            throw new ConflictException($"Já existe um livro com o ISBN '{isbn}'");
        }

        var livro = new Livro
        {
            Titulo = dto.Titulo.Trim(),
            Autor = dto.Autor.Trim(),
            Isbn = isbn,
            TotalExemplares = dto.TotalExemplares
        };

        _livroRepository.Adicionar(livro);
        await SalvarTratandoConflitoAsync(isbn);

        _logger.LogInformation("[Domínio] Livro {Id} cadastrado, regras OK", livro.Id);
        return ToDto(livro);
    }

    public async Task<LivroResponseDto> EditarAsync(int id, LivroUpdateDto dto)
    {
        _logger.LogInformation("[Domínio] Validando regras para editar livro {Id}", id);

        ValidarCampos(dto.Titulo, dto.Autor, dto.Isbn, dto.TotalExemplares);
        var livro = await ObterOuFalharAsync(id);
        var isbn = NormalizarIsbn(dto.Isbn);

        if (await _livroRepository.ExisteIsbnAsync(isbn, id))
        {
            _logger.LogWarning("[Domínio] Regra violada: ISBN '{Isbn}' duplicado", isbn);
            throw new ConflictException($"Já existe um livro com o ISBN '{isbn}'");
        }

        // Regra que cruza Livro x Empréstimo
        if (dto.TotalExemplares < livro.EmprestimosAtivos)
        {
            _logger.LogWarning("[Domínio] Regra violada: total ({Total}) menor que emprestados ({Ativos})", dto.TotalExemplares, livro.EmprestimosAtivos);
            throw new RegraNegocioException(
                $"O livro tem {livro.EmprestimosAtivos} exemplar(es) emprestado(s); o total não pode ser menor que isso");
        }

        livro.Titulo = dto.Titulo.Trim();
        livro.Autor = dto.Autor.Trim();
        livro.Isbn = isbn;
        livro.TotalExemplares = dto.TotalExemplares;

        await SalvarTratandoConflitoAsync(isbn);

        _logger.LogInformation("[Domínio] Livro {Id} atualizado, regras OK", id);
        return ToDto(livro);
    }

    public async Task ExcluirAsync(int id)
    {
        _logger.LogInformation("[Domínio] Validando regras para excluir livro {Id}", id);

        var livro = await ObterOuFalharAsync(id);

        // Regra que cruza Livro x Empréstimo
        if (livro.EmprestimosAtivos > 0)
        {
            _logger.LogWarning("[Domínio] Regra violada: livro {Id} tem empréstimos ativos", id);
            throw new RegraNegocioException(
                $"O livro '{livro.Titulo}' tem {livro.EmprestimosAtivos} empréstimo(s) ativo(s) e não pode ser excluído");
        }

        _livroRepository.Remover(livro);
        await _livroRepository.SalvarAsync();

        _logger.LogInformation("[Domínio] Livro {Id} removido", id);
    }

    private async Task<Livro> ObterOuFalharAsync(int id)
    {
        var livro = await _livroRepository.ObterPorIdAsync(id);
        if (livro is null)
        {
            _logger.LogWarning("[Domínio] Livro {Id} não encontrado", id);
            throw new NotFoundException($"Livro {id} não encontrado");
        }
        return livro;
    }

    // O gRPC não passa pelas DataAnnotations dos DTOs, então o Domínio garante o mínimo sozinho.
    private static void ValidarCampos(string titulo, string autor, string isbn, int totalExemplares)
    {
        if (string.IsNullOrWhiteSpace(titulo)) throw new RegraNegocioException("O título é obrigatório");
        if (string.IsNullOrWhiteSpace(autor)) throw new RegraNegocioException("O autor é obrigatório");
        if (string.IsNullOrWhiteSpace(isbn)) throw new RegraNegocioException("O ISBN é obrigatório");
        if (totalExemplares < 1) throw new RegraNegocioException("O livro precisa ter pelo menos 1 exemplar");
    }

    private static string NormalizarIsbn(string isbn) => isbn.Replace("-", "").Replace(" ", "").Trim();

    // Rede de segurança: duas requisições simultâneas com o mesmo ISBN esbarram no índice UNIQUE.
    private async Task SalvarTratandoConflitoAsync(string isbn)
    {
        try { await _livroRepository.SalvarAsync(); }
        catch (DbUpdateException) { throw new ConflictException($"Já existe um livro com o ISBN '{isbn}'"); }
    }

    private static LivroResponseDto ToDto(Livro l) =>
        new(l.Id, l.Titulo, l.Autor, l.Isbn, l.TotalExemplares, l.ExemplaresDisponiveis);
}
