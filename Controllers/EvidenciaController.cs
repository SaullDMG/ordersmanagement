using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrdersManagement.Data;
using OrdersManagement.Models;
using System.Text.Json;

namespace OrdersManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EvidenciaController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public EvidenciaController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET: api/evidencia
        // Listar todas las evidencias
        [HttpGet]
        public async Task<ActionResult<object>> GetEvidencias()
        {
            try
            {
                var evidencias = await _db.Evidencias
                    .Include(e => e.OrdenServicio)
                    .Select(e => new
                    {
                        e.EvidenciaId,
                        e.Descripcion,
                        e.Url,
                        e.Registro,
                        e.Extension,
                        e.OrdenServicioId,
                        // Información de la orden de servicio sin referencias circulares
                        OrdenServicio = e.OrdenServicio != null ? new
                        {
                            e.OrdenServicio.OrdenServicioId,
                            e.OrdenServicio.Falla,
                            e.OrdenServicio.Estado,
                            e.OrdenServicio.FechaCreacion,
                            // No incluir la colección de evidencias para evitar ciclo
                            Equipo = e.OrdenServicio.Equipo != null ? new
                            {
                                e.OrdenServicio.Equipo.EquipoId,
                                e.OrdenServicio.Equipo.Marca,
                                e.OrdenServicio.Equipo.Modelo,
                                e.OrdenServicio.Equipo.Serie
                            } : null
                        } : null
                    })
                    .OrderByDescending(e => e.Registro)
                    .ToListAsync();

                if (evidencias == null || evidencias.Count == 0)
                {
                    return NotFound(new { mensaje = "No hay evidencias registradas" });
                }

                // Estadísticas generales
                var estadisticas = new
                {
                    TotalEvidencias = evidencias.Count,
                    PorExtension = evidencias
                        .GroupBy(e => e.Extension)
                        .Select(g => new { Extension = g.Key, Cantidad = g.Count() }),
                    EvidenciasConOrden = evidencias.Count(e => e.OrdenServicioId > 0)
                };

                return Ok(new
                {
                    mensaje = "Evidencias obtenidas exitosamente",
                    total = evidencias.Count,
                    estadisticas = estadisticas,
                    evidencias = evidencias
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener evidencias", error = ex.Message });
            }
        }

        // GET: api/evidencia/{id}
        // Buscar evidencia por ID
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetEvidencia(int id)
        {
            try
            {
                var evidencia = await _db.Evidencias
                    .Include(e => e.OrdenServicio)
                        .ThenInclude(o => o!.Equipo)
                    .Where(e => e.EvidenciaId == id)
                    .Select(e => new
                    {
                        e.EvidenciaId,
                        e.Descripcion,
                        e.Url,
                        e.Registro,
                        e.Extension,
                        e.OrdenServicioId,
                        // Información de la orden de servicio sin referencias circulares
                        OrdenServicio = e.OrdenServicio != null ? new
                        {
                            e.OrdenServicio.OrdenServicioId,
                            e.OrdenServicio.Falla,
                            e.OrdenServicio.Estado,
                            e.OrdenServicio.Prioridad,
                            e.OrdenServicio.FechaCreacion,
                            e.OrdenServicio.FechaCierre,
                            e.OrdenServicio.Presupuesto,
                            // Información del equipo
                            Equipo = e.OrdenServicio.Equipo != null ? new
                            {
                                e.OrdenServicio.Equipo.EquipoId,
                                e.OrdenServicio.Equipo.Marca,
                                e.OrdenServicio.Equipo.Modelo,
                                e.OrdenServicio.Equipo.Serie,
                                e.OrdenServicio.Equipo.TipoEquipo
                            } : null
                        } : null
                    })
                    .FirstOrDefaultAsync();

                if (evidencia == null)
                {
                    return NotFound(new { mensaje = $"Evidencia con ID {id} no encontrada" });
                }

                return Ok(new
                {
                    mensaje = "Evidencia encontrada exitosamente",
                    evidencia = evidencia
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al buscar evidencia", error = ex.Message });
            }
        }

        // GET: api/evidencia/orden/{ordenServicioId}
        // Listar evidencias por orden de servicio
        [HttpGet("orden/{ordenServicioId}")]
        public async Task<ActionResult<object>> GetEvidenciasPorOrden(int ordenServicioId)
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

                var evidencias = await _db.Evidencias
                    .Where(e => e.OrdenServicioId == ordenServicioId)
                    .Select(e => new
                    {
                        e.EvidenciaId,
                        e.Descripcion,
                        e.Url,
                        e.Registro,
                        e.Extension,
                        e.OrdenServicioId
                    })
                    .OrderByDescending(e => e.Registro)
                    .ToListAsync();

                if (evidencias == null || evidencias.Count == 0)
                {
                    return NotFound(new { mensaje = $"No hay evidencias para la orden de servicio {ordenServicioId}" });
                }

                return Ok(new
                {
                    mensaje = $"Evidencias de la orden de servicio {ordenServicioId}",
                    ordenServicioId = ordenServicioId,
                    total = evidencias.Count,
                    evidencias = evidencias
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener evidencias por orden", error = ex.Message });
            }
        }

        // GET: api/evidencia/extension/{extension}
        // Listar evidencias por extensión
        [HttpGet("extension/{extension}")]
        public async Task<ActionResult<object>> GetEvidenciasPorExtension(string extension)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(extension))
                {
                    return BadRequest(new { mensaje = "La extensión no puede estar vacía" });
                }

                var evidencias = await _db.Evidencias
                    .Where(e => e.Extension != null && e.Extension.ToLower() == extension.ToLower())
                    .Select(e => new
                    {
                        e.EvidenciaId,
                        e.Descripcion,
                        e.Url,
                        e.Registro,
                        e.Extension,
                        e.OrdenServicioId
                    })
                    .OrderByDescending(e => e.Registro)
                    .ToListAsync();

                if (evidencias == null || evidencias.Count == 0)
                {
                    return NotFound(new { mensaje = $"No hay evidencias con extensión {extension}" });
                }

                return Ok(new
                {
                    mensaje = $"Evidencias con extensión {extension}",
                    extension = extension,
                    total = evidencias.Count,
                    evidencias = evidencias
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener evidencias por extensión", error = ex.Message });
            }
        }

        // GET: api/evidencia/rango-fechas
        // Listar evidencias por rango de fechas
        [HttpGet("rango-fechas")]
        public async Task<ActionResult<object>> GetEvidenciasPorRangoFechas(
            [FromQuery] DateTime fechaInicio,
            [FromQuery] DateTime fechaFin)
        {
            try
            {
                if (fechaInicio > fechaFin)
                {
                    return BadRequest(new { mensaje = "La fecha de inicio debe ser menor que la fecha de fin" });
                }

                var evidencias = await _db.Evidencias
                    .Where(e => e.Registro >= fechaInicio && e.Registro <= fechaFin)
                    .Select(e => new
                    {
                        e.EvidenciaId,
                        e.Descripcion,
                        e.Url,
                        e.Registro,
                        e.Extension,
                        e.OrdenServicioId
                    })
                    .OrderByDescending(e => e.Registro)
                    .ToListAsync();

                if (evidencias == null || evidencias.Count == 0)
                {
                    return NotFound(new { mensaje = $"No hay evidencias en el rango de fechas {fechaInicio:dd/MM/yyyy} - {fechaFin:dd/MM/yyyy}" });
                }

                return Ok(new
                {
                    mensaje = $"Evidencias del {fechaInicio:dd/MM/yyyy} al {fechaFin:dd/MM/yyyy}",
                    fechaInicio = fechaInicio,
                    fechaFin = fechaFin,
                    total = evidencias.Count,
                    evidencias = evidencias
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener evidencias por rango de fechas", error = ex.Message });
            }
        }

        // POST: api/evidencia
        // Crear nueva evidencia
        [HttpPost]
        public async Task<ActionResult<object>> CreateEvidencia([FromBody] Evidencia evidencia)
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

                // Validar que la orden de servicio exista
                var ordenServicio = await _db.OrdenesServicio
                    .FirstOrDefaultAsync(o => o.OrdenServicioId == evidencia.OrdenServicioId);

                if (ordenServicio == null)
                {
                    return BadRequest(new { mensaje = $"La orden de servicio con ID {evidencia.OrdenServicioId} no existe" });
                }

                // Validar extensión (opcional: lista de extensiones permitidas)
                var extensionesPermitidas = new[] { ".jpg", ".jpeg", ".png", ".pdf", ".mp4", ".zip" };
                if (!extensionesPermitidas.Contains(evidencia.Extension?.ToLower()))
                {
                    return BadRequest(new
                    {
                        mensaje = "Extensión no permitida",
                        extensionesPermitidas = extensionesPermitidas,
                        extensionRecibida = evidencia.Extension
                    });
                }

                // La fecha ya se establece en el constructor, pero aseguramos
                if (evidencia.Registro == null)
                {
                    var mexicoZone = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time (Mexico)");
                    evidencia.Registro = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, mexicoZone);
                }

                // Agregar evidencia
                await _db.Evidencias.AddAsync(evidencia);
                await _db.SaveChangesAsync();

                // Respuesta sin referencias circulares
                var evidenciaCreada = new
                {
                    evidencia.EvidenciaId,
                    evidencia.Descripcion,
                    evidencia.Url,
                    evidencia.Registro,
                    evidencia.Extension,
                    evidencia.OrdenServicioId,
                    OrdenServicio = new
                    {
                        ordenServicio.OrdenServicioId,
                        ordenServicio.Falla,
                        ordenServicio.Estado
                    }
                };

                return CreatedAtAction(nameof(GetEvidencia), new { id = evidencia.EvidenciaId }, new
                {
                    mensaje = "Evidencia creada exitosamente",
                    evidencia = evidenciaCreada
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error interno del servidor", error = ex.Message });
            }
        }

        // PUT: api/evidencia/{id}
        // Editar evidencia completa
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEvidencia(int id, [FromBody] Evidencia evidencia)
        {
            try
            {
                // Validar que el ID coincida
                if (id != evidencia.EvidenciaId)
                {
                    return BadRequest(new
                    {
                        mensaje = "El ID de la URL no coincide con el ID de la evidencia",
                        urlId = id,
                        bodyId = evidencia.EvidenciaId
                    });
                }

                // Buscar la evidencia existente
                var evidenciaExistente = await _db.Evidencias.FindAsync(id);

                if (evidenciaExistente == null)
                {
                    return NotFound(new { mensaje = $"Evidencia con ID {id} no encontrada" });
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

                // Validar que la orden de servicio exista
                var ordenServicio = await _db.OrdenesServicio
                    .FirstOrDefaultAsync(o => o.OrdenServicioId == evidencia.OrdenServicioId);

                if (ordenServicio == null)
                {
                    return BadRequest(new { mensaje = $"La orden de servicio con ID {evidencia.OrdenServicioId} no existe" });
                }

                // Validar extensión
                var extensionesPermitidas = new[] { ".jpg", ".jpeg", ".png", ".pdf", ".mp4", ".zip" };
                if (!extensionesPermitidas.Contains(evidencia.Extension?.ToLower()))
                {
                    return BadRequest(new
                    {
                        mensaje = "Extensión no permitida",
                        extensionesPermitidas = extensionesPermitidas,
                        extensionRecibida = evidencia.Extension
                    });
                }

                // Actualizar campos (no actualizar Registro)
                evidenciaExistente.Descripcion = evidencia.Descripcion;
                evidenciaExistente.Url = evidencia.Url;
                evidenciaExistente.Extension = evidencia.Extension;
                evidenciaExistente.OrdenServicioId = evidencia.OrdenServicioId;
                // Registro se mantiene como fue creado

                _db.Entry(evidenciaExistente).State = EntityState.Modified;
                await _db.SaveChangesAsync();

                // Respuesta sin referencias circulares
                var evidenciaActualizada = new
                {
                    evidenciaExistente.EvidenciaId,
                    evidenciaExistente.Descripcion,
                    evidenciaExistente.Url,
                    evidenciaExistente.Registro,
                    evidenciaExistente.Extension,
                    evidenciaExistente.OrdenServicioId
                };

                return Ok(new
                {
                    mensaje = "Evidencia actualizada exitosamente",
                    evidencia = evidenciaActualizada
                });
            }
            catch (DbUpdateConcurrencyException)
            {
                return StatusCode(409, new { mensaje = "Error de concurrencia al actualizar la evidencia" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error interno del servidor", error = ex.Message });
            }
        }

        // PATCH: api/evidencia/{id}
        // Editar evidencia parcialmente
        [HttpPatch("{id}")]
        public async Task<IActionResult> PatchEvidencia(int id, [FromBody] JsonElement updates)
        {
            try
            {
                var evidencia = await _db.Evidencias.FindAsync(id);

                if (evidencia == null)
                {
                    return NotFound(new { mensaje = $"Evidencia con ID {id} no encontrada" });
                }

                // Aplicar actualizaciones solo a los campos enviados
                if (updates.TryGetProperty("Descripcion", out var descripcionProp))
                {
                    evidencia.Descripcion = descripcionProp.GetString();
                }

                if (updates.TryGetProperty("Url", out var urlProp))
                {
                    evidencia.Url = urlProp.GetString();
                }

                if (updates.TryGetProperty("Extension", out var extensionProp))
                {
                    var extension = extensionProp.GetString();
                    var extensionesPermitidas = new[] { ".jpg", ".jpeg", ".png", ".pdf", ".mp4", ".zip" };
                    if (!extensionesPermitidas.Contains(extension?.ToLower()))
                    {
                        return BadRequest(new
                        {
                            mensaje = "Extensión no permitida",
                            extensionesPermitidas = extensionesPermitidas,
                            extensionRecibida = extension
                        });
                    }
                    evidencia.Extension = extension;
                }

                if (updates.TryGetProperty("OrdenServicioId", out var ordenIdProp))
                {
                    var nuevaOrdenId = ordenIdProp.GetInt32();
                    var ordenExiste = await _db.OrdenesServicio
                        .AnyAsync(o => o.OrdenServicioId == nuevaOrdenId);

                    if (!ordenExiste)
                    {
                        return BadRequest(new { mensaje = $"La orden de servicio con ID {nuevaOrdenId} no existe" });
                    }
                    evidencia.OrdenServicioId = nuevaOrdenId;
                }

                // No permitir actualizar Registro
                if (updates.TryGetProperty("Registro", out _))
                {
                    return BadRequest(new { mensaje = "No se puede modificar la fecha de registro" });
                }

                await _db.SaveChangesAsync();

                var evidenciaActualizada = new
                {
                    evidencia.EvidenciaId,
                    evidencia.Descripcion,
                    evidencia.Url,
                    evidencia.Registro,
                    evidencia.Extension,
                    evidencia.OrdenServicioId
                };

                return Ok(new
                {
                    mensaje = "Evidencia actualizada exitosamente",
                    evidencia = evidenciaActualizada
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al actualizar la evidencia", error = ex.Message });
            }
        }

        // DELETE: api/evidencia/{id}
        // Eliminar evidencia
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEvidencia(int id)
        {
            try
            {
                // Buscar la evidencia
                var evidencia = await _db.Evidencias
                    .Include(e => e.OrdenServicio)
                    .FirstOrDefaultAsync(e => e.EvidenciaId == id);

                if (evidencia == null)
                {
                    return NotFound(new
                    {
                        mensaje = $"Evidencia con ID {id} no encontrada",
                        evidenciaId = id
                    });
                }

                // Guardar información para la respuesta
                var evidenciaInfo = new
                {
                    evidencia.EvidenciaId,
                    evidencia.Descripcion,
                    evidencia.Url,
                    evidencia.Extension,
                    evidencia.Registro,
                    OrdenServicioId = evidencia.OrdenServicioId,
                    OrdenInfo = evidencia.OrdenServicio != null ? new
                    {
                        evidencia.OrdenServicio.OrdenServicioId,
                        evidencia.OrdenServicio.Falla,
                        evidencia.OrdenServicio.Estado
                    } : null
                };

                // Eliminar la evidencia
                _db.Evidencias.Remove(evidencia);
                await _db.SaveChangesAsync();

                return Ok(new
                {
                    mensaje = $"Evidencia '{evidencia.Descripcion}' eliminada exitosamente",
                    evidenciaEliminada = evidenciaInfo,
                    fechaEliminacion = DateTime.Now
                });
            }
            catch (DbUpdateConcurrencyException)
            {
                return StatusCode(409, new
                {
                    mensaje = "Error de concurrencia. La evidencia fue modificada por otro usuario",
                    evidenciaId = id
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    mensaje = "Error interno del servidor al eliminar la evidencia",
                    error = ex.Message,
                    evidenciaId = id
                });
            }
        }

        // DELETE: api/evidencia/orden/{ordenServicioId}
        // Eliminar todas las evidencias de una orden de servicio
        [HttpDelete("orden/{ordenServicioId}")]
        public async Task<IActionResult> DeleteEvidenciasPorOrden(int ordenServicioId)
        {
            try
            {
                // Verificar si la orden existe
                var ordenExiste = await _db.OrdenesServicio
                    .AnyAsync(o => o.OrdenServicioId == ordenServicioId);

                if (!ordenExiste)
                {
                    return NotFound(new { mensaje = $"Orden de servicio con ID {ordenServicioId} no encontrada" });
                }

                // Obtener las evidencias a eliminar
                var evidencias = await _db.Evidencias
                    .Where(e => e.OrdenServicioId == ordenServicioId)
                    .ToListAsync();

                if (evidencias == null || evidencias.Count == 0)
                {
                    return NotFound(new { mensaje = $"No hay evidencias para eliminar de la orden {ordenServicioId}" });
                }

                var cantidadEliminadas = evidencias.Count;
                var idsEliminados = evidencias.Select(e => e.EvidenciaId).ToList();

                _db.Evidencias.RemoveRange(evidencias);
                await _db.SaveChangesAsync();

                return Ok(new
                {
                    mensaje = $"Se eliminaron {cantidadEliminadas} evidencias de la orden de servicio {ordenServicioId}",
                    ordenServicioId = ordenServicioId,
                    cantidadEliminadas = cantidadEliminadas,
                    idsEliminados = idsEliminados,
                    fechaEliminacion = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    mensaje = "Error al eliminar las evidencias de la orden",
                    error = ex.Message,
                    ordenServicioId = ordenServicioId
                });
            }
        }
    }
}