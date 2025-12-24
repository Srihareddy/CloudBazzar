using Serilog;
using Yarp.ReverseProxy;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, lc) =>
{
    var seq = ctx.Configuration["SEQ_URL"] ?? "http://seq:80";
    lc.Enrich.FromLogContext()
      .WriteTo.Console()
      .WriteTo.Seq(seq);
});

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new
{
    service = "Gateway.API",
    routes = new[] { "/catalog", "/basket", "/ordering" }
}));

app.MapReverseProxy();

app.Run();
