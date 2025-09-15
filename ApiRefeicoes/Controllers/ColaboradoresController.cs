using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ApiRefeicoes.Data;
using ApiRefeicoes.Models;
using ApiRefeicoes.Services;

namespace ApiRefeicoes.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ColaboradoresController : ControllerBase
    {
        private readonly ApiRefeicoesDbContext _context;
        private readonly FaceApiService _faceApiService;

        public ColaboradoresController(ApiRefeicoesDbContext context, FaceApiService faceApiService)
        {
            _context = context;
            _faceApiService = faceApiService;
        }

        // GET: api/Colaboradores
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Colaborador>>> GetColaboradores()
        {
            return await _context.Colaboradores
                                 .Include(c => c.Departamento)
                                 .Include(c => c.Funcao)
                                 .ToListAsync();
        }

        // GET: api/Colaboradores/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Colaborador>> GetColaborador(int id)
        {
            var colaborador = await _context.Colaboradores
                                            .Include(c => c.Departamento)
                                            .Include(c => c.Funcao)
                                            .FirstOrDefaultAsync(c => c.Id == id);

            if (colaborador == null)
            {
                return NotFound();
            }

            return colaborador;
        }

        // POST: api/Colaboradores
        [HttpPost]
        public async Task<ActionResult<Colaborador>> PostColaborador([FromForm] ColaboradorDto colaboradorDto)
        {
            var colaborador = new Colaborador
            {
                Nome = colaboradorDto.Nome,
                CartaoPonto = colaboradorDto.CartaoPonto,
                DepartamentoId = colaboradorDto.DepartamentoId,
                FuncaoId = colaboradorDto.FuncaoId,
            };

            if (colaboradorDto.FotoFile != null && colaboradorDto.FotoFile.Length > 0)
            {
                using var memoryStream = new MemoryStream();
                await colaboradorDto.FotoFile.CopyToAsync(memoryStream);
                var imageBytes = memoryStream.ToArray();

                colaborador.Foto = imageBytes;
                colaborador.AzureId = await _faceApiService.DetectFaceAndGetId(imageBytes);
            }

            _context.Colaboradores.Add(colaborador);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetColaborador", new { id = colaborador.Id }, colaborador);
        }

        // PUT: api/Colaboradores/5  <-- MOVIDO PARA DENTRO DA CLASSE
        [HttpPut("{id}")]
        public async Task<IActionResult> PutColaborador(int id, [FromBody] ColaboradorUpdateDto colaboradorDto)
        {
            var colaborador = await _context.Colaboradores.FindAsync(id);

            if (colaborador == null)
            {
                return NotFound();
            }

            // Atualiza apenas os campos que foram preenchidos no formulário
            colaborador.Nome = colaboradorDto.Nome;
            colaborador.CartaoPonto = colaboradorDto.CartaoPonto;
            colaborador.DepartamentoId = colaboradorDto.DepartamentoId;
            colaborador.FuncaoId = colaboradorDto.FuncaoId;

            _context.Entry(colaborador).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ColaboradorExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent(); // Retorna sucesso sem conteúdo
        }


        // PUT: api/Colaboradores/5/foto
        [HttpPut("{id}/foto")]
        public async Task<IActionResult> PutColaboradorFoto(int id, IFormFile fotoFile)
        {
            var colaborador = await _context.Colaboradores.FindAsync(id);
            if (colaborador == null)
            {
                return NotFound("Colaborador não encontrado.");
            }

            if (fotoFile == null || fotoFile.Length == 0)
            {
                return BadRequest("Nenhuma foto foi enviada.");
            }

            using var memoryStream = new MemoryStream();
            await fotoFile.CopyToAsync(memoryStream);
            var imageBytes = memoryStream.ToArray();

            var novoAzureId = await _faceApiService.DetectFaceAndGetId(imageBytes);

            if (novoAzureId == null)
            {
                return BadRequest("Não foi possível detectar um rosto na imagem enviada.");
            }

            string? aviso = null;
            if (!string.IsNullOrEmpty(colaborador.AzureId) && colaborador.AzureId != novoAzureId)
            {
                aviso = "Aviso: A nova foto parece ser de uma pessoa diferente da anterior.";
            }

            colaborador.Foto = imageBytes;
            colaborador.AzureId = novoAzureId;

            _context.Entry(colaborador).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            if (aviso != null)
            {
                return Ok(new { message = "Foto do colaborador atualizada com sucesso.", warning = aviso });
            }

            return Ok(new { message = "Foto do colaborador atualizada com sucesso." });
        }


        // DELETE: api/Colaboradores/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteColaborador(int id)
        {
            var colaborador = await _context.Colaboradores.FindAsync(id);
            if (colaborador == null)
            {
                return NotFound();
            }

            _context.Colaboradores.Remove(colaborador);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ColaboradorExists(int id)
        {
            return _context.Colaboradores.Any(e => e.Id == id);
        }
    }

    // DTO para a criação (POST)
    public class ColaboradorDto
    {
        public string Nome { get; set; }
        public string CartaoPonto { get; set; }
        public int DepartamentoId { get; set; }
        public int FuncaoId { get; set; }
        public IFormFile? FotoFile { get; set; }
    }

    // DTO para a atualização (PUT)
    public class ColaboradorUpdateDto
    {
        public string Nome { get; set; }
        public string CartaoPonto { get; set; }
        public int DepartamentoId { get; set; }
        public int FuncaoId { get; set; }
    }
}