using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using WarehouseApi.Data;
using WarehouseApi.Hubs;
using WarehouseApi.Models;
using WarehouseApi.Services;
using Microsoft.AspNetCore.Identity;
using System.Data;
using System.Security.Cryptography;
using System.Net.Mail;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
QuestPDF.Settings.License = LicenseType.Community;

// Add services to the container.
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// SignalR
builder.Services.AddSignalR();

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "DefaultConnection is missing."
    );

builder.Services.AddDbContext<WarehouseDbContext>(options =>
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString)
    )
);

builder.Services.AddCors(options =>
{
    options.AddPolicy("GlobalSyntaxPolicy", policy =>
    {
        policy
            .WithOrigins(
                "https://gsyntaxhosting.com",
                "https://gsyntaxhserver.com",
                "http://gsyntaxserver.com",
                "http://gsyntaxhosting.com",
                "http://localhost:4200",
                "https://freshv-gnf6c8cfhxbdc9gt.westus2-01.azurewebsites.net"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
    });
});

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme =
            JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        var jwtKey = builder.Configuration["Jwt:Key"]
            ?? throw new InvalidOperationException(
                "JWT key is missing."
            );

        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,

                ValidIssuer =
                    builder.Configuration["Jwt:Issuer"],

                ValidAudience =
                    builder.Configuration["Jwt:Audience"],

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtKey)
                    ),

                ClockSkew = TimeSpan.Zero
            };
    });

builder.Services.AddAuthorization();
builder.Services.AddScoped<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<InvoicePdfService>();

var app = builder.Build();

if (args.Contains("--apply-package-overview-indexes", StringComparer.OrdinalIgnoreCase))
{
    await using var scope = app.Services.CreateAsyncScope();
    var context = scope.ServiceProvider.GetRequiredService<WarehouseDbContext>();
    var connection = context.Database.GetDbConnection();
    await connection.OpenAsync();
    await using var checkIndex = connection.CreateCommand();
    checkIndex.CommandText = "SELECT COUNT(*) FROM information_schema.statistics WHERE table_schema = DATABASE() AND table_name = 'UserPackages' AND index_name = 'IX_UserPackages_status';";
    if (Convert.ToInt32(await checkIndex.ExecuteScalarAsync()) == 0)
    {
        var scriptPath = Path.Combine(app.Environment.ContentRootPath, "Data", "Sql", "021_package_overview_indexes.sql");
        await context.Database.ExecuteSqlRawAsync(await File.ReadAllTextAsync(scriptPath));
        Console.WriteLine("Package overview query indexes were added.");
    }
    else Console.WriteLine("Package overview query indexes already exist; no changes were made.");
    return;
}

if (args.Contains("--apply-global-supplier-schema", StringComparer.OrdinalIgnoreCase))
{
    await using var scope = app.Services.CreateAsyncScope();
    var context = scope.ServiceProvider.GetRequiredService<WarehouseDbContext>();
    var connection = context.Database.GetDbConnection();
    await connection.OpenAsync();
    await using var checkColumn = connection.CreateCommand();
    checkColumn.CommandText = "SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = 'global_settings' AND column_name = 'supplier';";
    if (Convert.ToInt32(await checkColumn.ExecuteScalarAsync()) == 0)
    {
        var scriptPath = Path.Combine(app.Environment.ContentRootPath, "Data", "Sql", "020_global_supplier.sql");
        await context.Database.ExecuteSqlRawAsync(await File.ReadAllTextAsync(scriptPath));
        Console.WriteLine("The global supplier setting was added.");
    }
    else Console.WriteLine("The global supplier setting already exists; no changes were made.");
    return;
}

