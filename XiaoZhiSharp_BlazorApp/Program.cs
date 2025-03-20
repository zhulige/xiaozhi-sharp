using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Diagnostics;
using System.Runtime.InteropServices;
using XiaoZhiSharp_BlazorApp;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();

app.Lifetime.ApplicationStarted.Register(() =>
{
    AppHandle.OpenWebView();
});

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
