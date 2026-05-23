using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;
using OrdersManagement.Data;
using Microsoft.AspNetCore.Authorization;

namespace ordersmanagement.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthenticateController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IConfiguration _configuration;

        [ActivatorUtilitiesConstructor]
        public AuthenticateController(ApplicationDbContext db, IConfiguration configuration)
        {
            _db = db;
            _configuration = configuration;
        }

        private bool VerifyPassword(string passwordIngresada, string passwordAlmacenada)
        {
            // Aquí idealmente usarías: BCrypt.Net.BCrypt.Verify(passwordIngresada, passwordAlmacenada);
            // Por ahora, una comparación simple con tu método anterior:
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(passwordIngresada));
                var hashInput = Convert.ToBase64String(hashedBytes);
                return hashInput == passwordAlmacenada;
            }
        }

        public class TokenUserData
        {
            public string Id { get; set; } = string.Empty;
            public string Correo { get; set; } = string.Empty;
            public string? Rol { get; set; }
        }

        private string GenerarJwt(TokenUserData usuario)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var secretKey = _configuration["Jwt:Secret"] ?? "UnaClaveSuperSecretaYMuyLargaDeAlMenos32Caracteres!";
            var key = Encoding.ASCII.GetBytes(secretKey);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, usuario.Id),
                    new Claim(ClaimTypes.Email, usuario.Correo),
                    new Claim(ClaimTypes.Role, usuario.Rol ?? "Usuario")
                }),
                Expires = DateTime.UtcNow.AddHours(12),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        [Authorize]
        [HttpGet("login")]
        public async Task<ActionResult<object>> RefreshToken()
        {
            try
            {
                var usuarioId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var usuarioEmail = User.FindFirst(ClaimTypes.Email)?.Value;
                var usuarioRol = User.FindFirst(ClaimTypes.Role)?.Value;

                if (string.IsNullOrEmpty(usuarioId) || string.IsNullOrEmpty(usuarioEmail))
                {
                    return Unauthorized(new { mensaje = "Token inválido o corrupto." });
                }

                // Usamos el nuevo modelo tipado de forma estricta
                var usuarioParaToken = new TokenUserData
                {
                    Id = usuarioId,
                    Correo = usuarioEmail,
                    Rol = usuarioRol,
                };

                var nuevoToken = GenerarJwt(usuarioParaToken);
                var usr = await _db.Usuarios.FirstOrDefaultAsync(x => x.Id == Int32.Parse(usuarioId));
                if (usr == null)
                {
                    return Unauthorized(new { mensaje = "Credenciales incorrectas" });
                }
                
                return Ok(new
                {
                    mensaje = "Token renovado con éxito",
                    token = nuevoToken,
                    usuario = new { usr.Id, usr.Fullname, usr.Correo, usr.Rol } 
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("login")]
        public async Task<ActionResult<object>> Login([FromBody] LoginRequest auth)
        {
            if (auth == null || string.IsNullOrEmpty(auth.email) || string.IsNullOrEmpty(auth.password))
            {
                return BadRequest(new { mensaje = "El correo y la contraseña son obligatorios." });
            }

            try
            {
                var usuario = await _db.Usuarios.FirstOrDefaultAsync(x => x.Correo == auth.email);
                
                // Seguridad básica: Si el usuario no existe o la contraseña no coincide, devolvemos el mismo error genérico
                if (usuario == null || !VerifyPassword(auth.password, usuario.Contraseña))
                {
                    return Unauthorized(new { mensaje = "Credenciales incorrectas" });
                }

                // Generamos el Token
                var token = GenerarJwt(new TokenUserData 
                { 
                    Id = usuario.Id.ToString(), 
                    Correo = usuario.Correo!, 
                    Rol = usuario.Rol 
                });

                return Ok(new 
                { 
                    mensaje = "Login exitoso", 
                    token = token, // Este es el token que el frontend guardará (ej. en LocalStorage o Cookies)
                    usuario = new { usuario.Id, usuario.Fullname, usuario.Correo, usuario.Rol } 
                });
            }
            catch (Exception ex)
            {
                // En producción, evita devolver ex.Message directamente al cliente por motivos de seguridad (pistas de DB).
                return StatusCode(500, new { error = ex.Message });
            }
        }

    }

    public class LoginRequest
    {
        public string? email { get; set; }
        public string? password { get; set; }
    }
}