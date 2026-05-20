using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace OrdersManagement.Models
{
    public class Equipo
    {
    public int EquipoId { get; set; }
    [Required(ErrorMessage = "La marca es requerida")]
    public string? Marca { get; set; }
    [Required(ErrorMessage = "El modelo es requerido")]
    public string? Modelo { get; set; } 
    [Required(ErrorMessage = "La serie es requerida")]
    public string? Serie { get; set; }

    [Required(ErrorMessage = "El tipo de equipo es requerido")]
    [RegularExpression("Impresion|Computo", ErrorMessage = "El tipo debe ser 'Impresion' o 'Computo'")]
    public string? TipoEquipo { get; set; }

    //Propiedad para definir la llave foranea
    public int ClienteId { get; set; }
    //Propiedad de navegación con el modelo Cliente
    public Cliente? Cliente { get; set; }

    //Propiedad de navegación para establecer la relación con OrdenServicio
    public virtual HashSet<OrdenServicio>? OrdenesServicio { get; set; }
    }
}