using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        // Listar todos los equipos
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Equipo>>> GetEquipos()
        {
            try
            {
                var equipos = await _db.Equipos
                    .Include(e => e.Cliente)           // Incluir cliente relacionado
                    .Include(e => e.OrdenesServicio)    // Incluir órdenes de servicio
                    .ToListAsync();
                
                if (equipos == null || equipos.Count == 0)
                {
                    return NotFound(new { mensaje = "No hay equipos registrados" });
                }
                //retornar la lista sin referencia circular
                            var equiposSinCiclos = equipos.Select(e => new
                    {
                        e.EquipoId,
                        e.Marca,
                        e.Modelo,
                        e.Serie,
                        e.TipoEquipo,
                        e.ClienteId,
                        Cliente = e.Cliente != null ? new
                        {
                            e.Cliente.ClienteId,
                            e.Cliente.Nombre,
                            e.Cliente.Direccion,
                            e.Cliente.Telefono
                            // No incluir Equipos aquí para evitar el ciclo
                        } : null,
                        OrdenesServicio = e.OrdenesServicio != null ? e.OrdenesServicio.Select(o => new
                        {
                            o.OrdenServicioId,
                            o.Falla,
                            o.FechaCreacion,
                            o.Estado,
                            o.Prioridad,
                            o.Presupuesto
                            
                        }) : null
                    });
                
                return Ok(new 
                { 
                    mensaje = "Equipos obtenidos exitosamente",
                    total = equipos.Count,
                    equipos = equiposSinCiclos 
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener equipos", error = ex.Message });
            }
        }

        // GET: api/equipo/{id}
        // Buscar equipo por ID
        [HttpGet("{id}")]
        public async Task<ActionResult<Equipo>> GetEquipo(int id)
        {
            try
            {
                var equipo = await _db.Equipos
                    .Include(e => e.Cliente)
                    .Include(e => e.OrdenesServicio)
                    .FirstOrDefaultAsync(e => e.EquipoId == id);
                
                if (equipo == null)
                {
                    return NotFound(new { mensaje = $"Equipo con ID {id} no encontrado" });
                }
                //Evita la referencia circular al retornar el equipo
                    var equipoSinCiclo = new
                    {
                        equipo.EquipoId,
                        equipo.Marca,
                        equipo.Modelo,
                        equipo.Serie,
                        equipo.TipoEquipo,
                        equipo.ClienteId,
                    
                        Cliente = equipo.Cliente != null ? new
                        {
                            equipo.Cliente.ClienteId,
                            equipo.Cliente.Nombre,
                            equipo.Cliente.Direccion,
                            equipo.Cliente.Telefono
                            
                        } : null,
                        // Información de órdenes de servicio (sin incluir referencias circulares)
                        OrdenesServicio = equipo.OrdenesServicio != null && equipo.OrdenesServicio.Any() 
                            ? equipo.OrdenesServicio.Select(o => new
                            {
                                o.OrdenServicioId,
                                o.Falla,
                                o.FechaCreacion,
                                o.Estado,
                                o.Prioridad,
                                o.Presupuesto
                                // No incluir referencias al equipo aquí
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
        // Listar equipos por cliente
        [HttpGet("cliente/{clienteId}")]
        public async Task<ActionResult<object>> GetEquiposPorCliente(int clienteId)
        {
            try
            {
                // Verificar si el cliente existe
                var cliente = await _db.Clientes
                    .Where(c => c.ClienteId == clienteId)
                    .Select(c => new
                    {
                        c.ClienteId,
                        c.Nombre,
                        c.Direccion,
                        c.Telefono
                        // No incluir Equipos aquí para evitar el ciclo
                    })
                    .FirstOrDefaultAsync();
                    
                if (cliente == null)
                {
                    return NotFound(new { mensaje = $"Cliente con ID {clienteId} no encontrado" });
                }

                // Obtener equipos del cliente sin incluir el cliente en cada equipo
                var equipos = await _db.Equipos
                    .Where(e => e.ClienteId == clienteId)
                    .Include(e => e.OrdenesServicio)
                    .Select(e => new
                    {
                        e.EquipoId,
                        e.Marca,
                        e.Modelo,
                        e.Serie,
                        e.TipoEquipo,
                        e.ClienteId,
                        // Solo información básica de órdenes (sin referencia al equipo)
                        OrdenesServicio = e.OrdenesServicio != null && e.OrdenesServicio.Any()
                            ? e.OrdenesServicio.Select(o => new
                            {
                                o.OrdenServicioId,
                                o.Falla,
                                o.FechaCreacion,
                                o.Estado,
                                o.Prioridad,
                                o.Presupuesto
                                // No incluir referencia al equipo aquí
                            }).ToList()
                            : null,
                        // Contar órdenes activas sin traer todos los datos
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
                return StatusCode(500, new { 
                    mensaje = "Error al obtener equipos del cliente", 
                    error = ex.Message 
                });
            }
        }

        // GET: api/equipo/tipo/{tipo}
        // Listar equipos por tipo (Impresion/Computo)
        [HttpGet("tipo/{tipo}")]
        public async Task<ActionResult<object>> GetEquiposPorTipo(string tipo)
        {
            try
            {
                // Validar tipo
                if (string.IsNullOrWhiteSpace(tipo))
                {
                    return BadRequest(new { mensaje = "El tipo de equipo es requerido" });
                }
                
                if (tipo != "Impresion" && tipo != "Computo")
                {
                    return BadRequest(new { 
                        mensaje = "El tipo debe ser 'Impresion' o 'Computo'",
                        tipoInvalido = tipo
                    });
                }

                // Obtener equipos filtrados por tipo sin incluir referencias circulares
                var equipos = await _db.Equipos
                    .Where(e => e.TipoEquipo == tipo)
                    .Include(e => e.Cliente)
                    .Include(e => e.OrdenesServicio)
                    .Select(e => new
                    {
                        e.EquipoId,
                        e.Marca,
                        e.Modelo,
                        e.Serie,
                        e.TipoEquipo,
                        e.ClienteId,
                        // Información del cliente sin su lista de equipos
                        Cliente = e.Cliente != null ? new
                        {
                            e.Cliente.ClienteId,
                            e.Cliente.Nombre,
                            e.Cliente.Direccion,
                            e.Cliente.Telefono
                            // No incluir Equipos aquí
                        } : null,
                        // Resumen de órdenes de servicio sin referencias circulares
                        OrdenesServicio = e.OrdenesServicio != null && e.OrdenesServicio.Any()
                            ? e.OrdenesServicio.Select(o => new
                            {
                                o.OrdenServicioId,
                                o.Falla,
                                o.FechaCreacion,
                                o.Estado,
                                o.Prioridad,
                                o.Presupuesto
                                //  No incluir referencia al Equipo aquí
                            }).ToList()
                            : null,
                        // Estadísticas útiles
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
                
                // Obtener estadísticas adicionales
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
                return StatusCode(500, new { 
                    mensaje = "Error al obtener equipos por tipo", 
                    error = ex.Message 
                });
            }
        }

        // POST: api/equipo
        // Crear nuevo equipo
        [HttpPost]
        public async Task<ActionResult<Equipo>> CreateEquipo([FromBody] Equipo equipo)
        {
            try
            {
                // Validar modelo
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

                // Verificar si el cliente existe
                var cliente = await _db.Clientes.FindAsync(equipo.ClienteId);
                if (cliente == null)
                {
                    return BadRequest(new { mensaje = $"Cliente con ID {equipo.ClienteId} no existe" });
                }

                // Verificar si ya existe un equipo con la misma serie
                var serieExiste = await _db.Equipos
                    .AnyAsync(e => e.Serie == equipo.Serie);
                
                if (serieExiste)
                {
                    return Conflict(new { mensaje = $"Ya existe un equipo con la serie {equipo.Serie}" });
                }

                // Inicializar colección de órdenes si es null
                if (equipo.OrdenesServicio == null)
                {
                    equipo.OrdenesServicio = new HashSet<OrdenServicio>();
                }

                // Agregar equipo
                await _db.Equipos.AddAsync(equipo);
                await _db.SaveChangesAsync();

                // Cargar el cliente relacionado para la respuesta
                await _db.Entry(equipo).Reference(e => e.Cliente).LoadAsync();

                // Retornar el equipo creado
                return CreatedAtAction(nameof(GetEquipo), new { id = equipo.EquipoId }, new 
                { 
                    mensaje = "Equipo creado exitosamente",
                    equipo = new
                    {
                        equipo.EquipoId,
                        equipo.Marca,
                        equipo.Modelo,
                        equipo.Serie,
                        equipo.TipoEquipo,
                        equipo.ClienteId,
                        Cliente = new
                        {
                            cliente.ClienteId,
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
        // Editar equipo completo
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEquipo(int id, [FromBody] Equipo equipo)
        {
            try
            {
                // Validar que el ID coincida
                if (id != equipo.EquipoId)
                {
                    return BadRequest(new { mensaje = "El ID de la URL no coincide con el ID del equipo" });
                }

                // Buscar el equipo existente
                var equipoExistente = await _db.Equipos.FindAsync(id);
                
                if (equipoExistente == null)
                {
                    return NotFound(new { mensaje = $"Equipo con ID {id} no encontrado" });
                }

                // Validar modelo
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

                // Verificar si el cliente existe
                var cliente = await _db.Clientes.FindAsync(equipo.ClienteId);
                if (cliente == null)
                {
                    return BadRequest(new { mensaje = $"Cliente con ID {equipo.ClienteId} no existe" });
                }

                // Verificar si la serie ya existe en otro equipo
                var serieExiste = await _db.Equipos
                    .AnyAsync(e => e.Serie == equipo.Serie && e.EquipoId != id);
                
                if (serieExiste)
                {
                    return Conflict(new { mensaje = $"La serie {equipo.Serie} ya está registrada en otro equipo" });
                }

                // Actualizar los campos
                equipoExistente.Marca = equipo.Marca;
                equipoExistente.Modelo = equipo.Modelo;
                equipoExistente.Serie = equipo.Serie;
                equipoExistente.TipoEquipo = equipo.TipoEquipo;
                equipoExistente.ClienteId = equipo.ClienteId;

                _db.Entry(equipoExistente).State = EntityState.Modified;
                await _db.SaveChangesAsync();
                    //Evita Referencias circulares al retornar el equipo actualizado
                    var equipoActualizado = new
                    {
                        equipoExistente.EquipoId,
                        equipoExistente.Marca,
                        equipoExistente.Modelo,
                        equipoExistente.Serie,
                        equipoExistente.TipoEquipo,
                        equipoExistente.ClienteId,
                        Cliente = new
                        {
                            cliente.ClienteId,
                            cliente.Nombre,
                            cliente.Direccion,
                            cliente.Telefono
                        }
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
        // Eliminar equipo
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEquipo(int id)
        {
            try
            {
                // Buscar el equipo con sus relaciones necesarias para validación
                var equipo = await _db.Equipos
                    .Include(e => e.Cliente)
                    .Include(e => e.OrdenesServicio)
                    .FirstOrDefaultAsync(e => e.EquipoId == id);
                
                if (equipo == null)
                {
                    return NotFound(new { 
                        mensaje = $"Equipo con ID {id} no encontrado",
                        equipoId = id
                    });
                }

                // Verificar si tiene órdenes de servicio relacionadas
                if (equipo.OrdenesServicio != null && equipo.OrdenesServicio.Any())
                {
                    // Obtener información de las órdenes sin referencias circulares
                    var ordenesInfo = equipo.OrdenesServicio.Select(o => new
                    {
                        o.OrdenServicioId,
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

                // Guardar información del equipo para la respuesta
                var equipoInfo = new
                {
                    equipo.EquipoId,
                    equipo.Marca,
                    equipo.Modelo,
                    equipo.Serie,
                    equipo.TipoEquipo,
                    Cliente = equipo.Cliente != null ? new
                    {
                        equipo.Cliente.ClienteId,
                        equipo.Cliente.Nombre,
                        equipo.Cliente.Direccion,
                        equipo.Cliente.Telefono
                    } : null
                };

                // Eliminar el equipo
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
                return StatusCode(409, new { 
                    mensaje = "Error de concurrencia. El equipo fue modificado o eliminado por otro usuario",
                    equipoId = id
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    mensaje = "Error interno del servidor al eliminar el equipo",
                    error = ex.Message,
                    equipoId = id
                });
            }
        }

        // GET: api/equipo/sin-ordenes
        // Listar equipos que no tienen órdenes de servicio
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
                
                return Ok(new 
                { 
                    mensaje = $"Equipos sin órdenes de servicio",
                    total = equipos.Count,
                    equipos = equipos 
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener equipos sin órdenes", error = ex.Message });
            }
        }
    }
}