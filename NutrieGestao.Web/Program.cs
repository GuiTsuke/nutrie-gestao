using NutrieGestao.Web.Components;
using NutrieGestao.Web.Infrastructure;
using NutrieGestao.Services;
using System.Diagnostics;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using NutrieAuthenticationService = NutrieGestao.Services.AuthenticationService;

var bootstrapLogsPath = Path.Combine(AppContext.BaseDirectory, "Logs");
var bootstrapLogger = new FileLogger(bootstrapLogsPath, new FileLoggerOptions { MinimumLevel = "Information" });

AppDomain.CurrentDomain.UnhandledException += (_, eventArgs) =>
{
    bootstrapLogger.Critical(
        "Exceção não tratada no processo.",
        eventArgs.ExceptionObject as Exception,
        "UnhandledProcessException",
        new Dictionary<string, object?> { ["IsTerminating"] = eventArgs.IsTerminating });
};

TaskScheduler.UnobservedTaskException += (_, eventArgs) =>
{
    bootstrapLogger.Error("Exceção não observada em tarefa.", eventArgs.Exception, "UnobservedTaskException");
    eventArgs.SetObserved();
};

try
{
var builder = WebApplication.CreateBuilder(args);

var storage = new StorageOptions
{
    RootPath = builder.Configuration["NutrieGestao:Paths:RootPath"] ?? "%LocalAppData%\\NutrieGestao",
    DataDirectory = builder.Configuration["NutrieGestao:Paths:Data"] ?? "Data",
    LogsDirectory = builder.Configuration["NutrieGestao:Paths:Logs"] ?? "Logs",
    BackupsDirectory = builder.Configuration["NutrieGestao:Paths:Backups"] ?? "Backups"
};
var fileLoggerOptions = new FileLoggerOptions
{
    MinimumLevel = builder.Configuration["NutrieGestao:Diagnostics:File:MinimumLevel"] ?? "Information",
    RetentionDays = GetPositiveInt(builder.Configuration["NutrieGestao:Diagnostics:File:RetentionDays"], 30),
    MaximumFileSizeMb = GetPositiveInt(builder.Configuration["NutrieGestao:Diagnostics:File:MaximumFileSizeMb"], 10),
    LogHttpRequests = GetBoolean(builder.Configuration["NutrieGestao:Diagnostics:File:LogHttpRequests"], true)
};

storage.EnsureDirectories();
var security = new SecurityOptions
{
    BootstrapCode = builder.Configuration["NutrieGestao:Security:BootstrapCode"] ?? string.Empty
};
var fileLogger = new FileLogger(storage.LogsPath, fileLoggerOptions, bootstrapLogsPath);
fileLogger.Information(
    "Aplicação Web iniciando.",
    "ApplicationStarting",
    new Dictionary<string, object?>
    {
        ["Environment"] = builder.Environment.EnvironmentName,
        ["ContentRoot"] = builder.Environment.ContentRootPath,
        ["DataPath"] = storage.DataPath,
        ["LogsPath"] = storage.LogsPath,
        ["BackupsPath"] = storage.BackupsPath,
        ["DataProtectionKeysPath"] = Path.Combine(storage.DataPath, "Keys")
    });

builder.Logging.AddProvider(new FileLoggerProvider(fileLogger));

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddSingleton(storage);
builder.Services.AddSingleton(fileLogger);
builder.Services.AddSingleton(fileLoggerOptions);
builder.Services.AddSingleton(security);
builder.Services.AddSingleton<DataService>();
builder.Services.AddSingleton<ManagementService>();
builder.Services.AddSingleton<NutrieAuthenticationService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthorization();
// Cookies do mesmo host são compartilhados entre portas. Cada pasta de dados
// identifica uma instalação, evitando colisões entre o Debug e o site no IIS.
var cookieScopePath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(storage.DataPath));
if (OperatingSystem.IsWindows()) cookieScopePath = cookieScopePath.ToUpperInvariant();
var cookieScope = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(cookieScopePath)))[..12];
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = $"__Host-NutrieGestao.Auth.{cookieScope}";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.LoginPath = "/entrar";
        options.AccessDeniedPath = "/entrar";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Events.OnValidatePrincipal = async context =>
        {
            var userId = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var securityStamp = context.Principal?.FindFirst("nutrie-security-stamp")?.Value;
            var sessionIsValid = Guid.TryParse(userId, out var parsedUserId) &&
                context.HttpContext.RequestServices.GetRequiredService<NutrieAuthenticationService>()
                    .SessaoEhValida(parsedUserId, securityStamp);

            if (!sessionIsValid)
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        };
    });

builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name = $"NutrieGestao.Antiforgery.{cookieScope}";
});

