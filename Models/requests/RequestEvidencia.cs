using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace ordersmanagement.Models.requests
{
    public class RequestEvidencia
    {
        [Required(ErrorMessage = "La Descripción de la evidencia es requerida")]
        public string? Descripcion { get; set; }
        [Required(ErrorMessage = "El archivo es requerido")]
        public IFormFile? File { get; set; }
        public int OrdenServicioId { get; set; }
    }
}