if (args.Contains("--apply-bearer-role-schema", StringComparer.OrdinalIgnoreCase))
{
    await using var scope = app.Services.CreateAsyncScope();
    var context = scope.ServiceProvider.GetRequiredService<WarehouseDbContext>();
    var connection = context.Database.GetDbConnection();
    await connection.OpenAsync();
    await using var checkConstraint = connection.CreateCommand();
    checkConstraint.CommandText = "SELECT CHECK_CLAUSE FROM information_schema.check_constraints WHERE constraint_schema = DATABASE() AND constraint_name = 'CK_users_role' LIMIT 1;";
    var clause = Convert.ToString(await checkConstraint.ExecuteScalarAsync()) ?? string.Empty;
    if (!clause.Contains("Bearer", StringComparison.OrdinalIgnoreCase))
    {
        var scriptPath = Path.Combine(app.Environment.ContentRootPath, "Data", "Sql", "019_bearer_user_role.sql");
        await context.Database.ExecuteSqlRawAsync(await File.ReadAllTextAsync(scriptPath));
        Console.WriteLine("Bearer was added to the permitted user roles.");
    }
    else Console.WriteLine("Bearer is already a permitted database role; no changes were made.");
    return;
}

if (args.Contains("--apply-collection-intake-schema", StringComparer.OrdinalIgnoreCase))
{
    await using var scope = app.Services.CreateAsyncScope();
    var context = scope.ServiceProvider.GetRequiredService<WarehouseDbContext>();
    var connection = context.Database.GetDbConnection();
    await connection.OpenAsync();
    await using var checkColumn = connection.CreateCommand();
    checkColumn.CommandText = "SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = 'supplier_collections' AND column_name = 'supplier_name';";
    if (Convert.ToInt32(await checkColumn.ExecuteScalarAsync()) == 0)
    {
        var scriptPath = Path.Combine(app.Environment.ContentRootPath, "Data", "Sql", "018_collection_intake_workflow.sql");
        await context.Database.ExecuteSqlRawAsync(await File.ReadAllTextAsync(scriptPath));
        Console.WriteLine("Collection intake workflow fields were added.");
    }
    else Console.WriteLine("Collection intake workflow already exists; no changes were made.");
    return;
}

if (args.Contains("--apply-supplier-collections-schema", StringComparer.OrdinalIgnoreCase))
{
    await using var scope = app.Services.CreateAsyncScope();
    var context = scope.ServiceProvider.GetRequiredService<WarehouseDbContext>();
    var connection = context.Database.GetDbConnection();
    await connection.OpenAsync();
    await using var checkTable = connection.CreateCommand();
    checkTable.CommandText = "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = 'supplier_collections';";
    if (Convert.ToInt32(await checkTable.ExecuteScalarAsync()) == 0)
    {
        var scriptPath = Path.Combine(app.Environment.ContentRootPath, "Data", "Sql", "017_supplier_collections.sql");
        await context.Database.ExecuteSqlRawAsync(await File.ReadAllTextAsync(scriptPath));
        Console.WriteLine("Supplier collection grouping tables were created.");
    }
    else Console.WriteLine("Supplier collection grouping already exists; no changes were made.");
    return;
}

if (args.Contains("--apply-supplier-settlement-schema", StringComparer.OrdinalIgnoreCase))
{
    await using var scope = app.Services.CreateAsyncScope();
    var context = scope.ServiceProvider.GetRequiredService<WarehouseDbContext>();
    var connection = context.Database.GetDbConnection();
    await connection.OpenAsync();
    await using var checkColumn = connection.CreateCommand();
    checkColumn.CommandText = """
        SELECT COUNT(*) FROM information_schema.columns
        WHERE table_schema = DATABASE() AND table_name = 'UserPackages' AND column_name = 'supplier_amount';
        """;
    if (Convert.ToInt32(await checkColumn.ExecuteScalarAsync()) == 0)
    {
        var scriptPath = Path.Combine(app.Environment.ContentRootPath, "Data", "Sql", "016_supplier_settlements.sql");
        await context.Database.ExecuteSqlRawAsync(await File.ReadAllTextAsync(scriptPath));
        Console.WriteLine("Supplier settlement fields were added to packages.");
    }
    else Console.WriteLine("Supplier settlement fields already exist; no changes were made.");
    return;
}

