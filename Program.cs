using MyShowController;
using System.Diagnostics;
using Microsoft.Extensions.FileProviders;
using System.IO;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://*:5000");
builder.Services.AddControllers();
builder.Services.AddSingleton<ShowManager>();

var app = builder.Build();

var cheminWwwroot = Path.Combine(builder.Environment.ContentRootPath, "wwwroot");
if (!Directory.Exists(cheminWwwroot))
    cheminWwwroot = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, @"..\..\..\..\wwwroot"));

if (Directory.Exists(cheminWwwroot))
{
    app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = new PhysicalFileProvider(cheminWwwroot) });
    app.UseStaticFiles(new StaticFileOptions { FileProvider = new PhysicalFileProvider(cheminWwwroot), RequestPath = "" });
}

app.MapControllers();

app.Lifetime.ApplicationStarted.Register(() => {
    // MODIFICATION ICI : On retire /admin.html pour arriver sur la page de choix
    var url = "http://localhost:5000/";
    try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); } catch { }
});

app.Run();