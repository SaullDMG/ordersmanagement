using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrdersManagement.Data;
using OrdersManagement.Models;
using System.Text.Json;

namespace OrdersManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DiagnosticoController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public DiagnosticoController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET: api/diagnostico
        // Listar todos los diagnósticos
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
                        d.DiagnosticoId,
                        d.DiagnosticoFalla,
                        d.CostoRep,
                        d.CostoRef,
                        d.OrdenServicioId,
                        // Calcular costo total
                        CostoTotal = d.CostoRep + d.CostoRef,
                        // Información de la orden de servicio sin referencias circulares
                        OrdenServicio = d.OrdenServicio != null ? new
                        {
                            d.OrdenServicio.OrdenServicioId,
                            d.OrdenServicio.Falla,
                            d.OrdenServicio.Estado,
                            d.OrdenServicio.Prioridad,
                            d.OrdenServicio.FechaCreacion,
                            // Información del equipo
                            Equipo = d.OrdenServicio.Equipo != null ? new
                            {
                                d.OrdenServicio.Equipo.EquipoId,
                                d.OrdenServicio.Equipo.Marca,
                                d.OrdenServicio.Equipo.Modelo,
                                d.OrdenServicio.Equipo.Serie,
                                d.OrdenServicio.Equipo.TipoEquipo
                            } : null
                        } : null
                    })
                    .OrderByDescending(d => d.DiagnosticoId)
                    .ToListAsync();

                if (diagnosticos == null || diagnosticos.Count == 0)
                {
                    return NotFound(new { mensaje = "No hay diagnósticos registrados" });
                }

                // Estadísticas generales
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
        // Buscar diagnóstico por ID
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetDiagnostico(int id)
        {
            try
            {
                var diagnostico = await _db.Diagnosticos
                    .Include(d => d.OrdenServicio)
                        .ThenInclude(o => o!.Equipo)
                    .Where(d => d.DiagnosticoId == id)
                    .Select(d => new
                    {
                        d.DiagnosticoId,
                        d.DiagnosticoFalla,
                        d.CostoRep,
                        d.CostoRef,
                        d.OrdenServicioId,
                        CostoTotal = d.CostoRep + d.CostoRef,
                        // Información de la orden de servicio sin referencias circulares
                        OrdenServicio = d.OrdenServicio != null ? new
                        {
                            d.OrdenServicio.OrdenServicioId,
                            d.OrdenServicio.Falla,
                            d.OrdenServicio.Estado,
                            d.OrdenServicio.Prioridad,
                            d.OrdenServicio.Presupuesto,
                            d.OrdenServicio.FechaCreacion,
                            d.OrdenServicio.FechaCierre,
                            // Información del equipo
                            Equipo = d.OrdenServicio.Equipo != null ? new
                            {
                                d.OrdenServicio.Equipo.EquipoId,
                                d.OrdenServicio.Equipo.Marca,
                                d.OrdenServicio.Equipo.Modelo,
                                d.OrdenServicio.Equipo.Serie,
                                d.OrdenServicio.Equipo.TipoEquipo
                            } : null,
                            // Información del cliente
                            Cliente = d.OrdenServicio.Equipo != null && d.OrdenServicio.Equipo.Cliente != null ? new
                            {
                                d.OrdenServicio.Equipo.Cliente.ClienteId,
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
        // Listar diagnósticos por orden de servicio
        [HttpGet("orden/{ordenServicioId}")]
        public async Task<ActionResult<object>> GetDiagnosticosPorOrden(int ordenServicioId)
        {
            try
            {
                // Verificar si la orden de servicio existe
                var ordenExiste = await _db.OrdenesServicio
                    .AnyAsync(o => o.OrdenServicioId == ordenServicioId);

                if (!ordenExiste)
                {
                    return NotFound(new { mensaje = $"Orden de servicio con ID {ordenServicioId} no encontrada" });
                }

                var diagnosticos = await _db.Diagnosticos
                    .Where(d => d.OrdenServicioId == ordenServicioId)
                    .Select(d => new
                    {
                        d.DiagnosticoId,
                        d.DiagnosticoFalla,
                        d.CostoRep,
                        d.CostoRef,
                        d.OrdenServicioId,
                        CostoTotal = d.CostoRep + d.CostoRef
                    })
                    .OrderByDescending(d => d.DiagnosticoId)
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
        // Listar diagnósticos por rango de costos
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
                        d.DiagnosticoId,
                        d.DiagnosticoFalla,
                        d.CostoRep,
                        d.CostoRef,
                        d.OrdenServicioId,
                        CostoTotal = d.CostoRep + d.CostoRef,
                        OrdenServicio = d.OrdenServicio != null ? new
                        {
                            d.OrdenServicio.OrdenServicioId,
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
        // Crear nuevo diagnóstico
        [HttpPost]
        public async Task<ActionResult<object>> CreateDiagnostico([FromBody] Diagnostico diagnostico)
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

                // Validar que los costos no sean negativos
                if (diagnostico.CostoRep < 0)
                {
                    return BadRequest(new { mensaje = "El costo de reparación no puede ser negativo" });
                }

                if (diagnostico.CostoRef < 0)
                {
                    return BadRequest(new { mensaje = "El costo de refacción no puede ser negativo" });
                }

                // Validar que la orden de servicio exista
                var ordenServicio = await _db.OrdenesServicio
                    .Include(o => o.Equipo)
                    .FirstOrDefaultAsync(o => o.OrdenServicioId == diagnostico.OrdenServicioId);

                if (ordenServicio == null)
                {
                    return BadRequest(new { mensaje = $"La orden de servicio con ID {diagnostico.OrdenServicioId} no existe" });
                }

                // Validar que la orden no esté finalizada
                if (ordenServicio.Estado == "Finalizada")
                {
                    return BadRequest(new { mensaje = "No se puede agregar un diagnóstico a una orden finalizada" });
                }

                // Agregar diagnóstico
                await _db.Diagnosticos.AddAsync(diagnostico);
                await _db.SaveChangesAsync();

                // Respuesta sin referencias circulares
                var diagnosticoCreado = new
                {
                    diagnostico.DiagnosticoId,
                    diagnostico.DiagnosticoFalla,
                    diagnostico.CostoRep,
                    diagnostico.CostoRef,
                    diagnostico.OrdenServicioId,
                    CostoTotal = diagnostico.CostoRep + diagnostico.CostoRef,
                    OrdenServicio = new
                    {
                        ordenServicio.OrdenServicioId,
                        ordenServicio.Falla,
                        ordenServicio.Estado,
                        ordenServicio.Presupuesto,
                        Equipo = ordenServicio.Equipo != null ? new
                        {
                            ordenServicio.Equipo.EquipoId,
                            ordenServicio.Equipo.Marca,
                            ordenServicio.Equipo.Modelo
                        } : null
                    }
                };

                return CreatedAtAction(nameof(GetDiagnostico), new { id = diagnostico.DiagnosticoId }, new
                {
                    mensaje = "Diagnóstico creado exitosamente",
                    diagnostico = diagnosticoCreado
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error interno del servidor", error = ex.Message });
            }
        }

        // PUT: api/diagnostico/{id}
        // Editar diagnóstico completo
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDiagnostico(int id, [FromBody] Diagnostico diagnostico)
        {
            try
            {
                // Validar que el ID coincida
                if (id != diagnostico.DiagnosticoId)
                {
                    return BadRequest(new
                    {
                        mensaje = "El ID de la URL no coincide con el ID del diagnóstico",
                        urlId = id,
                        bodyId = diagnostico.DiagnosticoId
                    });
                }

                // Buscar el diagnóstico existente
                var diagnosticoExistente = await _db.Diagnosticos
                    .Include(d => d.OrdenServicio)
                    .FirstOrDefaultAsync(d => d.DiagnosticoId == id);

                if (diagnosticoExistente == null)
                {
                    return NotFound(new { mensaje = $"Diagnóstico con ID {id} no encontrado" });
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

                // Validar que los costos no sean negativos
                if (diagnostico.CostoRep < 0)
                {
                    return BadRequest(new { mensaje = "El costo de reparación no puede ser negativo" });
                }

                if (diagnostico.CostoRef < 0)
                {
                    return BadRequest(new { mensaje = "El costo de refacción no puede ser negativo" });
                }

                // Validar que la orden de servicio exista
                var ordenServicio = await _db.OrdenesServicio
                    .FirstOrDefaultAsync(o => o.OrdenServicioId == diagnostico.OrdenServicioId);

                if (ordenServicio == null)
                {
                    return BadRequest(new { mensaje = $"La orden de servicio con ID {diagnostico.OrdenServicioId} no existe" });
                }

                // Validar que la orden no esté finalizada
                if (ordenServicio.Estado == "Finalizada")
                {
                    return BadRequest(new { mensaje = "No se puede modificar un diagnóstico de una orden finalizada" });
                }

                // Actualizar campos
                diagnosticoExistente.DiagnosticoFalla = diagnostico.DiagnosticoFalla;
                diagnosticoExistente.CostoRep = diagnostico.CostoRep;
                diagnosticoExistente.CostoRef = diagnostico.CostoRef;
                diagnosticoExistente.OrdenServicioId = diagnostico.OrdenServicioId;

                _db.Entry(diagnosticoExistente).State = EntityState.Modified;
                await _db.SaveChangesAsync();

                // Respuesta sin referencias circulares
                var diagnosticoActualizado = new
                {
                    diagnosticoExistente.DiagnosticoId,
                    diagnosticoExistente.DiagnosticoFalla,
                    diagnosticoExistente.CostoRep,
                    diagnosticoExistente.CostoRef,
                    diagnosticoExistente.OrdenServicioId,
                    CostoTotal = diagnosticoExistente.CostoRep + diagnosticoExistente.CostoRef
                };

                return Ok(new
                {
                    mensaje = "Diagnóstico actualizado exitosamente",
                    diagnostico = diagnosticoActualizado
                });
            }
            catch (DbUpdateConcurrencyException)
            {
                return StatusCode(409, new { mensaje = "Error de concurrencia al actualizar el diagnóstico" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error interno del servidor", error = ex.Message });
            }
        }

        // PATCH: api/diagnostico/{id}
        // Editar diagnóstico parcialmente
        [HttpPatch("{id}")]
        public async Task<IActionResult> PatchDiagnostico(int id, [FromBody] JsonElement updates)
        {
            try
            {
                var diagnostico = await _db.Diagnosticos
                    .Include(d => d.OrdenServicio)
                    .FirstOrDefaultAsync(d => d.DiagnosticoId == id);

                if (diagnostico == null)
                {
                    return NotFound(new { mensaje = $"Diagnóstico con ID {id} no encontrado" });
                }

                // Verificar si la orden está finalizada
                if (diagnostico.OrdenServicio?.Estado == "Finalizada")
                {
                    return BadRequest(new { mensaje = "No se puede modificar un diagnóstico de una orden finalizada" });
                }

                // Aplicar actualizaciones solo a los campos enviados
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
                        .FirstOrDefaultAsync(o => o.OrdenServicioId == nuevaOrdenId);

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
                    diagnostico.DiagnosticoId,
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
        // Eliminar diagnóstico
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDiagnostico(int id)
        {
            try
            {
                // Buscar el diagnóstico
                var diagnostico = await _db.Diagnosticos
                    .Include(d => d.OrdenServicio)
                    .FirstOrDefaultAsync(d => d.DiagnosticoId == id);

                if (diagnostico == null)
                {
                    return NotFound(new
                    {
                        mensaje = $"Diagnóstico con ID {id} no encontrado",
                        diagnosticoId = id
                    });
                }

                // Verificar si la orden está finalizada
                if (diagnostico.OrdenServicio?.Estado == "Finalizada")
                {
                    return BadRequest(new
                    {
                        mensaje = "No se puede eliminar un diagnóstico de una orden finalizada",
                        diagnosticoId = id,
                        ordenServicioId = diagnostico.OrdenServicioId
                    });
                }

                // Guardar información para la respuesta
                var diagnosticoInfo = new
                {
                    diagnostico.DiagnosticoId,
                    diagnostico.DiagnosticoFalla,
                    diagnostico.CostoRep,
                    diagnostico.CostoRef,
                    diagnostico.OrdenServicioId,
                    CostoTotal = diagnostico.CostoRep + diagnostico.CostoRef,
                    OrdenInfo = diagnostico.OrdenServicio != null ? new
                    {
                        diagnostico.OrdenServicio.OrdenServicioId,
                        diagnostico.OrdenServicio.Estado
                    } : null
                };

                // Eliminar el diagnóstico
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
                return StatusCode(409, new
                {
                    mensaje = "Error de concurrencia. El diagnóstico fue modificado por otro usuario",
                    diagnosticoId = id
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    mensaje = "Error interno del servidor al eliminar el diagnóstico",
                    error = ex.Message,
                    diagnosticoId = id
                });
            }
        }

        // DELETE: api/diagnostico/orden/{ordenServicioId}
        // Eliminar todos los diagnósticos de una orden de servicio
        [HttpDelete("orden/{ordenServicioId}")]
        public async Task<IActionResult> DeleteDiagnosticosPorOrden(int ordenServicioId)
        {
            try
            {
                // Verificar si la orden existe
                var ordenServicio = await _db.OrdenesServicio
                    .FirstOrDefaultAsync(o => o.OrdenServicioId == ordenServicioId);

                if (ordenServicio == null)
                {
                    return NotFound(new { mensaje = $"Orden de servicio con ID {ordenServicioId} no encontrada" });
                }

                // Verificar si la orden está finalizada
                if (ordenServicio.Estado == "Finalizada")
                {
                    return BadRequest(new
                    {
                        mensaje = "No se pueden eliminar diagnósticos de una orden finalizada",
                        ordenServicioId = ordenServicioId
                    });
                }

                // Obtener los diagnósticos a eliminar
                var diagnosticos = await _db.Diagnosticos
                    .Where(d => d.OrdenServicioId == ordenServicioId)
                    .ToListAsync();

                if (diagnosticos == null || diagnosticos.Count == 0)
                {
                    return NotFound(new { mensaje = $"No hay diagnósticos para eliminar de la orden {ordenServicioId}" });
                }

                var cantidadEliminados = diagnosticos.Count;
                var idsEliminados = diagnosticos.Select(d => d.DiagnosticoId).ToList();
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
                return StatusCode(500, new
                {
                    mensaje = "Error al eliminar los diagnósticos de la orden",
                    error = ex.Message,
                    ordenServicioId = ordenServicioId
                });
            }
        }

        // GET: api/diagnostico/resumen-costos
        // Resumen de costos por diagnóstico
        [HttpGet("resumen-costos")]
        public async Task<ActionResult<object>> GetResumenCostos()
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