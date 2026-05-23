using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace ordersmanagement.Models.requests
{
    public class RequestUpdateSucursal
    {
        [Required(ErrorMessage = "El nombre de la sucursal es obligatorio")]
        [StringLength(100)]
        public string Name { get; set; } = null!;

        [StringLength(200)]
        public string? Direccion { get; set; }

        [StringLength(20)]
        public string? Telefono { get; set; }

        [Column(TypeName = "decimal(9, 6)")]
        public decimal? Latitud { get; set; }

        [Column(TypeName = "decimal(10, 6)")]
        public decimal? Longitud { get; set; }
    }
}