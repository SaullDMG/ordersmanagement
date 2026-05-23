using System;
using System.Threading.Tasks;

namespace ordersmanagement.Interface
{
    public interface IA
    {
        string ApiKey { get; set; }
        string ModeloTexto { get; set; }
        string ModeloImagen { get; set; }

        Task<string> GenerarImagen(string prompt);
        Task<string> GenerarTexto(string prompt);
        Task<string> GenerarTextoV2(string prompt);
        Task<string> AnalizarArchvos(string prompt, string listData);
        Task<string> PredecirRiesgoPresupuesto(string equipo, string falla, decimal presupuestoPropuesto);
        Task<string> AuditarMetricasDeOperacion(int total, int pendientes, int enProceso, int alertasPresupuesto, int eficiencia);
    }

    // 🎯 NOMBRE ÚNICO: Adiós a las duplicaciones de nombres en el proyecto
    public class OpenAIOptions
    {
        public string ApiKey { get; set; } = string.Empty;
        public string ModeloTexto { get; set; } = string.Empty;
        public string ModeloImagen { get; set; } = string.Empty;
    }
}