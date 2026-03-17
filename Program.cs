using DotNetEnv;
using portfolio_api.Data;
using portfolio_api.Extensions;


namespace portfolio_api
{

public static class Program
{
    public static void Main(string[] args)
    {
        Env.TraversePath().Load();

        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddHttpClient();
        builder.Services.AddScoped<ITenantProvider, HttpTenantProvider>();
        builder.Services.AddHealthChecks();
        builder.Services.AddAppAuthentication(builder.Configuration);
        builder.Services.AddAppCors();
        builder.Services.AddAppDbContext();
        builder.Services.AddOpenApi();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/openapi/v1.json", "v1");
            }
            );

        }

        // Solo redirigir HTTPS en desarrollo local — en Render/producción el proxy ya maneja HTTPS
        if (app.Environment.IsDevelopment())
            app.UseHttpsRedirection();

        app.UseCors("DefaultCors");
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.MapHealthChecks("/health");
        app.Run();
    }


}
}