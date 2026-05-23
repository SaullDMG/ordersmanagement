using System;
using System.ComponentModel.DataAnnotations;

namespace OrdersManagement.Models
{
    public class Diagnostico
    {
        public int Id { get; set; } // Cambiado de DiagnosticoId a Id

        [Required(ErrorMessage = "Se requiere la descripción de la falla")]
        public string? DiagnosticoFalla { get; set; }

        [Required(ErrorMessage = "El costo de reparación es requerido")]
        public decimal CostoRep { get; set; } 

        [Required(ErrorMessage = "El costo de refacción es requerido")]
        public decimal CostoRef { get; set; }

        // Llave foránea hacia OrdenServicio
        public int OrdenServicioId { get; set; }
        public OrdenServicio? OrdenServicio { get; set; }
    }
}