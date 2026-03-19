
using Signum.Files;
using Signum.Word;
using System.IO;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using A = DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Document = DocumentFormat.OpenXml.Wordprocessing.Document;
using Aspose.Pdf;
using RRSP.Globals;
using Meros.Protocol;
using Meros.StatusReport;
using Meros.Project;
using Meros.Risk;
using Meros.PlanningProject.PSC;
using Meros.PlanningProject.WorkPackage;
using Meros.PlanningProject.BusinessCase;
using System.Text.RegularExpressions;
using Signum.Tour;

namespace RRSP.WordTemplates;

public static class RRSPWordLogic
{
    public static void Start(SchemaBuilder sb)
    {
        if (sb.AlreadyDefined(MethodInfo.GetCurrentMethod()))
            return;

        TourLogic.RegisterTourTriggers(RRSPTourTriggers.Introduction);

        new Aspose.Words.License().SetLicense(GetLicenseFile());
        new Aspose.Pdf.License().SetLicense(GetLicenseFile());

        WordTemplateLogic.RegisterConverter(RRSPWordConverter.AsposeToPdf, (wc, bytes) =>
        {
            var doc = new MemoryStream(bytes).Using(ms => new Aspose.Words.Document(ms));

            return new MemoryStream().Using(ms =>
            {
                doc.Save(ms, Aspose.Words.SaveFormat.Pdf);

                ms.Seek(0, SeekOrigin.Begin);
                return ms.ToArray();
            });
        });

        WordTemplateLogic.RegisterConverter(RRSPWordConverter.AsposeToPdfWithAttachments, (wc, bytes) =>
        {
            var doc = new MemoryStream(bytes).Using(ms => new Aspose.Words.Document(ms));

            return new MemoryStream().Using(ms =>
            {
                doc.Save(ms, Aspose.Words.SaveFormat.Pdf);

                Aspose.Pdf.Document pdfDocument = new Aspose.Pdf.Document(ms);


                var entity = wc.GetEntity();

                if (entity is MeetingProtocolEntity protocol)
                {
                    foreach (var attachment in protocol!.Attachments)
                    {
                        var msAtt = new MemoryStream();
                        attachment.OpenRead().CopyTo(msAtt);
                        msAtt.Seek(0, SeekOrigin.Begin);

                        var fileSpecification = new FileSpecification(msAtt, attachment.FileName);

                        pdfDocument.EmbeddedFiles.Add(fileSpecification);
                    }
                }
                else if (entity is ChangeRequestEntity changeRequest)
                {
                    foreach (var attachment in changeRequest!.Attachments)
                    {
                        var msAtt = new MemoryStream();
                        attachment.OpenRead().CopyTo(msAtt);
                        msAtt.Seek(0, SeekOrigin.Begin);

                        var fileSpecification = new FileSpecification(msAtt, attachment.FileName);

                        pdfDocument.EmbeddedFiles.Add(fileSpecification);
                    }
                }
                else if (entity is ProjectWorkPackageEntity workPackage)
                {
                    foreach (var attachment in workPackage!.Attachments)
                    {
                        var msAtt = new MemoryStream();
                        attachment.OpenRead().CopyTo(msAtt);
                        msAtt.Seek(0, SeekOrigin.Begin);

                        var fileSpecification = new FileSpecification(msAtt, attachment.FileName);

                        pdfDocument.EmbeddedFiles.Add(fileSpecification);
                    }
                }

                pdfDocument.Save(ms, Aspose.Pdf.SaveFormat.Pdf);

                ms.Seek(0, SeekOrigin.Begin);
                return ms.ToArray();
            });
        });

        WordModelLogic.RegisterWordModel<ChangeRequestWordModel>(null);
        WordModelLogic.RegisterWordModel<WorkPackageWordModel>(null);
        WordModelLogic.RegisterWordModel<BusinessCaseWordModel>(null);
        WordModelLogic.RegisterWordModel<ProjectCharterWordModel>(null);

        WordTemplateLogic.RegisterTransformer(RRSPWordTransformer.UpdateProjectStatusReport, (wc, package) =>
        {
            UpdateProjectStatusReport(package, ((StatusReportEntity)wc.Entity!));
        });

        WordTemplateLogic.RegisterTransformer(RRSPWordTransformer.InsertRiskCategoryTable, (wc, package) =>
        {
            InsertRiskCategoryTable(package, ((IDomainEntity)wc.Entity!));
        });
    }

