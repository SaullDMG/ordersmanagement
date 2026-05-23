using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ordersmanagement.Interface
{
    public interface IAlmacenamiento
    {
        Task<string> AlmacenarImagen(string contenedor, IFormFile archivo);
        Task Eliminar(string contenedor, string? ruta);
    }
}