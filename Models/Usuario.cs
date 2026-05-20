using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace OrdersManagement.Models
{
    public class Usuario
    {
       public int UsuarioId { get; set; }

        [Required(ErrorMessage = "Se reuiqere el nombre completo del usuario")]
        public string? Fullname { get; set; }
        [Required(ErrorMessage = "El correo electrónico es requerido")]
        [EmailAddress(ErrorMessage = "Formato de correo inválido")]
        public string? Correo { get; set; }

        [Required(ErrorMessage = "La contraseña es requerida")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Mínimo 8 caracteres")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]+$",
        ErrorMessage = "Debe contener mayúscula, minúscula, número y carácter especial")]
        [DataType(DataType.Password)]
        public string Contraseña { get; set; } = string.Empty;

        [Required(ErrorMessage = "El rol del usuario es requerido")]
        [RegularExpression("Admin|Tecnico", ErrorMessage = "El rol debe ser 'Admin' o 'Tecnico'")]
        public string? Rol { get; set; }

        [Required(ErrorMessage = "La especialidad del usuario es requerida")]
        [RegularExpression("Impresion|Computo", ErrorMessage = "La especialidad debe ser 'Impresion' o 'Computo'")]
        public string? Especialidad { get; set; }

        [Required(ErrorMessage = "El teléfono es requerido")]
        [StringLength(10, ErrorMessage = "El teléfono debe tener 10 dígitos")]
        public string? Telefono { get; set; }

        //Propiedad de navegación para establecer la relación con OrdenServicio y con usuarios
        public virtual HashSet<OrdenServicio>? OrdenesServicio { get; set; }
        

    }
}