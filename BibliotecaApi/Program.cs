using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using BibliotecaApi.Data;
using BibliotecaApi.Grpc;
using BibliotecaApi.Middleware;
using BibliotecaApi.Repositories;
using BibliotecaApi.Services;

var builder = WebApplication.CreateBuilder(args);

Console.OutputEncoding = System.Text.Encoding.UTF8;

// Log em uma linha só, com horário: dá pra ver a chamada atravessando
// Apresentação -> Domínio -> Repositório no console.
builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.TimestampFormat = "HH:mm:ss ";
});

// Sem TLS o Kestrel não negocia HTTP/1.1 e HTTP/2 na mesma porta (ALPN exige TLS),
// então REST e gRPC ficam em portas dedicadas. Vale igual local e no Docker.
var portaRest = builder.Configuration.GetValue("Portas:Rest", 8080);
var portaGrpc = builder.Configuration.GetValue("Portas:Grpc", 8081);
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(portaRest, o => o.Protocols = HttpProtocols.Http1);
    options.ListenAnyIP(portaGrpc, o => o.Protocols = HttpProtocols.Http2);
});

// ---------------- Apresentação ----------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Biblioteca API",
        Version = "v1",
        Description = "Livros e empréstimos, expostos por REST e por gRPC sobre o mesmo Domínio."
    });
});

builder.Services.AddGrpc(options =>
{
    // Tradução exceção de Domínio -> status gRPC, num lugar só
    options.Interceptors.Add<DomainExceptionInterceptor>();
});
builder.Services.AddGrpcReflection(); // permite o Postman/grpcurl descobrir os serviços sozinhos

// ---------------- Repositório ----------------
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=biblioteca.db"));
builder.Services.AddScoped<ILivroRepository, LivroRepository>();
builder.Services.AddScoped<IEmprestimoRepository, EmprestimoRepository>();

// ---------------- Domínio ----------------
builder.Services.AddScoped<ILivroService, LivroService>();
builder.Services.AddScoped<IEmprestimoService, EmprestimoService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    DbSeeder.Seed(db);
}

app.UseSwagger();
app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "Biblioteca API v1"));

// Traduz as exceções de Domínio em respostas HTTP (lado REST)
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.MapControllers();

app.MapGrpcService<LivroGrpcService>();
app.MapGrpcService<EmprestimoGrpcService>();
app.MapGrpcReflectionService();

app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

app.Run();
