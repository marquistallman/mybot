using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telebot.Services;

namespace Telebot;

class Program
{
    private static string ConfigFile = "bot.config";
    private static string BotToken = "";
    private static string TemplatePath = "";

    static async Task Main(string[] args)
    {
        LoadConfig();

        // Asegurar que se borren las credenciales al salir (Shutdown Hook)
        AppDomain.CurrentDomain.ProcessExit += (s, e) => RandomizeConfig();
        Console.CancelKeyPress += (s, e) => 
        {
            RandomizeConfig();
        };

        var botClient = new TelegramBotClient(BotToken);
        using CancellationTokenSource cts = new CancellationTokenSource();

        ReceiverOptions receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<UpdateType>() // Recibir todos los tipos de actualizaciones
        };

        var botHandler = new BotHandler(TemplatePath);

        botClient.StartReceiving(
            updateHandler: botHandler.HandleUpdateAsync,
            errorHandler: botHandler.HandlePollingErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: cts.Token
        );

        var me = await botClient.GetMe();
        Console.WriteLine($"Jarvis iniciado. Escuchando a @{me.Username}");
        
        // Mantener la aplicación corriendo
        await Task.Delay(-1);
    }

    static void LoadConfig()
    {
        // Buscar el archivo en varias ubicaciones posibles para evitar errores de ruta
        string[] searchPaths = {
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../bot.config"), // Prioridad 1: Raíz del proyecto (Development)
            "bot.config", // Prioridad 2: Directorio actual (Script/Production)
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bot.config") // Prioridad 3: Junto al ejecutable
        };

        string? foundPath = searchPaths.FirstOrDefault(System.IO.File.Exists);

        if (foundPath == null)
        {
            Console.WriteLine($"[AVISO] No se encontró {ConfigFile}. Creando archivo vacío.");
            // Si no existe, lo creamos en el directorio actual para que el usuario sepa dónde ponerlo
            System.IO.File.WriteAllLines(ConfigFile, new[] { "BOT_TOKEN=", "TEMPLATE_PATH=" });
            Environment.Exit(1);
        }
        else
        {
            // Actualizamos la variable global para que RandomizeConfig borre el archivo correcto
            ConfigFile = foundPath;
            Console.WriteLine($"Configuración cargada desde: {Path.GetFullPath(ConfigFile)}");
        }

        foreach (var line in System.IO.File.ReadAllLines(ConfigFile))
        {
            var parts = line.Split('=', 2);
            if (parts.Length == 2)
            {
                if (parts[0].Trim() == "BOT_TOKEN") BotToken = parts[1].Trim().Trim('"');
                if (parts[0].Trim() == "TEMPLATE_PATH") TemplatePath = parts[1].Trim().Trim('"');
            }
        }

        if (string.IsNullOrWhiteSpace(BotToken) || string.IsNullOrWhiteSpace(TemplatePath))
        {
            Console.WriteLine("Error: BOT_TOKEN o TEMPLATE_PATH están vacíos en bot.config");
            Environment.Exit(1);
        }
    }

    static void RandomizeConfig()
    {
        // Sobrescribir el archivo con valores aleatorios
        var randomContent = new[] { $"BOT_TOKEN={Guid.NewGuid()}", $"TEMPLATE_PATH={Guid.NewGuid()}" };
        System.IO.File.WriteAllLines(ConfigFile, randomContent);
        Console.WriteLine("Configuración eliminada por seguridad.");
    }
}
