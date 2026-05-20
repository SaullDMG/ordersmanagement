using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace OrdersManagement.Models
{
    public class Cliente
    {
    public int ClienteId { get; set; }
    [Required(ErrorMessage = "El Nombre es requerido")]
    public string? Nombre { get; set; }
    [Required(ErrorMessage = "La Dirección es requerida")]
    public string? Direccion { get; set; } 
    [Required(ErrorMessage = "El teléfono es requerido")]
    [StringLength(10, ErrorMessage = "El teléfono debe tener 10 dígitos")]
    public string? Telefono { get; set; }
    
    //Propiedad de navegación para establecer la relación con Equipo
    public virtual HashSet<Equipo>? Equipos { get; set; }

    }
}