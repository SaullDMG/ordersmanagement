using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace OrdersManagement.Models
{
    public class OrdenServicio
    {
        public OrdenServicio()
        {
            var mexicoZone = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time (Mexico)");
            FechaCreacion = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, mexicoZone);
        }
    public int OrdenServicioId { get; set; }

    [Required(ErrorMessage = "La fecha de creación es requerida")]
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime FechaCierre { get; set; }

    [Required(ErrorMessage = "La Descripción de la falla es requerida")]
    public string? Falla { get; set; }

    [Required(ErrorMessage = "El estado es requerido")]
    [RegularExpression("Pendiente|Finalizada", ErrorMessage = "La especialidad debe ser 'Pendiente' o 'Finalizada'")]
    public string? Estado { get; set; }

    [Required(ErrorMessage = "La prioridad es requerida")]
    [RegularExpression("Alta|Media|Baja", ErrorMessage = "La prioridad debe ser 'Alta', 'Media' o 'Baja'")]
    public string? Prioridad { get; set; }

    [Required(ErrorMessage = "El presupuesto es requerido")]
    public decimal Presupuesto { get; set; }

    //Propiedad para definir la llave foranea
    public int UsuarioId { get; set; }
    public int EquipoId { get; set; }

    //Propiedad de navegación para los modelos Tecnico y Equipo
    public Usuario? Usuario { get; set; }   
    public Equipo? Equipo { get; set; }

    //Propuedad de navegación para establecer la relación con Diagnóstico y Evidencia
    public virtual HashSet<Diagnostico>? Diagnosticos { get; set; } 
    public virtual HashSet<Evidencia>? Evidencias { get; set; }

    }
}