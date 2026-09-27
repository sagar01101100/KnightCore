using System.Globalization;
using KnightCore.Application;
using KnightCore.Domain;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;

namespace KnightCore.Infrastructure;

public sealed class BundledFonts : IFontResolver
{
    public FontResolverInfo? ResolveTypeface(string familyName,bool bold,bool italic)=>new(bold?"sans-bold":"sans");
    public byte[]? GetFont(string faceName)=>File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Fonts",faceName=="sans-bold"?"DejaVuSans-Bold.ttf":"DejaVuSans.ttf"));
}
public sealed class PdfInvoiceRenderer : IInvoiceRenderer
{
    public PdfInvoiceRenderer(){GlobalFontSettings.FontResolver??=new BundledFonts();}
    public byte[] Render(Invoice invoice,CommercialSnapshot snapshot)
    {
        using var document=new PdfDocument();
        document.Info.Title=invoice.Number;document.Info.Author=snapshot.Seller.Name;
        var regular=new XFont("DejaVu Sans",10,XFontStyleEx.Regular);
        var small=new XFont("DejaVu Sans",8,XFontStyleEx.Regular);
        var bold=new XFont("DejaVu Sans",11,XFontStyleEx.Bold);
        var title=new XFont("DejaVu Sans",28,XFontStyleEx.Bold);
        XGraphics? graphics=null;double y=0;double width=0;int pageNumber=0;
        void Page()
        {
            graphics?.Dispose();var page=document.AddPage();page.Size=PdfSharp.PageSize.A4;
            graphics=XGraphics.FromPdfPage(page);width=page.Width.Point;pageNumber++;
            graphics.DrawString("KNIGHTCORE",bold,XBrushes.DarkSlateGray,new XPoint(44,40));
            graphics.DrawString(invoice.Number+"  ·  "+pageNumber,small,XBrushes.Gray,new XPoint(335,40));
            graphics.DrawLine(XPens.LightGray,44,52,width-44,52);y=80;
        }
        void Ensure(double height){if(y+height>760)Page();}
        void Text(string text,XFont font,double x=44,double maxWidth=500)
        {
            foreach(var paragraph in text.Replace("\r","").Split('\n'))
            {
                var line="";
                foreach(var word in paragraph.Split(' ',StringSplitOptions.RemoveEmptyEntries))
                {
                    if(line.Length>0&&graphics!.MeasureString(line+" "+word,font).Width>maxWidth){Ensure(16);graphics.DrawString(line,font,XBrushes.DarkSlateGray,new XPoint(x,y));y+=16;line="";}
                    var rest=word;
                    while(graphics!.MeasureString(rest,font).Width>maxWidth&&rest.Length>1)
                    {
                        if(line.Length>0){Ensure(16);graphics.DrawString(line,font,XBrushes.DarkSlateGray,new XPoint(x,y));y+=16;line="";}
                        var cut=rest.Length-1;while(cut>1&&graphics.MeasureString(rest[..cut],font).Width>maxWidth)cut--;
                        Ensure(16);graphics.DrawString(rest[..cut],font,XBrushes.DarkSlateGray,new XPoint(x,y));y+=16;rest=rest[cut..];
                    }
                    line=line.Length==0?rest:line+" "+rest;
                }
                Ensure(16);graphics!.DrawString(line,font,XBrushes.DarkSlateGray,new XPoint(x,y));y+=16;
            }
        }
        string Money(decimal amount)=>"INR "+amount.ToString("N2",CultureInfo.InvariantCulture);
        Page();
        Text(snapshot.Evaluation?"Sample invoice":"Invoice",title);y+=15;
        if(snapshot.Evaluation){Text("EVALUATION DOCUMENT — NOT A TAX INVOICE",bold);y+=8;}
        Text($"Issued {invoice.IssuedUtc:dd MMM yyyy}   |   Due {invoice.DueUtc:dd MMM yyyy}",regular);y+=14;
        Text("FROM",small);Text(snapshot.Seller.Name,bold);Text(snapshot.Seller.Address,regular);
        Text(snapshot.Seller.Email,regular);if(!string.IsNullOrWhiteSpace(snapshot.Seller.TaxId))Text("Tax ID: "+snapshot.Seller.TaxId,regular);
        y+=14;Text("BILL TO",small);Text(snapshot.Billing.BusinessName,bold);Text(snapshot.Billing.Address,regular);
        Text($"{snapshot.Billing.City}, {snapshot.Billing.State} {snapshot.Billing.PostalCode}",regular);
        if(!string.IsNullOrWhiteSpace(snapshot.Billing.TaxId))Text("Tax ID: "+snapshot.Billing.TaxId,regular);
        y+=18;Text("AGREED SCOPE",small);
        foreach(var line in snapshot.Lines)
        {
            Ensure(65);graphics!.DrawLine(XPens.LightGray,44,y-6,width-44,y-6);y+=12;
            Text(line.Name,bold);
            Text($"{line.Quantity} × {Money(line.UnitPrice)}  =  {Money(line.Amount)}",regular);
            if(line.Code!="base")Text(line.Description,small);
            y+=8;
        }
        Ensure(105);y+=8;Text("Subtotal: "+Money(snapshot.Subtotal),regular);
        Text($"Tax ({snapshot.TaxRate*100:0.##}%): "+Money(snapshot.TaxAmount),regular);
        Text("TOTAL: "+Money(snapshot.Total),bold);y+=16;
        Text("Included: "+string.Join("; ",snapshot.Included),small);
        Text("Estimated delivery: "+snapshot.Delivery,regular);
        Text("Terms: "+snapshot.TermsVersion,small);
        Text("This invoice records an amount due. It is not a payment receipt.",small);
        graphics?.Dispose();using var stream=new MemoryStream();document.Save(stream,false);return stream.ToArray();
    }
}

