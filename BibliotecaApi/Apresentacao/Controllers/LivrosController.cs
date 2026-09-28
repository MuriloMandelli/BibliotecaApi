using Microsoft.AspNetCore.Mvc;
using BibliotecaApi.DTOs;
using BibliotecaApi.Services;

namespace BibliotecaApi.Controllers;

/// <summary>
/// Camada de Apresentação (REST): só entende HTTP (rotas, verbos, status de sucesso).
/// Não conhece EF Core nem regra de negócio, e não decide status de erro: isso é do
/// ExceptionHandlingMiddleware.
/// </summary>
[ApiController]
[Route("api/livros")]
public class LivrosController : ControllerBase
{
    private readonly ILivroService _livroService;
    private readonly ILogger<LivrosController> _logger;

    public LivrosController(ILivroService livroService, ILogger<LivrosController> logger)
    {
        _livroService = livroService;
        _logger = logger;
    }

    /// <summary>GET /api/livros: lista livros, com busca opcional por título ou autor.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<LivroResponseDto>>> Listar([FromQuery] string? busca)
    {
        _logger.LogInformation("[Apresentação-REST] GET /api/livros (busca={Busca})", busca);
        return Ok(await _livroService.ListarAsync(busca));
    }

    /// <summary>GET /api/livros/{id}</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<LivroResponseDto>> ObterPorId(int id)
    {
        _logger.LogInformation("[Apresentação-REST] GET /api/livros/{Id}", id);
        return Ok(await _livroService.ObterPorIdAsync(id));
    }

    /// <summary>POST /api/livros: cadastra um livro (ISBN precisa ser único).</summary>
    [HttpPost]
    public async Task<ActionResult<LivroResponseDto>> Criar(LivroCreateDto dto)
    {
        _logger.LogInformation("[Apresentação-REST] POST /api/livros ('{Titulo}')", dto.Titulo);
        var livro = await _livroService.CriarAsync(dto);
        return CreatedAtAction(nameof(ObterPorId), new { id = livro.Id }, livro);
    }

    /// <summary>PUT /api/livros/{id}: edita um livro (total não pode ficar abaixo dos exemplares emprestados).</summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<LivroResponseDto>> Editar(int id, LivroUpdateDto dto)
    {
        _logger.LogInformation("[Apresentação-REST] PUT /api/livros/{Id}", id);
        return Ok(await _livroService.EditarAsync(id, dto));
    }

    /// <summary>DELETE /api/livros/{id}: exclui um livro sem empréstimos ativos.</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Excluir(int id)
    {
        _logger.LogInformation("[Apresentação-REST] DELETE /api/livros/{Id}", id);
        await _livroService.ExcluirAsync(id);
        return NoContent();
    }
}
