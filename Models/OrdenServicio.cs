using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ordersmanagement.Models;

namespace OrdersManagement.Models
{
    public class OrdenServicio
    {
        public OrdenServicio()
        {
            var mexicoZone = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time (Mexico)");
            FechaCreacion = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, mexicoZone);
        }

        public int Id { get; set; } 

        [Required(ErrorMessage = "La fecha de creación es requerida")]
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaCierre { get; set; } // nulleable si arranca en "Pendiente"
        public DateTime? FechaInicio { get; set; } // nulleable si arranca en "Pendiente"

        [Required(ErrorMessage = "La Descripción de la falla es requerida")]
        public string? Falla { get; set; }

        [Required(ErrorMessage = "El estado es requerido")]
        [RegularExpression("Pendiente|Procesando|Cancelado|Finalizada", ErrorMessage = "El estado debe ser 'Pendiente' o 'Procesando' o 'Cancelado' o 'Finalizada'")]
        public string? Estado { get; set; }

        [Required(ErrorMessage = "La prioridad es requerida")]
        [RegularExpression("Alta|Media|Baja", ErrorMessage = "La prioridad debe ser 'Alta', 'Media' o 'Baja'")]
        public string? Prioridad { get; set; }

        [Required(ErrorMessage = "El presupuesto es requerido")]
        [Column(TypeName = "decimal(18, 2)")] // Configuración explícita para SQL Server
        public decimal Presupuesto { get; set; }

        // =========================================================
        // LLAVES FORÁNEAS Y LLAVES DE CONTROL HISTÓRICO
        // =========================================================
        
        [Required(ErrorMessage = "La sucursal es obligatoria para congelar la ubicación del servicio")]
        public int SucursalId { get; set; } // <-- Amarre histórico de ubicación

        [Required(ErrorMessage = "El equipo es requerido")]
        public int EquipoId { get; set; }

        [Required(ErrorMessage = "El técnico asignado es requerido")]
        public int UsuarioId { get; set; }

        // =========================================================
        // PROPIEDADES DE NAVEGACIÓN (VIRTUALES)
        // =========================================================
        
        [ForeignKey("SucursalId")]
        public virtual Sucursal? Sucursal { get; set; }

        [ForeignKey("EquipoId")]
        public virtual Equipo? Equipo { get; set; }

        [ForeignKey("UsuarioId")]
        public virtual Usuario? Usuario { get; set; }   

        public virtual ICollection<Diagnostico> Diagnosticos { get; set; } = new HashSet<Diagnostico>();
        public virtual ICollection<Evidencia> Evidencias { get; set; } = new HashSet<Evidencia>();
    }
}