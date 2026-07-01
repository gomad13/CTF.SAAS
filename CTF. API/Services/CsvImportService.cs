using System.Text;
using System.Data;
using Microsoft.EntityFrameworkCore;
using ExcelDataReader;
using CTF.Api.Data;
using CTF.Api.Models;

namespace CTF.Api.Services;

public class CsvImportService
{
    private readonly AppDbContext _context;
    private readonly ILogger<CsvImportService> _logger;

    public CsvImportService(AppDbContext context, ILogger<CsvImportService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public record ImportResult(
        int Created,
        int Updated,
        int Skipped,
        int Errors,
        List<string> ErrorMessages,
        List<string> CreatedEmails,
        List<string> UpdatedEmails,
        // [PENTEST] mot de passe aleatoire unique par utilisateur importe :
        // identifiants temporaires des comptes CRÉÉS, format "email:password".
        List<string> Credentials);

    public record CsvUserRow(string Email, string FirstName, string LastName, string Role, bool IsActive);

    public async Task<ImportResult> ImportUsersAsync(Stream csvStream, Guid tenantId, bool updateExisting = true)
    {
        var created = new List<string>();
        var updated = new List<string>();
        // [PENTEST] mot de passe aleatoire unique par utilisateur importe :
        // on remonte les identifiants temporaires des comptes CRÉÉS à l'admin.
        var credentials = new List<string>();
        var skipped = 0;
        var errors  = new List<string>();
        var lineNumber = 0;

        using var reader = new StreamReader(csvStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var header = await reader.ReadLineAsync();
        lineNumber++;

        if (header == null || !IsValidHeader(header))
        {
            errors.Add("En-tête CSV invalide. Format attendu : Email,Prénom,Nom,Rôle,Actif");
            return new ImportResult(0, 0, 0, 1, errors, created, updated, credentials);
        }

        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line)) { skipped++; continue; }

            var (row, error) = ParseCsvLine(line, lineNumber);
            if (error != null) { errors.Add(error); continue; }
            if (row == null) continue;

            try
            {
                var existing = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == row.Email.ToLower() && u.TenantId == tenantId);

                if (existing != null)
                {
                    if (!updateExisting) { skipped++; continue; }
                    existing.FirstName = row.FirstName;
                    existing.LastName = row.LastName;
                    existing.DisplayName = $"{row.FirstName} {row.LastName}";
                    existing.Role = row.Role.ToLowerInvariant();
                    existing.IsActive = row.IsActive;
                    updated.Add(row.Email);
                }
                else
                {
                    // [PENTEST] mot de passe aleatoire unique par utilisateur importe
                    var initialPassword = GenerateInitialPassword();
                    var normalizedEmail = row.Email.ToLowerInvariant().Trim();
                    _context.Users.Add(new User
                    {
                        Id = Guid.NewGuid(),
                        Email = normalizedEmail,
                        FirstName = row.FirstName,
                        LastName = row.LastName,
                        DisplayName = $"{row.FirstName} {row.LastName}",
                        Role = row.Role.ToLowerInvariant(),
                        TenantId = tenantId,
                        IsActive = row.IsActive,
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(initialPassword, workFactor: 12),
                        CreatedAt = DateTime.UtcNow,
                        LastLoginAt = null,
                    });
                    created.Add(row.Email);
                    // On ne loggue jamais le mot de passe ; il n'est remonté qu'à l'admin via le résultat.
                    credentials.Add($"{normalizedEmail}:{initialPassword}");
                }

                if ((created.Count + updated.Count) % 50 == 0)
                    await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                errors.Add($"Ligne {lineNumber} ({row.Email}) : {ex.Message}");
                _logger.LogError(ex, "Import error line {Line}", lineNumber);
            }
        }

        await _context.SaveChangesAsync();

        return new ImportResult(created.Count, updated.Count, skipped, errors.Count, errors, created, updated, credentials);
    }

    // [PENTEST] mot de passe aleatoire unique par utilisateur importe :
    // génère un mot de passe conforme à la politique (>=8, maj/min/chiffre/spécial).
    // Préfixe "Aa1@" garantit la conformité ; le reste est tiré d'un charset sans
    // caractères ambigus via un RNG cryptographique.
    private static string GenerateInitialPassword()
    {
        const string charset = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";
        const int randomLength = 12;
        var sb = new StringBuilder("Aa1@");
        for (int i = 0; i < randomLength; i++)
        {
            var idx = System.Security.Cryptography.RandomNumberGenerator.GetInt32(charset.Length);
            sb.Append(charset[idx]);
        }
        return sb.ToString();
    }

    private static bool IsValidHeader(string header)
    {
        var n = header.ToLowerInvariant().Replace(" ", "").Replace("é", "e").Replace("ô", "o").Replace("è", "e");
        return n.Contains("email")
            && (n.Contains("prenom") || n.Contains("firstname"))
            && (n.Contains("nom") || n.Contains("lastname"));
    }

    private static (CsvUserRow? Row, string? Error) ParseCsvLine(string line, int lineNumber)
    {
        var parts = line.Split(',');
        if (parts.Length < 3)
            return (null, $"Ligne {lineNumber} : format invalide (colonnes manquantes)");

        var email = parts[0].Trim().ToLowerInvariant();
        if (!IsValidEmail(email))
            return (null, $"Ligne {lineNumber} : email invalide ({email})");

        var firstName = parts[1].Trim();
        var lastName = parts[2].Trim();
        if (string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName))
            return (null, $"Ligne {lineNumber} : prénom et nom obligatoires");

        var roleRaw = parts.Length > 3 ? parts[3].Trim() : "User";
        var role = string.Equals(roleRaw, "Admin", StringComparison.OrdinalIgnoreCase) ? "admin" : "user";

        var isActive = true;
        if (parts.Length > 4) bool.TryParse(parts[4].Trim(), out isActive);

        return (new CsvUserRow(email, firstName, lastName, role, isActive), null);
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch { return false; }
    }

    public async Task<byte[]> ExportUsersToCsvAsync(Guid tenantId)
    {
        var users = await _context.Users.AsNoTracking()
            .Where(u => u.TenantId == tenantId)
            .OrderBy(u => u.LastName)
            .Select(u => new
            {
                u.Id, u.Email, u.FirstName, u.LastName, u.Role, u.IsActive, u.CreatedAt, u.LastLoginAt,
            })
            .ToListAsync();

        var stats = await _context.ChallengeCompletions.AsNoTracking()
            .Where(cc => users.Select(u => u.Id).Contains(cc.UserId))
            .GroupBy(cc => cc.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count(), Points = g.Sum(c => c.PointsEarned) })
            .ToDictionaryAsync(x => x.UserId, x => new { x.Count, x.Points });

        var csv = new StringBuilder();
        csv.AppendLine("Email,Prénom,Nom,Rôle,Actif,Créé le,Dernier login,Formations,Points");
        foreach (var u in users)
        {
            var s = stats.TryGetValue(u.Id, out var v) ? v : new { Count = 0, Points = 0 };
            csv.AppendLine($"{u.Email},{u.FirstName},{u.LastName},{u.Role},{u.IsActive},{u.CreatedAt:dd/MM/yyyy},{u.LastLoginAt:dd/MM/yyyy},{s.Count},{s.Points}");
        }
        return Encoding.UTF8.GetBytes("\uFEFF" + csv.ToString());
    }

    public async Task<ImportResult> ImportFromExcelAsync(Stream excelStream, Guid tenantId, bool updateExisting = true)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        var errors = new List<string>();

        using var reader = ExcelReaderFactory.CreateReader(excelStream);
        var dataSet = reader.AsDataSet(new ExcelDataSetConfiguration
        {
            ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = true }
        });

        if (dataSet.Tables.Count == 0)
        {
            errors.Add("Fichier Excel vide ou invalide");
            return new ImportResult(0, 0, 0, 1, errors, new(), new(), new());
        }

        var table = dataSet.Tables[0];

        int emailCol = -1, firstNameCol = -1, lastNameCol = -1, roleCol = -1, activeCol = -1;

        for (int c = 0; c < table.Columns.Count; c++)
        {
            var name = table.Columns[c].ColumnName.ToLowerInvariant().Trim()
                .Replace("é", "e").Replace("è", "e").Replace("ô", "o").Replace("ê", "e");

            if (name.Contains("email")) emailCol = c;
            else if (name.Contains("prenom") || name.Contains("firstname") || name.Contains("first")) firstNameCol = c;
            else if (name == "nom" || name.Contains("lastname") || name.Contains("last name")) lastNameCol = c;
            else if (name.Contains("role") || name.Contains("rôle")) roleCol = c;
            else if (name.Contains("actif") || name.Contains("active")) activeCol = c;
        }

        if (emailCol < 0 || firstNameCol < 0 || lastNameCol < 0)
        {
            errors.Add("Colonnes obligatoires manquantes. En-têtes attendus : Email, Prénom, Nom (+ optionnel : Rôle, Actif)");
            return new ImportResult(0, 0, 0, 1, errors, new(), new(), new());
        }

        // Convert to CSV and reuse existing logic
        var csv = new StringBuilder();
        csv.AppendLine("Email,Prénom,Nom,Rôle,Actif");

        for (int r = 0; r < table.Rows.Count; r++)
        {
            var row = table.Rows[r];
            var email = row[emailCol]?.ToString()?.Trim() ?? "";
            if (string.IsNullOrEmpty(email)) continue;

            var firstName = row[firstNameCol]?.ToString()?.Trim() ?? "";
            var lastName = row[lastNameCol]?.ToString()?.Trim() ?? "";
            var role = roleCol >= 0 ? (row[roleCol]?.ToString()?.Trim() ?? "User") : "User";
            var active = "true";
            if (activeCol >= 0)
            {
                var v = row[activeCol]?.ToString()?.ToLowerInvariant().Trim() ?? "true";
                active = (v == "false" || v == "0" || v == "non" || v == "no") ? "false" : "true";
            }

            csv.AppendLine($"{email},{firstName},{lastName},{role},{active}");
        }

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv.ToString()));
        var result = await ImportUsersAsync(stream, tenantId, updateExisting);

        var allErrors = result.ErrorMessages.Concat(errors).ToList();
        return result with
        {
            Errors = result.Errors + errors.Count,
            ErrorMessages = allErrors,
        };
    }

    public byte[] GenerateCsvTemplate()
    {
        var csv = new StringBuilder();
        csv.AppendLine("Email,Prénom,Nom,Rôle,Actif");
        csv.AppendLine("jean.dupont@votre-entreprise.fr,Jean,Dupont,User,true");
        csv.AppendLine("marie.martin@votre-entreprise.fr,Marie,Martin,Admin,true");
        csv.AppendLine("paul.bernard@votre-entreprise.fr,Paul,Bernard,User,false");
        return Encoding.UTF8.GetBytes("\uFEFF" + csv.ToString());
    }
}
