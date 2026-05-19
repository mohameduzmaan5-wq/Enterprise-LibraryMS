using BCrypt.Net;
using Microsoft.Data.SqlClient;

namespace LibraryMS.Data.Database;

/// <summary>
/// Manages database initialization, connection testing, and schema creation.
/// </summary>
public static class DatabaseManager
{
    /// <summary>
    /// Tests the database connection.
    /// </summary>
    public static async Task<bool> TestConnectionAsync()
    {
        try
        {
            using var connection = new SqlConnection(ConnectionString.Value);
            await connection.OpenAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Initializes the database and creates tables if they don't exist.
    /// </summary>
    public static async Task InitializeDatabaseAsync()
    {
        // First, ensure the database exists
        await EnsureDatabaseExistsAsync();

        // Then create tables
        using var connection = new SqlConnection(ConnectionString.Value);
        await connection.OpenAsync();

        var sql = GetInitializationScript();
        using var command = new SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();

        // Seed sample data if empty
        await SeedDataIfEmptyAsync(connection);

        // Always ensure default admin exists (runs even on pre-existing databases)
        await SeedDefaultAdminAsync(connection);
    }

    private static async Task EnsureDatabaseExistsAsync()
    {
        var builder = new SqlConnectionStringBuilder(ConnectionString.Value);
        var dbName = builder.InitialCatalog;
        builder.InitialCatalog = "master";

        using var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync();

        var checkSql = $"IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = '{dbName}') CREATE DATABASE [{dbName}]";
        using var command = new SqlCommand(checkSql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private static string GetInitializationScript()
    {
        return @"
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Categories')
            BEGIN
                CREATE TABLE Categories (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    Name NVARCHAR(100) NOT NULL,
                    Description NVARCHAR(500) NULL,
                    CreatedAt DATETIME2 DEFAULT GETDATE(),
                    UpdatedAt DATETIME2 NULL
                );
            END

            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Books')
            BEGIN
                CREATE TABLE Books (
                    BookId INT IDENTITY(1,1) PRIMARY KEY,
                    Title NVARCHAR(300) NOT NULL,
                    Author NVARCHAR(200) NOT NULL,
                    ISBN NVARCHAR(20) NULL,
                    Category NVARCHAR(100) NOT NULL,
                    Quantity INT DEFAULT 1,
                    AvailableQuantity INT DEFAULT 1,
                    CreatedAt DATETIME2 DEFAULT GETDATE()
                );
            END

            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Members')
            BEGIN
                CREATE TABLE Members (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    FirstName NVARCHAR(100) NOT NULL,
                    LastName NVARCHAR(100) NOT NULL,
                    Email NVARCHAR(200) NULL,
                    Phone NVARCHAR(20) NULL,
                    Address NVARCHAR(500) NULL,
                    MembershipType NVARCHAR(20) DEFAULT 'Standard',
                    JoinDate DATETIME2 DEFAULT GETDATE(),
                    IsActive BIT DEFAULT 1,
                    CreatedAt DATETIME2 DEFAULT GETDATE(),
                    UpdatedAt DATETIME2 NULL
                );
            END

            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Loans')
            BEGIN
                CREATE TABLE Loans (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    BookId INT NOT NULL,
                    MemberId INT NOT NULL,
                    BorrowDate DATETIME2 DEFAULT GETDATE(),
                    DueDate DATETIME2 NOT NULL,
                    ReturnDate DATETIME2 NULL,
                    Status NVARCHAR(20) DEFAULT 'Active',
                    FineAmount DECIMAL(10,2) DEFAULT 0,
                    CreatedAt DATETIME2 DEFAULT GETDATE(),
                    UpdatedAt DATETIME2 NULL,
                    FOREIGN KEY (BookId) REFERENCES Books(BookId),
                    FOREIGN KEY (MemberId) REFERENCES Members(Id)
                );
            END

            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AppUsers')
            BEGIN
                CREATE TABLE AppUsers (
                    Id           INT IDENTITY(1,1) PRIMARY KEY,
                    Username     NVARCHAR(50)  NOT NULL UNIQUE,
                    PasswordHash NVARCHAR(255) NOT NULL,
                    FullName     NVARCHAR(150) NOT NULL,
                    Email        NVARCHAR(200) NULL,
                    Role         NVARCHAR(20)  NOT NULL DEFAULT 'Librarian',
                    IsActive     BIT           NOT NULL DEFAULT 1,
                    FailedLogins INT           NOT NULL DEFAULT 0,
                    IsLocked     BIT           NOT NULL DEFAULT 0,
                    LockedUntil  DATETIME2     NULL,
                    LastLogin    DATETIME2     NULL,
                    CreatedAt    DATETIME2     NOT NULL DEFAULT GETDATE()
                );
            END

            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ActivityLogs')
            BEGIN
                CREATE TABLE ActivityLogs (
                    Id        INT IDENTITY(1,1) PRIMARY KEY,
                    UserId    INT           NULL,
                    Username  NVARCHAR(50)  NOT NULL,
                    Action    NVARCHAR(100) NOT NULL,
                    Details   NVARCHAR(500) NULL,
                    IpAddress NVARCHAR(50)  NOT NULL DEFAULT 'localhost',
                    Success   BIT           NOT NULL DEFAULT 1,
                    Timestamp DATETIME2     NOT NULL DEFAULT GETDATE()
                );
            END

            -- Indexes for performance
            IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Books_Category')
                CREATE INDEX IX_Books_Category ON Books(Category);

            IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Loans_BookId')
                CREATE INDEX IX_Loans_BookId ON Loans(BookId);

            IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Loans_MemberId')
                CREATE INDEX IX_Loans_MemberId ON Loans(MemberId);

            IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Loans_Status')
                CREATE INDEX IX_Loans_Status ON Loans(Status);

            IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Books_Title')
                CREATE INDEX IX_Books_Title ON Books(Title);

            IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ActivityLogs_Timestamp')
                CREATE INDEX IX_ActivityLogs_Timestamp ON ActivityLogs(Timestamp);
        ";
    }

    private static async Task SeedDataIfEmptyAsync(SqlConnection connection)
    {
        // Check if categories exist
        using var checkCmd = new SqlCommand("SELECT COUNT(*) FROM Categories", connection);
        var count = (int)await checkCmd.ExecuteScalarAsync();

        if (count > 0) return;

        var seedSql = @"
            -- Seed Categories
            INSERT INTO Categories (Name, Description) VALUES 
                ('Fiction', 'Novels, short stories, and literary fiction'),
                ('Non-Fiction', 'Factual and informational books'),
                ('Science & Technology', 'Scientific and technical publications'),
                ('History', 'Historical accounts and analyses'),
                ('Biography', 'Life stories and memoirs'),
                ('Children', 'Books for young readers'),
                ('Reference', 'Encyclopedias, dictionaries, and guides'),
                ('Art & Design', 'Visual arts, photography, and design');

            -- Seed Books
            INSERT INTO Books (Title, Author, ISBN, Category, Quantity, AvailableQuantity) VALUES
                ('The Great Gatsby', 'F. Scott Fitzgerald', '978-0743273565', 'Fiction', 5, 3),
                ('To Kill a Mockingbird', 'Harper Lee', '978-0446310789', 'Fiction', 4, 2),
                ('1984', 'George Orwell', '978-0451524935', 'Fiction', 6, 4),
                ('Sapiens', 'Yuval Noah Harari', '978-0062316097', 'Non-Fiction', 3, 2),
                ('A Brief History of Time', 'Stephen Hawking', '978-0553380163', 'Science & Technology', 2, 1),
                ('The Art of War', 'Sun Tzu', '978-1599869773', 'History', 3, 3),
                ('Steve Jobs', 'Walter Isaacson', '978-1451648539', 'Biography', 4, 3),
                ('Charlotte''s Web', 'E.B. White', '978-0061124952', 'Children', 5, 4),
                ('Clean Code', 'Robert C. Martin', '978-0132350884', 'Science & Technology', 3, 1),
                ('The Design of Everyday Things', 'Don Norman', '978-0465050659', 'Art & Design', 2, 2),
                ('Educated', 'Tara Westover', '978-0399590504', 'Biography', 3, 2),
                ('Atomic Habits', 'James Clear', '978-0735211292', 'Non-Fiction', 5, 3),
                ('The Hobbit', 'J.R.R. Tolkien', '978-0547928227', 'Fiction', 4, 2),
                ('Thinking, Fast and Slow', 'Daniel Kahneman', '978-0374533557', 'Non-Fiction', 3, 1),
                ('The Selfish Gene', 'Richard Dawkins', '978-0198788607', 'Science & Technology', 2, 2);

            -- Seed Members
            INSERT INTO Members (FirstName, LastName, Email, Phone, MembershipType, JoinDate) VALUES
                ('Arun', 'Kumar', 'arun.kumar@email.com', '+94-771-234567', 'Premium', '2024-01-15'),
                ('Priya', 'Sharma', 'priya.s@email.com', '+94-772-345678', 'Standard', '2024-02-20'),
                ('Mohamed', 'Ali', 'mali@email.com', '+94-773-456789', 'Student', '2024-03-10'),
                ('Lakshmi', 'Nair', 'lakshmi.n@email.com', '+94-774-567890', 'Premium', '2024-04-05'),
                ('David', 'Fernando', 'david.f@email.com', '+94-775-678901', 'Standard', '2024-05-12'),
                ('Nithya', 'Raj', 'nithya.r@email.com', '+94-776-789012', 'Student', '2024-06-18'),
                ('Kasun', 'Perera', 'kasun.p@email.com', '+94-777-890123', 'Standard', '2024-07-22'),
                ('Amara', 'Silva', 'amara.s@email.com', '+94-778-901234', 'Premium', '2024-08-30');

            -- Seed some Loans
            INSERT INTO Loans (BookId, MemberId, BorrowDate, DueDate, Status) VALUES
                (1, 1, DATEADD(DAY, -10, GETDATE()), DATEADD(DAY, 4, GETDATE()), 'Active'),
                (2, 2, DATEADD(DAY, -7, GETDATE()), DATEADD(DAY, 7, GETDATE()), 'Active'),
                (5, 3, DATEADD(DAY, -20, GETDATE()), DATEADD(DAY, -6, GETDATE()), 'Active'),
                (9, 1, DATEADD(DAY, -5, GETDATE()), DATEADD(DAY, 9, GETDATE()), 'Active'),
                (3, 4, DATEADD(DAY, -30, GETDATE()), DATEADD(DAY, -16, GETDATE()), 'Returned'),
                (4, 5, DATEADD(DAY, -15, GETDATE()), DATEADD(DAY, -1, GETDATE()), 'Active'),
                (13, 6, DATEADD(DAY, -3, GETDATE()), DATEADD(DAY, 11, GETDATE()), 'Active'),
                (14, 7, DATEADD(DAY, -12, GETDATE()), DATEADD(DAY, 2, GETDATE()), 'Active');

            UPDATE Loans SET ReturnDate = DATEADD(DAY, -14, GETDATE()), FineAmount = 0 WHERE Id = 5;
        ";

        using var seedCmd = new SqlCommand(seedSql, connection);
        await seedCmd.ExecuteNonQueryAsync();
    }

    private static async Task SeedDefaultAdminAsync(SqlConnection connection)
    {
        // Hash default passwords with BCrypt work-factor 12
        var adminHash = BCrypt.Net.BCrypt.HashPassword("Admin@123", 12);
        var libHash   = BCrypt.Net.BCrypt.HashPassword("Lib@1234",  12);

        // Ensure admin user exists, then ALWAYS reset password + unlock
        var sql = @"
            IF NOT EXISTS (SELECT 1 FROM AppUsers WHERE Username = 'admin')
                INSERT INTO AppUsers (Username, PasswordHash, FullName, Email, Role, IsActive, FailedLogins, IsLocked)
                VALUES ('admin', @AdminHash, 'System Administrator', 'admin@libraryms.com', 'Admin', 1, 0, 0);

            -- Always reset admin password and clear lockout (ensures default credentials work)
            UPDATE AppUsers
            SET PasswordHash = @AdminHash,
                FailedLogins = 0,
                IsLocked     = 0,
                LockedUntil  = NULL
            WHERE Username = 'admin';

            IF NOT EXISTS (SELECT 1 FROM AppUsers WHERE Username = 'librarian')
                INSERT INTO AppUsers (Username, PasswordHash, FullName, Email, Role, IsActive, FailedLogins, IsLocked)
                VALUES ('librarian', @LibHash, 'Head Librarian', 'library@libraryms.com', 'Librarian', 1, 0, 0);";

        using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@AdminHash", adminHash);
        cmd.Parameters.AddWithValue("@LibHash",   libHash);
        await cmd.ExecuteNonQueryAsync();
    }
}