if (args.Contains("--simplify-global-settings-schema", StringComparer.OrdinalIgnoreCase))
{
    await using var scope = app.Services.CreateAsyncScope();
    var context = scope.ServiceProvider.GetRequiredService<WarehouseDbContext>();
    var connection = context.Database.GetDbConnection();
    await connection.OpenAsync();
    await using var checkColumn = connection.CreateCommand();
    checkColumn.CommandText = """
        SELECT COUNT(*) FROM information_schema.columns
        WHERE table_schema = DATABASE() AND table_name = 'global_settings' AND column_name = 'app_name';
        """;
    if (Convert.ToInt32(await checkColumn.ExecuteScalarAsync()) == 0)
    {
        var scriptPath = Path.Combine(app.Environment.ContentRootPath, "Data", "Sql", "015_simplify_global_settings.sql");
        await context.Database.ExecuteSqlRawAsync(await File.ReadAllTextAsync(scriptPath));
        Console.WriteLine("Global settings were simplified to app_name and logo_url.");
    }
    else Console.WriteLine("Simplified global settings already exist; no changes were made.");
    return;
}

if (args.Contains("--apply-global-settings-schema", StringComparer.OrdinalIgnoreCase))
{
    await using var scope = app.Services.CreateAsyncScope();
    var context = scope.ServiceProvider.GetRequiredService<WarehouseDbContext>();
    var connection = context.Database.GetDbConnection();
    await connection.OpenAsync();
    await using var checkTable = connection.CreateCommand();
    checkTable.CommandText = """
        SELECT COUNT(*) FROM information_schema.tables
        WHERE table_schema = DATABASE() AND table_name = 'global_settings';
        """;
    if (Convert.ToInt32(await checkTable.ExecuteScalarAsync()) == 0)
    {
        var scriptPath = Path.Combine(app.Environment.ContentRootPath, "Data", "Sql", "014_global_settings.sql");
        await context.Database.ExecuteSqlRawAsync(await File.ReadAllTextAsync(scriptPath));
        Console.WriteLine("Global settings table and application logo setting were created.");
    }
    else Console.WriteLine("Global settings table already exists; no changes were made.");
    return;
}

if (args.Contains("--apply-batch-paid-date-schema", StringComparer.OrdinalIgnoreCase))
{
    await using var scope = app.Services.CreateAsyncScope();
    var context = scope.ServiceProvider.GetRequiredService<WarehouseDbContext>();
    var connection = context.Database.GetDbConnection();
    await connection.OpenAsync();
    await using var checkColumn = connection.CreateCommand();
    checkColumn.CommandText = """
        SELECT COUNT(*) FROM information_schema.columns
        WHERE table_schema = DATABASE() AND table_name = 'package_batches' AND column_name = 'paid_date';
        """;
    if (Convert.ToInt32(await checkColumn.ExecuteScalarAsync()) == 0)
    {
        var scriptPath = Path.Combine(app.Environment.ContentRootPath, "Data", "Sql", "013_batch_paid_date.sql");
        await context.Database.ExecuteSqlRawAsync(await File.ReadAllTextAsync(scriptPath));
        Console.WriteLine("Batch paid-date tracking was added.");
    }
    else Console.WriteLine("Batch paid-date tracking already exists; no changes were made.");
    return;
}

