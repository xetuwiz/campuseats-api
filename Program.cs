using CampusEats.Api.Services;
using CampusEats.Api.Data;
using CampusEats.Api.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Paste ONLY the token - no 'Bearer' prefix."
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { new OpenApiSecurityScheme { Reference = new OpenApiReference
        { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
        Array.Empty<string>() }
    });
});

builder.Services.AddScoped<IMenuService, MenuService>();

// CORS -- lets the Vercel-hosted frontend call this API from the browser.
// Origins are config-driven (Cors:AllowedOrigins) so a new domain can be added
// on the host via Cors__AllowedOrigins__0 without editing and redeploying code.
const string CorsPolicyName = "Frontend";

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[]
    {
        "https://campuseats-dashboard-three.vercel.app", // stable Vercel domain
        "http://localhost:5173",                        // Vite dev server
        "http://localhost:4173"                         // vite preview (prod build)
    };

builder.Services.AddCors(o => o.AddPolicy(CorsPolicyName, p =>
{
    p.SetIsOriginAllowed(origin =>
        allowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase)
        || IsVercelDeploymentOrigin(origin));

    p.AllowAnyHeader().AllowAnyMethod();
}));

var jwt = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwt["Key"]!);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt => opt.TokenValidationParameters = new()
    {
        ValidateIssuer = true,
        ValidIssuer = jwt["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwt["Audience"],
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateLifetime = true
    });

builder.Services.AddAuthorization();

// Vercel issues a unique hostname per deployment, e.g.
// campuseats-dashboard-<hash>-xetuwizs-projects.vercel.app, so the stable
// domain alone would break CORS on the next redeploy. Allow that shape, but
// only hostnames under THIS Vercel team -- never every *.vercel.app, so an
// unrelated tenant's deployment cannot call this API.
static bool IsVercelDeploymentOrigin(string origin)
{
    if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri)) return false;
    if (!string.Equals(uri.Scheme, "https", StringComparison.OrdinalIgnoreCase)) return false;

    // Must end with "-xetuwizs-projects.vercel.app". Vercel joins the team with a
    // HYPHEN ("<project>-<hash>-<team>.vercel.app"), not a dot, so matching on
    // a dot would reject every real deployment URL.
    const string TeamSuffix = "-xetuwizs-projects.vercel.app";

    return uri.Host.EndsWith(TeamSuffix, StringComparison.OrdinalIgnoreCase)
        && uri.Host.Length > TeamSuffix.Length;
}

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (!db.MenuItems.Any())
    {
        db.MenuItems.AddRange(
            new MenuItem { Name = "Kottu Roti", Price = 750m, Category = "Mains" },
            new MenuItem { Name = "Fried Rice", Price = 850m, Category = "Mains" },
            new MenuItem { Name = "Watalappan", Price = 350m, Category = "Dessert" });
        db.SaveChanges();
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Must sit before authentication/authorization and MapControllers so rejected
// CORS preflights are short-circuited before any auth logic runs.
app.UseCors(CorsPolicyName);

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
