using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ordersmanagement.Models;
using ordersmanagement.Models.requests;
using OrdersManagement.Data;

namespace ordersmanagement.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SucursalesController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public SucursalesController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<ActionResult<object>> GetSucursales([FromQuery] int? clienteId)
        {
            try
            {
                var query = _db.Sucursales
                     .Include(c => c.Cliente)
                    .AsQueryable();

                // Si mandan el clienteId en el query string, filtramos inmediatamente
                if (clienteId.HasValue)
                {
                    query = query.Where(s => s.ClienteId == clienteId.Value);
                }

                var sucursales = await query
                    .Select(s => new
                    {
                        s.Id,
                        Value = s.Id.ToString(), // Listo para mapear en el value de tu AutoInput de React
                        s.Name,
                        s.Direccion,
                        s.Telefono,
                        s.Latitud,
                        s.Longitud,
                        s.ClienteId,
                        nombreCliente =  s.Cliente  != null ? s.Cliente.Nombre:null,
                    })
                    .OrderBy(s => s.Name)
                    .ToListAsync();

                return Ok(sucursales);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener sucursales", error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<ActionResult<object>> CreateSucursal([FromBody] Sucursal nuevaSucursal)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);

                // Validación de integridad referencial manual en BD
                var clienteExiste = await _db.Clientes.AnyAsync(c => c.Id == nuevaSucursal.ClienteId);
                if (!clienteExiste)
                {
                    return BadRequest(new { mensaje = $"El cliente con ID {nuevaSucursal.ClienteId} no existe en el sistema." });
                }

                await _db.Sucursales.AddAsync(nuevaSucursal);
                await _db.SaveChangesAsync();

                var sucursalCreada = new
                {
                    nuevaSucursal.Id,
                    nuevaSucursal.Name,
                    nuevaSucursal.Direccion,
                    nuevaSucursal.Telefono,
                    nuevaSucursal.Latitud,
                    nuevaSucursal.Longitud,
                    nuevaSucursal.ClienteId
                };

                return Ok(sucursalCreada);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al crear la sucursal", error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSucursal(int id, [FromBody] RequestUpdateSucursal sucursalActualizada)
        {
            try
            {
                var sucursalExiste = await _db.Sucursales.FindAsync(id);
                if (sucursalExiste == null)
                {
                    return NotFound(new { mensaje = $"Equipo con ID {id} no encontrado" });
                }
                if (!ModelState.IsValid) return BadRequest(ModelState);

                sucursalExiste.Name = sucursalActualizada.Name;
                sucursalExiste.Direccion = sucursalActualizada.Direccion;
                sucursalExiste.Telefono = sucursalActualizada.Telefono;
                sucursalExiste.Latitud = sucursalActualizada.Latitud;
                sucursalExiste.Longitud = sucursalActualizada.Longitud;

                // Marcamos como modificado para que EF guarde todas las columnas (incluyendo geolocalización)
                _db.Entry(sucursalExiste).State = EntityState.Modified;
                await _db.SaveChangesAsync();

                return Ok(new { mensaje = $"Sucursal #{id} modificada por completo de forma exitosa", sucursal = sucursalExiste });
            }
            catch (DbUpdateConcurrencyException)
            {
                return StatusCode(409, new { mensaje = "Error de concurrencia: la sucursal fue editada por otro usuario simultáneamente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error interno al actualizar la sucursal", error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSucursal(int id)
        {
            try
            {
                var sucursal = await _db.Sucursales.FindAsync(id);
                if (sucursal == null) return NotFound(new { mensaje = $"Equipo con ID {id} no encontrado", equipoId = id });

                _db.Sucursales.Remove(sucursal);
                await _db.SaveChangesAsync();

                return Ok(new 
                { 
                    mensaje = $"Equipo eliminado exitosamente",
                });
            }
            catch (DbUpdateConcurrencyException)
            {
                return StatusCode(409, new { mensaje = "Error de concurrencia. El equipo fue modificado o eliminado por otro usuario", equipoId = id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error interno del servidor al eliminar el equipo", error = ex.Message, equipoId = id });
            }
        }
        
    }
}