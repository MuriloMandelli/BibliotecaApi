using BibliotecaApi.DTOs;
using BibliotecaApi.Exceptions;
using BibliotecaApi.Models;
using BibliotecaApi.Repositories;

namespace BibliotecaApi.Services;

/// <summary>
/// Camada de Domínio de Empréstimo. Depende dos DOIS repositórios porque a regra
/// principal cruza os dois agregados: só empresta se o LIVRO tiver exemplar disponível
/// (equivalente à regra "a marca precisa estar ativa" da VeiculosApi).
/// </summary>
public class EmprestimoService : IEmprestimoService
{
    public const int LimiteEmprestimosPorLeitor = 3;

    private readonly IEmprestimoRepository _emprestimoRepository;
    private readonly ILivroRepository _livroRepository;
    private readonly ILogger<EmprestimoService> _logger;

    public EmprestimoService(IEmprestimoRepository emprestimoRepository, ILivroRepository livroRepository, ILogger<EmprestimoService> logger)
    {
        _emprestimoRepository = emprestimoRepository;
        _livroRepository = livroRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<EmprestimoResponseDto>> ListarAsync(int? livroId, bool? ativos)
    {
        _logger.LogInformation("[Domínio] Listando empréstimos (livroId={LivroId}, ativos={Ativos})", livroId, ativos);
        var emprestimos = await _emprestimoRepository.ListarAsync(livroId, ativos);
        return emprestimos.Select(ToDto);
    }

    public async Task<EmprestimoResponseDto> ObterPorIdAsync(int id)
    {
        _logger.LogInformation("[Domínio] Buscando empréstimo {Id}", id);
        return ToDto(await ObterOuFalharAsync(id));
    }

    public async Task<EmprestimoResponseDto> CriarAsync(EmprestimoCreateDto dto)
    {
        _logger.LogInformation("[Domínio] Validando regras para emprestar livro {LivroId} a '{Leitor}'", dto.LivroId, dto.NomeLeitor);

        if (string.IsNullOrWhiteSpace(dto.NomeLeitor))
            throw new RegraNegocioException("O nome do leitor é obrigatório");
        if (dto.PrazoDias is < 1 or > 30)
            throw new RegraNegocioException("O prazo do empréstimo deve ser entre 1 e 30 dias");

        var nomeLeitor = dto.NomeLeitor.Trim();

        // Regra que cruza Empréstimo x Livro: consulta o OUTRO agregado
        var livro = await _livroRepository.ObterPorIdAsync(dto.LivroId);
        if (livro is null)
        {
            _logger.LogWarning("[Domínio] Regra violada: livro {LivroId} não existe", dto.LivroId);
            throw new NotFoundException($"Livro {dto.LivroId} não encontrado");
        }

        if (livro.ExemplaresDisponiveis <= 0)
        {
            _logger.LogWarning("[Domínio] Regra violada: livro '{Titulo}' sem exemplares disponíveis", livro.Titulo);
            throw new RegraNegocioException($"O livro '{livro.Titulo}' não tem exemplares disponíveis no momento");
        }

        if (await _emprestimoRepository.ContarAtivosDoLeitorAsync(nomeLeitor) >= LimiteEmprestimosPorLeitor)
        {
            _logger.LogWarning("[Domínio] Regra violada: '{Leitor}' já atingiu o limite de empréstimos", nomeLeitor);
            throw new RegraNegocioException(
                $"O leitor '{nomeLeitor}' já tem {LimiteEmprestimosPorLeitor} empréstimos ativos (limite)");
        }

        var agora = DateTime.UtcNow;
        var emprestimo = new Emprestimo
        {
            LivroId = livro.Id,
            Livro = livro,
            NomeLeitor = nomeLeitor,
            DataEmprestimo = agora,
            DataPrevistaDevolucao = agora.AddDays(dto.PrazoDias)
        };

        _emprestimoRepository.Adicionar(emprestimo);
        await _emprestimoRepository.SalvarAsync();

        _logger.LogInformation("[Domínio] Empréstimo {Id} criado, regras OK", emprestimo.Id);
        return ToDto(emprestimo);
    }

    public async Task<EmprestimoResponseDto> DevolverAsync(int id)
    {
        _logger.LogInformation("[Domínio] Registrando devolução do empréstimo {Id}", id);

        var emprestimo = await ObterOuFalharAsync(id);
        if (!emprestimo.Ativo)
        {
            _logger.LogWarning("[Domínio] Regra violada: empréstimo {Id} já foi devolvido", id);
            throw new RegraNegocioException($"O empréstimo {id} já foi devolvido");
        }

        emprestimo.DataDevolucao = DateTime.UtcNow;
        await _emprestimoRepository.SalvarAsync();

        _logger.LogInformation("[Domínio] Empréstimo {Id} devolvido", id);
        return ToDto(emprestimo);
    }

    public async Task ExcluirAsync(int id)
    {
        _logger.LogInformation("[Domínio] Validando regras para excluir empréstimo {Id}", id);

        var emprestimo = await ObterOuFalharAsync(id);
        if (emprestimo.Ativo)
        {
            _logger.LogWarning("[Domínio] Regra violada: empréstimo {Id} ainda está ativo", id);
            throw new RegraNegocioException("Não é possível excluir um empréstimo ativo; registre a devolução antes");
        }

        _emprestimoRepository.Remover(emprestimo);
        await _emprestimoRepository.SalvarAsync();

        _logger.LogInformation("[Domínio] Empréstimo {Id} removido", id);
    }

    private async Task<Emprestimo> ObterOuFalharAsync(int id)
    {
        var emprestimo = await _emprestimoRepository.ObterPorIdAsync(id);
        if (emprestimo is null)
        {
            _logger.LogWarning("[Domínio] Empréstimo {Id} não encontrado", id);
            throw new NotFoundException($"Empréstimo {id} não encontrado");
        }
        return emprestimo;
    }

    // SQLite devolve DateTime sem Kind; aqui garantimos que sai sempre como UTC.
    private static DateTime Utc(DateTime d) => DateTime.SpecifyKind(d, DateTimeKind.Utc);

    private static EmprestimoResponseDto ToDto(Emprestimo e) => new(
        e.Id, e.LivroId, e.Livro?.Titulo ?? string.Empty, e.NomeLeitor,
        Utc(e.DataEmprestimo), Utc(e.DataPrevistaDevolucao),
        e.DataDevolucao is null ? null : Utc(e.DataDevolucao.Value), e.Ativo);
}
