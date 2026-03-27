using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using portfolio_api.Data;

namespace portfolio_api.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAppAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var jwtKey = configuration["Jwt:Key"];
            if (string.IsNullOrWhiteSpace(jwtKey))
                jwtKey = Environment.GetEnvironmentVariable("JWT_SECRET");
            if (string.IsNullOrWhiteSpace(jwtKey))
                jwtKey = Environment.GetEnvironmentVariable("API_KEY");

            if (string.IsNullOrWhiteSpace(jwtKey))
                throw new InvalidOperationException("JWT key no configurada");

            var jwtIssuer = configuration["Jwt:Issuer"] ?? "portfolio_api";
            var jwtAudience = configuration["Jwt:Audience"] ?? "portfolio_api";

            var googleClientId = configuration["Authentication:Google:ClientId"];
            if (string.IsNullOrWhiteSpace(googleClientId))
                googleClientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID");

            var googleClientSecret = configuration["Authentication:Google:ClientSecret"];
            if (string.IsNullOrWhiteSpace(googleClientSecret))
                googleClientSecret = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET");

            var githubClientId = configuration["Authentication:GitHub:ClientId"];
            if (string.IsNullOrWhiteSpace(githubClientId))
                githubClientId = Environment.GetEnvironmentVariable("GITHUB_CLIENT_ID");

            var githubClientSecret = configuration["Authentication:GitHub:ClientSecret"];
            if (string.IsNullOrWhiteSpace(githubClientSecret))
                githubClientSecret = Environment.GetEnvironmentVariable("GITHUB_CLIENT_SECRET");
            
            var authBuilder = services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtIssuer,
                        ValidAudience = jwtAudience,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                        ClockSkew = TimeSpan.Zero
                    };
                })
                .AddCookie("External");

            if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
            {
                authBuilder.AddGoogle(options =>
                {
                    options.SignInScheme = "External";
                    options.ClientId = googleClientId;
                    options.ClientSecret = googleClientSecret;
                });
            }

            if (!string.IsNullOrWhiteSpace(githubClientId) && !string.IsNullOrWhiteSpace(githubClientSecret))
            {
                authBuilder.AddOAuth("GitHub", options =>
                {
                    options.SignInScheme = "External";
                    options.ClientId = githubClientId;
                    options.ClientSecret = githubClientSecret;
                    options.CallbackPath = "/signin-github";
                    options.AuthorizationEndpoint = "https://github.com/login/oauth/authorize";
                    options.TokenEndpoint = "https://github.com/login/oauth/access_token";
                    options.UserInformationEndpoint = "https://api.github.com/user";
                    options.Scope.Add("user:email");
                    options.SaveTokens = true;
                });
            }

            services.AddAuthorization();

            return services;
        }

        public static IServiceCollection AddAppCors(this IServiceCollection services)
        {
            var corsOrigins = Environment.GetEnvironmentVariable("CORS_ORIGINS")
                ?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                ?? Array.Empty<string>();

            services.AddCors(options =>
            {
                options.AddPolicy("DefaultCors", policy =>
                {
                    if (corsOrigins.Length > 0)
                    {
                        policy.WithOrigins(corsOrigins)
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                            .AllowCredentials();
                    }
                    else
                    {
                        // Sin orígenes configurados: permite cualquiera (solo desarrollo)
                        policy.AllowAnyOrigin()
                            .AllowAnyHeader()
                            .AllowAnyMethod();
                    }
                });
            });

            return services;
        }

        public static IServiceCollection AddAppDbContext(this IServiceCollection services)
        {
            var connectionString = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION");

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(connectionString));

            return services;
        }
    }
}

