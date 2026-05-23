using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrdersManagement.Data;
using OrdersManagement.Models;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using ordersmanagement.Models.requests;

namespace OrdersManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsuarioController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public UsuarioController(ApplicationDbContext db)
        {
            _db = db;
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        [HttpGet]
        public async Task<ActionResult<object>> GetUsuarios([FromQuery] string? role)
        {
            try
            {
                // 1. Creamos la consulta base como IQueryable (Aún no va a la BD)
                var query = _db.Usuarios.Include(c => c.OrdenesServicio).AsQueryable();

                // 2. Evaluamos dinámicamente: Si viene un rol en la URL, filtramos la consulta
                if (!string.IsNullOrWhiteSpace(role))
                {
                    // Nota: Usamos StringComparison por si mandan "tecnico" o "Tecnico"
                    query = query.Where(u => u.Rol != null && u.Rol.ToUpper() == role.ToUpper());
                }

                // 3. Ahora sí, ejecutamos la consulta ordenada en la Base de Datos
                var usuariosDb = await query.OrderBy(c => c.Fullname).ToListAsync();

                if (usuariosDb.Count == 0)
                {
                    return Ok(Array.Empty<object>());
                }

                // 4. Mapeamos al objeto anónimo con la estructura limpia
                var usuarios = usuariosDb.Select(u => new
                {
                    u.Id,
                    u.Fullname,
                    u.Correo,
                    u.Rol,
                    u.Especialidad,
                    u.Telefono,
                    OrdenesServicio = u.OrdenesServicio != null && u.OrdenesServicio.Any()
                        ? new
                        {
                            Total = u.OrdenesServicio.Count,
                            Pendientes = u.OrdenesServicio.Count(o => o.Estado == "Pendiente"),
                            Finalizadas = u.OrdenesServicio.Count(o => o.Estado == "Finalizada"),
                            UltimasOrdenes = u.OrdenesServicio
                                .OrderByDescending(o => o.FechaCreacion).Take(5)
                                .Select(o => new { o.Id, o.Falla, o.Estado })
                        } : null
                }).ToList();

                return Ok(usuarios);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetUsuario(int id)
        {
            try
            {
                var usuario = await _db.Usuarios
                    .Include(u => u.OrdenesServicio)
                    .Where(u => u.Id == id) // Antes: UsuarioId
                    .Select(u => new
                    {
                        u.Id, // Antes: UsuarioId
                        u.Fullname,
                        u.Correo,
                        u.Rol,
                        u.Especialidad,
                        u.Telefono,
                        OrdenesServicio = u.OrdenesServicio != null ? u.OrdenesServicio.Select(o => new { o.Id, o.Falla, o.Estado }) : null // Antes: OrdenServicioId
                    }).FirstOrDefaultAsync();

                if (usuario == null) return NotFound();
                return Ok(usuario);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<ActionResult<object>> CreateUsuario([FromBody] Usuario usuario)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);

                var correoExiste = await _db.Usuarios.AnyAsync(u => u.Correo == usuario.Correo);
                if (correoExiste) return Conflict(new { mensaje = "El correo electronico previamente registrado" });

                usuario.Contraseña = HashPassword(usuario.Contraseña);
                await _db.Usuarios.AddAsync(usuario);
                await _db.SaveChangesAsync();

                return CreatedAtAction(nameof(GetUsuario), new { id = usuario.Id }, new { mensaje = "Usuario creado con éxito", id = usuario.Id }); // Antes: UsuarioId
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUsuario(int id, [FromBody] RequestUpdateUser usuario)
        {
            try
            {
                // if (id != usuario.Id) return BadRequest(); // Antes: UsuarioId
                var existente = await _db.Usuarios.FindAsync(id);
                if (existente == null) return NotFound();

                existente.Fullname = usuario.Fullname;
                existente.Correo = usuario.Correo;
                existente.Rol = usuario.Rol;
                existente.Especialidad = usuario.Especialidad;
                existente.Telefono = usuario.Telefono;

                if (!string.IsNullOrEmpty(usuario.Contraseña))
                {
                    existente.Contraseña = HashPassword(usuario.Contraseña);
                }

                await _db.SaveChangesAsync();
                return Ok(new { mensaje = "Usuario actualizado exitosamente" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n[USR ERROR]: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUsuario(int id)
        {
            try
            {
                var usuario = await _db.Usuarios.FindAsync(id);
                if (usuario == null) return NotFound();

                _db.Usuarios.Remove(usuario);
                await _db.SaveChangesAsync();
                return Ok(new { mensaje = "Usuario eliminado" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

    }
}