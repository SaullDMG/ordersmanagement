using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ordersmanagement.Interface;

namespace ordersmanagement.Services
{
    public class AlmacenamientoService : IAlmacenamiento
    {
        private readonly IWebHostEnvironment _env;
        private readonly IHttpContextAccessor _contextAccesor;

        public AlmacenamientoService(
            IWebHostEnvironment env,
            IHttpContextAccessor contextAccesor
        )
        {
            _env = env;
            _contextAccesor = contextAccesor;
        }

        public async Task<string> AlmacenarImagen(string contenedor, IFormFile archivo)
        {
            var extension = Path.GetExtension(archivo.FileName);
            var nameFile = $"{Guid.NewGuid()}{extension}";
            var carpeta = Path.Combine(_env.WebRootPath, contenedor);

            if (!Directory.Exists(carpeta)) Directory.CreateDirectory(contenedor);

            string ruta = Path.Combine(carpeta, nameFile);
            using (var ms = new MemoryStream())
            {
                await archivo.CopyToAsync(ms);
                var contenido = ms.ToArray();
                await File.WriteAllBytesAsync(ruta, contenido);
            }

            var request = _contextAccesor.HttpContext!.Request;
            var url = $"{request.Scheme}://{request.Host}";

            string ubicacion = Path.Combine(url, contenedor, nameFile);//.Replace("","/");

            return ubicacion;
        }

        public async Task Eliminar(string contenedor, string? ruta)
        {
            if (string.IsNullOrWhiteSpace(ruta)) return;
            var nameFile = Path.GetFileName(ruta);
            var directorio = Path.Combine(_env.WebRootPath, contenedor, nameFile);
            if (File.Exists(directorio)) File.Delete(directorio);
        }
    }
}