# Geo-helper project (Unity version)
## Short description
**Geo-helper** - AR layer of the world in which objects of augmented reality are displayed depending on the specified geo position
## How to download and init project
1. Download repository 
2. Open project in Unity 2019.x
3. Open “Github” window, connect to your account, click “pull” button
4. Create folder ```EnvironmentConstants``` in ```Assets``` folder. Create file in ```EnvironmentConstants``` folder with name ```LocalEnvironment.cs```. Fill it by this template and your data for properties:
```csharp
namespace EnvironmentConstants
{
    internal static class LocalEnvironment
    {
        public static string SERVER_API = "requestItFromDevelopers";
        public static string SERVER_WS_CONNECTION_STRING = "requestItFromDevelopers";
        public static string AUTH_USERNAME = "requestItFromDevelopers";
        public static string AUTH_PASSWORD = "requestItFromDevelopers";
    }
}
```