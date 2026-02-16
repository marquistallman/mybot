using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;
using Telebot.Models;

namespace Telebot.Toolkit;

public static class DocumentService
{
    // Ahora devuelve la ruta del DOCX modificado, no el PDF
    public static void LlenarPlantilla(string sourcePath, string destPath, LabReport datos)
    {
        if (!File.Exists(sourcePath)) 
            throw new FileNotFoundException("No se encontró la plantilla", sourcePath);

        // Copiamos la plantilla para no modificar la original
        File.Copy(sourcePath, destPath, true);

        // 2. Editar el Word usando OpenXML (Gratis, rápido y nativo)
        using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(destPath, true))
        {
            var body = wordDoc.MainDocumentPart?.Document.Body;
            if (body != null)
            {
                // Reemplazo de metadatos
                ReplaceText(body, "{TITULO}", datos.Titulo);
                ReplaceText(body, "{GRUPO}", datos.Grupo);
                ReplaceText(body, "{INTEGRANTES}", datos.Integrantes);
                ReplaceText(body, "{FECHA}", datos.Fecha.ToString("dd/MM/yyyy"));

                // Reemplazo de contenido
                ReplaceText(body, "{RESUMEN}", datos.Resumen);
                ReplaceText(body, "{INTRODUCCION}", datos.Introduccion);
                ReplaceText(body, "{OBJETIVOS}", datos.Objetivos);
                ReplaceText(body, "{MARCO}", datos.MarcoTeorico);
                ReplaceText(body, "{MET_INTRO}", datos.MetodologiaIntro);
                ReplaceText(body, "{MATERIALES}", datos.Materiales);
                ReplaceText(body, "{MONTAJE}", datos.Montaje);
                ReplaceText(body, "{PROCEDIMIENTO}", datos.Procedimiento);
                ReplaceText(body, "{RESULTADOS}", datos.Resultados);
                ReplaceText(body, "{CONCLUSIONES}", datos.Conclusiones);
                ReplaceText(body, "{REFERENCIAS}", datos.Referencias);

                // Insertar Gráfica si existe
                if (!string.IsNullOrEmpty(datos.RutaGrafica))
                {
                    InsertImage(wordDoc, body, "{GRAFICA}", datos.RutaGrafica);
                }

                // Insertar Foto del Montaje si existe
                if (!string.IsNullOrEmpty(datos.RutaMontaje))
                {
                    InsertImage(wordDoc, body, "{FOTO_MONTAJE}", datos.RutaMontaje);
                }
            }
            wordDoc.Save();
        }
    }

    private static void ReplaceText(DocumentFormat.OpenXml.Wordprocessing.Body body, string placeholder, string newValue)
    {
        // Usamos ToList() para evitar errores al modificar la colección mientras la recorremos
        foreach (var text in body.Descendants<Text>().ToList())
        {
            if (text.Text.Contains(placeholder))
            {
                text.Text = text.Text.Replace(placeholder, newValue);
                Console.WriteLine($"[Reemplazo Exitoso] {placeholder}");
            }
        }
    }

    private static void InsertImage(WordprocessingDocument wordDoc, Body body, string placeholder, string imagePath)
    {
        // Buscar el párrafo que contiene el placeholder
        var paragraphs = body.Descendants<Paragraph>().Where(p => p.InnerText.Contains(placeholder)).ToList();

        foreach (var paragraph in paragraphs)
        {
            // Borrar el texto del placeholder
            paragraph.RemoveAllChildren<Run>();

            // Crear la parte de imagen en el documento
            MainDocumentPart mainPart = wordDoc.MainDocumentPart!;
            ImagePart imagePart = mainPart.AddImagePart(ImagePartType.Png);

            using (FileStream stream = new FileStream(imagePath, FileMode.Open))
            {
                imagePart.FeedData(stream);
            }

            AddImageToParagraph(paragraph, mainPart.GetIdOfPart(imagePart));
        }
    }

    private static void AddImageToParagraph(Paragraph paragraph, string relationshipId)
    {
        // Definir dimensiones (aprox 15cm de ancho)
        long cx = 5670000L; 
        long cy = 3780000L; 

        var element =
            new Run(
                new Drawing(
                    new DW.Inline(
                        new DW.Extent() { Cx = cx, Cy = cy },
                        new DW.EffectExtent() { LeftEdge = 0L, TopEdge = 0L, RightEdge = 0L, BottomEdge = 0L },
                        new DW.DocProperties() { Id = (UInt32Value)1U, Name = "Grafica Generada" },
                        new DW.NonVisualGraphicFrameDrawingProperties(new A.GraphicFrameLocks() { NoChangeAspect = true }),
                        new A.Graphic(
                            new A.GraphicData(
                                new PIC.Picture(
                                    new PIC.NonVisualPictureProperties(
                                        new PIC.NonVisualDrawingProperties() { Id = (UInt32Value)0U, Name = "New Bitmap Image.png" },
                                        new PIC.NonVisualPictureDrawingProperties()),
                                    new PIC.BlipFill(
                                        new A.Blip(new A.BlipExtensionList(new A.BlipExtension() { Uri = "{28A0092B-C50C-407E-A947-70E740481C1C}" })) { Embed = relationshipId, CompressionState = A.BlipCompressionValues.Print },
                                        new A.Stretch(new A.FillRectangle())),
                                    new PIC.ShapeProperties(
                                        new A.Transform2D(new A.Offset() { X = 0L, Y = 0L }, new A.Extents() { Cx = cx, Cy = cy }),
                                        new A.PresetGeometry(new A.AdjustValueList()) { Preset = A.ShapeTypeValues.Rectangle }))
                            ) { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" })
                    ) { DistanceFromTop = (UInt32Value)0U, DistanceFromBottom = (UInt32Value)0U, DistanceFromLeft = (UInt32Value)0U, DistanceFromRight = (UInt32Value)0U }
                ));
        paragraph.AppendChild(element);
    }
}