using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrdersManagement.Data;
using OrdersManagement.Models;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;

namespace OrdersManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsuarioController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public UsuarioController(ApplicationDbContext db)
        {
            _db = db;
        }

        // Método privado para hashear contraseñas
        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        // GET: api/usuario
        // Listar todos los usuarios
        [HttpGet]
        public async Task<ActionResult<object>> GetUsuarios()
        {
            try
            {
                var usuarios = await _db.Usuarios
                    .Include(u => u.OrdenesServicio)
                    .Select(u => new
                    {
                        u.UsuarioId,
                        u.Fullname,
                        u.Correo,
                        u.Rol,
                        u.Especialidad,
                        u.Telefono,
                        // No incluir la contraseña por seguridad
                        // Resumen de órdenes sin referencias circulares
                        OrdenesServicio = u.OrdenesServicio != null && u.OrdenesServicio.Any()
                            ? new
                            {
                                Total = u.OrdenesServicio.Count,
                                Pendientes = u.OrdenesServicio.Count(o => o.Estado == "Pendiente"),
                                Finalizadas = u.OrdenesServicio.Count(o => o.Estado == "Finalizada"),
                                UltimasOrdenes = u.OrdenesServicio
                                    .OrderByDescending(o => o.FechaCreacion)
                                    .Take(5)
                                    .Select(o => new
                                    {
                                        o.OrdenServicioId,
                                        o.Falla,
                                        o.Estado,
                                        o.Prioridad,
                                        o.FechaCreacion
                                    })
                            } : null
                    })
                    .OrderBy(u => u.Fullname)
                    .ToListAsync();

                if (usuarios == null || usuarios.Count == 0)
                {
                    return NotFound(new { mensaje = "No hay usuarios registrados" });
                }

                // Estadísticas generales
                var estadisticas = new
                {
                    TotalUsuarios = usuarios.Count,
                    Admins = usuarios.Count(u => u.Rol == "Admin"),
                    Tecnicos = usuarios.Count(u => u.Rol == "Tecnico"),
                    EspecialidadImpresion = usuarios.Count(u => u.Especialidad == "Impresion"),
                    EspecialidadComputo = usuarios.Count(u => u.Especialidad == "Computo"),
                    UsuariosConOrdenes = usuarios.Count(u => u.OrdenesServicio?.Total > 0)
                };

                return Ok(new
                {
                    mensaje = "Usuarios obtenidos exitosamente",
                    total = usuarios.Count,
                    estadisticas = estadisticas,
                    usuarios = usuarios
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener usuarios", error = ex.Message });
            }
        }

        // GET: api/usuario/{id}
        // Buscar usuario por ID
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetUsuario(int id)
        {
            try
            {
                var usuario = await _db.Usuarios
                    .Include(u => u.OrdenesServicio)
                    .Where(u => u.UsuarioId == id)
                    .Select(u => new
                    {
                        u.UsuarioId,
                        u.Fullname,
                        u.Correo,
                        u.Rol,
                        u.Especialidad,
                        u.Telefono,
                        // No incluir la contraseña por seguridad
                        // Órdenes de servicio sin referencias circulares
                        OrdenesServicio = u.OrdenesServicio != null && u.OrdenesServicio.Any()
                            ? u.OrdenesServicio.Select(o => new
                            {
                                o.OrdenServicioId,
                                o.Falla,
                                o.FechaCreacion,
                                o.Estado,
                                o.Prioridad,
                                // Información básica del equipo
                                Equipo = o.Equipo != null ? new
                                {
                                    o.Equipo.EquipoId,
                                    o.Equipo.Marca,
                                    o.Equipo.Modelo,
                                    o.Equipo.Serie,
                                    o.Equipo.TipoEquipo
                                } : null
                            }).ToList()
                            : null,
                        // Estadísticas del usuario
                        Estadisticas = new
                        {
                            TotalOrdenes = u.OrdenesServicio != null ? u.OrdenesServicio.Count : 0,
                            OrdenesPendientes = u.OrdenesServicio != null 
                                ? u.OrdenesServicio.Count(o => o.Estado == "Pendiente") 
                                : 0,
                            OrdenesFinalizadas = u.OrdenesServicio != null 
                                ? u.OrdenesServicio.Count(o => o.Estado == "Finalizada") 
                                : 0
                        }
                    })
                    .FirstOrDefaultAsync();

                if (usuario == null)
                {
                    return NotFound(new { mensaje = $"Usuario con ID {id} no encontrado" });
                }

                return Ok(new
                {
                    mensaje = "Usuario encontrado exitosamente",
                    usuario = usuario
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al buscar usuario", error = ex.Message });
            }
        }

        // GET: api/usuario/correo/{correo}
        // Buscar usuario por correo
        [HttpGet("correo/{correo}")]
        public async Task<ActionResult<object>> GetUsuarioPorCorreo(string correo)
        {
            try
            {
                var usuario = await _db.Usuarios
                    .Where(u => u.Correo == correo)
                    .Select(u => new
                    {
                        u.UsuarioId,
                        u.Fullname,
                        u.Correo,
                        u.Rol,
                        u.Especialidad,
                        u.Telefono,
                        TotalOrdenes = u.OrdenesServicio != null ? u.OrdenesServicio.Count : 0
                    })
                    .FirstOrDefaultAsync();

                if (usuario == null)
                {
                    return NotFound(new { mensaje = $"Usuario con correo {correo} no encontrado" });
                }

                return Ok(new
                {
                    mensaje = "Usuario encontrado exitosamente",
                    usuario = usuario
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al buscar usuario por correo", error = ex.Message });
            }
        }

        // GET: api/usuario/rol/{rol}
        // Listar usuarios por rol
        [HttpGet("rol/{rol}")]
        public async Task<ActionResult<object>> GetUsuariosPorRol(string rol)
        {
            try
            {
                // Validar rol
                if (rol != "Admin" && rol != "Tecnico")
                {
                    return BadRequest(new
                    {
                        mensaje = "El rol debe ser 'Admin' o 'Tecnico'",
                        rolInvalido = rol,
                        valoresPermitidos = new[] { "Admin", "Tecnico" }
                    });
                }

                var usuarios = await _db.Usuarios
                    .Where(u => u.Rol == rol)
                    .Select(u => new
                    {
                        u.UsuarioId,
                        u.Fullname,
                        u.Correo,
                        u.Rol,
                        u.Especialidad,
                        u.Telefono,
                        OrdenesPendientes = u.OrdenesServicio != null
                            ? u.OrdenesServicio.Count(o => o.Estado == "Pendiente")
                            : 0,
                        TotalOrdenes = u.OrdenesServicio != null ? u.OrdenesServicio.Count : 0
                    })
                    .OrderBy(u => u.Fullname)
                    .ToListAsync();

                if (usuarios == null || usuarios.Count == 0)
                {
                    return NotFound(new { mensaje = $"No hay usuarios con rol {rol}" });
                }

                return Ok(new
                {
                    mensaje = $"Usuarios con rol {rol}",
                    rol = rol,
                    total = usuarios.Count,
                    usuarios = usuarios
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener usuarios por rol", error = ex.Message });
            }
        }

        // GET: api/usuario/especialidad/{especialidad}
        // Listar usuarios por especialidad
        [HttpGet("especialidad/{especialidad}")]
        public async Task<ActionResult<object>> GetUsuariosPorEspecialidad(string especialidad)
        {
            try
            {
                // Validar especialidad
                if (especialidad != "Impresion" && especialidad != "Computo")
                {
                    return BadRequest(new
                    {
                        mensaje = "La especialidad debe ser 'Impresion' o 'Computo'",
                        especialidadInvalida = especialidad,
                        valoresPermitidos = new[] { "Impresion", "Computo" }
                    });
                }

                var usuarios = await _db.Usuarios
                    .Where(u => u.Especialidad == especialidad)
                    .Select(u => new
                    {
                        u.UsuarioId,
                        u.Fullname,
                        u.Correo,
                        u.Rol,
                        u.Especialidad,
                        u.Telefono,
                        OrdenesPendientes = u.OrdenesServicio != null
                            ? u.OrdenesServicio.Count(o => o.Estado == "Pendiente")
                            : 0
                    })
                    .OrderBy(u => u.Fullname)
                    .ToListAsync();

                if (usuarios == null || usuarios.Count == 0)
                {
                    return NotFound(new { mensaje = $"No hay usuarios con especialidad en {especialidad}" });
                }

                return Ok(new
                {
                    mensaje = $"Usuarios con especialidad en {especialidad}",
                    especialidad = especialidad,
                    total = usuarios.Count,
                    usuarios = usuarios
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener usuarios por especialidad", error = ex.Message });
            }
        }

        // POST: api/usuario
        // Crear nuevo usuario
        [HttpPost]
        public async Task<ActionResult<object>> CreateUsuario([FromBody] Usuario usuario)
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

                // Validar rol
                if (usuario.Rol != "Admin" && usuario.Rol != "Tecnico")
                {
                    return BadRequest(new
                    {
                        mensaje = "El rol debe ser 'Admin' o 'Tecnico'",
                        rolRecibido = usuario.Rol
                    });
                }

                // Validar especialidad
                if (usuario.Especialidad != "Impresion" && usuario.Especialidad != "Computo")
                {
                    return BadRequest(new
                    {
                        mensaje = "La especialidad debe ser 'Impresion' o 'Computo'",
                        especialidadRecibida = usuario.Especialidad
                    });
                }

                // Verificar si ya existe un usuario con el mismo correo
                var correoExiste = await _db.Usuarios
                    .AnyAsync(u => u.Correo == usuario.Correo);

                if (correoExiste)
                {
                    return Conflict(new { mensaje = $"Ya existe un usuario con el correo {usuario.Correo}" });
                }

                // Verificar si ya existe un usuario con el mismo teléfono
                var telefonoExiste = await _db.Usuarios
                    .AnyAsync(u => u.Telefono == usuario.Telefono);

                if (telefonoExiste)
                {
                    return Conflict(new { mensaje = $"Ya existe un usuario con el teléfono {usuario.Telefono}" });
                }

                // Hashear la contraseña
                var contraseñaHasheada = HashPassword(usuario.Contraseña);
                usuario.Contraseña = contraseñaHasheada;

                // Inicializar colecciones
                if (usuario.OrdenesServicio == null)
                {
                    usuario.OrdenesServicio = new HashSet<OrdenServicio>();
                }

                // Agregar usuario
                await _db.Usuarios.AddAsync(usuario);
                await _db.SaveChangesAsync();

                // Respuesta sin referencias circulares y sin contraseña
                var usuarioCreado = new
                {
                    usuario.UsuarioId,
                    usuario.Fullname,
                    usuario.Correo,
                    usuario.Rol,
                    usuario.Especialidad,
                    usuario.Telefono
                };

                return CreatedAtAction(nameof(GetUsuario), new { id = usuario.UsuarioId }, new
                {
                    mensaje = "Usuario creado exitosamente",
                    usuario = usuarioCreado
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error interno del servidor", error = ex.Message });
            }
        }

        // PUT: api/usuario/{id}
        // Editar usuario completo
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUsuario(int id, [FromBody] Usuario usuario)
        {
            try
            {
                // Validar que el ID coincida
                if (id != usuario.UsuarioId)
                {
                    return BadRequest(new
                    {
                        mensaje = "El ID de la URL no coincide con el ID del usuario",
                        urlId = id,
                        bodyId = usuario.UsuarioId
                    });
                }

                // Buscar el usuario existente
                var usuarioExistente = await _db.Usuarios.FindAsync(id);

                if (usuarioExistente == null)
                {
                    return NotFound(new { mensaje = $"Usuario con ID {id} no encontrado" });
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

                // Validar rol
                if (usuario.Rol != "Admin" && usuario.Rol != "Tecnico")
                {
                    return BadRequest(new
                    {
                        mensaje = "El rol debe ser 'Admin' o 'Tecnico'",
                        rolRecibido = usuario.Rol
                    });
                }

                // Validar especialidad
                if (usuario.Especialidad != "Impresion" && usuario.Especialidad != "Computo")
                {
                    return BadRequest(new
                    {
                        mensaje = "La especialidad debe ser 'Impresion' o 'Computo'",
                        especialidadRecibida = usuario.Especialidad
                    });
                }

                // Verificar si el correo ya existe en otro usuario
                var correoExiste = await _db.Usuarios
                    .AnyAsync(u => u.Correo == usuario.Correo && u.UsuarioId != id);

                if (correoExiste)
                {
                    return Conflict(new { mensaje = $"El correo {usuario.Correo} ya está registrado por otro usuario" });
                }

                // Verificar si el teléfono ya existe en otro usuario
                var telefonoExiste = await _db.Usuarios
                    .AnyAsync(u => u.Telefono == usuario.Telefono && u.UsuarioId != id);

                if (telefonoExiste)
                {
                    return Conflict(new { mensaje = $"El teléfono {usuario.Telefono} ya está registrado por otro usuario" });
                }

                // Actualizar campos
                usuarioExistente.Fullname = usuario.Fullname;
                usuarioExistente.Correo = usuario.Correo;
                usuarioExistente.Rol = usuario.Rol;
                usuarioExistente.Especialidad = usuario.Especialidad;
                usuarioExistente.Telefono = usuario.Telefono;

                // Solo actualizar contraseña si se proporcionó una nueva
                if (!string.IsNullOrWhiteSpace(usuario.Contraseña))
                {
                    usuarioExistente.Contraseña = HashPassword(usuario.Contraseña);
                }

                // No actualizar las colecciones de navegación
                _db.Entry(usuarioExistente).State = EntityState.Modified;
                await _db.SaveChangesAsync();

                // Respuesta sin referencias circulares y sin contraseña
                var usuarioActualizado = new
                {
                    usuarioExistente.UsuarioId,
                    usuarioExistente.Fullname,
                    usuarioExistente.Correo,
                    usuarioExistente.Rol,
                    usuarioExistente.Especialidad,
                    usuarioExistente.Telefono
                };

                return Ok(new
                {
                    mensaje = "Usuario actualizado exitosamente",
                    usuario = usuarioActualizado
                });
            }
            catch (DbUpdateConcurrencyException)
            {
                return StatusCode(409, new { mensaje = "Error de concurrencia al actualizar el usuario" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error interno del servidor", error = ex.Message });
            }
        }

        // PATCH: api/usuario/{id}
        // Editar usuario parcialmente
        [HttpPatch("{id}")]
        public async Task<IActionResult> PatchUsuario(int id, [FromBody] JsonElement updates)
        {
            try
            {
                var usuario = await _db.Usuarios.FindAsync(id);

                if (usuario == null)
                {
                    return NotFound(new { mensaje = $"Usuario con ID {id} no encontrado" });
                }

                // Aplicar actualizaciones solo a los campos enviados
                if (updates.TryGetProperty("Fullname", out var fullnameProp))
                {
                    usuario.Fullname = fullnameProp.GetString();
                }

                if (updates.TryGetProperty("Correo", out var correoProp))
                {
                    var nuevoCorreo = correoProp.GetString();

                    // Verificar si el correo ya existe en otro usuario
                    var correoExiste = await _db.Usuarios
                        .AnyAsync(u => u.Correo == nuevoCorreo && u.UsuarioId != id);

                    if (correoExiste)
                    {
                        return Conflict(new { mensaje = $"El correo {nuevoCorreo} ya está registrado" });
                    }

                    usuario.Correo = nuevoCorreo;
                }

                if (updates.TryGetProperty("Rol", out var rolProp))
                {
                    var rol = rolProp.GetString();
                    if (rol != "Admin" && rol != "Tecnico")
                    {
                        return BadRequest(new { mensaje = "El rol debe ser 'Admin' o 'Tecnico'" });
                    }
                    usuario.Rol = rol;
                }

                if (updates.TryGetProperty("Especialidad", out var especialidadProp))
                {
                    var especialidad = especialidadProp.GetString();
                    if (especialidad != "Impresion" && especialidad != "Computo")
                    {
                        return BadRequest(new { mensaje = "La especialidad debe ser 'Impresion' o 'Computo'" });
                    }
                    usuario.Especialidad = especialidad;
                }

                if (updates.TryGetProperty("Telefono", out var telefonoProp))
                {
                    var nuevoTelefono = telefonoProp.GetString();

                    // Verificar si el teléfono ya existe en otro usuario
                    var telefonoExiste = await _db.Usuarios
                        .AnyAsync(u => u.Telefono == nuevoTelefono && u.UsuarioId != id);

                    if (telefonoExiste)
                    {
                        return Conflict(new { mensaje = $"El teléfono {nuevoTelefono} ya está registrado" });
                    }

                    usuario.Telefono = nuevoTelefono;
                }

                if (updates.TryGetProperty("Contraseña", out var contraseñaProp))
                {
                    var nuevaContraseña = contraseñaProp.GetString();
                    if (!string.IsNullOrWhiteSpace(nuevaContraseña))
                    {
                        // Validar requisitos de contraseña
                        if (nuevaContraseña.Length < 8)
                        {
                            return BadRequest(new { mensaje = "La contraseña debe tener al menos 8 caracteres" });
                        }
                        usuario.Contraseña = HashPassword(nuevaContraseña);
                    }
                }

                await _db.SaveChangesAsync();

                var usuarioActualizado = new
                {
                    usuario.UsuarioId,
                    usuario.Fullname,
                    usuario.Correo,
                    usuario.Rol,
                    usuario.Especialidad,
                    usuario.Telefono
                };

                return Ok(new
                {
                    mensaje = "Usuario actualizado exitosamente",
                    usuario = usuarioActualizado
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al actualizar el usuario", error = ex.Message });
            }
        }

        // DELETE: api/usuario/{id}
        // Eliminar usuario
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUsuario(int id)
        {
            try
            {
                // Buscar el usuario con sus relaciones
                var usuario = await _db.Usuarios
                    .Include(u => u.OrdenesServicio)
                    .FirstOrDefaultAsync(u => u.UsuarioId == id);

                if (usuario == null)
                {
                    return NotFound(new
                    {
                        mensaje = $"Usuario con ID {id} no encontrado",
                        usuarioId = id
                    });
                }

                // Verificar si tiene órdenes pendientes
                var ordenesPendientes = usuario.OrdenesServicio?
                    .Where(o => o.Estado == "Pendiente")
                    .ToList() ?? new List<OrdenServicio>();

                if (ordenesPendientes.Any())
                {
                    return BadRequest(new
                    {
                        mensaje = $"No se puede eliminar el usuario porque tiene {ordenesPendientes.Count} órdenes pendientes",
                        usuarioId = id,
                        usuarioNombre = usuario.Fullname,
                        ordenesPendientes = ordenesPendientes.Select(o => new
                        {
                            o.OrdenServicioId,
                            o.Falla,
                            o.Estado
                        }).ToList(),
                        sugerencia = "Primero finalice las órdenes pendientes antes de eliminar el usuario"
                    });
                }

                // Guardar información para la respuesta
                var usuarioInfo = new
                {
                    usuario.UsuarioId,
                    usuario.Fullname,
                    usuario.Correo,
                    usuario.Rol,
                    usuario.Especialidad,
                    usuario.Telefono,
                    TotalOrdenesFinalizadas = usuario.OrdenesServicio != null 
                        ? usuario.OrdenesServicio.Count(o => o.Estado == "Finalizada") 
                        : 0
                };

                // Eliminar el usuario
                _db.Usuarios.Remove(usuario);
                await _db.SaveChangesAsync();

                return Ok(new
                {
                    mensaje = $"Usuario '{usuario.Fullname}' eliminado exitosamente",
                    usuarioEliminado = usuarioInfo,
                    fechaEliminacion = DateTime.Now
                });
            }
            catch (DbUpdateConcurrencyException)
            {
                return StatusCode(409, new
                {
                    mensaje = "Error de concurrencia. El usuario fue modificado por otro usuario",
                    usuarioId = id
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    mensaje = "Error interno del servidor al eliminar el usuario",
                    error = ex.Message,
                    usuarioId = id
                });
            }
        }

        // POST: api/usuario/login
        // Iniciar sesión (autenticación)
        [HttpPost("login")]
        public async Task<ActionResult<object>> Login([FromBody] LoginRequest loginRequest)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(loginRequest.Correo) || string.IsNullOrWhiteSpace(loginRequest.Contraseña))
                {
                    return BadRequest(new { mensaje = "Correo y contraseña son requeridos" });
                }

                var usuario = await _db.Usuarios
                    .FirstOrDefaultAsync(u => u.Correo == loginRequest.Correo);

                if (usuario == null)
                {
                    return Unauthorized(new { mensaje = "Correo o contraseña incorrectos" });
                }

                // Verificar contraseña
                var contraseñaHasheada = HashPassword(loginRequest.Contraseña);
                if (usuario.Contraseña != contraseñaHasheada)
                {
                    return Unauthorized(new { mensaje = "Correo o contraseña incorrectos" });
                }

                // Respuesta sin incluir la contraseña
                var usuarioInfo = new
                {
                    usuario.UsuarioId,
                    usuario.Fullname,
                    usuario.Correo,
                    usuario.Rol,
                    usuario.Especialidad,
                    usuario.Telefono
                };

                return Ok(new
                {
                    mensaje = "Inicio de sesión exitoso",
                    usuario = usuarioInfo,
                    fechaInicio = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al iniciar sesión", error = ex.Message });
            }
        }

        // GET: api/usuario/buscar/{nombre}
        // Buscar usuarios por nombre
        [HttpGet("buscar/{nombre}")]
        public async Task<ActionResult<object>> BuscarUsuariosPorNombre(string nombre)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(nombre))
                {
                    return BadRequest(new { mensaje = "El nombre de búsqueda no puede estar vacío" });
                }

                var usuarios = await _db.Usuarios
                    .Where(u => u.Fullname != null && u.Fullname.Contains(nombre))
                    .Select(u => new
                    {
                        u.UsuarioId,
                        u.Fullname,
                        u.Correo,
                        u.Rol,
                        u.Especialidad,
                        u.Telefono,
                        OrdenesPendientes = u.OrdenesServicio != null
                            ? u.OrdenesServicio.Count(o => o.Estado == "Pendiente")
                            : 0
                    })
                    .OrderBy(u => u.Fullname)
                    .ToListAsync();

                if (usuarios == null || usuarios.Count == 0)
                {
                    return NotFound(new { mensaje = $"No se encontraron usuarios con nombre que contenga '{nombre}'" });
                }

                return Ok(new
                {
                    mensaje = $"Se encontraron {usuarios.Count} usuario(s)",
                    terminoBusqueda = nombre,
                    total = usuarios.Count,
                    usuarios = usuarios
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al buscar usuarios", error = ex.Message });
            }
        }
    }

    // Clase para la solicitud de login
    public class LoginRequest
    {
        public string? Correo { get; set; }
        public string? Contraseña { get; set; }
    }
}