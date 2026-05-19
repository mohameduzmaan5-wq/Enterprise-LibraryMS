namespace LibraryMS.Data.Database;

/// <summary>
/// Centralized connection string management with offline fallback support.
/// </summary>
public static class ConnectionString
{
    private static string _connectionString = 
        @"Server=localhost\SQLEXPRESS;Database=LibraryMS;Trusted_Connection=True;TrustServerCertificate=True;";  
        
      /// <summary>
    /// Gets or sets the SQL Server connection string.
    /// </summary>
    public static string Value
    {
        get => _connectionString;
        set => _connectionString = value;
    }

    /// <summary>
    /// Configures the connection string from application settings.
    /// </summary>
    public static void Configure(string server, string database, bool trustedConnection = true)
    {
        if (trustedConnection)
        {
            _connectionString = $"Server={server};Database={database};Trusted_Connection=True;TrustServerCertificate=True;";
        }
        else
        {
            _connectionString = $"Server={server};Database={database};TrustServerCertificate=True;";
        }
    }

    /// <summary>
    /// Configures with SQL Server authentication.
    /// </summary>
    public static void Configure(string server, string database, string username, string password)
    {
        _connectionString = $"Server={server};Database={database};User Id={username};Password={password};TrustServerCertificate=True;";
    }
}
