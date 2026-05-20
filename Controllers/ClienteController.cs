using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrdersManagement.Data;
using OrdersManagement.Models;
using System.Text.Json;

namespace OrdersManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClienteController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public ClienteController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET: api/cliente
        // Listar todos los clientes
        [HttpGet]
        public async Task<ActionResult<object>> GetClientes()
        {
            try
            {
                var clientes = await _db.Clientes
                    .Include(c => c.Equipos)
                    .Select(c => new
                    {
                        c.ClienteId,
                        c.Nombre,
                        c.Direccion,
                        c.Telefono,
                        // Resumen de equipos sin referencias circulares
                        Equipos = c.Equipos != null && c.Equipos.Any()
                            ? new
                            {
                                Total = c.Equipos.Count,
                                Impresoras = c.Equipos.Count(e => e.TipoEquipo == "Impresion"),
                                Computos = c.Equipos.Count(e => e.TipoEquipo == "Computo"),
                                UltimosEquipos = c.Equipos
                                    .OrderByDescending(e => e.EquipoId)
                                    .Take(5)
                                    .Select(e => new
                                    {
                                        e.EquipoId,
                                        e.Marca,
                                        e.Modelo,
                                        e.Serie,
                                        e.TipoEquipo
                                    })
                            } : null,
                        TotalEquipos = c.Equipos != null ? c.Equipos.Count : 0
                    })
                    .OrderBy(c => c.Nombre)
                    .ToListAsync();

                if (clientes == null || clientes.Count == 0)
                {
                    return NotFound(new { mensaje = "No hay clientes registrados" });
                }

                // Estadísticas generales
                var estadisticas = new
                {
                    TotalClientes = clientes.Count,
                    TotalEquipos = clientes.Sum(c => c.TotalEquipos),
                    TotalImpresoras = clientes.Sum(c => c.Equipos?.Impresoras ?? 0),
                    TotalComputos = clientes.Sum(c => c.Equipos?.Computos ?? 0),
                    ClientesConEquipos = clientes.Count(c => c.TotalEquipos > 0),
                    ClientesSinEquipos = clientes.Count(c => c.TotalEquipos == 0)
                };

                return Ok(new
                {
                    mensaje = "Clientes obtenidos exitosamente",
                    total = clientes.Count,
                    estadisticas = estadisticas,
                    clientes = clientes
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener clientes", error = ex.Message });
            }
        }

        // GET: api/cliente/{id}
        // Buscar cliente por ID
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetCliente(int id)
        {
            try
            {
                var cliente = await _db.Clientes
                    .Include(c => c.Equipos)
                    .Where(c => c.ClienteId == id)
                    .Select(c => new
                    {
                        c.ClienteId,
                        c.Nombre,
                        c.Direccion,
                        c.Telefono,
                        // Lista completa de equipos sin referencias circulares
                        Equipos = c.Equipos != null && c.Equipos.Any()
                            ? c.Equipos.Select(e => new
                            {
                                e.EquipoId,
                                e.Marca,
                                e.Modelo,
                                e.Serie,
                                e.TipoEquipo,
                                // No incluir Cliente aquí para evitar ciclo
                                OrdenesCount = e.OrdenesServicio != null ? e.OrdenesServicio.Count : 0
                            }).ToList()
                            : null,
                        // Estadísticas del cliente
                        Estadisticas = new
                        {
                            TotalEquipos = c.Equipos != null ? c.Equipos.Count : 0,
                            Impresoras = c.Equipos != null ? c.Equipos.Count(e => e.TipoEquipo == "Impresion") : 0,
                            Computos = c.Equipos != null ? c.Equipos.Count(e => e.TipoEquipo == "Computo") : 0,
                            EquiposConOrdenes = c.Equipos != null 
                                ? c.Equipos.Count(e => e.OrdenesServicio != null && e.OrdenesServicio.Any())
                                : 0
                        }
                    })
                    .FirstOrDefaultAsync();

                if (cliente == null)
                {
                    return NotFound(new { mensaje = $"Cliente con ID {id} no encontrado" });
                }

                return Ok(new
                {
                    mensaje = "Cliente encontrado exitosamente",
                    cliente = cliente
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al buscar cliente", error = ex.Message });
            }
        }

        // GET: api/cliente/buscar/{nombre}
        // Buscar clientes por nombre
        [HttpGet("buscar/{nombre}")]
        public async Task<ActionResult<object>> BuscarClientesPorNombre(string nombre)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(nombre))
                {
                    return BadRequest(new { mensaje = "El nombre de búsqueda no puede estar vacío" });
                }

                var clientes = await _db.Clientes
                    .Where(c => c.Nombre != null && c.Nombre.Contains(nombre))
                    .Select(c => new
                    {
                        c.ClienteId,
                        c.Nombre,
                        c.Direccion,
                        c.Telefono,
                        TotalEquipos = c.Equipos != null ? c.Equipos.Count : 0
                    })
                    .OrderBy(c => c.Nombre)
                    .ToListAsync();

                if (clientes == null || clientes.Count == 0)
                {
                    return NotFound(new { mensaje = $"No se encontraron clientes con nombre que contenga '{nombre}'" });
                }

                return Ok(new
                {
                    mensaje = $"Se encontraron {clientes.Count} cliente(s)",
                    terminoBusqueda = nombre,
                    total = clientes.Count,
                    clientes = clientes
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al buscar clientes", error = ex.Message });
            }
        }

        // GET: api/cliente/telefono/{telefono}
        // Buscar cliente por teléfono
        [HttpGet("telefono/{telefono}")]
        public async Task<ActionResult<object>> BuscarClientePorTelefono(string telefono)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(telefono))
                {
                    return BadRequest(new { mensaje = "El teléfono no puede estar vacío" });
                }

                var cliente = await _db.Clientes
                    .Where(c => c.Telefono == telefono)
                    .Select(c => new
                    {
                        c.ClienteId,
                        c.Nombre,
                        c.Direccion,
                        c.Telefono,
                        TotalEquipos = c.Equipos != null ? c.Equipos.Count : 0
                    })
                    .FirstOrDefaultAsync();

                if (cliente == null)
                {
                    return NotFound(new { mensaje = $"No se encontró cliente con teléfono {telefono}" });
                }

                return Ok(new
                {
                    mensaje = "Cliente encontrado exitosamente",
                    cliente = cliente
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al buscar cliente por teléfono", error = ex.Message });
            }
        }

        // GET: api/cliente/con-equipos
        // Listar clientes que tienen equipos registrados
        [HttpGet("con-equipos")]
        public async Task<ActionResult<object>> GetClientesConEquipos()
        {
            try
            {
                var clientes = await _db.Clientes
                    .Where(c => c.Equipos != null && c.Equipos.Any())
                    .Include(c => c.Equipos)
                    .Select(c => new
                    {
                        c.ClienteId,
                        c.Nombre,
                        c.Direccion,
                        c.Telefono,
                        Equipos = c.Equipos != null ? c.Equipos.Select(e => new
                        {
                            e.EquipoId,
                            e.Marca,
                            e.Modelo,
                            e.TipoEquipo
                        }).ToList() : null,
                        TotalEquipos = c.Equipos != null ? c.Equipos.Count : 0
                    })
                    .OrderBy(c => c.Nombre)
                    .ToListAsync();

                if (clientes == null || clientes.Count == 0)
                {
                    return NotFound(new { mensaje = "No hay clientes con equipos registrados" });
                }

                return Ok(new
                {
                    mensaje = "Clientes con equipos registrados",
                    total = clientes.Count,
                    clientes = clientes
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener clientes con equipos", error = ex.Message });
            }
        }

        // GET: api/cliente/sin-equipos
        // Listar clientes que no tienen equipos registrados
        [HttpGet("sin-equipos")]
        public async Task<ActionResult<object>> GetClientesSinEquipos()
        {
            try
            {
                var clientes = await _db.Clientes
                    .Where(c => c.Equipos == null || !c.Equipos.Any())
                    .Select(c => new
                    {
                        c.ClienteId,
                        c.Nombre,
                        c.Direccion,
                        c.Telefono,
                        TotalEquipos = 0
                    })
                    .OrderBy(c => c.Nombre)
                    .ToListAsync();

                if (clientes == null || clientes.Count == 0)
                {
                    return NotFound(new { mensaje = "Todos los clientes tienen equipos registrados" });
                }

                return Ok(new
                {
                    mensaje = "Clientes sin equipos registrados",
                    total = clientes.Count,
                    clientes = clientes
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener clientes sin equipos", error = ex.Message });
            }
        }

        // POST: api/cliente
        // Crear nuevo cliente
        [HttpPost]
        public async Task<ActionResult<object>> CreateCliente([FromBody] Cliente cliente)
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

                // Validar que el teléfono tenga 10 dígitos
                if (cliente.Telefono != null && cliente.Telefono.Length != 10)
                {
                    return BadRequest(new { mensaje = "El teléfono debe tener exactamente 10 dígitos" });
                }

                // Verificar si ya existe un cliente con el mismo teléfono
                var telefonoExiste = await _db.Clientes
                    .AnyAsync(c => c.Telefono == cliente.Telefono);

                if (telefonoExiste)
                {
                    return Conflict(new { mensaje = $"Ya existe un cliente con el teléfono {cliente.Telefono}" });
                }

                // Inicializar colección de equipos si es null
                if (cliente.Equipos == null)
                {
                    cliente.Equipos = new HashSet<Equipo>();
                }

                // Agregar cliente
                await _db.Clientes.AddAsync(cliente);
                await _db.SaveChangesAsync();

                // Respuesta sin referencias circulares
                var clienteCreado = new
                {
                    cliente.ClienteId,
                    cliente.Nombre,
                    cliente.Direccion,
                    cliente.Telefono,
                    TotalEquipos = 0
                };

                return CreatedAtAction(nameof(GetCliente), new { id = cliente.ClienteId }, new
                {
                    mensaje = "Cliente creado exitosamente",
                    cliente = clienteCreado
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error interno del servidor", error = ex.Message });
            }
        }

        // PUT: api/cliente/{id}
        // Editar cliente completo
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCliente(int id, [FromBody] Cliente cliente)
        {
            try
            {
                // Validar que el ID coincida
                if (id != cliente.ClienteId)
                {
                    return BadRequest(new
                    {
                        mensaje = "El ID de la URL no coincide con el ID del cliente",
                        urlId = id,
                        bodyId = cliente.ClienteId
                    });
                }

                // Buscar el cliente existente
                var clienteExistente = await _db.Clientes
                    .Include(c => c.Equipos)
                    .FirstOrDefaultAsync(c => c.ClienteId == id);

                if (clienteExistente == null)
                {
                    return NotFound(new { mensaje = $"Cliente con ID {id} no encontrado" });
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

                // Validar que el teléfono tenga 10 dígitos
                if (cliente.Telefono != null && cliente.Telefono.Length != 10)
                {
                    return BadRequest(new { mensaje = "El teléfono debe tener exactamente 10 dígitos" });
                }

                // Verificar si el teléfono ya existe en otro cliente
                var telefonoExiste = await _db.Clientes
                    .AnyAsync(c => c.Telefono == cliente.Telefono && c.ClienteId != id);

                if (telefonoExiste)
                {
                    return Conflict(new { mensaje = $"El teléfono {cliente.Telefono} ya está registrado por otro cliente" });
                }

                // Actualizar campos (no actualizar Equipos aquí)
                clienteExistente.Nombre = cliente.Nombre;
                clienteExistente.Direccion = cliente.Direccion;
                clienteExistente.Telefono = cliente.Telefono;

                _db.Entry(clienteExistente).State = EntityState.Modified;
                await _db.SaveChangesAsync();

                // Respuesta sin referencias circulares
                var clienteActualizado = new
                {
                    clienteExistente.ClienteId,
                    clienteExistente.Nombre,
                    clienteExistente.Direccion,
                    clienteExistente.Telefono,
                    TotalEquipos = clienteExistente.Equipos != null ? clienteExistente.Equipos.Count : 0
                };

                return Ok(new
                {
                    mensaje = "Cliente actualizado exitosamente",
                    cliente = clienteActualizado
                });
            }
            catch (DbUpdateConcurrencyException)
            {
                return StatusCode(409, new { mensaje = "Error de concurrencia al actualizar el cliente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error interno del servidor", error = ex.Message });
            }
        }

        // PATCH: api/cliente/{id}
        // Editar cliente parcialmente
        [HttpPatch("{id}")]
        public async Task<IActionResult> PatchCliente(int id, [FromBody] JsonElement updates)
        {
            try
            {
                var cliente = await _db.Clientes.FindAsync(id);

                if (cliente == null)
                {
                    return NotFound(new { mensaje = $"Cliente con ID {id} no encontrado" });
                }

                // Aplicar actualizaciones solo a los campos enviados
                if (updates.TryGetProperty("Nombre", out var nombreProp))
                {
                    cliente.Nombre = nombreProp.GetString();
                }

                if (updates.TryGetProperty("Direccion", out var direccionProp))
                {
                    cliente.Direccion = direccionProp.GetString();
                }

                if (updates.TryGetProperty("Telefono", out var telefonoProp))
                {
                    var nuevoTelefono = telefonoProp.GetString();

                    if (!string.IsNullOrWhiteSpace(nuevoTelefono) && nuevoTelefono.Length != 10)
                    {
                        return BadRequest(new { mensaje = "El teléfono debe tener exactamente 10 dígitos" });
                    }

                    // Verificar si el teléfono ya existe en otro cliente
                    var telefonoExiste = await _db.Clientes
                        .AnyAsync(c => c.Telefono == nuevoTelefono && c.ClienteId != id);

                    if (telefonoExiste)
                    {
                        return Conflict(new { mensaje = $"El teléfono {nuevoTelefono} ya está registrado" });
                    }

                    cliente.Telefono = nuevoTelefono;
                }

                await _db.SaveChangesAsync();

                var clienteActualizado = new
                {
                    cliente.ClienteId,
                    cliente.Nombre,
                    cliente.Direccion,
                    cliente.Telefono
                };

                return Ok(new
                {
                    mensaje = "Cliente actualizado exitosamente",
                    cliente = clienteActualizado
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al actualizar el cliente", error = ex.Message });
            }
        }

        // DELETE: api/cliente/{id}
        // Eliminar cliente
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCliente(int id)
        {
            try
            {
                // Buscar el cliente con sus relaciones
                var cliente = await _db.Clientes
                    .Include(c => c.Equipos)
                    .FirstOrDefaultAsync(c => c.ClienteId == id);

                if (cliente == null)
                {
                    return NotFound(new
                    {
                        mensaje = $"Cliente con ID {id} no encontrado",
                        clienteId = id
                    });
                }

                // Verificar si tiene equipos asociados
                var tieneEquipos = cliente.Equipos != null && cliente.Equipos.Any();

                if (tieneEquipos)
                {
                    return BadRequest(new
                    {
                        mensaje = $"No se puede eliminar el cliente porque tiene {cliente.Equipos!.Count} equipos asociados",
                        clienteId = id,
                        clienteNombre = cliente.Nombre,
                        equiposAsociados = cliente.Equipos.Select(e => new
                        {
                            e.EquipoId,
                            e.Marca,
                            e.Modelo,
                            e.Serie,
                            e.TipoEquipo
                        }).ToList(),
                        sugerencia = "Primero elimine o reasigne los equipos asociados a este cliente"
                    });
                }

                // Guardar información para la respuesta
                var clienteInfo = new
                {
                    cliente.ClienteId,
                    cliente.Nombre,
                    cliente.Direccion,
                    cliente.Telefono
                };

                // Eliminar el cliente
                _db.Clientes.Remove(cliente);
                await _db.SaveChangesAsync();

                return Ok(new
                {
                    mensaje = $"Cliente '{cliente.Nombre}' eliminado exitosamente",
                    clienteEliminado = clienteInfo,
                    fechaEliminacion = DateTime.Now
                });
            }
            catch (DbUpdateConcurrencyException)
            {
                return StatusCode(409, new
                {
                    mensaje = "Error de concurrencia. El cliente fue modificado por otro usuario",
                    clienteId = id
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    mensaje = "Error interno del servidor al eliminar el cliente",
                    error = ex.Message,
                    clienteId = id
                });
            }
        }

        // GET: api/cliente/resumen
        // Resumen general de clientes
        [HttpGet("resumen")]
        public async Task<ActionResult<object>> GetResumenClientes()
        {
            try
            {
                var totalClientes = await _db.Clientes.CountAsync();
                var clientesConEquipos = await _db.Clientes
                    .CountAsync(c => c.Equipos != null && c.Equipos.Any());
                var clientesSinEquipos = totalClientes - clientesConEquipos;

                var topClientes = await _db.Clientes
                    .Where(c => c.Equipos != null && c.Equipos.Any())
                    .Select(c => new
                    {
                        c.ClienteId,
                        c.Nombre,
                        c.Telefono,
                        TotalEquipos = c.Equipos != null ? c.Equipos.Count : 0
                    })
                    .OrderByDescending(c => c.TotalEquipos)
                    .Take(5)
                    .ToListAsync();

                var resumen = new
                {
                    TotalClientes = totalClientes,
                    ClientesConEquipos = clientesConEquipos,
                    ClientesSinEquipos = clientesSinEquipos,
                    PorcentajeConEquipos = totalClientes > 0 
                        ? Math.Round((double)clientesConEquipos / totalClientes * 100, 2) 
                        : 0,
                    Top5ClientesConMasEquipos = topClientes
                };

                return Ok(new
                {
                    mensaje = "Resumen de clientes",
                    resumen = resumen
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener resumen de clientes", error = ex.Message });
            }
        }
    }
}