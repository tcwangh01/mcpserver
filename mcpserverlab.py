from mcp.server.fastmcp import FastMCP

# Create an instance of FastMCP
mcp = FastMCP("My MCP Server", "1.0.0")
# Add a simple tool
@mcp.tool()
def greet(name: str) -> str : 
    """ Greet a user """
    return f"Hello, {name}!"

# Add a simple resource
@mcp.resource("hello://message")
def get_hello() -> str :
    """ Get a hello message """
    return "Hello, MCP Server!"