    private static Stream GetLicenseFile()
    {
        return typeof(RRSPWordLogic).Assembly.GetManifestResourceStream("RRSP.WordTemplates.Aspose.Total.lic")!;
    }

    public static byte[] ToBytes(this Aspose.Pdf.Document result)
    {
        return new MemoryStream().Using(outMs =>
        {
            result.Save(outMs, Aspose.Pdf.SaveFormat.Pdf);
            outMs.Seek(0, SeekOrigin.Begin);
            return outMs.ToArray();
        });
    }

    private static string GetStatusColorCode(ProgressType p)
    {
        return p == ProgressType.Ok ? "6BB700" :
                p == ProgressType.Warning ? "FFAA44" : p == ProgressType.Danger ? "E4032E" : "FFFFFF";
    }

    private static void SetShapeStatusColor(SlidePart slidePart, ProgressType p, string caption)
    {
        var shape = slidePart.Slide?.GetFirstChild<CommonSlideData>()?.Descendants<Shape>().SingleOrDefault(a => 
            a.Descendants<NonVisualDrawingProperties>().SingleOrDefault()?.Description == caption);
        if (shape != null)
            shape.GetFirstChild<ShapeProperties>()!.GetFirstChild<A.SolidFill>()!.GetFirstChild<A.RgbColorModelHex>()!.Val = GetStatusColorCode(p);
    }

    private static void SetConnectionStatusColor(SlidePart slidePart, decimal progress, ProgressType p, string caption)
    {
        var connection = slidePart.Slide?.GetFirstChild<CommonSlideData>()?.Descendants<ConnectionShape>().SingleOrDefault(a =>
            a.Descendants<NonVisualDrawingProperties>().SingleOrDefault()?.Description == caption);
        if (connection != null)
        {
            connection.GetFirstChild<ShapeProperties>()!.GetFirstChild<A.Transform2D>()!.GetFirstChild<A.Extents>()!.Cx = (Int64)(progress * 1239288L);
            connection.GetFirstChild<ShapeProperties>()!.GetFirstChild<A.Outline>()!.GetFirstChild<A.SolidFill>()!.GetFirstChild<A.RgbColorModelHex>()!.Val = GetStatusColorCode(p);
        }
    }

    private static void SetConsumedStatusSize(SlidePart slidePart, decimal percent, string caption)
    {
        var connection = slidePart.Slide?.GetFirstChild<CommonSlideData>()?.Descendants<ConnectionShape>().SingleOrDefault(a =>
            a.Descendants<NonVisualDrawingProperties>().SingleOrDefault()?.Description == caption);
        if (connection != null)
            connection.GetFirstChild<ShapeProperties>()!.GetFirstChild<A.Transform2D>()!.GetFirstChild<A.Extents>()!.Cx = (Int64)(percent * 3382205L);
    }

    private static void SetTextStatusColor(SlidePart slidePart, ProgressType p, string caption)
    {
        var shape = slidePart.Slide?.GetFirstChild<CommonSlideData>()?.Descendants<Shape>().SingleOrDefault(a =>
            a.Descendants<NonVisualDrawingProperties>().SingleOrDefault()?.Description == caption);
        
        if (shape != null)
        {
            shape.GetFirstChild<TextBody>()!.GetFirstChild<A.Paragraph>()!.GetFirstChild<A.Run>()!.GetFirstChild<A.RunProperties>()!.GetFirstChild<A.SolidFill>()!
                .GetFirstChild<A.RgbColorModelHex>()!.Val = GetStatusColorCode(p);

            shape.GetFirstChild<TextBody>()!.GetFirstChild<A.Paragraph>()!.GetFirstChild<A.EndParagraphRunProperties>()!.GetFirstChild<A.SolidFill>()!
                .GetFirstChild<A.RgbColorModelHex>()!.Val = GetStatusColorCode(p);
        }
    }

