using System;
using System.Reflection;
using System.Linq;

Console.WriteLine("=== ModelContextProtocol Package Inspection ===");

try 
{
    // Try to load and inspect ModelContextProtocol
    var mcpAssembly = Assembly.Load("ModelContextProtocol");
    Console.WriteLine("\n--- ModelContextProtocol Types ---");
    foreach(var type in mcpAssembly.GetTypes().OrderBy(t => t.FullName))
    {
        Console.WriteLine($"  {type.FullName}");
    }
}
catch(Exception ex)
{
    Console.WriteLine($"Error loading ModelContextProtocol: {ex.Message}");
}

try 
{
    // Try to load and inspect ModelContextProtocol.Core
    var coreAssembly = Assembly.Load("ModelContextProtocol.Core");
    Console.WriteLine("\n--- ModelContextProtocol.Core Types ---");
    foreach(var type in coreAssembly.GetTypes().OrderBy(t => t.FullName))
    {
        Console.WriteLine($"  {type.FullName}");
    }
}
catch(Exception ex)
{
    Console.WriteLine($"Error loading ModelContextProtocol.Core: {ex.Message}");
}

try 
{
    // Try to load and inspect ModelContextProtocol.AspNetCore
    var aspNetCoreAssembly = Assembly.Load("ModelContextProtocol.AspNetCore");
    Console.WriteLine("\n--- ModelContextProtocol.AspNetCore Types ---");
    foreach(var type in aspNetCoreAssembly.GetTypes().OrderBy(t => t.FullName))
    {
        Console.WriteLine($"  {type.FullName}");
    }
}
catch(Exception ex)
{
    Console.WriteLine($"Error loading ModelContextProtocol.AspNetCore: {ex.Message}");
}

Console.WriteLine("\n=== Inspection Complete ===");