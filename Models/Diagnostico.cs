using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace OrdersManagement.Models
{
    public class Diagnostico
    {
    public int DiagnosticoId { get; set; }
    [Required(ErrorMessage = "Se requiere la descripción de la falla")]
    public string? DiagnosticoFalla { get; set; }
    [Required(ErrorMessage = "El costo de reparación es requerido")]
    public decimal CostoRep { get; set; } 
    [Required(ErrorMessage = "El costo de refacción es requerido")]
    public decimal CostoRef { get; set; }

    //Propiedad para definir la llave foranea
    public int OrdenServicioId { get; set; }
    
    //Propiedad de navegación con el modelo OrdenServicio
    public OrdenServicio? OrdenServicio { get; set; }
    }
}