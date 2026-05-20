using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrdersManagement.Data;
using OrdersManagement.Models;
using System.Text.Json;

namespace OrdersManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdenServicioController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public OrdenServicioController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET: api/ordenservicio
        // Listar todas las órdenes de servicio
        [HttpGet]
        public async Task<ActionResult<object>> GetOrdenesServicio()
        {
            try
            {
                var ordenes = await _db.OrdenesServicio
                    .Include(o => o.Usuario)
                    .Include(o => o.Equipo)
                        .ThenInclude(e => e!.Cliente)
                    .Include(o => o.Diagnosticos)
                    .Include(o => o.Evidencias)
                    .Select(o => new
                    {
                        o.OrdenServicioId,
                        o.FechaCreacion,
                        o.FechaCierre,
                        o.Falla,
                        o.Estado,
                        o.Prioridad,
                        o.Presupuesto,
                        o.UsuarioId,
                        o.EquipoId,
                        // Información del usuario sin referencias circulares
                        Usuario = o.Usuario != null ? new
                        {
                            o.Usuario.UsuarioId,
                            o.Usuario.Fullname,
                            o.Usuario.Correo,
                            o.Usuario.Rol,
                            o.Usuario.Especialidad,
                            o.Usuario.Telefono
                        } : null,
                        // Información del equipo sin referencias circulares
                        Equipo = o.Equipo != null ? new
                        {
                            o.Equipo.EquipoId,
                            o.Equipo.Marca,
                            o.Equipo.Modelo,
                            o.Equipo.Serie,
                            o.Equipo.TipoEquipo,
                            Cliente = o.Equipo.Cliente != null ? new
                            {
                                o.Equipo.Cliente.ClienteId,
                                o.Equipo.Cliente.Nombre,
                                o.Equipo.Cliente.Telefono
                            } : null
                        } : null,
                        // Resumen de diagnósticos
                        Diagnosticos = o.Diagnosticos != null && o.Diagnosticos.Any()
                            ? o.Diagnosticos.Select(d => new
                            {
                                d.DiagnosticoId,
                                d.DiagnosticoFalla,
                                d.CostoRep,
                                d.CostoRef,
                                CostoTotal = d.CostoRep + d.CostoRef
                            }).ToList()
                            : null,
                        // Resumen de evidencias
                        Evidencias = o.Evidencias != null && o.Evidencias.Any()
                            ? o.Evidencias.Select(e => new
                            {
                                e.EvidenciaId,
                                e.Descripcion,
                                e.Url,
                                e.Extension,
                                e.Registro
                            }).ToList()
                            : null,
                        // Estadísticas de la orden
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
                    })
                    .OrderByDescending(o => o.FechaCreacion)
                    .ToListAsync();

                if (ordenes == null || ordenes.Count == 0)
                {
                    return NotFound(new { mensaje = "No hay órdenes de servicio registradas" });
                }

                // Estadísticas generales
                var estadisticas = new
                {
                    TotalOrdenes = ordenes.Count,
                    Pendientes = ordenes.Count(o => o.Estado == "Pendiente"),
                    Finalizadas = ordenes.Count(o => o.Estado == "Finalizada"),
                    PrioridadAlta = ordenes.Count(o => o.Prioridad == "Alta"),
                    PrioridadMedia = ordenes.Count(o => o.Prioridad == "Media"),
                    PrioridadBaja = ordenes.Count(o => o.Prioridad == "Baja"),
                    PresupuestoTotal = ordenes.Sum(o => o.Presupuesto),
                    CostoTotalDiagnosticos = ordenes.Sum(o => o.Estadisticas.CostoTotalDiagnosticos)
                };

                return Ok(new
                {
                    mensaje = "Órdenes de servicio obtenidas exitosamente",
                    total = ordenes.Count,
                    estadisticas = estadisticas,
                    ordenes = ordenes
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener órdenes de servicio", error = ex.Message });
            }
        }

        // GET: api/ordenservicio/{id}
        // Buscar orden de servicio por ID
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetOrdenServicio(int id)
        {
            try
            {
                var orden = await _db.OrdenesServicio
                    .Include(o => o.Usuario)
                    .Include(o => o.Equipo)
                        .ThenInclude(e => e!.Cliente)
                    .Include(o => o.Diagnosticos)
                    .Include(o => o.Evidencias)
                    .Where(o => o.OrdenServicioId == id)
                    .Select(o => new
                    {
                        o.OrdenServicioId,
                        o.FechaCreacion,
                        o.FechaCierre,
                        o.Falla,
                        o.Estado,
                        o.Prioridad,
                        o.Presupuesto,
                        o.UsuarioId,
                        o.EquipoId,
                        // Información del usuario sin referencias circulares
                        Usuario = o.Usuario != null ? new
                        {
                            o.Usuario.UsuarioId,
                            o.Usuario.Fullname,
                            o.Usuario.Correo,
                            o.Usuario.Rol,
                            o.Usuario.Especialidad,
                            o.Usuario.Telefono
                        } : null,
                        // Información del equipo sin referencias circulares
                        Equipo = o.Equipo != null ? new
                        {
                            o.Equipo.EquipoId,
                            o.Equipo.Marca,
                            o.Equipo.Modelo,
                            o.Equipo.Serie,
                            o.Equipo.TipoEquipo,
                            Cliente = o.Equipo.Cliente != null ? new
                            {
                                o.Equipo.Cliente.ClienteId,
                                o.Equipo.Cliente.Nombre,
                                o.Equipo.Cliente.Direccion,
                                o.Equipo.Cliente.Telefono
                            } : null
                        } : null,
                        // Diagnósticos completos
                        Diagnosticos = o.Diagnosticos != null && o.Diagnosticos.Any()
                            ? o.Diagnosticos.Select(d => new
                            {
                                d.DiagnosticoId,
                                d.DiagnosticoFalla,
                                d.CostoRep,
                                d.CostoRef,
                                CostoTotal = d.CostoRep + d.CostoRef
                            }).ToList()
                            : null,
                        // Evidencias completas
                        Evidencias = o.Evidencias != null && o.Evidencias.Any()
                            ? o.Evidencias.Select(e => new
                            {
                                e.EvidenciaId,
                                e.Descripcion,
                                e.Url,
                                e.Extension,
                                e.Registro
                            }).ToList()
                            : null,
                        // Estadísticas detalladas
                        Estadisticas = new
                        {
                            TotalDiagnosticos = o.Diagnosticos != null ? o.Diagnosticos.Count : 0,
                            TotalEvidencias = o.Evidencias != null ? o.Evidencias.Count : 0,
                            CostoTotalReparacion = o.Diagnosticos != null 
                                ? o.Diagnosticos.Sum(d => d.CostoRep) 
                                : 0,
                            CostoTotalRefaccion = o.Diagnosticos != null 
                                ? o.Diagnosticos.Sum(d => d.CostoRef) 
                                : 0,
                            CostoTotalDiagnosticos = o.Diagnosticos != null 
                                ? o.Diagnosticos.Sum(d => d.CostoRep + d.CostoRef) 
                                : 0,
                            DiferenciaPresupuesto = o.Presupuesto - (o.Diagnosticos != null 
                                ? o.Diagnosticos.Sum(d => d.CostoRep + d.CostoRef) 
                                : 0),
                            EstaSobrePresupuesto = (o.Diagnosticos != null 
                                ? o.Diagnosticos.Sum(d => d.CostoRep + d.CostoRef) 
                                : 0) > o.Presupuesto
                        }
                    })
                    .FirstOrDefaultAsync();

                if (orden == null)
                {
                    return NotFound(new { mensaje = $"Orden de servicio con ID {id} no encontrada" });
                }

                return Ok(new
                {
                    mensaje = "Orden de servicio encontrada exitosamente",
                    orden = orden
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al buscar orden de servicio", error = ex.Message });
            }
        }

        // GET: api/ordenservicio/usuario/{usuarioId}
        // Listar órdenes por usuario (técnico)
        [HttpGet("usuario/{usuarioId}")]
        public async Task<ActionResult<object>> GetOrdenesPorUsuario(int usuarioId)
        {
            try
            {
                var usuario = await _db.Usuarios
                    .Where(u => u.UsuarioId == usuarioId)
                    .Select(u => new { u.UsuarioId, u.Fullname, u.Rol })
                    .FirstOrDefaultAsync();

                if (usuario == null)
                {
                    return NotFound(new { mensaje = $"Usuario con ID {usuarioId} no encontrado" });
                }

                var ordenes = await _db.OrdenesServicio
                    .Where(o => o.UsuarioId == usuarioId)
                    .Include(o => o.Equipo)
                    .Select(o => new
                    {
                        o.OrdenServicioId,
                        o.FechaCreacion,
                        o.FechaCierre,
                        o.Falla,
                        o.Estado,
                        o.Prioridad,
                        o.Presupuesto,
                        Equipo = o.Equipo != null ? new
                        {
                            o.Equipo.EquipoId,
                            o.Equipo.Marca,
                            o.Equipo.Modelo,
                            o.Equipo.Serie,
                            o.Equipo.TipoEquipo
                        } : null,
                        TotalDiagnosticos = o.Diagnosticos != null ? o.Diagnosticos.Count : 0,
                        CostoTotal = o.Diagnosticos != null 
                            ? o.Diagnosticos.Sum(d => d.CostoRep + d.CostoRef) 
                            : 0
                    })
                    .OrderByDescending(o => o.FechaCreacion)
                    .ToListAsync();

                if (ordenes == null || ordenes.Count == 0)
                {
                    return Ok(new
                    {
                        mensaje = $"El usuario {usuario.Fullname} no tiene órdenes de servicio asignadas",
                        usuario = usuario,
                        total = 0,
                        ordenes = new List<object>()
                    });
                }

                return Ok(new
                {
                    mensaje = $"Órdenes de servicio del usuario {usuario.Fullname}",
                    usuario = usuario,
                    total = ordenes.Count,
                    pendientes = ordenes.Count(o => o.Estado == "Pendiente"),
                    finalizadas = ordenes.Count(o => o.Estado == "Finalizada"),
                    ordenes = ordenes
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener órdenes por usuario", error = ex.Message });
            }
        }

        // GET: api/ordenservicio/equipo/{equipoId}
        // Listar órdenes por equipo
        [HttpGet("equipo/{equipoId}")]
        public async Task<ActionResult<object>> GetOrdenesPorEquipo(int equipoId)
        {
            try
            {
                var equipo = await _db.Equipos
                    .Where(e => e.EquipoId == equipoId)
                    .Select(e => new { e.EquipoId, e.Marca, e.Modelo, e.Serie })
                    .FirstOrDefaultAsync();

                if (equipo == null)
                {
                    return NotFound(new { mensaje = $"Equipo con ID {equipoId} no encontrado" });
                }

                var ordenes = await _db.OrdenesServicio
                    .Where(o => o.EquipoId == equipoId)
                    .Select(o => new
                    {
                        o.OrdenServicioId,
                        o.FechaCreacion,
                        o.FechaCierre,
                        o.Falla,
                        o.Estado,
                        o.Prioridad,
                        o.Presupuesto,
                        o.UsuarioId,
                        UsuarioNombre = o.Usuario != null ? o.Usuario.Fullname : null,
                        TotalDiagnosticos = o.Diagnosticos != null ? o.Diagnosticos.Count : 0,
                        CostoTotal = o.Diagnosticos != null 
                            ? o.Diagnosticos.Sum(d => d.CostoRep + d.CostoRef) 
                            : 0
                    })
                    .OrderByDescending(o => o.FechaCreacion)
                    .ToListAsync();

                if (ordenes == null || ordenes.Count == 0)
                {
                    return Ok(new
                    {
                        mensaje = $"El equipo {equipo.Marca} {equipo.Modelo} no tiene órdenes de servicio",
                        equipo = equipo,
                        total = 0,
                        ordenes = new List<object>()
                    });
                }

                return Ok(new
                {
                    mensaje = $"Órdenes de servicio del equipo {equipo.Marca} {equipo.Modelo}",
                    equipo = equipo,
                    total = ordenes.Count,
                    pendientes = ordenes.Count(o => o.Estado == "Pendiente"),
                    finalizadas = ordenes.Count(o => o.Estado == "Finalizada"),
                    ordenes = ordenes
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener órdenes por equipo", error = ex.Message });
            }
        }

        // GET: api/ordenservicio/estado/{estado}
        // Listar órdenes por estado
        [HttpGet("estado/{estado}")]
        public async Task<ActionResult<object>> GetOrdenesPorEstado(string estado)
        {
            try
            {
                if (estado != "Pendiente" && estado != "Finalizada")
                {
                    return BadRequest(new
                    {
                        mensaje = "El estado debe ser 'Pendiente' o 'Finalizada'",
                        estadoInvalido = estado,
                        valoresPermitidos = new[] { "Pendiente", "Finalizada" }
                    });
                }

                var ordenes = await _db.OrdenesServicio
                    .Where(o => o.Estado == estado)
                    .Include(o => o.Usuario)
                    .Include(o => o.Equipo)
                    .Select(o => new
                    {
                        o.OrdenServicioId,
                        o.FechaCreacion,
                        o.FechaCierre,
                        o.Falla,
                        o.Prioridad,
                        o.Presupuesto,
                        Usuario = o.Usuario != null ? new
                        {
                            o.Usuario.UsuarioId,
                            o.Usuario.Fullname
                        } : null,
                        Equipo = o.Equipo != null ? new
                        {
                            o.Equipo.EquipoId,
                            o.Equipo.Marca,
                            o.Equipo.Modelo,
                            o.Equipo.Serie
                        } : null,
                        TotalDiagnosticos = o.Diagnosticos != null ? o.Diagnosticos.Count : 0,
                        CostoTotal = o.Diagnosticos != null 
                            ? o.Diagnosticos.Sum(d => d.CostoRep + d.CostoRef) 
                            : 0
                    })
                    .OrderByDescending(o => o.FechaCreacion)
                    .ToListAsync();

                if (ordenes == null || ordenes.Count == 0)
                {
                    return NotFound(new { mensaje = $"No hay órdenes con estado {estado}" });
                }

                return Ok(new
                {
                    mensaje = $"Órdenes con estado {estado}",
                    estado = estado,
                    total = ordenes.Count,
                    ordenes = ordenes
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener órdenes por estado", error = ex.Message });
            }
        }

        // GET: api/ordenservicio/prioridad/{prioridad}
        // Listar órdenes por prioridad
        [HttpGet("prioridad/{prioridad}")]
        public async Task<ActionResult<object>> GetOrdenesPorPrioridad(string prioridad)
        {
            try
            {
                if (prioridad != "Alta" && prioridad != "Media" && prioridad != "Baja")
                {
                    return BadRequest(new
                    {
                        mensaje = "La prioridad debe ser 'Alta', 'Media' o 'Baja'",
                        prioridadInvalida = prioridad,
                        valoresPermitidos = new[] { "Alta", "Media", "Baja" }
                    });
                }

                var ordenes = await _db.OrdenesServicio
                    .Where(o => o.Prioridad == prioridad)
                    .Select(o => new
                    {
                        o.OrdenServicioId,
                        o.FechaCreacion,
                        o.Falla,
                        o.Estado,
                        o.Prioridad,
                        o.Presupuesto,
                        TecnicoNombre = o.Usuario != null ? o.Usuario.Fullname : null,
                        EquipoInfo = o.Equipo != null 
                            ? $"{o.Equipo.Marca} {o.Equipo.Modelo} - {o.Equipo.Serie}"
                            : null,
                        CostoTotal = o.Diagnosticos != null 
                            ? o.Diagnosticos.Sum(d => d.CostoRep + d.CostoRef) 
                            : 0
                    })
                    .OrderBy(o => o.FechaCreacion)
                    .ToListAsync();

                if (ordenes == null || ordenes.Count == 0)
                {
                    return NotFound(new { mensaje = $"No hay órdenes con prioridad {prioridad}" });
                }

                return Ok(new
                {
                    mensaje = $"Órdenes con prioridad {prioridad}",
                    prioridad = prioridad,
                    total = ordenes.Count,
                    pendientes = ordenes.Count(o => o.Estado == "Pendiente"),
                    finalizadas = ordenes.Count(o => o.Estado == "Finalizada"),
                    ordenes = ordenes
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener órdenes por prioridad", error = ex.Message });
            }
        }

        // GET: api/ordenservicio/rango-fechas
        // Listar órdenes por rango de fechas
        [HttpGet("rango-fechas")]
        public async Task<ActionResult<object>> GetOrdenesPorRangoFechas(
            [FromQuery] DateTime fechaInicio,
            [FromQuery] DateTime fechaFin)
        {
            try
            {
                if (fechaInicio > fechaFin)
                {
                    return BadRequest(new { mensaje = "La fecha de inicio debe ser menor que la fecha de fin" });
                }

                var ordenes = await _db.OrdenesServicio
                    .Where(o => o.FechaCreacion >= fechaInicio && o.FechaCreacion <= fechaFin)
                    .Select(o => new
                    {
                        o.OrdenServicioId,
                        o.FechaCreacion,
                        o.FechaCierre,
                        o.Falla,
                        o.Estado,
                        o.Prioridad,
                        o.Presupuesto,
                        TecnicoNombre = o.Usuario != null ? o.Usuario.Fullname : null,
                        CostoTotal = o.Diagnosticos != null 
                            ? o.Diagnosticos.Sum(d => d.CostoRep + d.CostoRef) 
                            : 0
                    })
                    .OrderBy(o => o.FechaCreacion)
                    .ToListAsync();

                if (ordenes == null || ordenes.Count == 0)
                {
                    return NotFound(new { mensaje = $"No hay órdenes en el rango de fechas {fechaInicio:dd/MM/yyyy} - {fechaFin:dd/MM/yyyy}" });
                }

                return Ok(new
                {
                    mensaje = $"Órdenes del {fechaInicio:dd/MM/yyyy} al {fechaFin:dd/MM/yyyy}",
                    fechaInicio = fechaInicio,
                    fechaFin = fechaFin,
                    total = ordenes.Count,
                    pendientes = ordenes.Count(o => o.Estado == "Pendiente"),
                    finalizadas = ordenes.Count(o => o.Estado == "Finalizada"),
                    costoTotalGeneral = ordenes.Sum(o => o.CostoTotal),
                    ordenes = ordenes
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener órdenes por rango de fechas", error = ex.Message });
            }
        }

        // POST: api/ordenservicio
        // Crear nueva orden de servicio
        [HttpPost]
        public async Task<ActionResult<object>> CreateOrdenServicio([FromBody] OrdenServicio orden)
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

                // Validar estado
                if (orden.Estado != "Pendiente" && orden.Estado != "Finalizada")
                {
                    return BadRequest(new { mensaje = "El estado debe ser 'Pendiente' o 'Finalizada'" });
                }

                // Validar prioridad
                if (orden.Prioridad != "Alta" && orden.Prioridad != "Media" && orden.Prioridad != "Baja")
                {
                    return BadRequest(new { mensaje = "La prioridad debe ser 'Alta', 'Media' o 'Baja'" });
                }

                // Validar que el usuario exista
                var usuario = await _db.Usuarios.FindAsync(orden.UsuarioId);
                if (usuario == null)
                {
                    return BadRequest(new { mensaje = $"El usuario con ID {orden.UsuarioId} no existe" });
                }

                // Validar que el equipo exista
                var equipo = await _db.Equipos.FindAsync(orden.EquipoId);
                if (equipo == null)
                {
                    return BadRequest(new { mensaje = $"El equipo con ID {orden.EquipoId} no existe" });
                }

                // Validar presupuesto
                if (orden.Presupuesto < 0)
                {
                    return BadRequest(new { mensaje = "El presupuesto no puede ser negativo" });
                }

                // Inicializar colecciones
                if (orden.Diagnosticos == null)
                {
                    orden.Diagnosticos = new HashSet<Diagnostico>();
                }

                if (orden.Evidencias == null)
                {
                    orden.Evidencias = new HashSet<Evidencia>();
                }

                // Si la orden se crea como Finalizada, establecer FechaCierre
                if (orden.Estado == "Finalizada")
                {
                    var mexicoZone = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time (Mexico)");
                    orden.FechaCierre = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, mexicoZone);
                }

                // Agregar orden
                await _db.OrdenesServicio.AddAsync(orden);
                await _db.SaveChangesAsync();

                // Respuesta sin referencias circulares
                var ordenCreada = new
                {
                    orden.OrdenServicioId,
                    orden.FechaCreacion,
                    orden.FechaCierre,
                    orden.Falla,
                    orden.Estado,
                    orden.Prioridad,
                    orden.Presupuesto,
                    orden.UsuarioId,
                    orden.EquipoId,
                    Usuario = new
                    {
                        usuario.UsuarioId,
                        usuario.Fullname,
                        usuario.Rol
                    },
                    Equipo = new
                    {
                        equipo.EquipoId,
                        equipo.Marca,
                        equipo.Modelo,
                        equipo.Serie,
                        equipo.TipoEquipo
                    }
                };

                return CreatedAtAction(nameof(GetOrdenServicio), new { id = orden.OrdenServicioId }, new
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
        // Editar orden de servicio completa
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOrdenServicio(int id, [FromBody] OrdenServicio orden)
        {
            try
            {
                // Validar que el ID coincida
                if (id != orden.OrdenServicioId)
                {
                    return BadRequest(new
                    {
                        mensaje = "El ID de la URL no coincide con el ID de la orden",
                        urlId = id,
                        bodyId = orden.OrdenServicioId
                    });
                }

                // Buscar la orden existente
                var ordenExistente = await _db.OrdenesServicio
                    .Include(o => o.Diagnosticos)
                    .FirstOrDefaultAsync(o => o.OrdenServicioId == id);

                if (ordenExistente == null)
                {
                    return NotFound(new { mensaje = $"Orden de servicio con ID {id} no encontrada" });
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

                // Validar estado
                if (orden.Estado != "Pendiente" && orden.Estado != "Finalizada")
                {
                    return BadRequest(new { mensaje = "El estado debe ser 'Pendiente' o 'Finalizada'" });
                }

                // Validar prioridad
                if (orden.Prioridad != "Alta" && orden.Prioridad != "Media" && orden.Prioridad != "Baja")
                {
                    return BadRequest(new { mensaje = "La prioridad debe ser 'Alta', 'Media' o 'Baja'" });
                }

                // Validar que el usuario exista
                var usuario = await _db.Usuarios.FindAsync(orden.UsuarioId);
                if (usuario == null)
                {
                    return BadRequest(new { mensaje = $"El usuario con ID {orden.UsuarioId} no existe" });
                }

                // Validar que el equipo exista
                var equipo = await _db.Equipos.FindAsync(orden.EquipoId);
                if (equipo == null)
                {
                    return BadRequest(new { mensaje = $"El equipo con ID {orden.EquipoId} no existe" });
                }

                // Validar presupuesto
                if (orden.Presupuesto < 0)
                {
                    return BadRequest(new { mensaje = "El presupuesto no puede ser negativo" });
                }

                // Si se cambia a Finalizada y antes estaba Pendiente, actualizar FechaCierre
                var mexicoZone = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time (Mexico)");
                if (orden.Estado == "Finalizada" && ordenExistente.Estado == "Pendiente")
                {
                    orden.FechaCierre = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, mexicoZone);
                }
                else if (orden.Estado == "Pendiente" && ordenExistente.Estado == "Finalizada")
                {
                    // No permitir cambiar de Finalizada a Pendiente
                    return BadRequest(new { mensaje = "No se puede cambiar una orden finalizada a pendiente" });
                }
                else
                {
                    orden.FechaCierre = ordenExistente.FechaCierre;
                }

                // Actualizar campos (no actualizar FechaCreacion)
                ordenExistente.Falla = orden.Falla;
                ordenExistente.Estado = orden.Estado;
                ordenExistente.Prioridad = orden.Prioridad;
                ordenExistente.Presupuesto = orden.Presupuesto;
                ordenExistente.UsuarioId = orden.UsuarioId;
                ordenExistente.EquipoId = orden.EquipoId;
                ordenExistente.FechaCierre = orden.FechaCierre;

                _db.Entry(ordenExistente).State = EntityState.Modified;
                await _db.SaveChangesAsync();

                // Respuesta sin referencias circulares
                var ordenActualizada = new
                {
                    ordenExistente.OrdenServicioId,
                    ordenExistente.FechaCreacion,
                    ordenExistente.FechaCierre,
                    ordenExistente.Falla,
                    ordenExistente.Estado,
                    ordenExistente.Prioridad,
                    ordenExistente.Presupuesto,
                    ordenExistente.UsuarioId,
                    ordenExistente.EquipoId
                };

                return Ok(new
                {
                    mensaje = "Orden de servicio actualizada exitosamente",
                    orden = ordenActualizada
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
        // Editar orden de servicio parcialmente
        [HttpPatch("{id}")]
        public async Task<IActionResult> PatchOrdenServicio(int id, [FromBody] JsonElement updates)
        {
            try
            {
                var orden = await _db.OrdenesServicio.FindAsync(id);

                if (orden == null)
                {
                    return NotFound(new { mensaje = $"Orden de servicio con ID {id} no encontrada" });
                }

                var mexicoZone = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time (Mexico)");
                var estadoAnterior = orden.Estado;

                // Aplicar actualizaciones solo a los campos enviados
                if (updates.TryGetProperty("Falla", out var fallaProp))
                {
                    orden.Falla = fallaProp.GetString();
                }

                if (updates.TryGetProperty("Estado", out var estadoProp))
                {
                    var nuevoEstado = estadoProp.GetString();
                    if (nuevoEstado != "Pendiente" && nuevoEstado != "Finalizada")
                    {
                        return BadRequest(new { mensaje = "El estado debe ser 'Pendiente' o 'Finalizada'" });
                    }

                    if (nuevoEstado == "Finalizada" && estadoAnterior == "Pendiente")
                    {
                        orden.FechaCierre = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, mexicoZone);
                    }
                    else if (nuevoEstado == "Pendiente" && estadoAnterior == "Finalizada")
                    {
                        return BadRequest(new { mensaje = "No se puede cambiar una orden finalizada a pendiente" });
                    }

                    orden.Estado = nuevoEstado;
                }

                if (updates.TryGetProperty("Prioridad", out var prioridadProp))
                {
                    var prioridad = prioridadProp.GetString();
                    if (prioridad != "Alta" && prioridad != "Media" && prioridad != "Baja")
                    {
                        return BadRequest(new { mensaje = "La prioridad debe ser 'Alta', 'Media' o 'Baja'" });
                    }
                    orden.Prioridad = prioridad;
                }

                if (updates.TryGetProperty("Presupuesto", out var presupuestoProp))
                {
                    var presupuesto = presupuestoProp.GetDecimal();
                    if (presupuesto < 0)
                    {
                        return BadRequest(new { mensaje = "El presupuesto no puede ser negativo" });
                    }
                    orden.Presupuesto = presupuesto;
                }

                if (updates.TryGetProperty("UsuarioId", out var usuarioIdProp))
                {
                    var nuevoUsuarioId = usuarioIdProp.GetInt32();
                    var usuarioExiste = await _db.Usuarios.AnyAsync(u => u.UsuarioId == nuevoUsuarioId);
                    if (!usuarioExiste)
                    {
                        return BadRequest(new { mensaje = $"El usuario con ID {nuevoUsuarioId} no existe" });
                    }
                    orden.UsuarioId = nuevoUsuarioId;
                }

                if (updates.TryGetProperty("EquipoId", out var equipoIdProp))
                {
                    var nuevoEquipoId = equipoIdProp.GetInt32();
                    var equipoExiste = await _db.Equipos.AnyAsync(e => e.EquipoId == nuevoEquipoId);
                    if (!equipoExiste)
                    {
                        return BadRequest(new { mensaje = $"El equipo con ID {nuevoEquipoId} no existe" });
                    }
                    orden.EquipoId = nuevoEquipoId;
                }

                // No permitir actualizar FechaCreacion
                if (updates.TryGetProperty("FechaCreacion", out _))
                {
                    return BadRequest(new { mensaje = "No se puede modificar la fecha de creación" });
                }

                await _db.SaveChangesAsync();

                var ordenActualizada = new
                {
                    orden.OrdenServicioId,
                    orden.FechaCreacion,
                    orden.FechaCierre,
                    orden.Falla,
                    orden.Estado,
                    orden.Prioridad,
                    orden.Presupuesto,
                    orden.UsuarioId,
                    orden.EquipoId
                };

                return Ok(new
                {
                    mensaje = "Orden de servicio actualizada exitosamente",
                    orden = ordenActualizada
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al actualizar la orden", error = ex.Message });
            }
        }

        // DELETE: api/ordenservicio/{id}
        // Eliminar orden de servicio
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrdenServicio(int id)
        {
            try
            {
                // Buscar la orden con sus relaciones
                var orden = await _db.OrdenesServicio
                    .Include(o => o.Diagnosticos)
                    .Include(o => o.Evidencias)
                    .FirstOrDefaultAsync(o => o.OrdenServicioId == id);

                if (orden == null)
                {
                    return NotFound(new
                    {
                        mensaje = $"Orden de servicio con ID {id} no encontrada",
                        ordenId = id
                    });
                }

                // Guardar información para la respuesta
                var ordenInfo = new
                {
                    orden.OrdenServicioId,
                    orden.Falla,
                    orden.Estado,
                    orden.Prioridad,
                    orden.FechaCreacion,
                    TotalDiagnosticos = orden.Diagnosticos != null ? orden.Diagnosticos.Count : 0,
                    TotalEvidencias = orden.Evidencias != null ? orden.Evidencias.Count : 0
                };

                // Eliminar la orden (los diagnósticos y evidencias se eliminarán en cascada si está configurado)
                _db.OrdenesServicio.Remove(orden);
                await _db.SaveChangesAsync();

                return Ok(new
                {
                    mensaje = $"Orden de servicio #{id} eliminada exitosamente",
                    ordenEliminada = ordenInfo,
                    fechaEliminacion = DateTime.Now
                });
            }
            catch (DbUpdateConcurrencyException)
            {
                return StatusCode(409, new
                {
                    mensaje = "Error de concurrencia. La orden fue modificada por otro usuario",
                    ordenId = id
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    mensaje = "Error interno del servidor al eliminar la orden",
                    error = ex.Message,
                    ordenId = id
                });
            }
        }

        // POST: api/ordenservicio/{id}/finalizar
        // Finalizar una orden de servicio
        [HttpPost("{id}/finalizar")]
        public async Task<IActionResult> FinalizarOrden(int id)
        {
            try
            {
                var orden = await _db.OrdenesServicio
                    .Include(o => o.Diagnosticos)
                    .FirstOrDefaultAsync(o => o.OrdenServicioId == id);

                if (orden == null)
                {
                    return NotFound(new { mensaje = $"Orden de servicio con ID {id} no encontrada" });
                }

                if (orden.Estado == "Finalizada")
                {
                    return BadRequest(new { mensaje = "La orden ya está finalizada" });
                }

                var mexicoZone = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time (Mexico)");
                orden.Estado = "Finalizada";
                orden.FechaCierre = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, mexicoZone);

                _db.Entry(orden).State = EntityState.Modified;
                await _db.SaveChangesAsync();

                return Ok(new
                {
                    mensaje = $"Orden de servicio #{id} finalizada exitosamente",
                    ordenId = id,
                    fechaCierre = orden.FechaCierre,
                    costoTotalDiagnosticos = orden.Diagnosticos != null 
                        ? orden.Diagnosticos.Sum(d => d.CostoRep + d.CostoRef) 
                        : 0
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al finalizar la orden", error = ex.Message });
            }
        }

        // GET: api/ordenservicio/buscar/{termino}
        // Buscar órdenes por término en falla
        [HttpGet("buscar/{termino}")]
        public async Task<ActionResult<object>> BuscarOrdenesPorTermino(string termino)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(termino))
                {
                    return BadRequest(new { mensaje = "El término de búsqueda no puede estar vacío" });
                }

                var ordenes = await _db.OrdenesServicio
                    .Where(o => o.Falla != null && o.Falla.Contains(termino))
                    .Select(o => new
                    {
                        o.OrdenServicioId,
                        o.FechaCreacion,
                        o.Falla,
                        o.Estado,
                        o.Prioridad,
                        TecnicoNombre = o.Usuario != null ? o.Usuario.Fullname : null,
                        EquipoInfo = o.Equipo != null 
                            ? $"{o.Equipo.Marca} {o.Equipo.Modelo}" 
                            : null
                    })
                    .OrderByDescending(o => o.FechaCreacion)
                    .ToListAsync();

                if (ordenes == null || ordenes.Count == 0)
                {
                    return NotFound(new { mensaje = $"No se encontraron órdenes con '{termino}' en la descripción" });
                }

                return Ok(new
                {
                    mensaje = $"Órdenes encontradas con '{termino}'",
                    terminoBusqueda = termino,
                    total = ordenes.Count,
                    ordenes = ordenes
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al buscar órdenes", error = ex.Message });
            }
        }
    }
}