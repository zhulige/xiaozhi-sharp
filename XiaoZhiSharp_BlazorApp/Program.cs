using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.StaticFiles;
using System.Diagnostics;
using System.Runtime.InteropServices;
using XiaoZhiSharp_BlazorApp;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages();
builder.Services.AddAntDesign();
builder.Services.AddServerSideBlazor();

var app = builder.Build();

//¾²Ì¬ wwwroot
//Ìí¼ÓMIME
FileExtensionContentTypeProvider provider = new FileExtensionContentTypeProvider();
provider.Mappings[".log"] = "application/octet-stream";
provider.Mappings[".vue"] = "application/octet-stream";
provider.Mappings[".mat"] = "application/octet-stream";
provider.Mappings[".pqy"] = "application/octet-stream";
provider.Mappings[".ply"] = "application/octet-stream";
provider.Mappings[".exr"] = "application/octet-stream";
provider.Mappings[".epicraw"] = "application/octet-stream";
provider.Mappings[".moc"] = "application/octet-stream";
provider.Mappings[".mtn"] = "application/octet-stream";
app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = provider
});
//app.UseStaticFiles();
app.UseRouting();

app.Lifetime.ApplicationStarted.Register(() =>
{
    AppHandle.OpenWebView();
});

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
