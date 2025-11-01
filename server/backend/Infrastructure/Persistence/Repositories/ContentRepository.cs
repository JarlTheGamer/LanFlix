using Dapper;
using Lanflix.Domain.Entities;
using Lanflix.Domain.Enums;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Lanflix.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for Content entity with Dapper-optimized queries
/// </summary>
public class ContentRepository : Repository<Content>, IContentRepository
{
    private readonly string _connectionString;
    private readonly bool _isPostgres;

    public ContentRepository(ApplicationDbContext context) : base(context)
    {
        _connectionString = context.Database.GetConnectionString() ?? string.Empty;
        _isPostgres = context.Database.IsNpgsql();
    }

    public async Task<(IEnumerable<ContentListItem> Items, int TotalCount)> GetContentListAsync(
        ContentType? type,
        int page,
        int pageSize,
        string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        var offset = (page - 1) * pageSize;

        // Build the WHERE clause
        var whereClause = "WHERE IsDeleted = 0";
        var parameters = new DynamicParameters();

        if (type.HasValue)
        {
            whereClause += " AND Type = @Type";
            parameters.Add("Type", (int)type.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            whereClause += " AND (Title LIKE @SearchTerm OR OriginalTitle LIKE @SearchTerm)";
            parameters.Add("SearchTerm", $"%{searchTerm}%");
        }

        parameters.Add("Offset", offset);
        parameters.Add("PageSize", pageSize);

        // Query for items
        var itemsQuery = $@"
            SELECT Id, TmdbId, Type, Title, PosterPath, ReleaseDate, Rating, AddedAt
            FROM Contents
            {whereClause}
            ORDER BY AddedAt DESC
            LIMIT @PageSize OFFSET @Offset";

        // Query for total count
        var countQuery = $@"
            SELECT COUNT(*)
            FROM Contents
            {whereClause}";

        using var connection = CreateConnection();
        connection.Open();

        var items = await connection.QueryAsync<ContentListItem>(itemsQuery, parameters);
        var totalCount = await connection.ExecuteScalarAsync<int>(countQuery, parameters);

        return (items, totalCount);
    }

    public async Task<Content?> GetByTmdbIdAsync(int tmdbId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(c => c.Episodes)
            .FirstOrDefaultAsync(c => c.TmdbId == tmdbId, cancellationToken);
    }

    public async Task<IEnumerable<ContentListItem>> GetRecentlyAddedAsync(
        int count,
        ContentType? type = null,
        CancellationToken cancellationToken = default)
    {
        var whereClause = "WHERE IsDeleted = 0";
        var parameters = new DynamicParameters();

        if (type.HasValue)
        {
            whereClause += " AND Type = @Type";
            parameters.Add("Type", (int)type.Value);
        }

        parameters.Add("Count", count);

        var query = $@"
            SELECT Id, TmdbId, Type, Title, PosterPath, ReleaseDate, Rating, AddedAt
            FROM Contents
            {whereClause}
            ORDER BY AddedAt DESC
            LIMIT @Count";

        using var connection = CreateConnection();
        connection.Open();

        return await connection.QueryAsync<ContentListItem>(query, parameters);
    }

    public async Task<bool> ExistsByFilePathAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var query = "SELECT COUNT(*) FROM Contents WHERE FilePath = @FilePath AND IsDeleted = 0";
        var parameters = new { FilePath = filePath };

        using var connection = CreateConnection();
        connection.Open();

        var count = await connection.ExecuteScalarAsync<int>(query, parameters);
        return count > 0;
    }

    private System.Data.IDbConnection CreateConnection()
    {
        if (_isPostgres)
        {
            return new NpgsqlConnection(_connectionString);
        }
        else
        {
            return new SqliteConnection(_connectionString);
        }
    }
}