    private static void RemoveHiddenSlide(PresentationPart presentationPart, Slide slide)
    {
        var presentation = presentationPart.Presentation;

        if (presentation == null ||  presentation.SlideIdList == null || slide.SlidePart == null)
            return;

        string slideRelId = presentationPart.GetIdOfPart(slide.SlidePart);

        var slideId = presentation.SlideIdList.ChildElements.Where(s => ((SlideId)s).RelationshipId == slideRelId).FirstOrDefault() as SlideId;

        presentation.SlideIdList.RemoveChild(slideId);

        if (presentation.CustomShowList != null)
        {
            foreach (var customShow in presentation.CustomShowList.Elements<CustomShow>())
            {
                if (customShow.SlideList != null)
                {
                    var entry = customShow.SlideList.ChildElements.Where(s => ((SlideListEntry)s).Id?.Value == slideRelId).FirstOrDefault();
                    if (entry != null)
                        customShow.SlideList.RemoveChild(entry);
                }
            }
        }

        presentationPart.DeletePart(slideRelId);
    }

    public static void UpdateProjectStatusReport(OpenXmlPackage document, StatusReportEntity e)
    {
        var presentationPart = ((PresentationDocument)document!).PresentationPart;

        if (presentationPart == null)
            throw new Exception(StatusReportMessage.InvalidPresentationFormat.NiceToString());
        
        var slide = presentationPart.SlideParts.SingleOrDefaultEx(s => s.Slide != null && s.Slide.Descendants<DocumentFormat.OpenXml.Drawing.Paragraph>().Any(r => r.InnerText == "Order Position Slide"))?.Slide;

        if (slide != null )
        {
            if (!e.AddOrderPositionsToReport)
                RemoveHiddenSlide(presentationPart, slide);
            else
                slide.Descendants<ShapeTree>().SelectMany(st => st.Elements<Shape>()).SingleEx(s => s.InnerText == "Order Position Slide").Remove();
        }

        presentationPart.SlideParts.ToList().ForEach(slidePart =>
        {
            
            SetShapeStatusColor(slidePart, e.LastOverallProgress.Status, "LAST STATUS");
            SetShapeStatusColor(slidePart, e.LastOverallProgress.Status, "LAST STATUS BG");
            SetConnectionStatusColor(slidePart, e.LastOverallProgress.Progress, e.LastOverallProgress.Status, "LAST PROGRESS STATUSBAR");
            SetTextStatusColor(slidePart, e.LastOverallProgress.Status, "LAST PROGRESS TEXT");

            SetShapeStatusColor(slidePart, e.OverallProgress.Status, "CURRENT STATUS");
            SetShapeStatusColor(slidePart, e.OverallProgress.Status, "CURRENT STATUS BG");
            SetConnectionStatusColor(slidePart, e.OverallProgress.Progress, e.OverallProgress.Status, "PROGRESS STATUSBAR");
            SetTextStatusColor(slidePart, e.OverallProgress.Status, "PROGRESS TEXT");

            SetShapeStatusColor(slidePart, e.Budget.LastStatus, "LAST BUDGET");
            SetShapeStatusColor(slidePart, e.Budget.Status, "BUDGET");

            SetShapeStatusColor(slidePart, e.Scope.LastStatus, "LAST SCOPE");
            SetShapeStatusColor(slidePart, e.Scope.Status, "SCOPE");

            SetShapeStatusColor(slidePart, e.TimeLine.LastStatus, "LAST TIMELINE");
            SetShapeStatusColor(slidePart, e.TimeLine.Status, "TIMELINE");

            SetShapeStatusColor(slidePart, e.Resources.LastStatus, "LAST RESOURCES");
            SetShapeStatusColor(slidePart, e.Resources.Status, "RESOURCES");

            SetShapeStatusColor(slidePart, e.RisksAndIssues.LastStatus, "LAST RISK");
            SetShapeStatusColor(slidePart, e.RisksAndIssues.Status, "RISK");

            SetShapeStatusColor(slidePart, e.Dependencies.LastStatus, "LAST DEPENDENCIES");
            SetShapeStatusColor(slidePart, e.Dependencies.Status, "DEPENDENCIES");

            if(e.ConsumedCost == null)
            {
                SetConsumedStatusSize(slidePart, 0, "CC BILLED");
                SetConsumedStatusSize(slidePart, 0, "CC Consumed");
            }
            else
            {
                var maxCost = (e.ConsumedCost.MaxCost ?? e.ConsumedCost.ConsumedCost);
                if (maxCost == 0) 
                    maxCost = 1;
                SetConsumedStatusSize(slidePart, e.ConsumedCost.BilledCost / maxCost, "CC BILLED");
                SetConsumedStatusSize(slidePart, e.ConsumedCost.MaxCost.HasValue ? e.ConsumedCost.ConsumedCost/ maxCost : 1, "CC CONSUMED");
            }

            var maxTime = e.ConsumedTime.MaxTime ?? e.ConsumedTime.ConsumedTime;
            if (maxTime == 0)
                maxTime = 1;
            SetConsumedStatusSize(slidePart, e.ConsumedTime.BilledTime / maxTime, "CT BILLED");
            SetConsumedStatusSize(slidePart, e.ConsumedTime.MaxTime.HasValue ? e.ConsumedTime.ConsumedTime / maxTime : 1, "CT CONSUMED");
        });
    }

