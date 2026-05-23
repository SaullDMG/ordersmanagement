using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ordersmanagement.Models.requests;
using OrdersManagement.Data;
using OrdersManagement.Models;
using System.Text.Json;

namespace OrdersManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EquipoController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public EquipoController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET: api/equipo
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Equipo>>> GetEquipos([FromQuery] string? clienteId)
        {
            try
            {
                // 1. Creamos la consulta base como IQueryable (Aún no va a la BD)
                var query = _db.Equipos
                    .Include(c => c.Cliente)
                    .Include(e => e.OrdenesServicio)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(clienteId))
                {
                    query = query.Where(x => x.ClienteId == int.Parse(clienteId));
                }

                var equipos = await query.ToListAsync();
                
                if (equipos == null || equipos.Count == 0)
                {
                    return Ok(Array.Empty<Equipo>());
                }

                var equiposSinCiclos = equipos.Select(e => new
                {
                    e.Id, // Modificado
                    e.Marca,
                    e.Modelo,
                    e.Serie,
                    e.TipoEquipo,
                    e.ClienteId,
                    nombreCliente =  e.Cliente  != null ? e.Cliente.Nombre:null,
                    Cliente = e.Cliente != null ? new
                    {
                        e.Cliente.Id, // Modificado
                        e.Cliente.Nombre,
                        e.Cliente.Direccion,
                        e.Cliente.Telefono
                    } : null,
                    OrdenesServicio = e.OrdenesServicio != null ? e.OrdenesServicio.Select(o => new
                    {
                        o.Id, // Modificado
                        o.Falla,
                        o.FechaCreacion,
                        o.Estado,
                        o.Prioridad,
                        o.Presupuesto
                    }) : null
                });
                
                // return Ok(new 
                // { 
                //     mensaje = "Equipos obtenidos exitosamente",
                //     total = equipos.Count,
                //     equipos = equiposSinCiclos 
                // });
                return Ok(equiposSinCiclos);

            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener equipos", error = ex.Message });
            }
        }

        // GET: api/equipo/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Equipo>> GetEquipo(int id)
        {
            try
            {
                var equipo = await _db.Equipos
                    .Include(e => e.Cliente)
                    .Include(e => e.OrdenesServicio)
                    .FirstOrDefaultAsync(e => e.Id == id); // Modificado
                
                if (equipo == null)
                {
                    return NotFound(new { mensaje = $"Equipo con ID {id} no encontrado" });
                }

                var equipoSinCiclo = new
                {
                    equipo.Id, // Modificado
                    equipo.Marca,
                    equipo.Modelo,
                    equipo.Serie,
                    equipo.TipoEquipo,
                    equipo.ClienteId,
                    Cliente = equipo.Cliente != null ? new
                    {
                        equipo.Cliente.Id, // Modificado
                        equipo.Cliente.Nombre,
                        equipo.Cliente.Direccion,
                        equipo.Cliente.Telefono
                    } : null,
                    OrdenesServicio = equipo.OrdenesServicio != null && equipo.OrdenesServicio.Any() 
                        ? equipo.OrdenesServicio.Select(o => new
                        {
                            o.Id, // Modificado
                            o.Falla,
                            o.FechaCreacion,
                            o.Estado,
                            o.Prioridad,
                            o.Presupuesto
                        }).ToList()
                        : null
                };
                
                return Ok(new 
                { 
                    mensaje = "Equipo encontrado",
                    equipo = equipoSinCiclo 
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al buscar equipo", error = ex.Message });
            }
        }

        // GET: api/equipo/cliente/{clienteId}
        [HttpGet("cliente/{clienteId}")]
        public async Task<ActionResult<object>> GetEquiposPorCliente(int clienteId)
        {
            try
            {
                var cliente = await _db.Clientes
                    .Where(c => c.Id == clienteId) // Modificado
                    .Select(c => new
                    {
                        c.Id, // Modificado
                        c.Nombre,
                        c.Direccion,
                        c.Telefono
                    })
                    .FirstOrDefaultAsync();
                    
                if (cliente == null)
                {
                    return NotFound(new { mensaje = $"Cliente con ID {clienteId} no encontrado" });
                }

                var equipos = await _db.Equipos
                    .Where(e => e.ClienteId == clienteId)
                    .Include(e => e.OrdenesServicio)
                    .Select(e => new
                    {
                        e.Id, // Modificado
                        e.Marca,
                        e.Modelo,
                        e.Serie,
                        e.TipoEquipo,
                        e.ClienteId,
                        OrdenesServicio = e.OrdenesServicio != null && e.OrdenesServicio.Any()
                            ? e.OrdenesServicio.Select(o => new
                            {
                                o.Id, // Modificado
                                o.Falla,
                                o.FechaCreacion,
                                o.Estado,
                                o.Prioridad,
                                o.Presupuesto
                            }).ToList()
                            : null,
                        TotalOrdenes = e.OrdenesServicio != null ? e.OrdenesServicio.Count : 0,
                        OrdenesPendientes = e.OrdenesServicio != null 
                            ? e.OrdenesServicio.Count(o => o.Estado != "Finalizada" && o.Estado != "Cancelada")
                            : 0
                    })
                    .ToListAsync();
                
                if (equipos == null || equipos.Count == 0)
                {
                    return Ok(new 
                    { 
                        mensaje = $"El cliente {cliente.Nombre} no tiene equipos registrados",
                        cliente = cliente,
                        totalEquipos = 0,
                        equipos = new List<object>()
                    });
                }
                
                return Ok(new 
                { 
                    mensaje = $"Equipos del cliente {cliente.Nombre}",
                    cliente = cliente,
                    totalEquipos = equipos.Count,
                    equipos = equipos
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener equipos del cliente", error = ex.Message });
            }
        }

        // GET: api/equipo/tipo/{tipo}
        [HttpGet("tipo/{tipo}")]
        public async Task<ActionResult<object>> GetEquiposPorTipo(string tipo)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(tipo))
                {
                    return BadRequest(new { mensaje = "El tipo de equipo es requerido" });
                }
                
                if (tipo != "Impresion" && tipo != "Computo")
                {
                    return BadRequest(new { mensaje = "El tipo debe ser 'Impresion' o 'Computo'", tipoInvalido = tipo });
                }

                var equipos = await _db.Equipos
                    .Where(e => e.TipoEquipo == tipo)
                    .Include(e => e.Cliente)
                    .Include(e => e.OrdenesServicio)
                    .Select(e => new
                    {
                        e.Id, // Modificado
                        e.Marca,
                        e.Modelo,
                        e.Serie,
                        e.TipoEquipo,
                        e.ClienteId,
                        Cliente = e.Cliente != null ? new
                        {
                            e.Cliente.Id, // Modificado
                            e.Cliente.Nombre,
                            e.Cliente.Direccion,
                            e.Cliente.Telefono
                        } : null,
                        OrdenesServicio = e.OrdenesServicio != null && e.OrdenesServicio.Any()
                            ? e.OrdenesServicio.Select(o => new
                            {
                                o.Id, // Modificado
                                o.Falla,
                                o.FechaCreacion,
                                o.Estado,
                                o.Prioridad,
                                o.Presupuesto
                            }).ToList()
                            : null,
                        TotalOrdenes = e.OrdenesServicio != null ? e.OrdenesServicio.Count : 0,
                        OrdenesActivas = e.OrdenesServicio != null 
                            ? e.OrdenesServicio.Count(o => o.Estado != "Finalizada" && o.Estado != "Cancelada")
                            : 0,
                        OrdenesFinalizadas = e.OrdenesServicio != null 
                            ? e.OrdenesServicio.Count(o => o.Estado == "Finalizada")
                            : 0
                    })
                    .OrderBy(e => e.Marca)
                    .ThenBy(e => e.Modelo)
                    .ToListAsync();
                
                if (equipos == null || equipos.Count == 0)
                {
                    return Ok(new 
                    { 
                        mensaje = $"No hay equipos de tipo {tipo} registrados",
                        tipo = tipo,
                        totalEquipos = 0,
                        equipos = new List<object>()
                    });
                }
                
                var estadisticas = new
                {
                    PorMarca = equipos
                        .GroupBy(e => e.Marca)
                        .Select(g => new { Marca = g.Key, Cantidad = g.Count() })
                        .OrderByDescending(g => g.Cantidad),
                    TotalClientes = equipos.Select(e => e.ClienteId).Distinct().Count(),
                    EquiposConOrdenes = equipos.Count(e => e.TotalOrdenes > 0),
                    EquiposSinOrdenes = equipos.Count(e => e.TotalOrdenes == 0)
                };
                
                return Ok(new 
                { 
                    mensaje = $"Equipos de tipo {tipo}",
                    tipo = tipo,
                    totalEquipos = equipos.Count,
                    estadisticas = estadisticas,
                    equipos = equipos
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener equipos por tipo", error = ex.Message });
            }
        }

        // POST: api/equipo
        [HttpPost]
        public async Task<ActionResult<Equipo>> CreateEquipo([FromBody] Equipo equipo)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new 
                    { 
                        mensaje = "Datos inválidos",
                        errores = ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)
                    });
                }

                var cliente = await _db.Clientes.FindAsync(equipo.ClienteId);
                if (cliente == null)
                {
                    return BadRequest(new { mensaje = $"Cliente con ID {equipo.ClienteId} no existe" });
                }

                var serieExiste = await _db.Equipos
                    .AnyAsync(e => e.Serie == equipo.Serie);
                
                if (serieExiste)
                {
                    return Conflict(new { mensaje = $"Ya existe un equipo con la serie {equipo.Serie}" });
                }

                if (equipo.OrdenesServicio == null)
                {
                    equipo.OrdenesServicio = new HashSet<OrdenServicio>();
                }

                await _db.Equipos.AddAsync(equipo);
                await _db.SaveChangesAsync();

                await _db.Entry(equipo).Reference(e => e.Cliente).LoadAsync();

                return CreatedAtAction(nameof(GetEquipo), new { id = equipo.Id }, new // Modificado
                { 
                    mensaje = "Equipo creado exitosamente",
                    equipo = new
                    {
                        equipo.Id, // Modificado
                        equipo.Marca,
                        equipo.Modelo,
                        equipo.Serie,
                        equipo.TipoEquipo,
                        equipo.ClienteId,
                        Cliente = new
                        {
                            cliente.Id, // Modificado
                            cliente.Nombre,
                            cliente.Direccion,
                            cliente.Telefono
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error interno del servidor", error = ex.Message });
            }
        }

        // PUT: api/equipo/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEquipo(int id, [FromBody] RequestUpdateEquiment equipo)
        {
            try
            {
                var equipoExistente = await _db.Equipos.FindAsync(id);
                
                if (equipoExistente == null)
                {
                    return NotFound(new { mensaje = $"Equipo con ID {id} no encontrado" });
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(new 
                    { 
                        mensaje = "Datos inválidos",
                        errores = ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)
                    });
                }


                var serieExiste = await _db.Equipos.AnyAsync(e => e.Serie == equipo.Serie && e.Id != id); // Modificado
                if (serieExiste)
                {
                    return Conflict(new { mensaje = $"La serie {equipo.Serie} ya está registrada en otro equipo" });
                }

                equipoExistente.Marca = equipo.Marca;
                equipoExistente.Modelo = equipo.Modelo;
                equipoExistente.Serie = equipo.Serie;
                equipoExistente.TipoEquipo = equipo.TipoEquipo;

                _db.Entry(equipoExistente).State = EntityState.Modified;
                await _db.SaveChangesAsync();

                var equipoActualizado = new
                {
                    equipoExistente.Id, // Modificado
                    equipoExistente.Marca,
                    equipoExistente.Modelo,
                    equipoExistente.Serie,
                    equipoExistente.TipoEquipo,
                    equipoExistente.ClienteId,
                };

                return Ok(new 
                { 
                    mensaje = "Equipo actualizado exitosamente",
                    equipo = equipoActualizado
                });
            }
            catch (DbUpdateConcurrencyException)
            {
                return StatusCode(500, new { mensaje = "Error de concurrencia al actualizar el equipo" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error interno del servidor", error = ex.Message });
            }
        }

        // DELETE: api/equipo/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEquipo(int id)
        {
            try
            {
                var equipo = await _db.Equipos
                    .Include(e => e.Cliente)
                    .Include(e => e.OrdenesServicio)
                    .FirstOrDefaultAsync(e => e.Id == id); // Modificado
                
                if (equipo == null)
                {
                    return NotFound(new { mensaje = $"Equipo con ID {id} no encontrado", equipoId = id });
                }

                if (equipo.OrdenesServicio != null && equipo.OrdenesServicio.Any())
                {
                    var ordenesInfo = equipo.OrdenesServicio.Select(o => new
                    {
                        o.Id, // Modificado
                        o.Falla,
                        o.Estado,
                        o.FechaCreacion,
                        o.Prioridad,
                        o.Presupuesto
                    }).ToList();
                    
                    return BadRequest(new 
                    { 
                        mensaje = "No se puede eliminar el equipo porque tiene órdenes de servicio asociadas",
                        equipoId = id,
                        totalOrdenes = equipo.OrdenesServicio.Count,
                        ordenesActivas = equipo.OrdenesServicio.Count(o => o.Estado != "Finalizada" && o.Estado != "Cancelada"),
                        ordenes = ordenesInfo,
                        sugerencia = "Primero elimine o finalice las órdenes de servicio asociadas"
                    });
                }

                var equipoInfo = new
                {
                    equipo.Id, // Modificado
                    equipo.Marca,
                    equipo.Modelo,
                    equipo.Serie,
                    equipo.TipoEquipo,
                    Cliente = equipo.Cliente != null ? new
                    {
                        equipo.Cliente.Id, // Modificado
                        equipo.Cliente.Nombre,
                        equipo.Cliente.Direccion,
                        equipo.Cliente.Telefono
                    } : null
                };

                _db.Equipos.Remove(equipo);
                await _db.SaveChangesAsync();

                return Ok(new 
                { 
                    mensaje = $"Equipo eliminado exitosamente",
                    equipoEliminado = equipoInfo,
                    fechaEliminacion = DateTime.Now
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

        // GET: api/equipo/sin-ordenes
        [HttpGet("sin-ordenes")]
        public async Task<ActionResult<IEnumerable<Equipo>>> GetEquiposSinOrdenes()
        {
            try
            {
                var equipos = await _db.Equipos
                    .Where(e => e.OrdenesServicio == null || !e.OrdenesServicio.Any())
                    .Include(e => e.Cliente)
                    .ToListAsync();
                
                if (equipos == null || equipos.Count == 0)
                {
                    return NotFound(new { mensaje = "Todos los equipos tienen órdenes de servicio asociadas" });
                }

                var equiposMapeados = equipos.Select(e => new
                {
                    e.Id, // Modificado
                    e.Marca,
                    e.Modelo,
                    e.Serie,
                    e.TipoEquipo,
                    e.ClienteId,
                    Cliente = e.Cliente != null ? new
                    {
                        e.Cliente.Id, // Modificado
                        e.Cliente.Nombre,
                        e.Cliente.Direccion,
                        e.Cliente.Telefono
                    } : null
                });
                
                return Ok(new 
                { 
                    mensaje = "Equipos sin órdenes de servicio",
                    total = equipos.Count,
                    equipos = equiposMapeados 
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener equipos sin órdenes", error = ex.Message });
            }
        }
    }
}