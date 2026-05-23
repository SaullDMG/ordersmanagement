
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ordersmanagement.Models.requests;
using ordersmanagement.Services;
using ordersmanagement.Interface;
using OrdersManagement.Data;
using OrdersManagement.Models;
using OrdersManagement.Services;
using System.Text.Json;

namespace OrdersManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdenServicioController : ControllerBase
    {
        private readonly IA _iaService;
        private readonly ApplicationDbContext _db;
        private readonly WebSocketService _webSocketService;

        public OrdenServicioController(ApplicationDbContext db, WebSocketService webSocketService, IA iaService)
        {
            _db = db;
             _webSocketService = webSocketService;
             _iaService = iaService;
        }

        // =================================================================
        // 🔥 BLOQUE PRINCIPAL: CRUD DIRECTO
        // =================================================================

        // DTO para recibir la data desde React
        public class BudgetPredictionRequest
        {
            public string Equipo { get; set; }
            public string Falla { get; set; }
            public decimal PresupuestoPropuesto { get; set; }
        }

        [HttpPost("predict-budget-risk")]
        public async Task<IActionResult> PredictBudgetRisk([FromBody] BudgetPredictionRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Equipo) || string.IsNullOrEmpty(request.Falla))
                {
                    return BadRequest(new { mensaje = "El equipo y la falla son obligatorios para el análisis predictivo." });
                }

                // 🤖 Invocación exitosa a OpenAI
                string jsonStringIA = await _iaService.PredecirRiesgoPresupuesto(
                    request.Equipo, 
                    request.Falla, 
                    request.PresupuestoPropuesto
                );

                // 🛠️ FIX: Devolvemos el string JSON crudo indicando el Content-Type correcto.
                // Esto evita el ObjectDisposedException de raíz y es mucho más rápido.
                return Content(jsonStringIA, "application/json");
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error en el análisis predictivo de la IA", error = ex.Message });
            }
        }

        // DTO para recibir la foto fija de las métricas desde React
        public class DashboardMetricsRequest
        {
            public int TotalOrdenes { get; set; }
            public int OrdenesPendientes { get; set; }
            public int OrdenesEnProceso { get; set; }
            public int AlertasPresupuesto { get; set; }
            public int EficienciaActual { get; set; }
        }

        [HttpPost("analyze-dashboard-metrics")]
        public async Task<IActionResult> AnalyzeDashboardMetrics([FromBody] DashboardMetricsRequest request)
        {
            try
            {
                // Llamamos al servicio de IA pasándole el objeto con los contadores reales
                string jsonReporteIA = await _iaService.AuditarMetricasDeOperacion(
                    request.TotalOrdenes,
                    request.OrdenesPendientes,
                    request.OrdenesEnProceso,
                    request.AlertasPresupuesto,
                    request.EficienciaActual
                );

                // Retornamos el JSON crudo directo a React
                return Content(jsonReporteIA, "application/json");
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error en la auditoría de métricas", error = ex.Message });
            }
        }

        // GET: api/ordenservicio
        [HttpGet]
        public async Task<ActionResult<object>> GetOrdenesServicio()
        {
            try
            {
                var ordenesDb = await _db.OrdenesServicio
                    .Include(o => o.Usuario)
                    .Include(o => o.Sucursal) // <-- Incluido para la logística geográfica
                    .Include(o => o.Equipo)
                        .ThenInclude(e => e!.Cliente)
                    .Include(o => o.Diagnosticos)
                    .Include(o => o.Evidencias)
                    .OrderByDescending(o => o.FechaCreacion)
                    .ToListAsync();
                
                if (ordenesDb == null || ordenesDb.Count == 0) return Ok(Array.Empty<object>());

                var ordenes = ordenesDb.Select(o => new
                {
                    o.Id,
                    order = $"{o.Id:D4}",
                    o.FechaCreacion,
                    o.FechaCierre,
                    o.Falla,
                    o.Estado,
                    o.Prioridad,
                    o.Presupuesto,
                    o.UsuarioId,
                    o.EquipoId,
                    o.SucursalId, // <-- Mapeado
                    tecnico = o.Usuario != null ? o.Usuario.Fullname : "",
                    cliente = o.Equipo != null ? o.Equipo.Cliente != null ? o.Equipo.Cliente.Nombre:"" : "",
                    Usuario = o.Usuario != null ? new
                    {
                        o.Usuario.Id,
                        o.Usuario.Fullname,
                        o.Usuario.Correo,
                        o.Usuario.Rol,
                        o.Usuario.Especialidad,
                        o.Usuario.Telefono
                    } : null,
                    Sucursal = o.Sucursal != null ? new // <-- Ficha congelada en el histórico de la orden
                    {
                        o.Sucursal.Id,
                        o.Sucursal.Name,
                        o.Sucursal.Direccion,
                        o.Sucursal.Telefono,
                        o.Sucursal.Latitud,
                        o.Sucursal.Longitud
                    } : null,
                    Equipo = o.Equipo != null ? new
                    {
                        o.Equipo.Id,
                        o.Equipo.Marca,
                        o.Equipo.Modelo,
                        o.Equipo.Serie,
                        o.Equipo.TipoEquipo,
                        Cliente = o.Equipo.Cliente != null ? new
                        {
                            o.Equipo.Cliente.Id,
                            o.Equipo.Cliente.Nombre,
                            o.Equipo.Cliente.Telefono
                        } : null
                    } : null,
                    Diagnosticos = o.Diagnosticos != null && o.Diagnosticos.Any()
                        ? o.Diagnosticos.Select(d => new
                        {
                            d.Id,
                            d.DiagnosticoFalla,
                            d.CostoRep,
                            d.CostoRef,
                            CostoTotal = d.CostoRep + d.CostoRef
                        }).ToList()
                        : null,
                    Evidencias = o.Evidencias != null && o.Evidencias.Any()
                        ? o.Evidencias.Select(e => new
                        {
                            e.Id,
                            e.Descripcion,
                            e.Url,
                            e.Extension,
                            e.Registro
                        }).ToList()
                        : null,
                    Estadisticas = new
                    {
                        TotalDiagnosticos = o.Diagnosticos != null ? o.Diagnosticos.Count : 0,
                        TotalEvidencias = o.Evidencias != null ? o.Evidencias.Count : 0,
                        CostoTotalDiagnosticos = o.Diagnosticos != null 
                            ? o.Diagnosticos.Sum(d => d.CostoRep + d.CostoRef) 
                            : 0,
                        DiferenciaPresupuesto = o.Presupuesto - (o.Diagnosticos != null 
                            ? o.Diagnosticos.Sum(d => d.CostoRep + d.CostoRef) 
                            : 0)
                    }
                }).ToList();

                return Ok(ordenes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener órdenes de servicio", error = ex.Message });
            }
        }

        // GET: api/ordenservicio/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetOrdenServicio(int id)
        {
            try
            {
                var ordenDb = await _db.OrdenesServicio
                    .Include(o => o.Usuario)
                    .Include(o => o.Sucursal) 
                    .Include(o => o.Equipo)
                        .ThenInclude(e => e!.Cliente)
                    .Include(o => o.Diagnosticos)
                    .Include(o => o.Evidencias)
                    .AsSplitQuery()
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (ordenDb == null)
                {
                    return NotFound(new { mensaje = $"Orden de servicio con ID {id} no encontrada" });
                }

                var resultado = new
                {
                    ordenDb.Id,
                    ordenDb.FechaCreacion,
                    ordenDb.FechaCierre,
                    ordenDb.Falla,
                    ordenDb.Estado,
                    ordenDb.Prioridad,
                    ordenDb.Presupuesto,
                    ordenDb.UsuarioId,
                    ordenDb.EquipoId,
                    ordenDb.SucursalId,
                    Usuario = ordenDb.Usuario != null ? new
                    {
                        ordenDb.Usuario.Id,
                        ordenDb.Usuario.Fullname,
                        ordenDb.Usuario.Correo,
                        ordenDb.Usuario.Rol,
                        ordenDb.Usuario.Especialidad,
                        ordenDb.Usuario.Telefono
                    } : null,
                    Sucursal = ordenDb.Sucursal != null ? new
                    {
                        ordenDb.Sucursal.Id,
                        ordenDb.Sucursal.Name,
                        ordenDb.Sucursal.Direccion,
                        ordenDb.Sucursal.Telefono,
                        ordenDb.Sucursal.Latitud,
                        ordenDb.Sucursal.Longitud
                    } : null,
                    Equipo = ordenDb.Equipo != null ? new
                    {
                        ordenDb.Equipo.Id,
                        ordenDb.Equipo.Marca,
                        ordenDb.Equipo.Modelo,
                        ordenDb.Equipo.Serie,
                        ordenDb.Equipo.TipoEquipo,
                        Cliente = ordenDb.Equipo.Cliente != null ? new
                        {
                            ordenDb.Equipo.Cliente.Id,
                            ordenDb.Equipo.Cliente.Nombre,
                            ordenDb.Equipo.Cliente.Direccion,
                            ordenDb.Equipo.Cliente.Telefono
                        } : null
                    } : null,
                    Diagnosticos = ordenDb.Diagnosticos != null && ordenDb.Diagnosticos.Any()
                        ? ordenDb.Diagnosticos.Select(d => new
                        {
                            d.Id,
                            d.DiagnosticoFalla,
                            d.CostoRep,
                            d.CostoRef,
                            CostoTotal = d.CostoRep + d.CostoRef
                        }).ToList()
                        : null,
                    Evidencias = ordenDb.Evidencias != null && ordenDb.Evidencias.Any()
                        ? ordenDb.Evidencias.Select(e => new
                        {
                            e.Id,
                            e.Descripcion,
                            e.Url,
                            e.Extension,
                            e.Registro
                        }).ToList()
                        : null,
                    Estadisticas = new
                    {
                        TotalDiagnosticos = ordenDb.Diagnosticos?.Count ?? 0,
                        TotalEvidencias = ordenDb.Evidencias?.Count ?? 0,
                        CostoTotalReparacion = ordenDb.Diagnosticos?.Sum(d => d.CostoRep) ?? 0,
                        CostoTotalRefaccion = ordenDb.Diagnosticos?.Sum(d => d.CostoRef) ?? 0,
                        CostoTotalDiagnosticos = ordenDb.Diagnosticos?.Sum(d => d.CostoRep + d.CostoRef) ?? 0,
                        DiferenciaPresupuesto = ordenDb.Presupuesto - (ordenDb.Diagnosticos?.Sum(d => d.CostoRep + d.CostoRef) ?? 0),
                        EstaSobrePresupuesto = (ordenDb.Diagnosticos?.Sum(d => d.CostoRep + d.CostoRef) ?? 0) > ordenDb.Presupuesto
                    }
                };

                return Ok(resultado);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al buscar orden de servicio", error = ex.Message });
            }
        }

        // POST: api/ordenservicio
        [HttpPost]
        public async Task<ActionResult<object>> CreateOrdenServicio([FromBody] OrdenServicio orden)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { mensaje = "Datos inválidos", errores = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
                }

                if (orden.Estado != "Pendiente" && orden.Estado != "Finalizada")
                {
                    return BadRequest(new { mensaje = "El estado debe ser 'Pendiente' o 'Finalizada'" });
                }

                if (orden.Prioridad != "Alta" && orden.Prioridad != "Media" && orden.Prioridad != "Baja")
                {
                    return BadRequest(new { mensaje = "La prioridad debe ser 'Alta', 'Media' o 'Baja'" });
                }

                // --- VALIDACIÓN DE INTEGRIDAD DE LA NUEVA SUCURSAL ---
                var sucursal = await _db.Sucursales.FindAsync(orden.SucursalId);
                if (sucursal == null)
                {
                    return BadRequest(new { mensaje = $"La sucursal con ID {orden.SucursalId} no existe en el catálogo" });
                }

                var usuario = await _db.Usuarios.FindAsync(orden.UsuarioId);
                if (usuario == null)
                {
                    return BadRequest(new { mensaje = $"El usuario con ID {orden.UsuarioId} no existe" });
                }

                var equipo = await _db.Equipos.FindAsync(orden.EquipoId);
                if (equipo == null)
                {
                    return BadRequest(new { mensaje = $"El equipo con ID {orden.EquipoId} no existe" });
                }

                if (orden.Presupuesto < 0)
                {
                    return BadRequest(new { mensaje = "El presupuesto no puede ser negativo" });
                }

                if (orden.Diagnosticos == null) orden.Diagnosticos = new HashSet<Diagnostico>();
                if (orden.Evidencias == null) orden.Evidencias = new HashSet<Evidencia>();

                if (orden.Estado == "Finalizada")
                {
                    var mexicoZone = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time (Mexico)");
                    orden.FechaCierre = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, mexicoZone);
                }

                await _db.OrdenesServicio.AddAsync(orden);
                await _db.SaveChangesAsync();

                var ordenCreada = new
                {
                    orden.Id,
                    orden.FechaCreacion,
                    orden.FechaCierre,
                    orden.Falla,
                    orden.Estado,
                    orden.Prioridad,
                    orden.Presupuesto,
                    orden.UsuarioId,
                    orden.EquipoId,
                    orden.SucursalId,
                    Usuario = new { usuario.Id, usuario.Fullname, usuario.Rol },
                    Sucursal = new { sucursal.Id, sucursal.Name, sucursal.Direccion },
                    Equipo = new { equipo.Id, equipo.Marca, equipo.Modelo, equipo.Serie }
                };

                var mensajeAlerta = new
                    {
                        tipo = "refresh",
                        update = true // Modificado
                    };

                await _webSocketService.SendAlertToClientsAsync(JsonSerializer.Serialize(mensajeAlerta));

                return CreatedAtAction(nameof(GetOrdenServicio), new { id = orden.Id }, new
                {
                    mensaje = "Orden de servicio creada exitosamente",
                    orden = ordenCreada
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error interno del servidor", error = ex.Message });
            }
        }

        // PUT: api/ordenservicio/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOrdenServicio(int id, [FromBody] RequestUpdateOrden orden)
        {
            try
            {
                var ordenExistente = await _db.OrdenesServicio
                    .Include(o => o.Diagnosticos)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (ordenExistente == null)
                {
                    return NotFound(new { mensaje = $"Orden de servicio con ID {id} no encontrada" });
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(new { mensaje = "Datos inválidos", errores = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
                }

                if (orden.Estado != "Pendiente" && orden.Estado != "Procesando" && orden.Estado != "Cancelado" && orden.Estado != "Finalizada")
                {
                    return BadRequest(new { mensaje = "El estado debe ser 'Pendiente' o 'Finalizada'" });
                }

                if (orden.Prioridad != "Alta" && orden.Prioridad != "Media" && orden.Prioridad != "Baja")
                {
                    return BadRequest(new { mensaje = "La prioridad debe ser 'Alta', 'Media' o 'Baja'" });
                }

                // Validar existencia de la sucursal al editar por si la cambian
                var sucursalExiste = await _db.Sucursales.AnyAsync(s => s.Id == orden.SucursalId);
                if (!sucursalExiste)
                {
                    return BadRequest(new { mensaje = $"La sucursal con ID {orden.SucursalId} no existe" });
                }

                var usuario = await _db.Usuarios.FindAsync(orden.UsuarioId);
                if (usuario == null) return BadRequest(new { mensaje = $"El usuario con ID {orden.UsuarioId} no existe" });

                var equipo = await _db.Equipos.FindAsync(orden.EquipoId);
                if (equipo == null) return BadRequest(new { mensaje = $"El equipo con ID {orden.EquipoId} no existe" });

                if (orden.Presupuesto < 0) return BadRequest(new { mensaje = "El presupuesto no puede ser negativo" });

                var mexicoZone = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time (Mexico)");
                if (orden.Estado == "Finalizada")
                {
                    orden.FechaCierre = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, mexicoZone);
                }
                if (orden.Estado == "Procesando")
                {
                    ordenExistente.FechaCierre = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, mexicoZone);
                }
                if (orden.Estado == "Cancelado")
                {
                    ordenExistente.FechaCierre = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, mexicoZone);
                }

                ordenExistente.Falla = orden.Falla;
                ordenExistente.Estado = orden.Estado;
                ordenExistente.Prioridad = orden.Prioridad;
                ordenExistente.Presupuesto = orden.Presupuesto;
                ordenExistente.UsuarioId = orden.UsuarioId;
                ordenExistente.EquipoId = orden.EquipoId;
                ordenExistente.SucursalId = orden.SucursalId;
                ordenExistente.FechaCierre = orden.FechaCierre;

                _db.Entry(ordenExistente).State = EntityState.Modified;
                await _db.SaveChangesAsync();

                return Ok(new
                {
                    mensaje = "Orden de servicio actualizada exitosamente",
                    orden = new { ordenExistente.Id, ordenExistente.FechaCreacion, ordenExistente.Estado, ordenExistente.SucursalId }
                });
            }
            catch (DbUpdateConcurrencyException)
            {
                return StatusCode(409, new { mensaje = "Error de concurrencia al actualizar la orden" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error interno del servidor", error = ex.Message });
            }
        }

        // PATCH: api/ordenservicio/{id}
        [HttpPatch("{id}")]
        public async Task<IActionResult> PatchOrdenServicio(int id, [FromBody] JsonElement updates)
        {
            try
            {
                var orden = await _db.OrdenesServicio.FindAsync(id);
                if (orden == null) return NotFound(new { mensaje = $"Orden de servicio con ID {id} no encontrada" });

                var mexicoZone = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time (Mexico)");
                var estadoAnterior = orden.Estado;

                if (updates.TryGetProperty("Falla", out var fallaProp)) orden.Falla = fallaProp.GetString();

                if (updates.TryGetProperty("Estado", out var estadoProp))
                {
                    var nuevoEstado = estadoProp.GetString();
                    if (nuevoEstado != "Pendiente" && nuevoEstado != "Finalizada") return BadRequest(new { mensaje = "El estado debe ser 'Pendiente' o 'Finalizada'" });

                    if (nuevoEstado == "Finalizada" && estadoAnterior == "Pendiente") orden.FechaCierre = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, mexicoZone);
                    else if (nuevoEstado == "Pendiente" && estadoAnterior == "Finalizada") return BadRequest(new { mensaje = "No se puede cambiar una orden finalizada a pendiente" });

                    orden.Estado = nuevoEstado;
                }

                if (updates.TryGetProperty("Prioridad", out var prioridadProp))
                {
                    var prioridad = prioridadProp.GetString();
                    if (prioridad != "Alta" && prioridad != "Media" && prioridad != "Baja") return BadRequest(new { mensaje = "La prioridad debe ser 'Alta', 'Media' o 'Baja'" });
                    orden.Prioridad = prioridad;
                }

                if (updates.TryGetProperty("Presupuesto", out var presupuestoProp))
                {
                    var presupuesto = presupuestoProp.GetDecimal();
                    if (presupuesto < 0) return BadRequest(new { mensaje = "El presupuesto no puede ser negativo" });
                    orden.Presupuesto = presupuesto;
                }

                // Validación de SucursalId en Patch parcial
                if (updates.TryGetProperty("SucursalId", out var sucursalIdProp))
                {
                    var nuevaSucursalId = sucursalIdProp.GetInt32();
                    var sucursalExiste = await _db.Sucursales.AnyAsync(s => s.Id == nuevaSucursalId);
                    if (!sucursalExiste) return BadRequest(new { mensaje = $"La sucursal con ID {nuevaSucursalId} no existe" });
                    orden.SucursalId = nuevaSucursalId;
                }

                if (updates.TryGetProperty("UsuarioId", out var usuarioIdProp))
                {
                    var nuevoUsuarioId = usuarioIdProp.GetInt32();
                    if (!await _db.Usuarios.AnyAsync(u => u.Id == nuevoUsuarioId)) return BadRequest(new { mensaje = $"El usuario con ID {nuevoUsuarioId} no existe" });
                    orden.UsuarioId = nuevoUsuarioId;
                }

                if (updates.TryGetProperty("EquipoId", out var equipoIdProp))
                {
                    var nuevoEquipoId = equipoIdProp.GetInt32();
                    if (!await _db.Equipos.AnyAsync(e => e.Id == nuevoEquipoId)) return BadRequest(new { mensaje = $"El equipo con ID {nuevoEquipoId} no existe" });
                    orden.EquipoId = nuevoEquipoId;
                }

                if (updates.TryGetProperty("FechaCreacion", out _)) return BadRequest(new { mensaje = "No se puede modificar la fecha de creación" });

                await _db.SaveChangesAsync();
                return Ok(new { mensaje = "Orden de servicio actualizada exitosamente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al actualizar la orden", error = ex.Message });
            }
        }

        // DELETE: api/ordenservicio/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrdenServicio(int id)
        {
            try
            {
                var orden = await _db.OrdenesServicio
                    .Include(o => o.Diagnosticos)
                    .Include(o => o.Evidencias)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (orden == null) return NotFound(new { mensaje = $"Orden de servicio con ID {id} no encontrada", ordenId = id });

                _db.OrdenesServicio.Remove(orden);
                await _db.SaveChangesAsync();

                return Ok(new { mensaje = $"Orden de servicio #{id} eliminada exitosamente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error interno del servidor al eliminar la orden", error = ex.Message });
            }
        }

        // =================================================================
        // 📈 ENDPOINTS ESPECIALIZADOS DE CONSULTA Y NEGOCIO
        // =================================================================

        // GET: api/ordenservicio/usuario/{usuarioId}
        [HttpGet("usuario/{usuarioId}")]
        public async Task<ActionResult<object>> GetOrdenesPorUsuario(int usuarioId)
        {
            try
            {
                var usuario = await _db.Usuarios.Where(u => u.Id == usuarioId).Select(u => new { u.Id, u.Fullname, u.Rol }).FirstOrDefaultAsync();
                if (usuario == null) return NotFound(new { mensaje = $"Usuario con ID {usuarioId} no encontrado" });

                var ordenes = await _db.OrdenesServicio
                    .Where(o => o.UsuarioId == usuarioId)
                    .Include(o => o.Equipo)
                    .Select(o => new
                    {
                        o.Id,
                        o.FechaCreacion,
                        o.FechaCierre,
                        o.Falla,
                        o.Estado,
                        o.Prioridad,
                        o.Presupuesto,
                        o.SucursalId,
                        Equipo = o.Equipo != null ? new { o.Equipo.Id, o.Equipo.Marca, o.Equipo.Modelo, o.Equipo.Serie } : null,
                        TotalDiagnosticos = o.Diagnosticos != null ? o.Diagnosticos.Count : 0,
                        CostoTotal = o.Diagnosticos != null ? o.Diagnosticos.Sum(d => d.CostoRep + d.CostoRef) : 0
                    })
                    .OrderByDescending(o => o.FechaCreacion)
                    .ToListAsync();

                return Ok(new { usuario, total = ordenes.Count, ordenes });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener órdenes por usuario", error = ex.Message });
            }
        }

        // GET: api/ordenservicio/equipo/{equipoId}
        [HttpGet("equipo/{equipoId}")]
        public async Task<ActionResult<object>> GetOrdenesPorEquipo(int equipoId)
        {
            try
            {
                var equipo = await _db.Equipos.Where(e => e.Id == equipoId).Select(e => new { e.Id, e.Marca, e.Modelo, e.Serie }).FirstOrDefaultAsync();
                if (equipo == null) return NotFound(new { mensaje = $"Equipo con ID {equipoId} no encontrado" });

                var ordenes = await _db.OrdenesServicio
                    .Where(o => o.EquipoId == equipoId)
                    .Select(o => new
                    {
                        o.Id,
                        o.FechaCreacion,
                        o.FechaCierre,
                        o.Falla,
                        o.Estado,
                        o.Prioridad,
                        o.Presupuesto,
                        o.UsuarioId,
                        o.SucursalId,
                        TotalDiagnosticos = o.Diagnosticos != null ? o.Diagnosticos.Count : 0,
                        CostoTotal = o.Diagnosticos != null ? o.Diagnosticos.Sum(d => d.CostoRep + d.CostoRef) : 0
                    })
                    .OrderByDescending(o => o.FechaCreacion)
                    .ToListAsync();

                return Ok(new { equipo, total = ordenes.Count, ordenes });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener órdenes por equipo", error = ex.Message });
            }
        }

        // GET: api/ordenservicio/estado/{estado}
        [HttpGet("estado/{estado}")]
        public async Task<ActionResult<object>> GetOrdenesPorEstado(string estado)
        {
            try
            {
                if (estado != "Pendiente" && estado != "Finalizada") return BadRequest(new { mensaje = "El estado debe ser 'Pendiente' o 'Finalizada'" });

                var ordenes = await _db.OrdenesServicio
                    .Where(o => o.Estado == estado)
                    .Select(o => new
                    {
                        o.Id,
                        o.FechaCreacion,
                        o.Falla,
                        o.Prioridad,
                        o.Presupuesto,
                        o.SucursalId,
                        Equipo = o.Equipo != null ? new { o.Equipo.Id, o.Equipo.Marca, o.Equipo.Modelo } : null,
                        TotalDiagnosticos = o.Diagnosticos != null ? o.Diagnosticos.Count : 0
                    })
                    .OrderByDescending(o => o.FechaCreacion)
                    .ToListAsync();

                return Ok(ordenes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener órdenes por estado", error = ex.Message });
            }
        }

        // GET: api/ordenservicio/prioridad/{prioridad}
        [HttpGet("prioridad/{prioridad}")]
        public async Task<ActionResult<object>> GetOrdenesPorPrioridad(string prioridad)
        {
            try
            {
                if (prioridad != "Alta" && prioridad != "Media" && prioridad != "Baja") return BadRequest(new { mensaje = "La prioridad debe ser 'Alta', 'Media' o 'Baja'" });

                var ordenes = await _db.OrdenesServicio
                    .Where(o => o.Prioridad == prioridad)
                    .Select(o => new
                    {
                        o.Id,
                        o.FechaCreacion,
                        o.Falla,
                        o.Estado,
                        o.Presupuesto,
                        o.SucursalId,
                        EquipoInfo = o.Equipo != null ? $"{o.Equipo.Marca} {o.Equipo.Modelo}" : null
                    })
                    .OrderBy(o => o.FechaCreacion)
                    .ToListAsync();

                return Ok(ordenes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener órdenes por prioridad", error = ex.Message });
            }
        }

        // GET: api/ordenservicio/rango-fechas
        [HttpGet("rango-fechas")]
        public async Task<ActionResult<object>> GetOrdenesPorRangoFechas([FromQuery] DateTime fechaInicio, [FromQuery] DateTime fechaFin)
        {
            try
            {
                if (fechaInicio > fechaFin) return BadRequest(new { mensaje = "La fecha de inicio debe ser menor que la fecha de fin" });

                var ordenes = await _db.OrdenesServicio
                    .Where(o => o.FechaCreacion >= fechaInicio && o.FechaCreacion <= fechaFin)
                    .Select(o => new
                    {
                        o.Id,
                        o.FechaCreacion,
                        o.Falla,
                        o.Estado,
                        o.Presupuesto,
                        o.SucursalId
                    })
                    .OrderBy(o => o.FechaCreacion)
                    .ToListAsync();

                return Ok(ordenes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener órdenes por rango de fechas", error = ex.Message });
            }
        }

        // POST: api/ordenservicio/{id}/finalizar
        [HttpPost("{id}/finalizar")]
        public async Task<IActionResult> FinalizarOrden(int id)
        {
            try
            {
                var orden = await _db.OrdenesServicio.Include(o => o.Diagnosticos).FirstOrDefaultAsync(o => o.Id == id);
                if (orden == null) return NotFound(new { mensaje = $"Orden de servicio con ID {id} no encontrada" });
                if (orden.Estado == "Finalizada") return BadRequest(new { mensaje = "La orden ya está finalizada" });

                var mexicoZone = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time (Mexico)");
                orden.Estado = "Finalizada";
                orden.FechaCierre = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, mexicoZone);

                _db.Entry(orden).State = EntityState.Modified;
                await _db.SaveChangesAsync();

                return Ok(new { mensaje = $"Orden de servicio #{id} finalizada exitosamente", fechaCierre = orden.FechaCierre });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al finalizar la orden", error = ex.Message });
            }
        }

        // GET: api/ordenservicio/buscar/{termino}
        [HttpGet("buscar/{termino}")]
        public async Task<ActionResult<object>> BuscarOrdenesPorTermino(string termino)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(termino)) return BadRequest(new { mensaje = "El término de búsqueda no puede estar vacío" });

                var ordenes = await _db.OrdenesServicio
                    .Where(o => o.Falla != null && o.Falla.Contains(termino))
                    .Select(o => new
                    {
                        o.Id,
                        o.FechaCreacion,
                        o.Falla,
                        o.Estado,
                        o.Prioridad,
                        o.SucursalId
                    })
                    .OrderByDescending(o => o.FechaCreacion)
                    .ToListAsync();

                return Ok(ordenes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al buscar órdenes", error = ex.Message });
            }
        }
    
        // GET: api/ordenservicio/{id}/diagnostico-resumen
        [HttpGet("{id}/diagnostico-resumen")]
        public async Task<ActionResult<object>> GetDiagnosticoResumen(int id)
        {
            try
            {
                var orden = await _db.OrdenesServicio
                    .Include(o => o.Equipo).ThenInclude(e => e.Cliente)
                    .Include(o => o.Sucursal) // Incluida en el resumen de costos
                    .Include(o => o.Diagnosticos)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (orden == null) return NotFound(new { mensaje = $"Orden con ID {id} no encontrada" });

                var costoTotalDiagnosticos = orden.Diagnosticos != null ? orden.Diagnosticos.Sum(d => d.CostoRep + d.CostoRef) : 0;

                var resumen = new
                {
                    orden.Id,
                    orden.Presupuesto,
                    CostoTotalDiagnosticos = costoTotalDiagnosticos,
                    SuperaPresupuesto = costoTotalDiagnosticos > orden.Presupuesto,
                    SucursalNombre = orden.Sucursal?.Name, // Datos de ubicación agregados
                    Cliente = orden.Equipo?.Cliente != null ? new { orden.Equipo.Cliente.Nombre } : null,
                    Equipo = orden.Equipo != null ? new { orden.Equipo.Marca, orden.Equipo.Modelo } : null
                };

                return Ok(resumen);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener resumen", error = ex.Message });
            }
        }
    }
}