using MyShowController;
using System.Diagnostics;
using Microsoft.Extensions.FileProviders;
using System.Net;
using System.Net.Sockets;

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

// --- API POUR LES MACROS VDJ ---
app.MapGet("/api/show/macros", (ShowManager sm) => Results.Ok(sm.LoadMacros()));
app.MapPost("/api/show/macro", (ShowManager sm, VdjMacro m) => { sm.SaveMacro(m); return Results.Ok(); });
app.MapDelete("/api/show/macro/{name}", (ShowManager sm, string name) => { sm.DeleteMacro(name); return Results.Ok(); });

// --- NOUVEAU : API POUR TROUVER L'IP LOCALE (ACCÈS MOBILE) ---
app.MapGet("/api/show/ip", () => {
    string localIP = "127.0.0.1";
    try
    {
        // Cette technique trouve la vraie IP du réseau local en simulant une connexion externe
        using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
        {
            socket.Connect("8.8.8.8", 65530);
            IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
            localIP = endPoint.Address.ToString();
        }
    }
    catch { }
    return Results.Ok(new { ip = localIP });
});
// -------------------------------------------------------------

app.Lifetime.ApplicationStarted.Register(() => {
    var url = "http://localhost:5000/admin.html";
    try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); } catch { }
});

app.Run();