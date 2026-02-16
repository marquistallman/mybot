namespace Telebot.Models;

public class FileRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "";
    public string Path { get; set; } = "";
    public string Type { get; set; } = ""; // .docx, .pdf, .png
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string Tag { get; set; } = "General"; // Para agrupar por experimento
}