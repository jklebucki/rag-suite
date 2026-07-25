using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using RAG.DocumentProcessing.Abstractions;
using System.Text;
using W = DocumentFormat.OpenXml.Wordprocessing;
using MarkdownTable = Markdig.Extensions.Tables.Table;
using MarkdownTableCell = Markdig.Extensions.Tables.TableCell;
using MarkdownTableRow = Markdig.Extensions.Tables.TableRow;

namespace RAG.Orchestrator.Api.Features.Chat.Artifacts;

public sealed class MarkdownArtifactContentRenderer : IArtifactContentRenderer
{
    private static readonly MarkdownPipeline MarkdownPipeline = new MarkdownPipelineBuilder()
        .UsePipeTables()
        .Build();

    public byte[] Render(GeneratedArtifactFormat format, string markdown)
    {
        return format switch
        {
            GeneratedArtifactFormat.Txt => new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(markdown),
            GeneratedArtifactFormat.Docx => RenderDocx(markdown),
            _ => throw new DocumentProcessingException("INVALID_ARTIFACT_FORMAT", "The generated artifact format is invalid.")
        };
    }

    public string GetContentType(GeneratedArtifactFormat format)
    {
        return format == GeneratedArtifactFormat.Docx
            ? "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
            : "text/plain; charset=utf-8";
    }

    private static byte[] RenderDocx(string markdown)
    {
        using var output = new MemoryStream();
        using (var wordDocument = WordprocessingDocument.Create(output, WordprocessingDocumentType.Document, autoSave: true))
        {
            var mainPart = wordDocument.AddMainDocumentPart();
            var body = new W.Body();
            mainPart.Document = new W.Document(body);
            var document = Markdown.Parse(markdown, MarkdownPipeline);
            foreach (var block in document)
            {
                AppendBlock(body, block);
            }

            if (!body.Elements().Any())
            {
                body.Append(CreateParagraph(string.Empty));
            }

            mainPart.Document.Save();
        }

        return output.ToArray();
    }

    private static void AppendBlock(W.Body body, Block block)
    {
        switch (block)
        {
            case HeadingBlock heading:
                body.Append(CreateParagraph(ExtractInlineText(heading.Inline), heading.Level));
                break;
            case ParagraphBlock paragraph:
                body.Append(CreateParagraph(ExtractInlineText(paragraph.Inline)));
                break;
            case ListBlock list:
                AppendList(body, list);
                break;
            case MarkdownTable table:
                body.Append(CreateTable(table));
                break;
            case CodeBlock code:
                body.Append(CreateParagraph(code.Lines.ToString(), code: true));
                break;
            case QuoteBlock quote:
                body.Append(CreateParagraph(ExtractContainerText(quote), quote: true));
                break;
            case ThematicBreakBlock:
                body.Append(new W.Paragraph(new W.ParagraphProperties(new W.ParagraphBorders(
                    new W.BottomBorder { Val = W.BorderValues.Single, Size = 6U, Space = 1U }))));
                break;
            case ContainerBlock container:
                foreach (var child in container)
                {
                    AppendBlock(body, child);
                }
                break;
            default:
                body.Append(CreateParagraph(ExtractBlockText(block)));
                break;
        }
    }

    private static void AppendList(W.Body body, ListBlock list)
    {
        var ordinal = 1;
        foreach (var item in list.OfType<ListItemBlock>())
        {
            var marker = list.IsOrdered ? $"{ordinal++}. " : "• ";
            body.Append(CreateParagraph(marker + ExtractContainerText(item)));
        }
    }

    private static W.Table CreateTable(MarkdownTable table)
    {
        var wordTable = new W.Table(
            new W.TableProperties(
                new W.TableBorders(
                    new W.TopBorder { Val = W.BorderValues.Single, Size = 4U },
                    new W.LeftBorder { Val = W.BorderValues.Single, Size = 4U },
                    new W.BottomBorder { Val = W.BorderValues.Single, Size = 4U },
                    new W.RightBorder { Val = W.BorderValues.Single, Size = 4U },
                    new W.InsideHorizontalBorder { Val = W.BorderValues.Single, Size = 4U },
                    new W.InsideVerticalBorder { Val = W.BorderValues.Single, Size = 4U })));

        foreach (var row in table.OfType<MarkdownTableRow>())
        {
            var wordRow = new W.TableRow();
            foreach (var cell in row.OfType<MarkdownTableCell>())
            {
                wordRow.Append(new W.TableCell(CreateParagraph(ExtractContainerText(cell), bold: row.IsHeader)));
            }

            wordTable.Append(wordRow);
        }

        return wordTable;
    }

    private static W.Paragraph CreateParagraph(
        string value,
        int? headingLevel = null,
        bool code = false,
        bool quote = false,
        bool bold = false)
    {
        var properties = new W.ParagraphProperties();
        if (headingLevel.HasValue)
        {
            properties.ParagraphStyleId = new W.ParagraphStyleId { Val = $"Heading{headingLevel.Value}" };
        }

        if (quote)
        {
            properties.Indentation = new W.Indentation { Left = "720" };
        }

        W.RunProperties? runProperties = null;
        if (code || bold)
        {
            runProperties = new W.RunProperties();
            if (code)
            {
                runProperties.Append(new W.RunFonts { Ascii = "Consolas", HighAnsi = "Consolas" });
            }

            if (bold)
            {
                runProperties.Append(new W.Bold());
            }
        }
        var run = new W.Run();
        if (runProperties != null)
        {
            run.Append(runProperties);
        }

        run.Append(new W.Text(value ?? string.Empty) { Space = SpaceProcessingModeValues.Preserve });
        return new W.Paragraph(properties, run);
    }

    private static string ExtractBlockText(Block block)
    {
        return block switch
        {
            CodeBlock code => code.Lines.ToString(),
            LeafBlock leaf => ExtractInlineText(leaf.Inline),
            ContainerBlock container => ExtractContainerText(container),
            _ => string.Empty
        };
    }

    private static string ExtractContainerText(ContainerBlock container)
    {
        return string.Join(Environment.NewLine, container.Select(ExtractBlockText).Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    private static string ExtractInlineText(ContainerInline? inline)
    {
        if (inline == null)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        for (Inline? child = inline.FirstChild; child != null; child = child.NextSibling)
        {
            switch (child)
            {
                case LiteralInline literal:
                    builder.Append(literal.Content.ToString());
                    break;
                case CodeInline code:
                    builder.Append(code.Content);
                    break;
                case LineBreakInline:
                    builder.AppendLine();
                    break;
                case ContainerInline container:
                    builder.Append(ExtractInlineText(container));
                    break;
            }
        }

        return builder.ToString();
    }
}
