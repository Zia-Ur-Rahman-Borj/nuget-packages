using FileAppendMcp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

// The only folder this server may touch: first argument, or MCP_ALLOWED_ROOT.
var root = args.FirstOrDefault() ?? Environment.GetEnvironmentVariable("MCP_ALLOWED_ROOT");
if (string.IsNullOrWhiteSpace(root))
{
    Console.Error.WriteLine("Usage: FileAppendMcp <allowed-folder>   (or set MCP_ALLOWED_ROOT)");
    return 1;
}

// No args passed to the builder: the folder path is not a configuration key.
var builder = Host.CreateApplicationBuilder();

// stdout carries the MCP protocol itself, so ALL logging must go to stderr.
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

builder.Services.AddSingleton(new PathSandbox(root));
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();
return 0;
