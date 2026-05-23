using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using ordersmanagement.Interface; // 👈 Importa la interfaz y el nuevo OpenAIOptions
using OpenAI;
using OpenAI.Chat;
using OpenAI.Images;

namespace ordersmanagement.Services
{
    public class IAService : IA
    {
        public string ApiKey { get; set; } = string.Empty;
        public string ModeloTexto { get; set; } = string.Empty;
        public string ModeloImagen { get; set; } = string.Empty;

        private readonly ChatClient chat;
        private readonly ImageClient imagenes;

        // 🎯 VINCULACIÓN INMUNE: Ahora lee directamente la configuración global mapeada
        public IAService(IOptions<OpenAIOptions> config) 
        {
            var info = config.Value;
            
            ApiKey = info.ApiKey;
            ModeloTexto = info.ModeloTexto;
            ModeloImagen = info.ModeloImagen;

            if (string.IsNullOrEmpty(ApiKey))
            {
                throw new ArgumentNullException(nameof(ApiKey), "La ApiKey sigue llegando vacía. Verifica la estructura de tu appsettings.Development.json");
            }

            var cliente = new OpenAIClient(ApiKey);
            chat = cliente.GetChatClient(ModeloTexto);
            imagenes = cliente.GetImageClient(ModeloImagen);
        }

        public async Task<string> GenerarImagen(string prompt) 
        {
            var resultado = await imagenes.GenerateImageAsync(prompt);
            var bytes = resultado.Value.ImageBytes;
            var base64 = Convert.ToBase64String(bytes);
            return $"data:image/png;base64,{base64}";
        }

        public async Task<string> GenerarTexto(string prompt) 
        {
            var resultado = await chat.CompleteChatAsync(new UserChatMessage(prompt));
            return resultado.Value.Content[0].Text;
        }

        public async Task<string> GenerarTextoV2(string prompt) 
        {
            var resultado = await chat.CompleteChatAsync(new ChatMessage[] {
                ChatMessage.CreateSystemMessage("Eres un experto en desarrollo de frontend, backend"),
                ChatMessage.CreateUserMessage(prompt)
            });
            return resultado.Value.Content[0].Text;
        }

        public async Task<string> AnalizarArchvos(string prompt, string listData) 
        {
            var resultado = await chat.CompleteChatAsync(new ChatMessage[] {
                ChatMessage.CreateSystemMessage("Eres un experto en analista de datos"),
                ChatMessage.CreateUserMessage(prompt),
                ChatMessage.CreateUserMessage(listData)
            });
            return resultado.Value.Content[0].Text;
        }

        public async Task<string> PredecirRiesgoPresupuesto(string equipo, string falla, decimal presupuestoPropuesto)
        {
            var promptUsuario = $"Equipo: {equipo}\nFalla Reportada: {falla}\nPresupuesto Estimado Técnico: ${presupuestoPropuesto} MXN";

            var resultado = await chat.CompleteChatAsync(
                new ChatMessage[]
                {
                    ChatMessage.CreateSystemMessage(
                        "Eres un analista de costos e ingeniero experto en un SaaS de soporte técnico de hardware. " +
                        "Tu tarea es evaluar si el presupuesto propuesto por el técnico es suficiente o si corre el riesgo de ser EXCEDIDO " +
                        "debido al costo típico de las refacciones de ese modelo o la complejidad de la falla.\n\n" +
                        "Debes responder ESTRICTAMENTE en formato JSON plano, sin bloques de código Markdown (no uses ```json), " +
                        "con la siguiente estructura exacta:\n" +
                        "{\n" +
                        "  \"riesgoExceso\": true/false,\n" +
                        "  \"nivelRiesgo\": \"Alto\" / \"Medio\" / \"Bajo\",\n" +
                        "  \"costoAproximadoReal\": 0.0,\n" +
                        "  \"motivo\": \"Explicación ultra breve de por qué se excedería o por qué está bien.\"\n" +
                        "}"
                    ),
                    ChatMessage.CreateUserMessage(promptUsuario),
                }
            );

            return resultado.Value.Content[0].Text;
        }

        // =========================================================================================================
        // 🤖 NUEVO MÉTODO COMPLETO: Auditoría Operativa Inteligente del Dashboard de Indicadores
        // =========================================================================================================
        public async Task<string> AuditarMetricasDeOperacion(int total, int pendientes, int enProceso, int alertasPresupuesto, int eficiencia)
        {
            var promptUsuario = $"Métricas operativas actuales del taller:\n" +
                                $"- Total de Órdenes Evaluadas: {total}\n" +
                                $"- Órdenes en Espera (Pendientes): {pendientes}\n" +
                                $"- Órdenes Activas en Taller (En Proceso): {enProceso}\n" +
                                $"- Alertas de Costos de Presupuesto: {alertasPresupuesto}\n" +
                                $"- Eficiencia Operativa de Cierre: {eficiencia}%";

            var resultado = await chat.CompleteChatAsync(
                new ChatMessage[]
                {
                    ChatMessage.CreateSystemMessage(
                        "Eres un consultor senior de operaciones de negocio y auditor de eficiencia para un SaaS de gestión de soporte técnico de hardware.\n\n" +
                        "Tu objetivo es analizar los indicadores agregados del taller de servicio técnico, detectar posibles cuellos de botella de personal o retrasos de refacciones y recetar recomendaciones inteligentes.\n\n" +
                        "REGLAS OPERATIVAS:\n" +
                        "1. Si las órdenes en taller ('enProceso') representan más del 40% del flujo total de la carga de trabajo, o las alertas de presupuesto excedido son mayores a 5, marca la propiedad 'alertaCritica' como true.\n" +
                        "2. Deduce una sucursal con problemas operativos potenciales considerando el volumen de sobrecarga.\n\n" +
                        "Debes responder ÚNICA y ESTRICTAMENTE con un formato de objeto JSON plano válido (no envuelvas tu respuesta en etiquetas markdown de bloque tipo http://googleusercontent.com/immersive_entry_chip/0",
                        "{\n" +
                        "  \"alertaCritica\": true,\n" +
                        "  \"sucursalProblema\": \"Nombre de la sucursal afectada\",\n" +
                        "  \"eficienciaActual\": 0,\n" +
                        "  \"diagnosticoGral\": \"Escribe una explicación analítica concisa de exactamente 3 líneas sobre las causas técnicas o administrativas de este flujo de trabajo.\",\n" +
                        "  \"recomendaciones\": [\n" +
                        "    \"Recomendación de acción inmediata 1\",\n" +
                        "    \"Recomendación de acción inmediata 2\",\n" +
                        "    \"Recomendación de acción inmediata 3\"\n" +
                        "  ]\n" +
                        "}"
                    ),
                    ChatMessage.CreateUserMessage(promptUsuario),
                }
            );

            return resultado.Value.Content[0].Text;
        }
    }
}