    private static void ReplaceTable(DocumentFormat.OpenXml.Wordprocessing.Table table, RiskType type, IDomainEntity e)
    {
        TableRow theRow = table.Elements<TableRow>().Last();
        table.RemoveChild(theRow);

        TableWidth width = new TableWidth();
        width.Width = "5000"; // for fitting table to page width
        width.Type = TableWidthUnitValues.Pct;

        UInt32Value bordersize = 1;
        TableProperties props = new TableProperties(
            width,
            new TableBorders(
            new TopBorder
            {
                Val = new EnumValue<BorderValues>(BorderValues.Single),
                Size = bordersize,
                Color = "FFFFFF"
            },
            new BottomBorder
            {
                Val = new EnumValue<BorderValues>(BorderValues.Single),
                Size = bordersize,
                Color = "FFFFFF"
            },
            new LeftBorder
            {
                Val = new EnumValue<BorderValues>(BorderValues.Single),
                Size = bordersize,
                Color = "FFFFFF"
            },
            new RightBorder
            {
                Val = new EnumValue<BorderValues>(BorderValues.Single),
                Size = bordersize,
                Color = "FFFFFF"
            },
            new InsideHorizontalBorder
            {
                Val = new EnumValue<BorderValues>(BorderValues.Single),
                Size = bordersize,
                Color = "FFFFFF"
            },
            new InsideVerticalBorder
            {
                Val = new EnumValue<BorderValues>(BorderValues.Single),
                Size = bordersize,
                Color = "FFFFFF"
            }));

        table.AppendChild<TableProperties>(props);

        if (((Entity)e).Mixin<DomainRiskMixin>().RiskManagement?.RiskCategoryTable == null || ((Entity)e).Mixin<DomainRiskMixin>().RiskManagement?.ChanceCategoryTable == null)
            throw new ApplicationException(RiskMessage.RiskManagementSettingsIsEmpty0.NiceToString(e.GetType().NiceName()));

        var rct = type == RiskType.Risk ? ((Entity)e).Mixin<DomainRiskMixin>().RiskManagement!.RiskCategoryTable!.Retrieve() : ((Entity)e).Mixin<DomainRiskMixin>().RiskManagement!.ChanceCategoryTable!.Retrieve();
        TableRow tr = NewTableRow();
        // FONT COLOR = #ffffff

        AddCell(tr, $"{RiskCategoryTableMessage.Probability.NiceToString()} / {RiskCategoryTableMessage.Impact.NiceToString()}", "#A6A6A6", "FFFFFF");

        rct.ImpactRanges.ForEach(impact => AddCell(tr, impact.ToString(), "#A6A6A6", "FFFFFF"));
        table.Append(tr);

        var risksOrChances = type == RiskType.Risk ? e.ActiveRisks().ToList() : e.ActiveChances().ToList();

        rct.ProbabilityRanges.OrderByDescending(r => r.MaxValue).ToMList().ForEach((probability, pi) =>
        {
            var tr = NewTableRow();
            AddCell(tr, probability.ToString(), "#A6A6A6", "FFFFFF");

            rct.ImpactRanges.ForEach((impact, ii) =>
            {
                var reverseIndex = rct.ProbabilityRanges.Count - 1 - pi;
                var cell = rct.Cells.FirstOrDefault(c => c.Probability == reverseIndex && c.Impact == ii);
                var category = cell != null ? rct.Categories.ElementAt(cell.Category) : null;
                var count = risksOrChances.Where(r => r.Probability >= probability.MinValue && r.Probability <= probability.MaxValue &&
                    r.Impact >= impact.MinValue && r.Impact <= impact.MaxValue).Count();

                if (category != null)
                    AddCell(tr, category.Name + (count > 0 ? $"  ({count})" : ""), category.Color, category.FontColor);
            });
            table.Append(tr);
        });
    }