if (args.Contains("--apply-batch-delivery-fee-schema", StringComparer.OrdinalIgnoreCase))
{
    await using var scope = app.Services.CreateAsyncScope();
    var context = scope.ServiceProvider.GetRequiredService<WarehouseDbContext>();
    var connection = context.Database.GetDbConnection();
    await connection.OpenAsync();
    await using var checkColumn = connection.CreateCommand();
    checkColumn.CommandText = """
        SELECT COUNT(*) FROM information_schema.columns
        WHERE table_schema = DATABASE()
          AND table_name = 'package_batches'
          AND column_name = 'delivery_fee';
        """;
    if (Convert.ToInt32(await checkColumn.ExecuteScalarAsync()) == 0)
    {
        var scriptPath = Path.Combine(app.Environment.ContentRootPath, "Data", "Sql", "012_batch_delivery_fees.sql");
        var script = await File.ReadAllTextAsync(scriptPath);
        await context.Database.ExecuteSqlRawAsync(script);
        Console.WriteLine("Client defaults and batch delivery fee fields were added.");
    }
    else
    {
        Console.WriteLine("Batch delivery fee fields already exist; no changes were made.");
    }
    return;
}

if (args.Contains("--apply-package-batching-schema", StringComparer.OrdinalIgnoreCase))
{
    await using var scope = app.Services.CreateAsyncScope();
    var context = scope.ServiceProvider.GetRequiredService<WarehouseDbContext>();
    var connection = context.Database.GetDbConnection();
    await connection.OpenAsync();
    await using var checkTable = connection.CreateCommand();
    checkTable.CommandText = """
        SELECT COUNT(*) FROM information_schema.tables
        WHERE table_schema = DATABASE() AND table_name = 'package_batches';
        """;
    if (Convert.ToInt32(await checkTable.ExecuteScalarAsync()) == 0)
    {
        var scriptPath = Path.Combine(
            app.Environment.ContentRootPath, "Data", "Sql", "011_package_batching.sql");
        var script = await File.ReadAllTextAsync(scriptPath);
        await context.Database.ExecuteSqlRawAsync(script);
        Console.WriteLine("Client batch handling and package batch tables were created.");
    }
    else
    {
        Console.WriteLine("Package batching already exists; no changes were made.");
    }
    return;
}

if (args.Contains("--recalculate-invoice-amounts", StringComparer.OrdinalIgnoreCase))
{
    await using var scope = app.Services.CreateAsyncScope();
    var context = scope.ServiceProvider.GetRequiredService<WarehouseDbContext>();
    var scriptPath = Path.Combine(
        app.Environment.ContentRootPath, "Data", "Sql", "010_recalculate_invoice_amount_due.sql");
    var script = await File.ReadAllTextAsync(scriptPath);
    var affected = await context.Database.ExecuteSqlRawAsync(script);
    Console.WriteLine($"Invoice amounts and unpaid balances were recalculated. Rows affected: {affected}.");
    return;
}

if (args.Contains("--apply-additional-markup-schema", StringComparer.OrdinalIgnoreCase))
{
    await using var scope = app.Services.CreateAsyncScope();
    var context = scope.ServiceProvider.GetRequiredService<WarehouseDbContext>();
    var connection = context.Database.GetDbConnection();
    await connection.OpenAsync();
    await using var checkColumn = connection.CreateCommand();
    checkColumn.CommandText = """
        SELECT COUNT(*) FROM information_schema.columns
        WHERE table_schema = DATABASE()
          AND table_name = 'UserPackages'
          AND column_name = 'additional_markup';
        """;
    if (Convert.ToInt32(await checkColumn.ExecuteScalarAsync()) == 0)
    {
        var scriptPath = Path.Combine(
            app.Environment.ContentRootPath, "Data", "Sql", "009_package_additional_markup.sql");
        var script = await File.ReadAllTextAsync(scriptPath);
        await context.Database.ExecuteSqlRawAsync(script);
        Console.WriteLine("Package additional markup was added and invoice costs were recalculated.");
    }
    else
    {
        Console.WriteLine("Package additional markup already exists; no changes were made.");
    }
    return;
}

