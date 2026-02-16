using Telegram.Bot;
using Telegram.Bot.Types;
using Telebot.Toolkit;
using Telebot.Models;

namespace Telebot.Services;

public class BotHandler
{
    // Estados de la conversación
    private enum ReportStep
    {
        None,
        WaitingForConfigGroup, // Nuevo estado para configurar
        WaitingForConfigMembers, // Nuevo estado para configurar
        WaitingForMemberRemoval, // Nuevo estado para confirmar integrantes
        WaitingForTitle,
        WaitingForGroup,
        WaitingForMembers,
        WaitingForAbstract,
        WaitingForIntro,
        WaitingForObjectives,
        WaitingForFramework,
        WaitingForMetIntro,
        WaitingForMaterials,
        WaitingForSetup,
        WaitingForProcedure,
        WaitingForVariables, // Definir variables del experimento
        WaitingForData,      // Ingresar datos
        WaitingForMontagePhoto, // Nueva foto del montaje
        WaitingForAnalysis,  // Análisis de resultados
        WaitingForConclusions,
        WaitingForReferences
    }

    // Configuración persistente por chat (Grupo e Integrantes fijos)
    private record ChatConfig(string Grupo, string Integrantes);
    private static readonly Dictionary<long, ChatConfig> _chatConfigs = new();

    // Almacenamiento temporal de sesiones en memoria (ChatId -> Datos)
    private static readonly Dictionary<long, (ReportStep Step, LabReport Data)> _sessions = new();
    private readonly string _templatePath;
    private readonly FileRepository _fileRepo; // Nuevo repositorio

    public BotHandler(string templatePath)
    {
        _templatePath = templatePath;
        _fileRepo = new FileRepository();
    }

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        // Procesamos mensajes (Texto o Fotos)
        if (update.Message is not { } message) return;
        
        long chatId = message.Chat.Id;
        string messageText = message.Text ?? "";

        // Manejo de Fotos (Si estamos en el estado correcto)
        if (message.Photo != null && _sessions.ContainsKey(chatId))
        {
            var session = _sessions[chatId];
            if (session.Step == ReportStep.WaitingForMontagePhoto)
            {
                await HandlePhotoUpload(botClient, message, session.Data, cancellationToken);
                _sessions[chatId] = (ReportStep.WaitingForAnalysis, session.Data);
                await botClient.SendMessage(chatId, "📸 Foto recibida.\n\n10. Escribe el ANÁLISIS DE RESULTADOS:", cancellationToken: cancellationToken);
                return;
            }
        }

        if (string.IsNullOrEmpty(messageText)) return;
        Console.WriteLine($"Comando recibido: '{messageText}' de {message.Chat.FirstName}");

        // 1. Comandos Globales
        if (messageText.StartsWith("/start") || messageText.StartsWith("/ayuda"))
        {
            await botClient.SendMessage(chatId, 
                "👋 ¡Hola! Soy Jarvis.\n\n" +
                "Comandos disponibles:\n" +
                "📝 /nuevo - Iniciar un nuevo reporte.\n" +
                "🧪 /experimento - Crear experimento rápido.\n" +
                "⚙️ /configurar - Guardar grupo e integrantes fijos.\n" +
                "📂 /archivos - Listar archivos guardados.\n" +
                "❌ /cancelar - Cancelar el proceso actual.", 
                cancellationToken: cancellationToken);
            return;
        }

        if (messageText.StartsWith("/archivos"))
        {
            var files = _fileRepo.ListFiles();
            string response = "📂 Archivos en el sistema:\n" + string.Join("\n", files.Select(f => $"- {f.Name} ({f.Type})"));
            await botClient.SendMessage(chatId, response, cancellationToken: cancellationToken);
            return;
        }

        if (messageText.StartsWith("/cancelar"))
        {
            _sessions.Remove(chatId);
            await botClient.SendMessage(chatId, "Operación cancelada.", cancellationToken: cancellationToken);
            return;
        }

        if (messageText.StartsWith("/configurar"))
        {
            _sessions[chatId] = (ReportStep.WaitingForConfigGroup, new LabReport());
            await botClient.SendMessage(chatId, "⚙️ Configuración de Chat.\n\nIngresa el número de GRUPO por defecto:", cancellationToken: cancellationToken);
            return;
        }

