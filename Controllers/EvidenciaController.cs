using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ordersmanagement.Interface;
using ordersmanagement.Models.requests;
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
        private readonly IAlmacenamiento _IAlmacenamiento;

        public EvidenciaController(
            ApplicationDbContext db,
            IAlmacenamiento almacenamiento
            )
        {
            _IAlmacenamiento = almacenamiento;
            _db = db;
        }

        // GET: api/evidencia
        [HttpGet]
        public async Task<ActionResult<object>> GetEvidencias()
        {
            try
            {
                var evidencias = await _db.Evidencias
                    .Include(e => e.OrdenServicio)
                    .Select(e => new
                    {
                        e.Id, // Modificado
                        e.Descripcion,
                        e.Url,
                        e.Registro,
                        e.Extension,
                        e.OrdenServicioId,
                        OrdenServicio = e.OrdenServicio != null ? new
                        {
                            e.OrdenServicio.Id, // Modificado
                            e.OrdenServicio.Falla,
                            e.OrdenServicio.Estado,
                            e.OrdenServicio.FechaCreacion,
                            Equipo = e.OrdenServicio.Equipo != null ? new
                            {
                                e.OrdenServicio.Equipo.Id, // Modificado
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
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetEvidencia(int id)
        {
            try
            {
                var evidencia = await _db.Evidencias
                    .Include(e => e.OrdenServicio)
                        .ThenInclude(o => o!.Equipo)
                    .Where(e => e.Id == id) // Modificado
                    .Select(e => new
                    {
                        e.Id, // Modificado
                        e.Descripcion,
                        e.Url,
                        e.Registro,
                        e.Extension,
                        e.OrdenServicioId,
                        OrdenServicio = e.OrdenServicio != null ? new
                        {
                            e.OrdenServicio.Id, // Modificado
                            e.OrdenServicio.Falla,
                            e.OrdenServicio.Estado,
                            e.OrdenServicio.Prioridad,
                            e.OrdenServicio.FechaCreacion,
                            e.OrdenServicio.FechaCierre,
                            e.OrdenServicio.Presupuesto,
                            Equipo = e.OrdenServicio.Equipo != null ? new
                            {
                                e.OrdenServicio.Equipo.Id, // Modificado
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
        [HttpGet("orden/{ordenServicioId}")]
        public async Task<ActionResult<object>> GetEvidenciasPorOrden(int ordenServicioId)
        {
            try
            {
                var ordenExiste = await _db.OrdenesServicio
                    .AnyAsync(o => o.Id == ordenServicioId); // Modificado

                if (!ordenExiste)
                {
                    return NotFound(new { mensaje = $"Orden de servicio con ID {ordenServicioId} no encontrada" });
                }

                var evidencias = await _db.Evidencias
                    .Where(e => e.OrdenServicioId == ordenServicioId)
                    .Select(e => new
                    {
                        e.Id, // Modificado
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
                        e.Id, // Modificado
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
                        e.Id, // Modificado
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
        // [HttpPost]
        // public async Task<ActionResult<object>> CreateEvidencia([FromBody] Evidencia evidencia)
        // {
        //     try
        //     {
        //         if (!ModelState.IsValid)
        //         {
        //             return BadRequest(new
        //             {
        //                 mensaje = "Datos inválidos",
        //                 errores = ModelState.Values
        //                     .SelectMany(v => v.Errors)
        //                     .Select(e => e.ErrorMessage)
        //             });
        //         }

        //         var ordenServicio = await _db.OrdenesServicio
        //             .FirstOrDefaultAsync(o => o.Id == evidencia.OrdenServicioId); // Modificado

        //         if (ordenServicio == null)
        //         {
        //             return BadRequest(new { mensaje = $"La orden de servicio con ID {evidencia.OrdenServicioId} no existe" });
        //         }

        //         var extensionesPermitidas = new[] { ".jpg", ".jpeg", ".png", ".pdf", ".mp4", ".zip" };
        //         if (!extensionesPermitidas.Contains(evidencia.Extension?.ToLower()))
        //         {
        //             return BadRequest(new { mensaje = "Extensión no permitida", extensionesPermitidas, extensionRecibida = evidencia.Extension });
        //         }

        //         if (evidencia.Registro == null)
        //         {
        //             var mexicoZone = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time (Mexico)");
        //             evidencia.Registro = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, mexicoZone);
        //         }

        //         await _db.Evidencias.AddAsync(evidencia);
        //         await _db.SaveChangesAsync();

        //         var evidenciaCreada = new
        //         {
        //             evidencia.Id, // Modificado
        //             evidencia.Descripcion,
        //             evidencia.Url,
        //             evidencia.Registro,
        //             evidencia.Extension,
        //             evidencia.OrdenServicioId,
        //             OrdenServicio = new
        //             {
        //                 ordenServicio.Id, // Modificado
        //                 ordenServicio.Falla,
        //                 ordenServicio.Estado
        //             }
        //         };

        //         return CreatedAtAction(nameof(GetEvidencia), new { id = evidencia.Id }, new // Modificado
        //         {
        //             mensaje = "Evidencia creada exitosamente",
        //             cliente = evidenciaCreada
        //         });
        //     }
        //     catch (Exception ex)
        //     {
        //         return StatusCode(500, new { mensaje = "Error interno del servidor", error = ex.Message });
        //     }
        // }

        [HttpPost]
        public async Task<IActionResult> Create([FromForm]RequestEvidencia evidencia)
        {
            if (!ModelState.IsValid) return BadRequest(evidencia);

            string url = "";
            string ext = "";
            if (evidencia.File is not null && evidencia.File.Length > 0)
            {
                ext = Path.GetExtension(evidencia.File.FileName);
                url = await _IAlmacenamiento.AlmacenarImagen("files", evidencia.File);
            }

            var mexicoZone = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time (Mexico)");

            Evidencia file = new Evidencia
            {
                Descripcion = evidencia.Descripcion,
                Extension = ext,
                Url = url,
                Registro = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, mexicoZone),
                OrdenServicioId = evidencia.OrdenServicioId
            };

            await _db.Evidencias.AddAsync(file);
            await _db.SaveChangesAsync();
            
            return Ok(new {
                mensaje = "Evidencia creada exitosamente",
            });
        }

        // PUT: api/evidencia/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEvidencia(int id, [FromBody] Evidencia evidencia)
        {
            try
            {
                if (id != evidencia.Id) // Modificado
                {
                    return BadRequest(new { mensaje = "El ID de la URL no coincide con el ID de la evidencia", urlId = id, bodyId = evidencia.Id });
                }

                var evidenciaExistente = await _db.Evidencias.FindAsync(id);

                if (evidenciaExistente == null)
                {
                    return NotFound(new { mensaje = $"Evidencia con ID {id} no encontrada" });
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(new { mensaje = "Datos inválidos", errores = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
                }

                var ordenServicio = await _db.OrdenesServicio
                    .FirstOrDefaultAsync(o => o.Id == evidencia.OrdenServicioId); // Modificado

                if (ordenServicio == null)
                {
                    return BadRequest(new { mensaje = $"La orden de servicio con ID {evidencia.OrdenServicioId} no existe" });
                }

                var extensionesPermitidas = new[] { ".jpg", ".jpeg", ".png", ".pdf", ".mp4", ".zip" };
                if (!extensionesPermitidas.Contains(evidencia.Extension?.ToLower()))
                {
                    return BadRequest(new { mensaje = "Extensión no permitida", extensionesPermitidas, extensionRecibida = evidencia.Extension });
                }

                evidenciaExistente.Descripcion = evidencia.Descripcion;
                evidenciaExistente.Url = evidencia.Url;
                evidenciaExistente.Extension = evidencia.Extension;
                evidenciaExistente.OrdenServicioId = evidencia.OrdenServicioId;

                _db.Entry(evidenciaExistente).State = EntityState.Modified;
                await _db.SaveChangesAsync();

                var evidenciaActualizada = new
                {
                    evidenciaExistente.Id, // Modificado
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
                        return BadRequest(new { mensaje = "Extensión no permitida", extensionesPermitidas, extensionRecibida = extension });
                    }
                    evidencia.Extension = extension;
                }

                if (updates.TryGetProperty("OrdenServicioId", out var ordenIdProp))
                {
                    var nuevaOrdenId = ordenIdProp.GetInt32();
                    var ordenExiste = await _db.OrdenesServicio
                        .AnyAsync(o => o.Id == nuevaOrdenId); // Modificado

                    if (!ordenExiste)
                    {
                        return BadRequest(new { mensaje = $"La orden de servicio con ID {nuevaOrdenId} no existe" });
                    }
                    evidencia.OrdenServicioId = nuevaOrdenId;
                }

                if (updates.TryGetProperty("Registro", out _))
                {
                    return BadRequest(new { mensaje = "No se puede modificar la fecha de registro" });
                }

                await _db.SaveChangesAsync();

                var evidenciaActualizada = new
                {
                    evidencia.Id, // Modificado
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
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEvidencia(int id)
        {
            try
            {
                var evidencia = await _db.Evidencias
                    .Include(e => e.OrdenServicio)
                    .FirstOrDefaultAsync(e => e.Id == id); // Modificado

                if (evidencia == null)
                {
                    return NotFound(new { mensaje = $"Evidencia con ID {id} no encontrada", evidenciaId = id });
                }

                var evidenciaInfo = new
                {
                    evidencia.Id, // Modificado
                    evidencia.Descripcion,
                    evidencia.Url,
                    evidencia.Extension,
                    evidencia.Registro,
                    OrdenServicioId = evidencia.OrdenServicioId,
                    OrdenInfo = evidencia.OrdenServicio != null ? new
                    {
                        evidencia.OrdenServicio.Id, // Modificado
                        evidencia.OrdenServicio.Falla,
                        evidencia.OrdenServicio.Estado
                    } : null
                };

                await _IAlmacenamiento.Eliminar("files", evidencia.Url);

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
                return StatusCode(409, new { mensaje = "Error de concurrencia. La evidencia fue modificada por otro usuario", evidenciaId = id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error interno del servidor al eliminar la evidencia", error = ex.Message, evidenciaId = id });
            }
        }

        // DELETE: api/evidencia/orden/{ordenServicioId}
        [HttpDelete("orden/{ordenServicioId}")]
        public async Task<IActionResult> DeleteEvidenciasPorOrden(int ordenServicioId)
        {
            try
            {
                var ordenExiste = await _db.OrdenesServicio
                    .AnyAsync(o => o.Id == ordenServicioId); // Modificado

                if (!ordenExiste)
                {
                    return NotFound(new { mensaje = $"Orden de servicio con ID {ordenServicioId} no encontrada" });
                }

                var evidencias = await _db.Evidencias
                    .Where(e => e.OrdenServicioId == ordenServicioId)
                    .ToListAsync();

                if (evidencias == null || evidencias.Count == 0)
                {
                    return NotFound(new { mensaje = $"No hay evidencias para eliminar de la orden {ordenServicioId}" });
                }

                var cantidadEliminadas = evidencias.Count;
                var idsEliminados = evidencias.Select(e => e.Id).ToList(); // Modificado

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
                return StatusCode(500, new { mensaje = "Error al eliminar las evidencias de la orden", error = ex.Message, ordenServicioId = ordenServicioId });
            }
        }
    }
}