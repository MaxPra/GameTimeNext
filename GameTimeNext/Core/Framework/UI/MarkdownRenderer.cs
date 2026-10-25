using Markdig;
using Markdig.Extensions.Alerts;
using Markdig.Extensions.Tables;
using Markdig.Extensions.TaskLists;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using MdBlock = Markdig.Syntax.Block;
using MdInline = Markdig.Syntax.Inlines.Inline;
using MdTable = Markdig.Extensions.Tables.Table;
using MdTableCell = Markdig.Extensions.Tables.TableCell;
using MdTableRow = Markdig.Extensions.Tables.TableRow;
using WpfBlock = System.Windows.Documents.Block;
using WpfInline = System.Windows.Documents.Inline;
using WpfList = System.Windows.Documents.List;
using WpfTable = System.Windows.Documents.Table;
using WpfTableCell = System.Windows.Documents.TableCell;
using WpfTableRow = System.Windows.Documents.TableRow;

namespace GameTimeNext.Core.Framework.UI
{
    /// <summary>
    /// Renders Markdown (GitHub flavored, parsed with Markdig) into a WPF FlowDocument styled like GitHub on the app
    /// theme: headings (h1/h2 underlined), paragraphs, bullet / numbered / task lists, quotes, alerts ("> [!NOTE]"),
    /// code, tables, links and horizontal rules. Colors are theme resource references, so the document follows the theme.
    /// </summary>
    public static class MarkdownRenderer
    {
        private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();

        private static readonly FontFamily BodyFont = new FontFamily("Segoe UI");
        private static readonly FontFamily CodeFont = new FontFamily("Cascadia Mono, Consolas");

        private const double BodyFontSize = 13.5;
        private const double BlockSpacing = 10;

        // Light glass, like code on GitHub's dark theme (a dark tint is barely visible on the dark surfaces)
        private static readonly Brush CodeBackground = Freeze(new SolidColorBrush(Color.FromArgb(0x1F, 0xFF, 0xFF, 0xFF)));
        private static readonly Brush HairlineBrush = Freeze(new SolidColorBrush(Color.FromArgb(0x24, 0xFF, 0xFF, 0xFF)));

        public static FlowDocument ToFlowDocument(string markdown)
        {
            var document = new FlowDocument
            {
                FontFamily = BodyFont,
                FontSize = BodyFontSize,
                LineHeight = 21,
                PagePadding = new Thickness(0),
                TextAlignment = TextAlignment.Left
            };

            document.SetResourceReference(TextElement.ForegroundProperty, "Brush.TextPrimary");

            MarkdownDocument parsed = Markdown.Parse(markdown ?? string.Empty, Pipeline);

            foreach (MdBlock block in parsed)
            {
                WpfBlock? rendered = RenderBlock(block, 0);
                if (rendered != null)
                    document.Blocks.Add(rendered);
            }

            // No spacing above the first / below the last block (the host provides the padding)
            if (document.Blocks.FirstBlock != null)
                document.Blocks.FirstBlock.Margin = WithTop(document.Blocks.FirstBlock.Margin, 0);

            if (document.Blocks.LastBlock != null)
                document.Blocks.LastBlock.Margin = WithBottom(document.Blocks.LastBlock.Margin, 0);

            return document;
        }

        // ===== Blocks =====

        private static WpfBlock? RenderBlock(MdBlock block, int listDepth)
        {
            return block switch
            {
                HeadingBlock heading => RenderHeading(heading),
                ParagraphBlock paragraph => RenderParagraph(paragraph),
                ListBlock list => RenderList(list, listDepth),
                AlertBlock alert => RenderAlert(alert, listDepth),
                QuoteBlock quote => RenderQuote(quote, listDepth),
                FencedCodeBlock code => RenderCode(code),
                CodeBlock code => RenderCode(code),
                ThematicBreakBlock => RenderHorizontalRule(),
                MdTable table => RenderTable(table),
                _ => null
            };
        }

        private static WpfBlock RenderHeading(HeadingBlock heading)
        {
            var paragraph = new Paragraph
            {
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 18, 0, BlockSpacing)
            };