        if (messageText.StartsWith("/nuevo"))
        {
            var report = new LabReport();
            
            // Verificar si hay configuración guardada
            if (_chatConfigs.TryGetValue(chatId, out var config))
            {
                report.Grupo = config.Grupo;
                report.Integrantes = config.Integrantes;
                _sessions[chatId] = (ReportStep.WaitingForMemberRemoval, report);
                await botClient.SendMessage(chatId, $"📋 Configuración cargada:\nGrupo: {config.Grupo}\nIntegrantes: {config.Integrantes}\n\n¿Deseas ELIMINAR a alguien para este informe? Escribe el nombre o 'no' para continuar.", cancellationToken: cancellationToken);
            }
            else
            {
                _sessions[chatId] = (ReportStep.WaitingForTitle, report);
                await botClient.SendMessage(chatId, "Vamos a crear el reporte. \n\n1. ¿Cuál es el TÍTULO de la práctica?", cancellationToken: cancellationToken);
            }
            return;
        }

        // Atajo para experimento rápido
        if (messageText.StartsWith("/experimento"))
        {
            // Aquí podrías inicializar un reporte parcial solo con datos de experimento
            // Por ahora redirigimos a /nuevo para mantener coherencia
        }

        // 2. Máquina de Estados (Procesar respuestas)
        if (_sessions.ContainsKey(chatId))
        {
            var session = _sessions[chatId];
            var data = session.Data;

            switch (session.Step)
            {
                // --- FLUJO DE CONFIGURACIÓN ---
                case ReportStep.WaitingForConfigGroup:
                    data.Grupo = messageText;
                    _sessions[chatId] = (ReportStep.WaitingForConfigMembers, data);
                    await botClient.SendMessage(chatId, "Ingresa los INTEGRANTES fijos (separados por comas):", cancellationToken: cancellationToken);
                    break;

                case ReportStep.WaitingForConfigMembers:
                    _chatConfigs[chatId] = new ChatConfig(data.Grupo, messageText);
                    _sessions.Remove(chatId);
                    await botClient.SendMessage(chatId, "✅ Configuración guardada. Usa /nuevo para empezar.", cancellationToken: cancellationToken);
                    break;

                // --- FLUJO DE REPORTE ---
                case ReportStep.WaitingForMemberRemoval:
                    if (!messageText.Trim().ToLower().Equals("no"))
                    {
                        // Lógica simple de reemplazo
                        var currentMembers = data.Integrantes.Split(',').Select(x => x.Trim()).ToList();
                        var toRemove = messageText.Split(',').Select(x => x.Trim());
                        
                        foreach(var rem in toRemove)
                        {
                            currentMembers.RemoveAll(m => m.Contains(rem, StringComparison.OrdinalIgnoreCase));
                        }
                        data.Integrantes = string.Join(", ", currentMembers);
                        await botClient.SendMessage(chatId, $"Integrantes actualizados: {data.Integrantes}", cancellationToken: cancellationToken);
                    }
                    _sessions[chatId] = (ReportStep.WaitingForTitle, data);
                    await botClient.SendMessage(chatId, "1. ¿Cuál es el TÍTULO de la práctica?", cancellationToken: cancellationToken);
                    break;

                case ReportStep.WaitingForTitle:
                    data.Titulo = messageText;
                    _sessions[chatId] = (ReportStep.WaitingForGroup, data);
                    await botClient.SendMessage(chatId, "2. ¿Cuál es el número de GRUPO (ej: NN(1-7))?", cancellationToken: cancellationToken);
                    break;

                case ReportStep.WaitingForGroup:
                    data.Grupo = messageText;
                    _sessions[chatId] = (ReportStep.WaitingForMembers, data);
                    await botClient.SendMessage(chatId, "3. Nombres de los INTEGRANTES (separados por comas):", cancellationToken: cancellationToken);
                    break;

                case ReportStep.WaitingForMembers:
                    data.Integrantes = messageText;
                    _sessions[chatId] = (ReportStep.WaitingForAbstract, data);
                    await botClient.SendMessage(chatId, "4. Escribe el RESUMEN (máximo 5 líneas):", cancellationToken: cancellationToken);
                    break;

                case ReportStep.WaitingForAbstract:
                    data.Resumen = messageText;
                    _sessions[chatId] = (ReportStep.WaitingForIntro, data);
                    await botClient.SendMessage(chatId, "5. Escribe la INTRODUCCIÓN:", cancellationToken: cancellationToken);
                    break;

                case ReportStep.WaitingForIntro:
                    data.Introduccion = messageText;
                    _sessions[chatId] = (ReportStep.WaitingForObjectives, data);
                    await botClient.SendMessage(chatId, "6. Escribe los OBJETIVOS (en lista):", cancellationToken: cancellationToken);
                    break;

                case ReportStep.WaitingForObjectives:
                    data.Objetivos = messageText;
                    _sessions[chatId] = (ReportStep.WaitingForFramework, data);
                    await botClient.SendMessage(chatId, "7. Escribe el MARCO TEÓRICO:", cancellationToken: cancellationToken);
                    break;

                case ReportStep.WaitingForFramework:
                    data.MarcoTeorico = messageText;
                    _sessions[chatId] = (ReportStep.WaitingForMetIntro, data);
                    await botClient.SendMessage(chatId, "8.1 METODOLOGÍA - Introducción (Máx 5 líneas):", cancellationToken: cancellationToken);
                    break;

                case ReportStep.WaitingForMetIntro:
                    data.MetodologiaIntro = messageText;
                    _sessions[chatId] = (ReportStep.WaitingForMaterials, data);
                    await botClient.SendMessage(chatId, "8.2 Materiales y Equipos (Lista con marcas/detalles):", cancellationToken: cancellationToken);
                    break;

                case ReportStep.WaitingForMaterials:
                    data.Materiales = messageText;
                    _sessions[chatId] = (ReportStep.WaitingForSetup, data);
                    await botClient.SendMessage(chatId, "8.3 Montaje Experimental (Descripción o referencia a figuras):", cancellationToken: cancellationToken);
                    break;

                case ReportStep.WaitingForSetup:
                    data.Montaje = messageText;
                    _sessions[chatId] = (ReportStep.WaitingForProcedure, data);
                    await botClient.SendMessage(chatId, "8.4 Procedimiento (Pasos realizados):", cancellationToken: cancellationToken);
                    break;

                case ReportStep.WaitingForProcedure:
                    data.Procedimiento = messageText;
                    _sessions[chatId] = (ReportStep.WaitingForVariables, data);
                    await botClient.SendMessage(chatId, "🧪 9. EXPERIMENTO: Vamos a graficar.\nDefine las variables (Eje X, Eje Y) separadas por coma.\nEjemplo: 'Voltaje (V), Corriente (A)'", cancellationToken: cancellationToken);
                    break;

                case ReportStep.WaitingForVariables:
                    data.Variables = messageText.Split(',').Select(v => v.Trim()).ToList();
                    _sessions[chatId] = (ReportStep.WaitingForData, data);
                    await botClient.SendMessage(chatId, $"Ingresa los datos para: {string.Join(" vs ", data.Variables)}.\n\nEscribe los valores separados por coma (ej: '1.5, 0.2').\nEscribe 'fin' cuando termines.", cancellationToken: cancellationToken);
                    break;

                case ReportStep.WaitingForData:
                    if (messageText.Trim().ToLower() == "fin")
                    {
                        // Generar Gráfica
                        try 
                        {
                            await botClient.SendMessage(chatId, "📊 Generando gráfica...", cancellationToken: cancellationToken);
                            data.RutaGrafica = GraphService.GenerarGrafica(data);
                            
                            if (!string.IsNullOrEmpty(data.RutaGrafica))
                            {
                                using var stream = System.IO.File.OpenRead(data.RutaGrafica);
                                await botClient.SendPhoto(chatId, InputFile.FromStream(stream), caption: "Así se ve tu gráfica.", cancellationToken: cancellationToken);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error graficando: {ex.Message}");
                            await botClient.SendMessage(chatId, "⚠️ No se pudo generar la gráfica, pero continuamos.", cancellationToken: cancellationToken);
                        }

                        _sessions[chatId] = (ReportStep.WaitingForMontagePhoto, data);
                        await botClient.SendMessage(chatId, "📸 Envía una FOTO del montaje experimental (o escribe 'no' para saltar):", cancellationToken: cancellationToken);
                    }
                    else
                    {
                        // Parsear datos
                        try
                        {
                            var row = messageText.Split(',').Select(x => double.Parse(x.Trim())).ToList();
                            data.Datos.Add(row);
                            // Confirmación silenciosa o visual (reacción) podría ir aquí
                        }
                        catch
                        {
                            await botClient.SendMessage(chatId, "❌ Formato inválido. Usa números separados por coma (ej: 10.5, 20).", cancellationToken: cancellationToken);
                        }
                    }
                    break;

                case ReportStep.WaitingForMontagePhoto:
                    if (messageText.Trim().ToLower() == "no")
                    {
                        _sessions[chatId] = (ReportStep.WaitingForAnalysis, data);
                        await botClient.SendMessage(chatId, "10. Escribe el ANÁLISIS DE RESULTADOS:", cancellationToken: cancellationToken);
                    }
                    break;

                case ReportStep.WaitingForAnalysis:
                    data.Resultados = messageText; // Guardamos el análisis en el campo Resultados
                    _sessions[chatId] = (ReportStep.WaitingForConclusions, data);
                    await botClient.SendMessage(chatId, "11. Escribe las CONCLUSIONES:", cancellationToken: cancellationToken);
                    break;

                case ReportStep.WaitingForConclusions:
                    data.Conclusiones = messageText;
                    _sessions[chatId] = (ReportStep.WaitingForReferences, data);
                    await botClient.SendMessage(chatId, "12. Finalmente, escribe las REFERENCIAS:", cancellationToken: cancellationToken);
                    break;

                case ReportStep.WaitingForReferences:
                    data.Referencias = messageText;
                    // Finalizar y generar
                    await GenerarReporteFinal(botClient, chatId, data, cancellationToken);
                    _sessions.Remove(chatId);
                    break;
            }
        }
    }