var dataProtection = builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(storage.DataPath, "Keys")))
    .SetApplicationName("NutrieGestao.Web");

if (OperatingSystem.IsWindows())
{
    dataProtection.ProtectKeysWithDpapi();
}
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = (context, _) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter =
                ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
        var page = context.HttpContext.Request.Path == "/auth/primeiro-acesso"
            ? "/primeiro-acesso"
            : "/entrar";
        context.HttpContext.Response.Redirect($"{page}?erro=limite");
        return ValueTask.CompletedTask;
    };
    options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(15),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0,
            AutoReplenishment = true
        }));
});

var app = builder.Build();
var logger = app.Services.GetRequiredService<FileLogger>();
app.Lifetime.ApplicationStarted.Register(() => logger.Information("Aplicação Web iniciada e pronta para receber requisições.", "ApplicationStarted"));
app.Lifetime.ApplicationStopping.Register(() => logger.Warning("Aplicação Web está sendo encerrada.", "ApplicationStopping"));
app.Lifetime.ApplicationStopped.Register(() => logger.Information("Aplicação Web encerrada.", "ApplicationStopped"));

// Configure the HTTP request pipeline.
app.Use(async (context, next) =>
{
    var startedAt = Stopwatch.GetTimestamp();
    Exception? unhandledException = null;
    if (!context.Response.HasStarted) context.Response.Headers["X-Request-Id"] = context.TraceIdentifier;

    try
    {
        await next();
    }
    catch (Exception exception)
    {
        unhandledException = exception;
        logger.Error(
            "Exceção não tratada durante a requisição.",
            exception,
            "UnhandledHttpException",
            RequestProperties(context, Stopwatch.GetElapsedTime(startedAt)));
        throw;
    }
    finally
    {
        var exceptionHandledByPipeline = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        if (unhandledException is null && exceptionHandledByPipeline is not null)
        {
            logger.Error(
                "Exceção tratada pelo pipeline de erros.",
                exceptionHandledByPipeline,
                "HandledHttpException",
                RequestProperties(context, Stopwatch.GetElapsedTime(startedAt)));
        }

        if (fileLoggerOptions.LogHttpRequests && ShouldLogHttpRequest(context.Request.Path))
        {
            var level = context.Response.StatusCode >= StatusCodes.Status500InternalServerError
                ? FileLogLevel.Error
                : context.Response.StatusCode >= StatusCodes.Status400BadRequest
                    ? FileLogLevel.Warning
                    : FileLogLevel.Information;

            logger.Write(
                level,
                "Requisição HTTP concluída.",
                "HttpRequestCompleted",
                null,
                RequestProperties(context, Stopwatch.GetElapsedTime(startedAt)));
        }
    }
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapPost("/auth/login", async (HttpContext context, IAntiforgery antiforgery, NutrieAuthenticationService authentication) =>
{
    if (!await RequestIsValidAsync(context, antiforgery, logger))
    {
        return Results.Redirect("/entrar?erro=solicitacao");
    }

    var form = await context.Request.ReadFormAsync(context.RequestAborted);
    var returnUrl = GetLocalReturnUrl(form["retorno"].ToString());
    var usuario = authentication.Autenticar(form["usuario"].ToString(), form["senha"].ToString());
    if (usuario is null)
    {
        return Results.Redirect($"/entrar?erro=credenciais&retorno={Uri.EscapeDataString(returnUrl)}");
    }

    await SignInAsync(context, usuario);
    return Results.LocalRedirect(returnUrl);
}).AllowAnonymous().RequireRateLimiting("login");

app.MapPost("/auth/primeiro-acesso", async (HttpContext context, IAntiforgery antiforgery, NutrieAuthenticationService authentication) =>
{
    if (!await RequestIsValidAsync(context, antiforgery, logger))
    {
        return Results.Redirect("/primeiro-acesso?erro=solicitacao");
    }

    var form = await context.Request.ReadFormAsync(context.RequestAborted);
    var result = authentication.CriarAdministradorInicial(
        form["nome"].ToString(),
        form["usuario"].ToString(),
        form["email"].ToString(),
        form["senha"].ToString(),
        form["confirmacaoSenha"].ToString(),
        form["codigoDeAtivacao"].ToString());

    if (!result.Sucesso || result.Usuario is null)
    {
        return Results.Redirect("/primeiro-acesso?erro=validacao");
    }

    await SignInAsync(context, result.Usuario);
    return Results.LocalRedirect("/");
}).AllowAnonymous().RequireRateLimiting("login");

app.MapGet("/sair", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/entrar?mensagem=saiu");
}).RequireAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
}
catch (Exception exception)
{
    bootstrapLogger.Critical(
        "A aplicação Web foi interrompida durante a inicialização ou execução.",
        exception,
        "ApplicationFatalError");
    throw;
}

