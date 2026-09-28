using Microsoft.AspNetCore.Mvc;
using BibliotecaApi.DTOs;
using BibliotecaApi.Services;

namespace BibliotecaApi.Controllers;

/// <summary>Camada de Apresentação (REST) de Empréstimo. Só delega para IEmprestimoService.</summary>
[ApiController]
[Route("api/emprestimos")]
public class EmprestimosController : ControllerBase
{
    private readonly IEmprestimoService _emprestimoService;
    private readonly ILogger<EmprestimosController> _logger;

    public EmprestimosController(IEmprestimoService emprestimoService, ILogger<EmprestimosController> logger)
    {
        _emprestimoService = emprestimoService;
        _logger = logger;
    }

    /// <summary>GET /api/emprestimos: lista empréstimos, filtrando por livro e/ou só ativos.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<EmprestimoResponseDto>>> Listar(
        [FromQuery] int? livroId, [FromQuery] bool? ativos)
    {
        _logger.LogInformation("[Apresentação-REST] GET /api/emprestimos (livroId={LivroId}, ativos={Ativos})", livroId, ativos);
        return Ok(await _emprestimoService.ListarAsync(livroId, ativos));
    }

    /// <summary>GET /api/emprestimos/{id}</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<EmprestimoResponseDto>> ObterPorId(int id)
    {
        _logger.LogInformation("[Apresentação-REST] GET /api/emprestimos/{Id}", id);
        return Ok(await _emprestimoService.ObterPorIdAsync(id));
    }

    /// <summary>POST /api/emprestimos: empresta um livro (precisa ter exemplar disponível).</summary>
    [HttpPost]
    public async Task<ActionResult<EmprestimoResponseDto>> Criar(EmprestimoCreateDto dto)
    {
        _logger.LogInformation("[Apresentação-REST] POST /api/emprestimos (livroId={LivroId})", dto.LivroId);
        var emprestimo = await _emprestimoService.CriarAsync(dto);
        return CreatedAtAction(nameof(ObterPorId), new { id = emprestimo.Id }, emprestimo);
    }

    /// <summary>POST /api/emprestimos/{id}/devolucao: registra a devolução do livro.</summary>
    [HttpPost("{id:int}/devolucao")]
    public async Task<ActionResult<EmprestimoResponseDto>> Devolver(int id)
    {
        _logger.LogInformation("[Apresentação-REST] POST /api/emprestimos/{Id}/devolucao", id);
        return Ok(await _emprestimoService.DevolverAsync(id));
    }

    /// <summary>DELETE /api/emprestimos/{id}: remove um empréstimo já devolvido do histórico.</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Excluir(int id)
    {
        _logger.LogInformation("[Apresentação-REST] DELETE /api/emprestimos/{Id}", id);
        await _emprestimoService.ExcluirAsync(id);
        return NoContent();
    }
}