            switch (heading.Level)
            {
                case 1:
                    paragraph.FontSize = 22;
                    paragraph.LineHeight = 30;
                    paragraph.Padding = new Thickness(0, 0, 0, 6);
                    paragraph.BorderBrush = HairlineBrush;
                    paragraph.BorderThickness = new Thickness(0, 0, 0, 1);
                    break;

                case 2:
                    paragraph.FontSize = 17;
                    paragraph.LineHeight = 25;
                    paragraph.Padding = new Thickness(0, 0, 0, 5);
                    paragraph.BorderBrush = HairlineBrush;
                    paragraph.BorderThickness = new Thickness(0, 0, 0, 1);
                    break;

                case 3:
                    paragraph.FontSize = 15;
                    break;

                default:
                    paragraph.FontSize = BodyFontSize;
                    break;
            }

            AddInlines(paragraph.Inlines, heading.Inline);
            return paragraph;
        }

        private static Paragraph RenderParagraph(ParagraphBlock block)
        {
            var paragraph = new Paragraph { Margin = new Thickness(0, 0, 0, BlockSpacing) };
            AddInlines(paragraph.Inlines, block.Inline);
            return paragraph;
        }

        private static WpfList RenderList(ListBlock block, int listDepth)
        {
            var list = new WpfList
            {
                MarkerStyle = block.IsOrdered
                    ? TextMarkerStyle.Decimal
                    : listDepth == 0 ? TextMarkerStyle.Disc : TextMarkerStyle.Circle,
                Padding = new Thickness(22, 0, 0, 0),
                Margin = new Thickness(0, 0, 0, listDepth == 0 ? BlockSpacing : 0)
            };

            if (block.IsOrdered && int.TryParse(block.OrderedStart, out int start))
                list.StartIndex = start;

            foreach (MdBlock item in block)
            {
                if (item is not ListItemBlock listItem)
                    continue;

                var wpfItem = new ListItem();

                foreach (MdBlock child in listItem)
                {
                    WpfBlock? rendered = RenderBlock(child, listDepth + 1);
                    if (rendered == null)
                        continue;

                    // Items of a "tight" list: paragraphs without spacing in between
                    if (rendered is Paragraph)
                        rendered.Margin = new Thickness(0, 0, 0, block.IsLoose ? BlockSpacing / 2 : 3);

                    wpfItem.Blocks.Add(rendered);
                }

                // Task list item: checkbox glyph instead of the bullet
                if (wpfItem.Blocks.FirstBlock is Paragraph first && first.Inlines.FirstInline is Run { Tag: TaskList })
                    list.MarkerStyle = TextMarkerStyle.None;

                list.ListItems.Add(wpfItem);
            }

            return list;
        }

        private static WpfBlock RenderQuote(QuoteBlock block, int listDepth)
        {
            var section = new Section
            {
                Margin = new Thickness(0, 0, 0, BlockSpacing),
                Padding = new Thickness(12, 2, 0, 2),
                BorderThickness = new Thickness(3, 0, 0, 0)
            };

            section.SetResourceReference(TextElement.ForegroundProperty, "Brush.TextSecondary");
            section.SetResourceReference(WpfBlock.BorderBrushProperty, "Brush.Accent");

            foreach (MdBlock child in block)
            {
                WpfBlock? rendered = RenderBlock(child, listDepth);
                if (rendered != null)
                    section.Blocks.Add(rendered);
            }

            if (section.Blocks.LastBlock != null)
                section.Blocks.LastBlock.Margin = WithBottom(section.Blocks.LastBlock.Margin, 0);

            return section;
        }