if (args.Contains("--apply-invoice-cost-schema", StringComparer.OrdinalIgnoreCase))
{
    await using var scope = app.Services.CreateAsyncScope();
    var context = scope.ServiceProvider.GetRequiredService<WarehouseDbContext>();
    var connection = context.Database.GetDbConnection();
    await connection.OpenAsync();
    await using var checkColumn = connection.CreateCommand();
    checkColumn.CommandText = """
        SELECT COUNT(*) FROM information_schema.columns
        WHERE table_schema = DATABASE()
          AND table_name = 'user_package_assignments'
          AND column_name = 'invoice_cost';
        """;
    if (Convert.ToInt32(await checkColumn.ExecuteScalarAsync()) == 0)
    {
        var scriptPath = Path.Combine(
            app.Environment.ContentRootPath, "Data", "Sql", "008_assignment_invoice_cost.sql");
        var script = await File.ReadAllTextAsync(scriptPath);
        await context.Database.ExecuteSqlRawAsync(script);
        Console.WriteLine("Invoice-cost tracking was added and existing assignments were recalculated.");
    }
    else
    {
        Console.WriteLine("Invoice-cost tracking already exists; no changes were made.");
    }
    return;
}

if (args.Contains("--apply-client-rate-schema", StringComparer.OrdinalIgnoreCase))
{
    await using var scope = app.Services.CreateAsyncScope();
    var context = scope.ServiceProvider.GetRequiredService<WarehouseDbContext>();
    var connection = context.Database.GetDbConnection();
    await connection.OpenAsync();
    await using var checkColumn = connection.CreateCommand();
    checkColumn.CommandText = """
        SELECT COUNT(*) FROM information_schema.columns
        WHERE table_schema = DATABASE()
          AND table_name = 'clients'
          AND column_name = 'per_lb_cost';
        """;
    if (Convert.ToInt32(await checkColumn.ExecuteScalarAsync()) == 0)
    {
        var scriptPath = Path.Combine(
            app.Environment.ContentRootPath, "Data", "Sql", "007_client_per_lb_rates.sql");
        var script = await File.ReadAllTextAsync(scriptPath);
        await context.Database.ExecuteSqlRawAsync(script);
        Console.WriteLine("The tenant per-pound rate fields were added successfully.");
    }
    else
    {
        Console.WriteLine("The tenant per-pound rate fields already exist; no changes were made.");
    }
    return;
}

if (args.Contains("--apply-package-status-schema", StringComparer.OrdinalIgnoreCase))
{
    await using var scope = app.Services.CreateAsyncScope();
    var context = scope.ServiceProvider.GetRequiredService<WarehouseDbContext>();
    var scriptPath = Path.Combine(
        app.Environment.ContentRootPath, "Data", "Sql", "006_package_statuses.sql");
    var script = await File.ReadAllTextAsync(scriptPath);
    await context.Database.ExecuteSqlRawAsync(script);
    var statusCount = await context.PackageStatuses.CountAsync();
    Console.WriteLine($"The package status table is ready with {statusCount} statuses.");
    return;
}

