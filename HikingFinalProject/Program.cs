using HikingFinalProject.Data;
using HikingFinalProject.Filters;
using HikingFinalProject.Mappings;
using HikingFinalProject.Repositories;
using HikingFinalProject.Repositories.Interfaces;
using HikingFinalProject.DTOs.Mapbox;
using HikingFinalProject.DTOs.Routes;
using HikingFinalProject.DTOs.Dashboard;
using HikingFinalProject.Services;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.FileProviders;
using Microsoft.OpenApi.Models;
using System.Data;
using System.Text.Json;

// Optional alias to resolve ambiguity
using MapboxDto = HikingFinalProject.DTOs.Mapbox.MapboxOptions;
using MapboxOptions = HikingFinalProject.Services.MapboxOptions;

var builder = WebApplication.CreateBuilder(args);

// ========================
// Configure Mapbox
// ========================
builder.Services.Configure<MapboxOptions>(
    builder.Configuration.GetSection("Mapbox"));

// ========================
// Dapper Context & DB Connection
// ========================
builder.Services.AddScoped<IDapperContext, DapperContext>();
builder.Services.AddScoped<IDbConnection>(sp =>
    sp.GetRequiredService<IDapperContext>().CreateConnection()
);

// ========================
// Repositories
// ========================
builder.Services.AddScoped<IHikingRouteRepository, HikingRouteRepository>();
builder.Services.AddScoped<IParkRepository, ParkRepository>();
builder.Services.AddScoped<IRoutePointRepository, RoutePointRepository>();
builder.Services.AddScoped<IRouteImageRepository, RouteImageRepository>();
builder.Services.AddScoped<IRouteFeedbackRepository, RouteFeedbackRepository>();
builder.Services.AddScoped<IDashboardRepository, DashboardRepository>();

// ========================
// Services
// ========================
builder.Services.AddScoped<IParkService, ParkService>();
builder.Services.AddScoped<IHikingRouteService, HikingRouteService>();
builder.Services.AddScoped<IRoutePointService, RoutePointService>();
builder.Services.AddScoped<IRouteImageService, RouteImageService>();
builder.Services.AddScoped<IRouteFeedbackService, RouteFeedbackService>();
builder.Services.AddScoped<DashboardService>();

// Mapbox Geocoding Service (via HttpClient)
builder.Services.AddHttpClient<IMapboxGeocodingService, MapboxGeocodingService>();

builder.Services.AddLogging();

// ========================
// MVC + API
// ========================
builder.Services.AddControllersWithViews()
    .AddJsonOptions(opts =>
        opts.JsonSerializerOptions.PropertyNamingPolicy = null
    );
builder.Services.AddRazorPages();
builder.Services.AddEndpointsApiExplorer();

// ========================
// Swagger/OpenAPI
// ========================
builder.Services.AddSwaggerGen(c =>
{
    // Swagger docs for each group
    c.SwaggerDoc("Dashboard", new OpenApiInfo { Title = "Dashboard API", Version = "v1" });
    c.SwaggerDoc("Routes", new OpenApiInfo { Title = "Routes API", Version = "v1" });
    c.SwaggerDoc("Parks", new OpenApiInfo { Title = "Parks API", Version = "v1" });
    c.SwaggerDoc("Feedback", new OpenApiInfo { Title = "Feedback API", Version = "v1" });
    c.SwaggerDoc("Images", new OpenApiInfo { Title = "Images API", Version = "v1" });
    c.SwaggerDoc("Points", new OpenApiInfo { Title = "Points API", Version = "v1" });

    // Include controllers by GroupName
    c.DocInclusionPredicate((docName, apiDesc) =>
    {
        if (apiDesc.ActionDescriptor is not ControllerActionDescriptor controllerDesc) return false;

        var groupName = controllerDesc.ControllerTypeInfo
            .GetCustomAttributes(typeof(ApiExplorerSettingsAttribute), true)
            .Cast<ApiExplorerSettingsAttribute>()
            .FirstOrDefault()?.GroupName;

        return groupName == docName;
    });

    // Tag actions by controller name
    c.TagActionsBy(apiDesc =>
    {
        var controllerName = (apiDesc.ActionDescriptor as ControllerActionDescriptor)?.ControllerName;
        return new[] { controllerName ?? "Default" };
    });

    // File upload filter
    c.OperationFilter<FileUploadOperationFilter>();
});

// ========================
// AutoMapper
// ========================
builder.Services.AddAutoMapper(typeof(AutoMapperProfile).Assembly);

// ========================
// CORS
// ========================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

// ========================
// Kestrel URLs
// ========================
builder.WebHost.UseUrls("https://localhost:7149", "http://localhost:5149");

var app = builder.Build();

// ========================
// Middleware
// ========================
app.UseCors("AllowAll");
app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();

// ========================
// Uploads folder
// ========================
var uploadsPath = Path.Combine(app.Environment.ContentRootPath, "wwwroot", "uploads");
Directory.CreateDirectory(uploadsPath);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads",
    ServeUnknownFileTypes = true
});

// ========================
// Swagger UI
// ========================
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/Dashboard/swagger.json", "Dashboard API");
    c.SwaggerEndpoint("/swagger/Routes/swagger.json", "Routes API");
    c.SwaggerEndpoint("/swagger/Parks/swagger.json", "Parks API");
    c.SwaggerEndpoint("/swagger/Feedback/swagger.json", "Feedback API");
    c.SwaggerEndpoint("/swagger/Images/swagger.json", "Images API");
    c.SwaggerEndpoint("/swagger/Points/swagger.json", "Points API");

    c.RoutePrefix = "swagger";
});

// ========================
// Authorization Middleware
// ========================
app.UseAuthorization();

// ========================
// Map routes
// ========================
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}"
);
app.MapControllers();

// ========================
// SPA fallback
// ========================
if (File.Exists(Path.Combine(app.Environment.WebRootPath!, "index.html")))
{
    app.MapFallbackToFile("index.html");
}

app.Run();



