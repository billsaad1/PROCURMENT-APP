using System;
using System.Security.Cryptography;
using System.Text;

public class SecurityHelper
{
    private const string GlobalSalt = "JAAHD_SALT_2024_!";

    public static string HashPassword(string password)
    {
        using (var sha256 = SHA256.Create())
        {
            var combined = password + GlobalSalt;
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));
            return "$SHA2$" + BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
        }
    }
}

public class Program
{
    public static void Main()
    {
        Console.WriteLine("admin:" + SecurityHelper.HashPassword("admin"));
        Console.WriteLine("pm:" + SecurityHelper.HashPassword("pm"));
        Console.WriteLine("log:" + SecurityHelper.HashPassword("log"));
        Console.WriteLine("proc:" + SecurityHelper.HashPassword("proc"));
        Console.WriteLine("fin:" + SecurityHelper.HashPassword("fin"));
        Console.WriteLine("store:" + SecurityHelper.HashPassword("store"));
        Console.WriteLine("head:" + SecurityHelper.HashPassword("head"));
    }
}
