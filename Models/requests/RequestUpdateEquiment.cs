using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace ordersmanagement.Models.requests
{
    public class RequestUpdateEquiment
    {

        [Required(ErrorMessage = "La marca es requerida")]
        public string? Marca { get; set; }

        [Required(ErrorMessage = "El modelo es requerido")]
        public string? Modelo { get; set; } 

        [Required(ErrorMessage = "La serie es requerida")]
        public string? Serie { get; set; }

        [Required(ErrorMessage = "El tipo de equipo es requerido")]
        [RegularExpression("Impresion|Computo", ErrorMessage = "El tipo debe ser 'Impresion' o 'Computo'")]
        public string? TipoEquipo { get; set; }
    }
}