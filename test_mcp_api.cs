using System;
using System.Reflection;
using ModelContextProtocol;
using ModelContextProtocol.Core;
using ModelContextProtocol.AspNetCore;

class Program
{
    static void Main()
    {
        Console.WriteLine("=== MCP SDK API Investigation ===");
        
        // Check available types in ModelContextProtocol
        Console.WriteLine("\n--- ModelContextProtocol Assembly ---");
        var mcpAssembly = typeof(ModelContextProtocol.Core.McpServer).Assembly;
        LogPublicTypes(mcpAssembly, "ModelContextProtocol");
        
        // Check ModelContextProtocol.Core
        Console.WriteLine("\n--- ModelContextProtocol.Core Assembly ---");
        var coreAssembly = Assembly.LoadFrom("/home/codespace/.nuget/packages/modelcontextprotocol.core/0.2.0-preview.3/lib/net8.0/ModelContextProtocol.Core.dll");
        LogPublicTypes(coreAssembly, "ModelContextProtocol.Core");
        
        // Check ModelContextProtocol.AspNetCore
        Console.WriteLine("\n--- ModelContextProtocol.AspNetCore Assembly ---");
        var aspNetCoreAssembly = Assembly.LoadFrom("/home/codespace/.nuget/packages/modelcontextprotocol.aspnetcore/0.2.0-preview.3/lib/net8.0/ModelContextProtocol.AspNetCore.dll");
        LogPublicTypes(aspNetCoreAssembly, "ModelContextProtocol.AspNetCore");
    }
    
    static void LogPublicTypes(Assembly assembly, string name)
    {
        try
        {
            var types = assembly.GetExportedTypes();
            Console.WriteLine($"{name} has {types.Length} public types:");
            foreach (var type in types.Take(20)) // First 20 to avoid spam
            {
                Console.WriteLine($"  - {type.FullName}");
            }
            if (types.Length > 20)
                Console.WriteLine($"  ... and {types.Length - 20} more");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading {name}: {ex.Message}");
        }
    }
}