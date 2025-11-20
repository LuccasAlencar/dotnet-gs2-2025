using Asp.Versioning;
using dotnet_gs2_2025.Configuration;
using dotnet_gs2_2025.Data;
using dotnet_gs2_2025.Repositories;
using dotnet_gs2_2025.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Data;
using Dapper;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using DotNetEnv;
using Oracle.EntityFrameworkCore;

// Carregar variáveis de ambiente do arquivo .env
Env.Load();

// Configuração do Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .Build())
    .Enrich.FromLogContext()
    .CreateLogger();

try
{
    Log.Information("Iniciando aplicação...");

    var builder = WebApplication.CreateBuilder(args);

    // Configurar Serilog
    builder.Host.UseSerilog();

    // Configuração do Oracle
    var oracleDataSource = Environment.GetEnvironmentVariable("ORACLE_DATA_SOURCE");
    var oracleUserId = Environment.GetEnvironmentVariable("ORACLE_USER_ID");
    var oraclePassword = Environment.GetEnvironmentVariable("ORACLE_PASSWORD");
    
    if (string.IsNullOrEmpty(oracleDataSource) || string.IsNullOrEmpty(oracleUserId) || string.IsNullOrEmpty(oraclePassword))
    {
        throw new InvalidOperationException("Variáveis de ambiente do Oracle não configuradas corretamente");
    }
    
    var connectionString = $"User Id={oracleUserId};Password={oraclePassword};Data Source={oracleDataSource};";
    
    // Configurar o DbContext para usar Oracle
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseOracle(connectionString));
        
    Log.Information("Banco de dados Oracle configurado com sucesso");

    // Dependency Injection
    builder.Services.AddScoped<IUserRepository, UserRepository>();
    builder.Services.AddScoped<IUserService, UserService>();
    
    builder.Services.Configure<HuggingFaceOptions>(builder.Configuration.GetSection(HuggingFaceOptions.SectionName));

    builder.Services.AddSingleton<IPdfTextExtractor, PdfTextExtractor>();
    builder.Services.AddScoped<IResumeService, ResumeService>();

    // Configurar HttpClient e serviços externos
    builder.Services.AddHttpClient<IAdzunaService, AdzunaService>();
    builder.Services.AddHttpClient<IHuggingFaceService, HuggingFaceService>(client =>
    {
        client.BaseAddress = new Uri("https://router.huggingface.co/hf-inference/");
        client.Timeout = TimeSpan.FromSeconds(60);
    }).SetHandlerLifetime(TimeSpan.FromMinutes(5));
    
    // Registrar o serviço de sugestão de cargos
    builder.Services.AddScoped<IJobSuggestionService, JobSuggestionService>();

    // Configuração de CORS
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend", policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
    });

    // Configuração de API Versioning
    builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
        options.ApiVersionReader = ApiVersionReader.Combine(
            new UrlSegmentApiVersionReader(),
            new HeaderApiVersionReader("X-API-Version"),
            new QueryStringApiVersionReader("api-version"));
    }).AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

    // Health Checks
    builder.Services.AddHealthChecks()
        .AddOracle(
            connectionString!,
            name: "oracle-database",
            timeout: TimeSpan.FromSeconds(3),
            tags: new[] { "db", "oracle", "database" });

    // OpenTelemetry (Tracing)
    builder.Services.AddOpenTelemetry()
        .WithTracing(tracerProviderBuilder =>
        {
            tracerProviderBuilder
                .SetResourceBuilder(ResourceBuilder.CreateDefault()
                    .AddService("UserAPI"))
                .AddAspNetCoreInstrumentation()
                .AddConsoleExporter();
        });

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();

    // Configuração do Swagger
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Users API - V1",
            Version = "v1",
            Description = "API RESTful buscadora de vagas com Adzuna, desenvolvida em .NET 8 com Oracle Database. ",
            Contact = new OpenApiContact
            {
                Name = "Suporte API",
                Email = "suporte@exemplo.com"
            }
        });

        c.SwaggerDoc("v2", new OpenApiInfo
        {
            Title = "Users API - V2",
            Version = "v2",
            Description = "API RESTful buscadora de vagas com Adzuna, desenvolvida em .NET 8 com Oracle Database. - Versão 2 (Melhorada)",
            Contact = new OpenApiContact
            {
                Name = "Suporte API",
                Email = "suporte@exemplo.com"
            }
        });

        // Incluir comentários XML se existir
        var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            c.IncludeXmlComments(xmlPath);
        }
    });

    var app = builder.Build();

    // Aplicar migrations automaticamente na primeira execução
    try
    {
        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            // Verificar se a tabela __EFMigrationsHistory existe (indica que já houve migrações)
            var migrationTableExists = dbContext.Database.GetDbConnection()
                .QueryFirstOrDefault<int?>("SELECT 1 FROM ALL_TABLES WHERE TABLE_NAME = '__EFMigrationsHistory' AND OWNER = USER") != null;

            if (!migrationTableExists)
            {
                // Se a tabela de migrações não existe, criamos ela manualmente
                Log.Information("Criando tabela de histórico de migrações...");
                dbContext.Database.ExecuteSqlRaw(@"
                    CREATE TABLE ""__EFMigrationsHistory"" (
                        ""MigrationId"" NVARCHAR2(150) NOT NULL,
                        ""ProductVersion"" NVARCHAR2(32) NOT NULL,
                        CONSTRAINT ""PK___EFMigrationsHistory"" PRIMARY KEY (""MigrationId"")
                    )");
                
                // Marca a migração inicial como aplicada
                var initialMigration = dbContext.Database.GetMigrations().First();
                dbContext.Database.ExecuteSqlRaw($@"
                    INSERT INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"")
                    VALUES ('{initialMigration}', '8.0.10')");
                
                Log.Information("✅ Tabela de histórico de migrações criada com sucesso");
            }
            
            // Verifica se há migrações pendentes
            var pendingMigrations = dbContext.Database.GetPendingMigrations().ToList();
            if (pendingMigrations.Any())
            {
                Log.Information($"Aplicando {pendingMigrations.Count} migração(ões) pendente(s)...");
                try 
                {
                    dbContext.Database.Migrate();
                    Log.Information("✅ Migrações aplicadas com sucesso");
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "⚠️  Não foi possível aplicar todas as migrações. Verifique se o banco de dados já está atualizado.");
                }
            }
            else
            {
                Log.Information("✅ Nenhuma migração pendente para ser aplicada");
            }
        }
    }
    catch (Exception ex)
    {
        Log.Error(ex, "❌ Erro ao aplicar migrations");
    }

    // Middleware para logging de requisições
    app.UseSerilogRequestLogging();

    // Configure the HTTP request pipeline
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Users API V1");
        c.SwaggerEndpoint("/swagger/v2/swagger.json", "Users API V2");
        c.RoutePrefix = string.Empty; // Swagger na raiz
    });

    // Health Check Endpoints
    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        Predicate = _ => true,
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });

    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready")
    });

    app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate = _ => false
    });

    app.UseHttpsRedirection();

    // Habilitar CORS
    app.UseCors("AllowFrontend");

    app.UseAuthorization();
    app.MapControllers();

    Log.Information("Aplicação iniciada com sucesso");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Aplicação falhou ao iniciar");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
