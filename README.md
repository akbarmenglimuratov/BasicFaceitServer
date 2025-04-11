# BasicFaceitServer

## Useful Links

- [CounterStrikeSharp API Documentation](https://docs.cssharp.dev/docs/guides/getting-started.html): The documentation for getting started.
- [CounterStrikeSharp Discord Channel](https://discord.gg/tfPyCqyCPv): Join the CounterStrikeSharp community on Discord for help and discussion about plugin development.

## Quick Start with Git

1. **Clone the git repository**

   Start by cloning a repository with the following command:

   ```bash
   git clone https://github.com/akbarmenglimuratov/BasicFaceitServer.git
   ```

2. **Build the plugin (make sure you have installed .NET and necessary addons (CounterStrikeSharp and Metamod)**

   ```bash
   dotnet build
   // or build right to the game plugin folder. It detects the file changes and auto builds the plugin
   dotnet watch build BasicFaceitServer.csproj --property:OutDir=path\to\game\csgo\addons\counterstrikesharp\plugins\BasicFaceitServer
   ```

4. **The config file will localed in path\to\game\csgo\addons\counterstrikesharp\plugins\BasicFaceitServer**
