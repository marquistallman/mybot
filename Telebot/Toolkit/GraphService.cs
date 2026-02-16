using ScottPlot;
using Telebot.Models;

namespace Telebot.Toolkit;

public static class GraphService
{
    public static string GenerarGrafica(LabReport datos)
    {
        if (datos.Variables.Count < 2 || datos.Datos.Count == 0)
            return string.Empty;

        // Crear la gráfica
        var plt = new ScottPlot.Plot(600, 400);
        
        // Preparar datos (Asumimos Columna 0 = X, Columna 1 = Y)
        double[] dataX = datos.Datos.Select(row => row.Count > 0 ? row[0] : 0).ToArray();
        double[] dataY = datos.Datos.Select(row => row.Count > 1 ? row[1] : 0).ToArray();

        var scatter = plt.AddScatter(dataX, dataY);
        scatter.LineWidth = 2;
        scatter.MarkerSize = 10;

        // Etiquetas
        plt.XLabel(datos.Variables[0]);
        plt.YLabel(datos.Variables[1]);
        plt.Title($"Resultados: {datos.Variables[1]} vs {datos.Variables[0]}");
        plt.Grid(true);

        // Guardar imagen temporal
        string tempPath = Path.Combine(Path.GetTempPath(), $"grafica_{Guid.NewGuid()}.png");
        plt.SaveFig(tempPath);

        return tempPath;
    }
}