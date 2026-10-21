using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using SkyNet;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpContextAccessor();
builder.Services.Configure<KestrelServerOptions>(o => o.Limits.MaxRequestBodySize = 110L * 1024 * 1024);
builder.Services.Configure<IISServerOptions>(o => o.MaxRequestBodySize = 110L * 1024 * 1024);
builder.Services.Configure<FormOptions>(o => o.MultipartBodyLengthLimit = 110L * 1024 * 1024);

var app = builder.Build();
DesignStudio.Core.Local.Accessor = app.Services.GetRequiredService<IHttpContextAccessor>();
DesignStudio.Core.Init.OnStart().GetAwaiter().GetResult();
app.UseMiddleware<IHandler>();

app.UseStaticHttpCurrent();

app.Run();