if (args.Contains("--import-gopak-users", StringComparer.OrdinalIgnoreCase))
{
    var fileIndex = Array.FindIndex(args, value =>
        value.Equals("--import-gopak-users", StringComparison.OrdinalIgnoreCase));
    if (fileIndex < 0 || fileIndex + 1 >= args.Length)
        throw new InvalidOperationException("A tab-separated user file path is required.");

    var importPath = Path.GetFullPath(args[fileIndex + 1]);
    if (!File.Exists(importPath))
        throw new FileNotFoundException("The user import file was not found.", importPath);

    await using var scope = app.Services.CreateAsyncScope();
    var context = scope.ServiceProvider.GetRequiredService<WarehouseDbContext>();
    var tenant = await context.Clients.SingleOrDefaultAsync(client => client.CompanyName == "Gopak")
        ?? throw new InvalidOperationException("The Gopak tenant does not exist.");

    var lines = await File.ReadAllLinesAsync(importPath);
    var imported = 0;
    var updated = 0;
    var skipped = 0;
    var firstNameCounts = lines.Skip(1)
        .Where(line => !string.IsNullOrWhiteSpace(line))
        .Select(line => line.Split('\t'))
        .Where(fields => fields.Length >= 2)
        .Select(fields => fields[1].Trim().Split(' ', 2,
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault())
        .Where(firstName => !string.IsNullOrWhiteSpace(firstName))
        .GroupBy(firstName => firstName!, StringComparer.OrdinalIgnoreCase)
        .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);

    static string UsernamePart(string value)
    {
        var part = System.Text.RegularExpressions.Regex.Replace(
            value.Trim().ToLowerInvariant(), "[^a-z0-9]+", "-");
        return part.Trim('-');
    }

    foreach (var line in lines.Skip(1))
    {
        if (string.IsNullOrWhiteSpace(line))
            continue;

        var fields = line.Split('\t');
        if (fields.Length < 13 || !int.TryParse(fields[0].Trim(), out var sourceUserId))
        {
            skipped++;
            continue;
        }

        static string? Clean(string value)
        {
            var cleaned = value.Trim();
            return string.IsNullOrWhiteSpace(cleaned) ||
                   cleaned.Equals("NULL", StringComparison.OrdinalIgnoreCase) ||
                   cleaned == "-" ? null : cleaned;
        }

        var fullName = Clean(fields[1]);
        var email = Clean(fields[2]);
        var legacyPassword = Clean(fields[3]);
        var normalizedEmail = email is not null && MailAddress.TryCreate(email, out _)
            ? email.ToUpperInvariant()
            : null;

        var nameParts = (fullName ?? string.Empty)
            .Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var firstName = nameParts.Length > 0 ? nameParts[0] : $"user-{sourceUserId}";
        var lastName = nameParts.Length > 1 ? nameParts[1] : string.Empty;
        var username = UsernamePart(firstName);
        if (firstNameCounts.GetValueOrDefault(firstName) > 1 && !string.IsNullOrWhiteSpace(lastName))
            username = $"{username}-{UsernamePart(lastName)}";

        if (normalizedEmail is not null && await context.Users.AnyAsync(user =>
                user.NormalizedEmail == normalizedEmail && user.Username != username))
            normalizedEmail = null;

        var existing = await context.Users.SingleOrDefaultAsync(user =>
            user.Id == sourceUserId);

        if (existing is null)
        {
            existing = new AppUser
            {
                Id = sourceUserId,
                ClientId = tenant.Id,
                Username = username,
                PasswordHash = legacyPassword ?? Convert.ToHexString(RandomNumberGenerator.GetBytes(32)),
                Role = UserRoles.Customer,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            context.Users.Add(existing);
            imported++;
        }
        else
        {
            updated++;
        }

        existing.Username = username;
        existing.FullName = fullName;
        existing.FirstName = nameParts.Length > 0 ? nameParts[0] : null;
        existing.LastName = nameParts.Length > 1 ? nameParts[1] : null;
        existing.Email = email;
        existing.NormalizedEmail = normalizedEmail;
        existing.Mobile = Clean(fields[4]);
        existing.HomePhone = Clean(fields[5]);
        existing.IdType = Clean(fields[6]);
        existing.IdNumber = Clean(fields[7]);
        existing.PickupLocation = Clean(fields[8]);
        existing.Address1 = Clean(fields[9]);
        existing.Address2 = Clean(fields[10]);
        existing.City = Clean(fields[11]);
        existing.Parish = Clean(fields[12]);
        existing.UpdatedAt = DateTime.UtcNow;
    }

    await context.SaveChangesAsync();
    Console.WriteLine($"Gopak users imported: {imported}; updated: {updated}; skipped: {skipped}.");
    return;
}

if (args.Contains("--apply-user-profile-schema", StringComparer.OrdinalIgnoreCase))
{
    await using var scope = app.Services.CreateAsyncScope();
    var context = scope.ServiceProvider.GetRequiredService<WarehouseDbContext>();
    var connection = context.Database.GetDbConnection();
    await connection.OpenAsync();
    await using var checkColumn = connection.CreateCommand();
    checkColumn.CommandText = """
        SELECT COUNT(*) FROM information_schema.columns
        WHERE table_schema = DATABASE()
          AND table_name = 'users'
          AND column_name = 'full_name';
        """;
    if (Convert.ToInt32(await checkColumn.ExecuteScalarAsync()) == 0)
    {
        var scriptPath = Path.Combine(
            app.Environment.ContentRootPath, "Data", "Sql", "004_user_profile_fields.sql");
        var script = await File.ReadAllTextAsync(scriptPath);
        await context.Database.ExecuteSqlRawAsync(script);
        Console.WriteLine("The user profile fields were added successfully.");
    }
    else
    {
        Console.WriteLine("The user profile fields already exist; no changes were made.");
    }
    return;
}

if (args.Contains("--apply-package-assignment-schema", StringComparer.OrdinalIgnoreCase))
{
    await using var scope = app.Services.CreateAsyncScope();
    var context = scope.ServiceProvider.GetRequiredService<WarehouseDbContext>();
    var connection = context.Database.GetDbConnection();
    await connection.OpenAsync();

    foreach (var requiredTable in new[] { "UserPackages", "clients", "users" })
    {
        await using var checkTable = connection.CreateCommand();
        checkTable.CommandText = """
            SELECT COUNT(*) FROM information_schema.tables
            WHERE table_schema = DATABASE() AND table_name = @tableName;
            """;
        var parameter = checkTable.CreateParameter();
        parameter.ParameterName = "@tableName";
        parameter.Value = requiredTable;
        checkTable.Parameters.Add(parameter);
        if (Convert.ToInt32(await checkTable.ExecuteScalarAsync()) == 0)
            throw new InvalidOperationException(
                $"The required {requiredTable} table does not exist in the configured database.");
    }

    var scriptPath = Path.Combine(
        app.Environment.ContentRootPath, "Data", "Sql", "003_user_package_assignments.sql");
    var script = await File.ReadAllTextAsync(scriptPath);
    await context.Database.ExecuteSqlRawAsync(script);
    Console.WriteLine("The package assignment table is ready.");
    return;
}

if (args.Contains("--create-demo-superadmin", StringComparer.OrdinalIgnoreCase))
{
    await using var scope = app.Services.CreateAsyncScope();
    var context = scope.ServiceProvider.GetRequiredService<WarehouseDbContext>();
    const string username = "superadmin";
    if (await context.Users.AnyAsync(user =>
            user.Username == username ||
            user.Role == UserRoles.SuperAdmin))
    {
        Console.WriteLine("A SuperAdmin account already exists; no record was created.");
        return;
    }

    var password = $"{Convert.ToHexString(RandomNumberGenerator.GetBytes(12))}!aA1";
    var user = new AppUser
    {
        ClientId = null,
        Username = username,
        FirstName = "MekMiCourier",
        LastName = "Administrator",
        Role = UserRoles.SuperAdmin,
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };
    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<AppUser>>();
    user.PasswordHash = passwordHasher.HashPassword(user, password);

    context.Users.Add(user);
    await context.SaveChangesAsync();

    Console.WriteLine($"SuperAdmin created. Username: {username}");
    Console.WriteLine($"One-time password: {password}");
    return;
}

if (args.Contains("--apply-user-schema", StringComparer.OrdinalIgnoreCase))
{
    await using var scope = app.Services.CreateAsyncScope();
    var context = scope.ServiceProvider.GetRequiredService<WarehouseDbContext>();
    var connection = context.Database.GetDbConnection();
    await connection.OpenAsync();

    await using var checkClients = connection.CreateCommand();
    checkClients.CommandText = """
        SELECT COUNT(*)
        FROM information_schema.tables
        WHERE table_schema = DATABASE() AND table_name = 'clients';
        """;
    var clientsExists = Convert.ToInt32(await checkClients.ExecuteScalarAsync()) > 0;
    if (!clientsExists)
    {
        var clientsScriptPath = Path.Combine(
            app.Environment.ContentRootPath, "Data", "Sql", "000_clients.sql");
        var clientsScript = await File.ReadAllTextAsync(clientsScriptPath);
        await context.Database.ExecuteSqlRawAsync(clientsScript);
        Console.WriteLine("The clients tenant table was created successfully.");
    }

    await using var checkUsers = connection.CreateCommand();
    checkUsers.CommandText = """
        SELECT COUNT(*)
        FROM information_schema.tables
        WHERE table_schema = DATABASE() AND table_name = 'users';
        """;
    var usersExists = Convert.ToInt32(await checkUsers.ExecuteScalarAsync()) > 0;

    if (!usersExists)
    {
        var scriptPath = Path.Combine(
            app.Environment.ContentRootPath, "Data", "Sql", "001_users.sql");
        var script = await File.ReadAllTextAsync(scriptPath);
        await context.Database.ExecuteSqlRawAsync(script);
        Console.WriteLine("The users table was created successfully.");
    }
    else
    {
        Console.WriteLine("The users table already exists; no changes were made.");
    }

    await using var checkLegacyRole = connection.CreateCommand();
    checkLegacyRole.CommandText = """
        SELECT COUNT(*)
        FROM information_schema.check_constraints
        WHERE constraint_schema = DATABASE()
          AND constraint_name = 'CK_users_role'
          AND check_clause LIKE '%PlatformAdmin%';
        """;
    var hasLegacyRole = Convert.ToInt32(await checkLegacyRole.ExecuteScalarAsync()) > 0;
    if (hasLegacyRole)
    {
        var roleScriptPath = Path.Combine(
            app.Environment.ContentRootPath, "Data", "Sql", "002_superadmin_role.sql");
        var roleScript = await File.ReadAllTextAsync(roleScriptPath);
        await context.Database.ExecuteSqlRawAsync(roleScript);
        Console.WriteLine("The SuperAdmin role was applied successfully.");
    }

    return;
}

// Middleware order
app.UseRouting();

app.UseWebSockets();

app.UseCors("GlobalSyntaxPolicy");

// Cache-Control Middleware
app.Use(async (context, next) =>
{
    context.Response.Headers["Cache-Control"] =
        "no-cache, no-store, must-revalidate";

    context.Response.Headers["Pragma"] = "no-cache";
    context.Response.Headers["Expires"] = "0";

    await next();
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapHub<PriceHub>("/pricehub");

// Database initialization
using (var scope = app.Services.CreateScope())
{
    var context =
        scope.ServiceProvider
            .GetRequiredService<WarehouseDbContext>();

    // context.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // Kept exactly as you had it
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            "/openapi/v1.json",
            "Warehouse API v1"
        );
    });
}

