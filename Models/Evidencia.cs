using System;
using System.ComponentModel.DataAnnotations;

namespace OrdersManagement.Models
{
    public class Evidencia
    {
        public Evidencia()
        {
            var mexicoZone = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time (Mexico)");
            Registro = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, mexicoZone);
        }

        public int Id { get; set; } // Cambiado de EvidenciaId a Id

        [Required(ErrorMessage = "La Descripción de la evidencia es requerida")]
        public string? Descripcion { get; set; }

        [Required(ErrorMessage = "La URL es requerida")]
        public string? Url { get; set; }

        [Required(ErrorMessage = "La fecha de registro es requerida")]
        public DateTime? Registro { get; set; }

        [Required(ErrorMessage = "Ingrese la extensión del archivo")]
        public string? Extension { get; set; }

        // Llave foránea hacia OrdenServicio
        public int OrdenServicioId { get; set; }
        public OrdenServicio? OrdenServicio { get; set; }
    }
}