static int GetPositiveInt(string? configuredValue, int defaultValue)
{
    return int.TryParse(configuredValue, out var value) && value > 0 ? value : defaultValue;
}

static bool GetBoolean(string? configuredValue, bool defaultValue)
{
    return bool.TryParse(configuredValue, out var value) ? value : defaultValue;
}

static IReadOnlyDictionary<string, object?> RequestProperties(HttpContext context, TimeSpan elapsed)
{
    return new Dictionary<string, object?>
    {
        ["RequestId"] = context.TraceIdentifier,
        ["Method"] = context.Request.Method,
        ["Path"] = context.Request.Path.Value,
        ["StatusCode"] = context.Response.StatusCode,
        ["ElapsedMilliseconds"] = Math.Round(elapsed.TotalMilliseconds, 2),
        ["RemoteIp"] = context.Connection.RemoteIpAddress?.ToString(),
        ["Authenticated"] = context.User.Identity?.IsAuthenticated ?? false
    };
}

static bool ShouldLogHttpRequest(PathString path)
{
    return !path.StartsWithSegments("/_framework") &&
           !path.StartsWithSegments("/css") &&
           !path.StartsWithSegments("/icons") &&
           !path.StartsWithSegments("/favicon.ico");
}

static async Task<bool> RequestIsValidAsync(HttpContext context, IAntiforgery antiforgery, FileLogger logger)
{
    try
    {
        await antiforgery.ValidateRequestAsync(context);
        return true;
    }
    catch (AntiforgeryValidationException exception)
    {
        var antiforgeryOptions = context.RequestServices.GetRequiredService<IOptions<AntiforgeryOptions>>().Value;
        var hasAntiforgeryCookie = !string.IsNullOrWhiteSpace(antiforgeryOptions.Cookie.Name) &&
            context.Request.Cookies.ContainsKey(antiforgeryOptions.Cookie.Name);
        var hasRequestToken = false;
        string? requestToken = null;
        var requestTokenCount = 0;

        if (context.Request.HasFormContentType)
        {
            try
            {
                var form = await context.Request.ReadFormAsync(context.RequestAborted);
                hasRequestToken = form.ContainsKey(antiforgeryOptions.FormFieldName);
                var tokens = form[antiforgeryOptions.FormFieldName];
                requestTokenCount = tokens.Count;
                requestToken = tokens.ToString();
            }
            catch
            {
                // O detalhe original da falha já será registrado junto com a requisição.
            }
        }

        logger.Error(
            "Solicitação recusada pela validação antiforgery.",
            exception,
            "AntiforgeryValidationFailed",
            new Dictionary<string, object?>
            {
                ["RequestId"] = context.TraceIdentifier,
                ["Path"] = context.Request.Path.Value,
                ["Scheme"] = context.Request.Scheme,
                ["Host"] = context.Request.Host.Value,
                ["PathBase"] = context.Request.PathBase.Value,
                ["HasAntiforgeryCookie"] = hasAntiforgeryCookie,
                ["HasRequestToken"] = hasRequestToken,
                ["RequestTokenCount"] = requestTokenCount,
                ["RequestTokenMetadata"] = AntiforgeryDiagnostics.Describe(requestToken),
                ["CookieTokenMetadata"] = AntiforgeryDiagnostics.Describe(hasAntiforgeryCookie
                    ? context.Request.Cookies[antiforgeryOptions.Cookie.Name!]
                    : null),
                ["Referer"] = context.Request.Headers.Referer.ToString(),
                ["Origin"] = context.Request.Headers.Origin.ToString()
            });
        return false;
    }
}

static string GetLocalReturnUrl(string? returnUrl)
{
    return !string.IsNullOrWhiteSpace(returnUrl) && returnUrl.StartsWith('/') && !returnUrl.StartsWith("//")
        ? returnUrl
        : "/";
}

static async Task SignInAsync(HttpContext context, NutrieGestao.Models.Usuario usuario)
{
    var claims = new[]
    {
        new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
        new Claim(ClaimTypes.Name, usuario.NomeUsuario),
        new Claim(ClaimTypes.GivenName, usuario.Nome),
        new Claim(ClaimTypes.Email, usuario.Email),
        new Claim(ClaimTypes.Role, usuario.Perfil.ToString()),
        new Claim("nutrie-security-stamp", usuario.CarimboSeguranca)
    };

    var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
    await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties
    {
        IsPersistent = false,
        AllowRefresh = true,
        ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
    });
}
