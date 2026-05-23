using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace ordersmanagement.Models.requests
{
    public class RequestUpdateUser
    {
        [Required(ErrorMessage = "Se requiere el nombre completo del usuario")]
        public string? Fullname { get; set; }

        [Required(ErrorMessage = "El correo electrónico es requerido")]
        [EmailAddress(ErrorMessage = "Formato de correo inválido")]
        public string? Correo { get; set; }

        [DataType(DataType.Password)]
        public string? Contraseña { get; set; } 

        [Required(ErrorMessage = "El rol del usuario es requerido")]
        [RegularExpression("Admin|Tecnico", ErrorMessage = "El rol debe ser 'Admin' o 'Tecnico'")]
        public string? Rol { get; set; }

        [Required(ErrorMessage = "La especialidad del usuario es requerida")]
        [RegularExpression("Impresion|Computo", ErrorMessage = "La especialidad debe ser 'Impresion' o 'Computo'")]
        public string? Especialidad { get; set; }

        [Required(ErrorMessage = "El teléfono es requerido")]
        [StringLength(10, ErrorMessage = "El teléfono debe tener 10 dígitos")]
        public string? Telefono { get; set; }
    }
}