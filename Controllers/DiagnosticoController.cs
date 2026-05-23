using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrdersManagement.Data;
using OrdersManagement.Models;
using System.Text.Json;
using OrdersManagement.Services;

namespace OrdersManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DiagnosticoController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly WebSocketService _webSocketService;

        public DiagnosticoController(ApplicationDbContext db, WebSocketService webSocketService)
        {
            _db = db;
            _webSocketService = webSocketService;
        }

        // GET: api/diagnostico
        [HttpGet]
        public async Task<ActionResult<object>> GetDiagnosticos()
        {
            try
            {
                var diagnosticos = await _db.Diagnosticos
                    .Include(d => d.OrdenServicio)
                        .ThenInclude(o => o!.Equipo)
                    .Select(d => new
                    {
                        d.Id, // Modificado
                        d.DiagnosticoFalla,
                        d.CostoRep,
                        d.CostoRef,
                        d.OrdenServicioId,
                        CostoTotal = d.CostoRep + d.CostoRef,
                        OrdenServicio = d.OrdenServicio != null ? new
                        {
                            d.OrdenServicio.Id, // Modificado
                            d.OrdenServicio.Falla,
                            d.OrdenServicio.Estado,
                            d.OrdenServicio.Prioridad,
                            d.OrdenServicio.FechaCreacion,
                            Equipo = d.OrdenServicio.Equipo != null ? new
                            {
                                d.OrdenServicio.Equipo.Id, // Modificado
                                d.OrdenServicio.Equipo.Marca,
                                d.OrdenServicio.Equipo.Modelo,
                                d.OrdenServicio.Equipo.Serie,
                                d.OrdenServicio.Equipo.TipoEquipo
                            } : null
                        } : null
                    })
                    .OrderByDescending(d => d.Id) // Modificado
                    .ToListAsync();

                if (diagnosticos == null || diagnosticos.Count == 0)
                {
                    return NotFound(new { mensaje = "No hay diagnósticos registrados" });
                }

                var estadisticas = new
                {
                    TotalDiagnosticos = diagnosticos.Count,
                    CostoTotalGeneral = diagnosticos.Sum(d => d.CostoTotal),
                    CostoRepPromedio = diagnosticos.Average(d => d.CostoRep),
                    CostoRefPromedio = diagnosticos.Average(d => d.CostoRef),
                    DiagnosticoMasCaro = diagnosticos.OrderByDescending(d => d.CostoTotal).FirstOrDefault(),
                    DiagnosticoMasBarato = diagnosticos.OrderBy(d => d.CostoTotal).FirstOrDefault()
                };

                return Ok(new
                {
                    mensaje = "Diagnósticos obtenidos exitosamente",
                    total = diagnosticos.Count,
                    estadisticas = estadisticas,
                    diagnosticos = diagnosticos
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener diagnósticos", error = ex.Message });
            }
        }

        // GET: api/diagnostico/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetDiagnostico(int id)
        {
            try
            {
                var diagnostico = await _db.Diagnosticos
                    .Include(d => d.OrdenServicio)
                        .ThenInclude(o => o!.Equipo)
                    .Where(d => d.Id == id) // Modificado
                    .Select(d => new
                    {
                        d.Id, // Modificado
                        d.DiagnosticoFalla,
                        d.CostoRep,
                        d.CostoRef,
                        d.OrdenServicioId,
                        CostoTotal = d.CostoRep + d.CostoRef,
                        OrdenServicio = d.OrdenServicio != null ? new
                        {
                            d.OrdenServicio.Id, // Modificado
                            d.OrdenServicio.Falla,
                            d.OrdenServicio.Estado,
                            d.OrdenServicio.Prioridad,
                            d.OrdenServicio.Presupuesto,
                            d.OrdenServicio.FechaCreacion,
                            d.OrdenServicio.FechaCierre,
                            Equipo = d.OrdenServicio.Equipo != null ? new
                            {
                                d.OrdenServicio.Equipo.Id, // Modificado
                                d.OrdenServicio.Equipo.Marca,
                                d.OrdenServicio.Equipo.Modelo,
                                d.OrdenServicio.Equipo.Serie,
                                d.OrdenServicio.Equipo.TipoEquipo
                            } : null,
                            Cliente = d.OrdenServicio.Equipo != null && d.OrdenServicio.Equipo.Cliente != null ? new
                            {
                                d.OrdenServicio.Equipo.Cliente.Id, // Modificado
                                d.OrdenServicio.Equipo.Cliente.Nombre,
                                d.OrdenServicio.Equipo.Cliente.Telefono
                            } : null
                        } : null
                    })
                    .FirstOrDefaultAsync();

                if (diagnostico == null)
                {
                    return NotFound(new { mensaje = $"Diagnóstico con ID {id} no encontrado" });
                }

                return Ok(new
                {
                    mensaje = "Diagnóstico encontrado exitosamente",
                    diagnostico = diagnostico
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al buscar diagnóstico", error = ex.Message });
            }
        }

        // GET: api/diagnostico/orden/{ordenServicioId}
        [HttpGet("orden/{ordenServicioId}")]
        public async Task<ActionResult<object>> GetDiagnosticosPorOrden(int ordenServicioId)
        {
            try
            {
                var ordenExiste = await _db.OrdenesServicio
                    .AnyAsync(o => o.Id == ordenServicioId); // Modificado

                if (!ordenExiste)
                {
                    return NotFound(new { mensaje = $"Orden de servicio con ID {ordenServicioId} no encontrada" });
                }

                var diagnosticos = await _db.Diagnosticos
                    .Where(d => d.OrdenServicioId == ordenServicioId)
                    .Select(d => new
                    {
                        d.Id, // Modificado
                        d.DiagnosticoFalla,
                        d.CostoRep,
                        d.CostoRef,
                        d.OrdenServicioId,
                        CostoTotal = d.CostoRep + d.CostoRef
                    })
                    .OrderByDescending(d => d.Id) // Modificado
                    .ToListAsync();

                if (diagnosticos == null || diagnosticos.Count == 0)
                {
                    return NotFound(new { mensaje = $"No hay diagnósticos para la orden de servicio {ordenServicioId}" });
                }

                return Ok(new
                {
                    mensaje = $"Diagnósticos de la orden de servicio {ordenServicioId}",
                    ordenServicioId = ordenServicioId,
                    total = diagnosticos.Count,
                    costoTotalOrden = diagnosticos.Sum(d => d.CostoTotal),
                    diagnosticos = diagnosticos
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener diagnósticos por orden", error = ex.Message });
            }
        }

        // GET: api/diagnostico/rango-costos
        [HttpGet("rango-costos")]
        public async Task<ActionResult<object>> GetDiagnosticosPorRangoCostos(
            [FromQuery] decimal? costoMin,
            [FromQuery] decimal? costoMax)
        {
            try
            {
                var query = _db.Diagnosticos
                    .Include(d => d.OrdenServicio)
                    .AsQueryable();

                if (costoMin.HasValue)
                {
                    query = query.Where(d => (d.CostoRep + d.CostoRef) >= costoMin.Value);
                }

                if (costoMax.HasValue)
                {
                    query = query.Where(d => (d.CostoRep + d.CostoRef) <= costoMax.Value);
                }

                var diagnosticos = await query
                    .Select(d => new
                    {
                        d.Id, // Modificado
                        d.DiagnosticoFalla,
                        d.CostoRep,
                        d.CostoRef,
                        d.OrdenServicioId,
                        CostoTotal = d.CostoRep + d.CostoRef,
                        OrdenServicio = d.OrdenServicio != null ? new
                        {
                            d.OrdenServicio.Id, // Modificado
                            d.OrdenServicio.Estado
                        } : null
                    })
                    .OrderBy(d => d.CostoTotal)
                    .ToListAsync();

                if (diagnosticos == null || diagnosticos.Count == 0)
                {
                    return NotFound(new { mensaje = "No hay diagnósticos en el rango de costos especificado" });
                }

                return Ok(new
                {
                    mensaje = "Diagnósticos obtenidos exitosamente",
                    filtros = new { costoMin, costoMax },
                    total = diagnosticos.Count,
                    costoPromedio = diagnosticos.Average(d => d.CostoTotal),
                    costoMinimo = diagnosticos.Min(d => d.CostoTotal),
                    costoMaximo = diagnosticos.Max(d => d.CostoTotal),
                    diagnosticos = diagnosticos
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener diagnósticos por rango de costos", error = ex.Message });
            }
        }

        // POST: api/diagnostico
        [HttpPost]
        public async Task<ActionResult<object>> CreateDiagnostico([FromBody] Diagnostico diagnostico)
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

                var ordenServicio = await _db.OrdenesServicio
                    .Include(o => o.Equipo)
                        .ThenInclude(e => e!.Cliente)
                    .FirstOrDefaultAsync(o => o.Id == diagnostico.OrdenServicioId); // Modificado

                if (ordenServicio == null)
                {
                    return BadRequest(new { mensaje = $"La orden de servicio con ID {diagnostico.OrdenServicioId} no existe" });
                }

                if (ordenServicio.Estado == "Finalizada")
                {
                    return BadRequest(new { mensaje = "No se puede agregar un diagnóstico a una orden finalizada" });
                }

                var diagnosticosExistentes = await _db.Diagnosticos
                    .Where(d => d.OrdenServicioId == diagnostico.OrdenServicioId)
                    .ToListAsync();
                
                var costoTotalActual = diagnosticosExistentes.Sum(d => d.CostoRep + d.CostoRef);
                var nuevoCostoTotal = costoTotalActual + diagnostico.CostoRep + diagnostico.CostoRef;
                
                bool superaPresupuesto = nuevoCostoTotal > ordenServicio.Presupuesto;

                await _db.Diagnosticos.AddAsync(diagnostico);
                await _db.SaveChangesAsync();

                if (superaPresupuesto)
                {
                    var mensajeAlerta = new
                    {
                        tipo = "presupuesto_excedido",
                        ordenServicioId = ordenServicio.Id, // Modificado
                        clienteNombre = ordenServicio.Equipo?.Cliente?.Nombre ?? "Cliente no especificado",
                        equipoDescripcion = $"{ordenServicio.Equipo?.Marca} {ordenServicio.Equipo?.Modelo} - {ordenServicio.Equipo?.Serie}",
                        presupuesto = ordenServicio.Presupuesto,
                        costoTotal = nuevoCostoTotal,
                        diferencia = nuevoCostoTotal - ordenServicio.Presupuesto,
                        fecha = DateTime.Now,
                        diagnosticos = new
                        {
                            existentes = diagnosticosExistentes.Count,
                            nuevo = new { diagnostico.Id, diagnostico.DiagnosticoFalla, costo = diagnostico.CostoRep + diagnostico.CostoRef } // Modificado
                        }
                    };

                    await _webSocketService.SendAlertToClientsAsync(JsonSerializer.Serialize(mensajeAlerta));
                }

                return Ok(new
                {
                    mensaje = superaPresupuesto 
                        ? "Diagnóstico creado. ATENCIÓN: El costo total supera el presupuesto"
                        : "Diagnóstico creado exitosamente",
                    superaPresupuesto = superaPresupuesto,
                    presupuesto = ordenServicio.Presupuesto,
                    costoTotalAnterior = costoTotalActual,
                    costoTotalActual = nuevoCostoTotal,
                    diferencia = nuevoCostoTotal - ordenServicio.Presupuesto,
                    diagnostico = new
                    {
                        diagnostico.Id, // Modificado
                        diagnostico.DiagnosticoFalla,
                        diagnostico.CostoRep,
                        diagnostico.CostoRef,
                        CostoTotal = diagnostico.CostoRep + diagnostico.CostoRef
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error interno del servidor", error = ex.Message });
            }
        }

        // PUT: api/diagnostico/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDiagnostico(int id, [FromBody] Diagnostico diagnostico)
        {
            try
            {
                var diagnosticoExistente = await _db.Diagnosticos
                    .FirstOrDefaultAsync(d => d.Id == id); // Modificado

                if (diagnosticoExistente == null)
                {
                    return NotFound(new { mensaje = $"Diagnóstico con ID {id} no encontrado" });
                }

                var ordenServicio = await _db.OrdenesServicio
                    .Include(o => o.Equipo)
                        .ThenInclude(e => e.Cliente)
                    .FirstOrDefaultAsync(o => o.Id == diagnostico.OrdenServicioId); // Modificado

                if (ordenServicio == null)
                {
                    return BadRequest(new { mensaje = $"La orden de servicio con ID {diagnostico.OrdenServicioId} no existe" });
                }

                var otrosDiagnosticos = await _db.Diagnosticos
                    .Where(d => d.OrdenServicioId == diagnostico.OrdenServicioId && d.Id != id) // Modificado
                    .ToListAsync();
                
                var costoOtros = otrosDiagnosticos.Sum(d => d.CostoRep + d.CostoRef);
                var nuevoCostoTotal = costoOtros + diagnostico.CostoRep + diagnostico.CostoRef;
                
                bool superaPresupuesto = nuevoCostoTotal > ordenServicio.Presupuesto;

                var costoAnterior = diagnosticoExistente.CostoRep + diagnosticoExistente.CostoRef;
                var costoAnteriorTotal = costoOtros + costoAnterior;

                diagnosticoExistente.DiagnosticoFalla = diagnostico.DiagnosticoFalla;
                diagnosticoExistente.CostoRep = diagnostico.CostoRep;
                diagnosticoExistente.CostoRef = diagnostico.CostoRef;

                await _db.SaveChangesAsync();

                bool empeoroSituacion = superaPresupuesto && !(costoAnteriorTotal > ordenServicio.Presupuesto);

                if (superaPresupuesto)
                {
                    var mensajeAlerta = new
                    {
                        tipo = "presupuesto_excedido_actualizado",
                        ordenServicioId = ordenServicio.Id, // Modificado
                        clienteNombre = ordenServicio.Equipo?.Cliente?.Nombre ?? "Cliente no especificado",
                        equipoDescripcion = $"{ordenServicio.Equipo?.Marca} {ordenServicio.Equipo?.Modelo}",
                        presupuesto = ordenServicio.Presupuesto,
                        costoAnterior = costoAnteriorTotal,
                        costoActual = nuevoCostoTotal,
                        diferencia = nuevoCostoTotal - ordenServicio.Presupuesto,
                        fecha = DateTime.Now,
                        mensajeAdicional = empeoroSituacion ? "Se ha superado el presupuesto con esta actualización" : "El presupuesto sigue siendo superado"
                    };

                    await _webSocketService.SendAlertToClientsAsync(JsonSerializer.Serialize(mensajeAlerta));
                }

                return Ok(new
                {
                    mensaje = superaPresupuesto 
                        ? "Diagnóstico actualizado. ATENCIÓN: El costo total supera el presupuesto"
                        : "Diagnóstico actualizado exitosamente",
                    superaPresupuesto = superaPresupuesto,
                    presupuesto = ordenServicio.Presupuesto,
                    costoTotalAnterior = costoAnteriorTotal,
                    costoTotalActual = nuevoCostoTotal,
                    diferencia = nuevoCostoTotal - ordenServicio.Presupuesto
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error interno del servidor", error = ex.Message });
            }
        }

        // PATCH: api/diagnostico/{id}
        [HttpPatch("{id}")]
        public async Task<IActionResult> PatchDiagnostico(int id, [FromBody] JsonElement updates)
        {
            try
            {
                var diagnostico = await _db.Diagnosticos
                    .Include(d => d.OrdenServicio)
                    .FirstOrDefaultAsync(d => d.Id == id); // Modificado

                if (diagnostico == null)
                {
                    return NotFound(new { mensaje = $"Diagnóstico con ID {id} no encontrado" });
                }

                if (diagnostico.OrdenServicio?.Estado == "Finalizada")
                {
                    return BadRequest(new { mensaje = "No se puede modificar un diagnóstico de una orden finalizada" });
                }

                if (updates.TryGetProperty("DiagnosticoFalla", out var fallaProp))
                {
                    diagnostico.DiagnosticoFalla = fallaProp.GetString();
                }

                if (updates.TryGetProperty("CostoRep", out var costoRepProp))
                {
                    var costoRep = costoRepProp.GetDecimal();
                    if (costoRep < 0)
                    {
                        return BadRequest(new { mensaje = "El costo de reparación no puede ser negativo" });
                    }
                    diagnostico.CostoRep = costoRep;
                }

                if (updates.TryGetProperty("CostoRef", out var costoRefProp))
                {
                    var costoRef = costoRefProp.GetDecimal();
                    if (costoRef < 0)
                    {
                        return BadRequest(new { mensaje = "El costo de refacción no puede ser negativo" });
                    }
                    diagnostico.CostoRef = costoRef;
                }

                if (updates.TryGetProperty("OrdenServicioId", out var ordenIdProp))
                {
                    var nuevaOrdenId = ordenIdProp.GetInt32();
                    var ordenExiste = await _db.OrdenesServicio
                        .FirstOrDefaultAsync(o => o.Id == nuevaOrdenId); // Modificado

                    if (ordenExiste == null)
                    {
                        return BadRequest(new { mensaje = $"La orden de servicio con ID {nuevaOrdenId} no existe" });
                    }

                    if (ordenExiste.Estado == "Finalizada")
                    {
                        return BadRequest(new { mensaje = "No se puede asignar el diagnóstico a una orden finalizada" });
                    }

                    diagnostico.OrdenServicioId = nuevaOrdenId;
                }

                await _db.SaveChangesAsync();

                var diagnosticoActualizado = new
                {
                    diagnostico.Id, // Modificado
                    diagnostico.DiagnosticoFalla,
                    diagnostico.CostoRep,
                    diagnostico.CostoRef,
                    diagnostico.OrdenServicioId,
                    CostoTotal = diagnostico.CostoRep + diagnostico.CostoRef
                };

                return Ok(new
                {
                    mensaje = "Diagnóstico actualizado exitosamente",
                    diagnostico = diagnosticoActualizado
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al actualizar el diagnóstico", error = ex.Message });
            }
        }

        // DELETE: api/diagnostico/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDiagnostico(int id)
        {
            try
            {
                var diagnostico = await _db.Diagnosticos
                    .Include(d => d.OrdenServicio)
                    .FirstOrDefaultAsync(d => d.Id == id); // Modificado

                if (diagnostico == null)
                {
                    return NotFound(new
                    {
                        mensaje = $"Diagnóstico con ID {id} no encontrado",
                        diagnosticoId = id
                    });
                }

                if (diagnostico.OrdenServicio?.Estado == "Finalizada")
                {
                    return BadRequest(new
                    {
                        mensaje = "No se puede eliminar un diagnóstico de una orden finalizada",
                        diagnosticoId = id,
                        ordenServicioId = diagnostico.OrdenServicioId
                    });
                }

                var diagnosticoInfo = new
                {
                    diagnostico.Id, // Modificado
                    diagnostico.DiagnosticoFalla,
                    diagnostico.CostoRep,
                    diagnostico.CostoRef,
                    diagnostico.OrdenServicioId,
                    CostoTotal = diagnostico.CostoRep + diagnostico.CostoRef,
                    OrdenInfo = diagnostico.OrdenServicio != null ? new
                    {
                        diagnostico.OrdenServicio.Id, // Modificado
                        diagnostico.OrdenServicio.Estado
                    } : null
                };

                _db.Diagnosticos.Remove(diagnostico);
                await _db.SaveChangesAsync();

                return Ok(new
                {
                    mensaje = $"Diagnóstico eliminado exitosamente",
                    diagnosticoEliminado = diagnosticoInfo,
                    fechaEliminacion = DateTime.Now
                });
            }
            catch (DbUpdateConcurrencyException)
            {
                return StatusCode(409, new { mensaje = "Error de concurrencia. El diagnóstico fue modificado por otro usuario", diagnosticoId = id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error interno del servidor al eliminar el diagnóstico", error = ex.Message, diagnosticoId = id });
            }
        }

        // DELETE: api/diagnostico/orden/{ordenServicioId}
        [HttpDelete("orden/{ordenServicioId}")]
        public async Task<IActionResult> DeleteDiagnosticosPorOrden(int ordenServicioId)
        {
            try
            {
                var ordenServicio = await _db.OrdenesServicio
                    .FirstOrDefaultAsync(o => o.Id == ordenServicioId); // Modificado

                if (ordenServicio == null)
                {
                    return NotFound(new { mensaje = $"Orden de servicio con ID {ordenServicioId} no encontrada" });
                }

                if (ordenServicio.Estado == "Finalizada")
                {
                    return BadRequest(new { mensaje = "No se pueden eliminar diagnósticos de una orden finalizada", ordenServicioId = ordenServicioId });
                }

                var diagnosticos = await _db.Diagnosticos
                    .Where(d => d.OrdenServicioId == ordenServicioId)
                    .ToListAsync();

                if (diagnosticos == null || diagnosticos.Count == 0)
                {
                    return NotFound(new { mensaje = $"No hay diagnósticos para eliminar de la orden {ordenServicioId}" });
                }

                var cantidadEliminados = diagnosticos.Count;
                var idsEliminados = diagnosticos.Select(d => d.Id).ToList(); // Modificado
                var costoTotalEliminado = diagnosticos.Sum(d => d.CostoRep + d.CostoRef);

                _db.Diagnosticos.RemoveRange(diagnosticos);
                await _db.SaveChangesAsync();

                return Ok(new
                {
                    mensaje = $"Se eliminaron {cantidadEliminados} diagnósticos de la orden de servicio {ordenServicioId}",
                    ordenServicioId = ordenServicioId,
                    cantidadEliminados = cantidadEliminados,
                    idsEliminados = idsEliminados,
                    costoTotalEliminado = costoTotalEliminado,
                    fechaEliminacion = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al eliminar los diagnósticos de la orden", error = ex.Message, ordenServicioId = ordenServicioId });
            }
        }

        // GET: api/diagnostico/resumen-costos
        [HttpGet("resumen-costos")]
        public async Task<ActionResult<object>> GetResumenCosts()
        {
            try
            {
                var diagnosticos = await _db.Diagnosticos
                    .Include(d => d.OrdenServicio)
                    .ToListAsync();

                if (diagnosticos == null || diagnosticos.Count == 0)
                {
                    return NotFound(new { mensaje = "No hay diagnósticos registrados" });
                }

                var resumen = new
                {
                    TotalDiagnosticos = diagnosticos.Count,
                    SumaTotal = diagnosticos.Sum(d => d.CostoRep + d.CostoRef),
                    PromedioReparacion = diagnosticos.Average(d => d.CostoRep),
                    PromedioRefaccion = diagnosticos.Average(d => d.CostoRef),
                    MaximoCosto = diagnosticos.Max(d => d.CostoRep + d.CostoRef),
                    MinimoCosto = diagnosticos.Min(d => d.CostoRep + d.CostoRef),
                    DistribucionPorRango = new
                    {
                        Bajo = diagnosticos.Count(d => (d.CostoRep + d.CostoRef) < 500),
                        Medio = diagnosticos.Count(d => (d.CostoRep + d.CostoRef) >= 500 && (d.CostoRep + d.CostoRef) < 1000),
                        Alto = diagnosticos.Count(d => (d.CostoRep + d.CostoRef) >= 1000)
                    }
                };

                return Ok(new
                {
                    mensaje = "Resumen de costos de diagnósticos",
                    resumen = resumen
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener resumen de costos", error = ex.Message });
            }
        }
    }
}