app.UseHttpsRedirection();

app.Run();

internal sealed class BearerSecuritySchemeTransformer(
    IAuthenticationSchemeProvider authenticationSchemeProvider)
    : IOpenApiDocumentTransformer
{
    public async Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        var authenticationSchemes =
            await authenticationSchemeProvider
                .GetAllSchemesAsync();

        var bearerIsRegistered =
            authenticationSchemes.Any(
                scheme =>
                    scheme.Name ==
                    JwtBearerDefaults.AuthenticationScheme
            );

        if (!bearerIsRegistered)
        {
            return;
        }

        document.Components ??= new OpenApiComponents();

        document.Components.SecuritySchemes =
            new Dictionary<string, OpenApiSecurityScheme>
            {
                ["Bearer"] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description =
                        "Enter the JWT token returned by the login endpoint."
                }
            };

        foreach (var operation in document.Paths.Values
                     .SelectMany(path => path.Operations))
        {
            operation.Value.Security ??=
                new List<OpenApiSecurityRequirement>();

            operation.Value.Security.Add(
                new OpenApiSecurityRequirement
                {
                    [
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Id = "Bearer",
                                Type =
                                    ReferenceType.SecurityScheme
                            }
                        }
                    ] = Array.Empty<string>()
                }
            );
        }
    }
}