    public static void AddRiskCategoryTable(Document doc, IDomainEntity e)
    {
        IEnumerable<TableProperties> tableProperties = doc.Body!.Descendants<TableProperties>().Where(tp => tp.TableCaption != null);
        foreach (TableProperties tProp in tableProperties)
        {
            if (tProp.TableCaption!.Val!.Equals("RiskCategoryTable")) 
            {               
                DocumentFormat.OpenXml.Wordprocessing.Table table = (DocumentFormat.OpenXml.Wordprocessing.Table)tProp.Parent!;
                ReplaceTable(table, RiskType.Risk, e);
            }
            else if (tProp.TableCaption!.Val!.Equals("ChanceCategoryTable"))
            {
                DocumentFormat.OpenXml.Wordprocessing.Table table = (DocumentFormat.OpenXml.Wordprocessing.Table)tProp.Parent!;
                ReplaceTable(table, RiskType.Chance, e);
            }
        }

        doc.Save();
    }

    private static TableRow NewTableRow()
    {
        var tr = new TableRow();
        TableRowProperties rowProps = new TableRowProperties();
        TableRowHeight tbh = new TableRowHeight();
        tbh.Val = 500;
        tbh.HeightType = HeightRuleValues.Exact;
        rowProps.Append(tbh);
        tr.Append(rowProps);
        return tr;
    }

    private static void AddCell(TableRow tr, string content, string? backgroundColor = null, string? fontColor = null)
    {
        var tc = new TableCell();

        TableCellProperties tcp = new TableCellProperties(new TableCellWidth { Type = TableWidthUnitValues.Auto, });
        TableCellVerticalAlignment tcVerticalAlignment = new TableCellVerticalAlignment() { Val = TableVerticalAlignmentValues.Center };
        tcp.Append(tcVerticalAlignment);


        Run run = new Run();
        if (fontColor != null)
        {
            RunProperties rp = new RunProperties();
            rp.Append(new DocumentFormat.OpenXml.Wordprocessing.Color { Val = fontColor});
            run.RunProperties = rp;
        }

        if (backgroundColor != null)
        {
            Shading shading = new Shading()
            {
                Color = "auto",
                Fill = backgroundColor,
                Val = ShadingPatternValues.Clear
            };
            tcp.Append(shading);
        }

        run.Append(new DocumentFormat.OpenXml.Wordprocessing.Text(content));

        tc.Append(new Paragraph(run));

        tc.Append(tcp);

        tr.Append(tc);
    }

    public static void InsertRiskCategoryTable(OpenXmlPackage document, IDomainEntity e)
    {
        var doc = ((WordprocessingDocument)document!).MainDocumentPart?.Document;

        if (doc == null)
            throw new Exception(RiskMessage.InvalidProejctRiskDocumentFile.NiceToString());

        AddRiskCategoryTable(doc, e);
    }
}
