using System;
using BCrypt.Net;

namespace DataForgeStudio.PasswordHashGenerator;

/// <summary>
/// Simple console application to generate BCrypt password hashes
/// </summary>
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("DataForgeStudio V4 - BCrypt Password Hash Generator");
        Console.WriteLine("====================================================");
        Console.WriteLine();

        if (args.Length == 0)
        {
            Console.WriteLine("Usage: dotnet run <password>");
            Console.WriteLine("Example: dotnet run admin123");
            return;
        }

        var password = args[0];

        // Generate BCrypt hash with work factor 12
        var hash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

        Console.WriteLine($"Password: {password}");
        Console.WriteLine();
        Console.WriteLine($"BCrypt Hash (work factor 12):");
        Console.WriteLine(hash);
        Console.WriteLine();

        // Verify the hash
        var isValid = BCrypt.Net.BCrypt.Verify(password, hash);
        Console.WriteLine($"Verification: {(isValid ? "PASSED" : "FAILED")}");
    }
}
