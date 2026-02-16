using System;
using BCrypt.Net;

class Program 
{ 
    static void Main() 
    { 
        var password = "admin123";
        var hash = BCrypt.Net.BCrypt.HashPassword(password);
        Console.WriteLine(hash);
    } 
}
