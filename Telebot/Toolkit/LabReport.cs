namespace Telebot.Models;

public class LabReport
{
    public string Titulo { get; set; } = "";
    public string Grupo { get; set; } = "";
    public string Integrantes { get; set; } = "";
    public string Resumen { get; set; } = "";
    public string Introduccion { get; set; } = "";
    public string Objetivos { get; set; } = "";
    public string MarcoTeorico { get; set; } = "";
    public string MetodologiaIntro { get; set; } = "";
    public string Materiales { get; set; } = "";
    public string Montaje { get; set; } = "";
    public string Procedimiento { get; set; } = "";
    public string Resultados { get; set; } = "";
    public string Conclusiones { get; set; } = "";
    public string Referencias { get; set; } = "";
    public DateTime Fecha { get; set; } = DateTime.Now;
    public List<string> Variables { get; set; } = new();
    public List<List<double>> Datos { get; set; } = new();
    public string RutaGrafica { get; set; } = "";
    public string RutaMontaje { get; set; } = ""; // Nueva foto del montaje
}