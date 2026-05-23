using Microsoft.EntityFrameworkCore;
using OrdersManagement.Data;
using OrdersManagement.Services;
using Pomelo.EntityFrameworkCore.MySql;
using System.Text; // Requerido para Encoding.ASCII
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json;
using ordersmanagement.Interface;
using ordersmanagement.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. REGISTRAR EL SERVICIO DE CORS (Añádelo antes del builder.Build())
builder.Services.AddCors(options =>
{
    options.AddPolicy("PermitirReact", policy =>
    {
        policy.WithOrigins("*") // El puerto exacto de tu React
              .AllowAnyHeader()                     // Permite cabeceras como Authorization, Content-Type, etc.
              .AllowAnyMethod();                    // Permite GET, POST, PUT, DELETE, OPTIONS
    });
});

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddHttpContextAccessor();

builder.Services.AddSingleton<IAlmacenamiento, AlmacenamientoService>();
builder.Services.Configure<OpenAIOptions>(builder.Configuration.GetSection("OpenAI"));
builder.Services.AddTransient<IA, IAService>();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // options.JsonSerializerOptions.PropertyNamingPolicy = null;
        // 1. Forzamos el uso de camelCase para TODO de forma automática
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        
        // 2. Opcional: Esto evita que los objetos anónimos ignoren la regla
        options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
    });

var connectionString = builder.Configuration.GetConnectionString("Conexion");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString)
    )
);

// 1. Configurar JWT de forma manual y compatible
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "UnaClaveSuperSecretaYMuyLargaDeAlMenos32Caracteres!";
var key = Encoding.ASCII.GetBytes(jwtSecret);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = false; 
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

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
app.UseCors("PermitirReact");
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
