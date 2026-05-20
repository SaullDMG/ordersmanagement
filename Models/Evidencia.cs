using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

    public int EvidenciaId { get; set; }
    [Required(ErrorMessage = "La Descripción de la evidencia es requerida")]
    public string? Descripcion { get; set; }
    [Required(ErrorMessage = "La URL es requerida")]
    public string? Url { get; set; }

    [Required(ErrorMessage = "La fecha de registro es requerida")]
    public DateTime? Registro { get; set; } = DateTime.UtcNow; 

    [Required(ErrorMessage = "Ingrese la extensión del archivo")]
    public string? Extension { get; set; }

    //Propiedad para definir la llave foranea
    public int OrdenServicioId { get; set; }

     //Propiedad de navegación con el modeloOrdenServicio
    public OrdenServicio? OrdenServicio { get; set; }


    }
}