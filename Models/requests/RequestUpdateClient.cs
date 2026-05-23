using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace ordersmanagement.Models.requests
{
    public class RequestUpdateClient
    {
        [Required(ErrorMessage = "El Nombre es requerido")]
        public string? Nombre { get; set; }

        [Required(ErrorMessage = "La Dirección es requerida")]
        public string? Direccion { get; set; } 

        [Required(ErrorMessage = "El teléfono es requerido")]
        [StringLength(10, ErrorMessage = "El teléfono debe tener 10 dígitos")]
        public string? Telefono { get; set; }
    }
}