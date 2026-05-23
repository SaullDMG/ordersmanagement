using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace ordersmanagement.Models.requests
{
    public class RequestUpdateOrden
    {
        [Required(ErrorMessage = "La fecha de creación es requerida")]
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaCierre { get; set; } // nulleable si arranca en "Pendiente"

        [Required(ErrorMessage = "La Descripción de la falla es requerida")]
        public string? Falla { get; set; }

        [Required(ErrorMessage = "El estado es requerido")]
        [RegularExpression("Pendiente|Procesando|Cancelado|Finalizada", ErrorMessage = "El estado debe ser 'Pendiente' o 'Procesando' o 'Finalizada'")]
        public string? Estado { get; set; }

        [Required(ErrorMessage = "La prioridad es requerida")]
        [RegularExpression("Alta|Media|Baja", ErrorMessage = "La prioridad debe ser 'Alta', 'Media' o 'Baja'")]
        public string? Prioridad { get; set; }

        [Required(ErrorMessage = "El presupuesto es requerido")]
        [Column(TypeName = "decimal(18, 2)")] // Configuración explícita para SQL Server
        public decimal Presupuesto { get; set; }

        public int SucursalId { get; set; } // <-- Amarre histórico de ubicación

        public int EquipoId { get; set; }

        public int UsuarioId { get; set; }
    }
}