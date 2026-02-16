// c:\Users\davir\OneDrive\Desktop\Jarvis\Telebot\Services\FileRepository.cs
using System.Diagnostics;
using System.Text.Json;
using Telebot.Models;

namespace Telebot.Services;

public class FileRepository
{
    private readonly string _storagePath;
    private readonly string _jsonPath;
    private List<FileRecord> _files;

    public FileRepository()
    {
        _storagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Storage");
        _jsonPath = Path.Combine(_storagePath, "files_index.json");

        if (!Directory.Exists(_storagePath))
            Directory.CreateDirectory(_storagePath);

        LoadIndex();
    }

    private void LoadIndex()
    {
        if (File.Exists(_jsonPath))
        {
            string json = File.ReadAllText(_jsonPath);
            _files = JsonSerializer.Deserialize<List<FileRecord>>(json) ?? new List<FileRecord>();
        }
        else
        {
            _files = new List<FileRecord>();
        }
    }

    private void SaveIndex()
    {
        string json = JsonSerializer.Serialize(_files, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_jsonPath, json);
    }

    public async Task<FileRecord> SaveFileAsync(Stream stream, string fileName, string tag = "General")
    {
        string extension = Path.GetExtension(fileName);
        string uniqueName = $"{Guid.NewGuid()}{extension}";
        string fullPath = Path.Combine(_storagePath, uniqueName);

        using (var fileStream = new FileStream(fullPath, FileMode.Create))
        {
            await stream.CopyToAsync(fileStream);
        }

        var record = new FileRecord
        {
            Name = fileName,
            Path = fullPath,
            Type = extension,
            Tag = tag
        };

        _files.Add(record);
        SaveIndex();
        return record;
    }

    public FileRecord? GetFile(string id)
    {
        return _files.FirstOrDefault(f => f.Id == id);
    }

    public List<FileRecord> ListFiles() => _files;

    public FileRecord ConvertToPdf(FileRecord docxFile)
    {
        string outputDir = _storagePath;
        string pdfName = Path.ChangeExtension(Path.GetFileName(docxFile.Path), ".pdf");
        string pdfPath = Path.Combine(outputDir, pdfName);

        if (OperatingSystem.IsWindows())
        {
            // Intento con PowerShell/Word COM
            try
            {
                var psScript = $"""
                    $word = New-Object -ComObject Word.Application;
                    $word.Visible = $false;
                    $doc = $word.Documents.Open('{docxFile.Path}');
                    $doc.SaveAs('{pdfPath}', 17); 
                    $doc.Close();
                    $word.Quit();
                    """;
                RunProcess("powershell", $"-NoProfile -Command \"{psScript}\"");
            }
            catch
            {
                RunLibreOffice(docxFile.Path, outputDir);
            }
        }
        else
        {
            RunLibreOffice(docxFile.Path, outputDir);
        }

        if (!File.Exists(pdfPath))
            throw new Exception("Error en conversión a PDF.");

        var record = new FileRecord
        {
            Name = Path.ChangeExtension(docxFile.Name, ".pdf"),
            Path = pdfPath,
            Type = ".pdf",
            Tag = docxFile.Tag
        };
        
        _files.Add(record);
        SaveIndex();
        return record;
    }

    public FileRecord ConvertWithPandoc(FileRecord sourceFile, string targetExtension)
    {
        string outputDir = _storagePath;
        // Asegurar que la extensión tenga punto
        if (!targetExtension.StartsWith(".")) targetExtension = "." + targetExtension;
        
        string outputName = Path.ChangeExtension(Path.GetFileName(sourceFile.Path), targetExtension);
        string outputPath = Path.Combine(outputDir, outputName);

        // Ejecutar Pandoc (debe estar en el PATH del sistema)
        // Sintaxis: pandoc input.ext -o output.ext
        RunProcess("pandoc", $"\"{sourceFile.Path}\" -o \"{outputPath}\"");

        if (!File.Exists(outputPath))
            throw new Exception($"Error en conversión con Pandoc a {targetExtension}. Verifica que Pandoc esté instalado.");

        var record = new FileRecord
        {
            Name = outputName,
            Path = outputPath,
            Type = targetExtension,
            Tag = sourceFile.Tag
        };
        
        _files.Add(record);
        SaveIndex();
        return record;
    }

    private void RunLibreOffice(string inputFile, string outputDir)
    {
        string fileName = OperatingSystem.IsWindows() ? 
            @"C:\Program Files\LibreOffice\program\soffice.exe" : "soffice";

        RunProcess(fileName, $"--headless --convert-to pdf \"{inputFile}\" --outdir \"{outputDir}\"");
    }

    private void RunProcess(string fileName, string args)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        process.Start();
        process.WaitForExit();
    }
}