        /// <summary>
        /// GitHub alert ("> [!NOTE]", TIP, IMPORTANT, WARNING, CAUTION): quote with a colored bar and a title with icon.
        /// </summary>
        private static WpfBlock RenderAlert(AlertBlock block, int listDepth)
        {
            string kind = block.Kind.ToString().ToUpperInvariant();

            (string glyph, string title, string brushKey) = kind switch
            {
                "TIP" => ("", "Tip", "Brush.Success"),
                "IMPORTANT" => ("", "Important", "Brush.Accent"),
                "WARNING" => ("", "Warning", "Brush.Warning"),
                "CAUTION" => ("", "Caution", "Brush.Danger"),
                _ => ("", "Note", "Brush.Accent")
            };

            var section = new Section
            {
                Margin = new Thickness(0, 0, 0, BlockSpacing),
                Padding = new Thickness(12, 2, 0, 2),
                BorderThickness = new Thickness(3, 0, 0, 0)
            };

            section.SetResourceReference(WpfBlock.BorderBrushProperty, brushKey);

            var titleParagraph = new Paragraph
            {
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 4)
            };

            titleParagraph.SetResourceReference(TextElement.ForegroundProperty, brushKey);
            titleParagraph.Inlines.Add(new Run(glyph + "  ") { FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"), BaselineAlignment = BaselineAlignment.Center });
            titleParagraph.Inlines.Add(new Run(title));
            section.Blocks.Add(titleParagraph);

            foreach (MdBlock child in block)
            {
                WpfBlock? rendered = RenderBlock(child, listDepth);
                if (rendered != null)
                    section.Blocks.Add(rendered);
            }

            if (section.Blocks.LastBlock != null)
                section.Blocks.LastBlock.Margin = WithBottom(section.Blocks.LastBlock.Margin, 0);

            return section;
        }

        private static WpfBlock RenderCode(LeafBlock block)
        {
            var paragraph = new Paragraph(new Run(block.Lines.ToString().TrimEnd('\r', '\n')))
            {
                FontFamily = CodeFont,
                FontSize = 12.5,
                LineHeight = 19,
                Background = CodeBackground,
                Padding = new Thickness(12, 10, 12, 10),
                Margin = new Thickness(0, 0, 0, BlockSpacing)
            };

            return paragraph;
        }

        private static WpfBlock RenderHorizontalRule()
        {
            return new Paragraph
            {
                Margin = new Thickness(0, 6, 0, 16),
                FontSize = 1,
                LineHeight = 1,
                BorderBrush = HairlineBrush,
                BorderThickness = new Thickness(0, 0, 0, 1)
            };
        }

        private static WpfBlock RenderTable(MdTable table)
        {
            var wpfTable = new WpfTable
            {
                CellSpacing = 0,
                Margin = new Thickness(0, 0, 0, BlockSpacing),
                BorderBrush = HairlineBrush,
                BorderThickness = new Thickness(1, 1, 0, 0)
            };

            var rowGroup = new TableRowGroup();
            wpfTable.RowGroups.Add(rowGroup);

            foreach (MdBlock rowBlock in table)
            {
                if (rowBlock is not MdTableRow row)
                    continue;

                var wpfRow = new WpfTableRow();

                if (row.IsHeader)
                    wpfRow.FontWeight = FontWeights.SemiBold;

                for (int i = 0; i < row.Count; i++)
                {
                    if (row[i] is not MdTableCell cell)
                        continue;

                    var wpfCell = new WpfTableCell
                    {
                        Padding = new Thickness(10, 5, 10, 5),
                        BorderBrush = HairlineBrush,
                        BorderThickness = new Thickness(0, 0, 1, 1)
                    };

                    TextAlignment alignment = i < table.ColumnDefinitions.Count
                        ? ToTextAlignment(table.ColumnDefinitions[i].Alignment)
                        : TextAlignment.Left;

                    foreach (MdBlock child in cell)
                    {
                        WpfBlock? rendered = RenderBlock(child, 0);
                        if (rendered == null)
                            continue;

                        rendered.Margin = new Thickness(0);
                        rendered.TextAlignment = alignment;
                        wpfCell.Blocks.Add(rendered);
                    }

                    wpfRow.Cells.Add(wpfCell);
                }

                rowGroup.Rows.Add(wpfRow);
            }

            return wpfTable;
        }

        private static TextAlignment ToTextAlignment(TableColumnAlign? align)
        {
            return align switch
            {
                TableColumnAlign.Center => TextAlignment.Center,
                TableColumnAlign.Right => TextAlignment.Right,
                _ => TextAlignment.Left
            };
        }

        // ===== Inlines =====

        private static void AddInlines(InlineCollection target, ContainerInline? container)
        {
            if (container == null)
                return;

            foreach (MdInline inline in container)
            {
                WpfInline? rendered = RenderInline(inline);
                if (rendered != null)
                    target.Add(rendered);
            }
        }

        private static WpfInline? RenderInline(MdInline inline)
        {
            switch (inline)
            {
                case LiteralInline literal:
                    return new Run(literal.Content.ToString());

                case LineBreakInline lineBreak:
                    // Soft breaks (single line break in the source) flow like a space, as on GitHub
                    return lineBreak.IsHard ? new LineBreak() : new Run(" ");

                case CodeInline code:
                    var codeRun = new Run(code.Content)
                    {
                        FontFamily = CodeFont,
                        FontSize = BodyFontSize - 1,
                        Background = CodeBackground
                    };
                    return codeRun;

                case EmphasisInline emphasis:
                    return RenderEmphasis(emphasis);

                case TaskList task:
                    // Marker of a task list item: checkbox glyph (Segoe Fluent Icons)
                    var taskRun = new Run(task.Checked ? "  " : "  ")
                    {
                        FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"),
                        Tag = task
                    };
                    return taskRun;

                case LinkInline link:
                    return RenderLink(link);

                case AutolinkInline autolink:
                    return CreateHyperlink(autolink.Url, new Run(autolink.Url));

                case HtmlEntityInline entity:
                    return new Run(entity.Transcoded.ToString());

                case ContainerInline container:
                    var span = new Span();
                    AddInlines(span.Inlines, container);
                    return span;

                default:
                    // Raw HTML and unsupported inlines are left out
                    return null;
            }
        }

        private static WpfInline RenderEmphasis(EmphasisInline emphasis)
        {
            var span = new Span();
            AddInlines(span.Inlines, emphasis);

            if (emphasis.DelimiterChar == '~')
            {
                span.TextDecorations = TextDecorations.Strikethrough;
                return span;
            }

            if (emphasis.DelimiterCount >= 2)
                span.FontWeight = FontWeights.SemiBold;
            else
                span.FontStyle = FontStyles.Italic;

            return span;
        }

        private static WpfInline RenderLink(LinkInline link)
        {
            if (link.IsImage)
            {
                // Images are not shown: their alternative text instead
                var alt = new Span();
                AddInlines(alt.Inlines, link);
                alt.SetResourceReference(TextElement.ForegroundProperty, "Brush.TextSecondary");
                return alt;
            }

            var content = new Span();
            AddInlines(content.Inlines, link);
            return CreateHyperlink(link.Url, content);
        }

        private static WpfInline CreateHyperlink(string? url, WpfInline content)
        {
            var hyperlink = new Hyperlink(content) { TextDecorations = null };
            hyperlink.SetResourceReference(TextElement.ForegroundProperty, "Brush.AccentHover");

            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                return hyperlink;
            }

            hyperlink.ToolTip = uri.AbsoluteUri;
            hyperlink.Cursor = System.Windows.Input.Cursors.Hand;

            // Hyperlinks only navigate by themselves inside a NavigationWindow: open in the browser
            hyperlink.Click += (_, _) => Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
            hyperlink.MouseEnter += (_, _) => hyperlink.TextDecorations = TextDecorations.Underline;
            hyperlink.MouseLeave += (_, _) => hyperlink.TextDecorations = null;

            return hyperlink;
        }

        // ===== Helpers =====

        private static Thickness WithTop(Thickness thickness, double top)
        {
            return new Thickness(thickness.Left, top, thickness.Right, thickness.Bottom);
        }

        private static Thickness WithBottom(Thickness thickness, double bottom)
        {
            return new Thickness(thickness.Left, thickness.Top, thickness.Right, bottom);
        }

        private static Brush Freeze(Brush brush)
        {
            brush.Freeze();
            return brush;
        }
    }
}
