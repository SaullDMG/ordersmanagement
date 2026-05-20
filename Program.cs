using Microsoft.EntityFrameworkCore;
using OrdersManagement.Data;
using OrdersManagement.Services;
using Pomelo.EntityFrameworkCore.MySql;

var builder = WebApplication.CreateBuilder(args);


// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("Conexion"),
        new MySqlServerVersion(new Version(8, 0, 0))  // Tu versión de MySQL
    )
);

// Registrar WebSocketService como singleton
builder.Services.AddSingleton<WebSocketService>();

builder.Services.AddSignalR();


var app = builder.Build();

app.UseWebSockets();
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/ws")
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            var webSocket = await context.WebSockets.AcceptWebSocketAsync();
            var webSocketService = context.RequestServices.GetRequiredService<WebSocketService>();
            await webSocketService.HandleWebSocketAsync(context, webSocket);
        }
        else
        {
            context.Response.StatusCode = 400;
        }
    }
    else
    {
        await next();
    }
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