    private async Task HandlePhotoUpload(ITelegramBotClient botClient, Message message, LabReport data, CancellationToken ct)
    {
        // Obtener la foto de mayor resolución
        var photoId = message.Photo!.Last().FileId;
        var fileInfo = await botClient.GetFileAsync(photoId, ct);
        
        using var stream = new MemoryStream();
        await botClient.DownloadFileAsync(fileInfo.FilePath!, stream, ct);
        stream.Position = 0;

        // Guardar usando el repositorio
        var savedFile = await _fileRepo.SaveFileAsync(stream, $"montaje_{DateTime.Now.Ticks}.jpg", "Montaje");
        data.RutaMontaje = savedFile.Path;
    }

    private async Task GenerarReporteFinal(ITelegramBotClient botClient, long chatId, LabReport data, CancellationToken cancellationToken)
    {
        await botClient.SendMessage(chatId, "✅ Datos completados. Generando PDF...", cancellationToken: cancellationToken);

            try
            {
                // 1. Crear DOCX temporal
                using var docxStream = new MemoryStream();
                // Guardamos un placeholder primero
                var docxRecord = await _fileRepo.SaveFileAsync(new MemoryStream(), $"Reporte_{data.Grupo}.docx", "Reporte");
                
                // 2. Llenar plantilla
                DocumentService.LlenarPlantilla(_templatePath, docxRecord.Path, data);

                // 3. Convertir a PDF usando el Repo
                var pdfRecord = _fileRepo.ConvertToPdf(docxRecord);

                using var stream = System.IO.File.OpenRead(pdfRecord.Path);
                await botClient.SendDocument(
                chatId: chatId,
                    document: InputFile.FromStream(stream, "Reporte_Generado.pdf"),
                    caption: "Aquí tienes el reporte editado y convertido a PDF.",
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
            await botClient.SendMessage(chatId, $"Error generando el reporte: {ex.Message}", cancellationToken: cancellationToken);
                Console.WriteLine($"Error: {ex}");
            }
    }

    public Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        Console.WriteLine(exception.ToString());
        return Task.CompletedTask;
    }
}