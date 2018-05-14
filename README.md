# WiredPlayers RolePlay Server
WiredPlayers is a RolePlay project made for RAGE Multiplayer, it uses C# as main server language and JavaScript for clien-side scripts. I started with it back in March 2017 and I'm still upgrading its functionality with suggestions received from people using this gamemode.

## Getting Started

### Prerequisites

* [RAGE Multiplayer](https://cdn.gtanet.work/RAGE_Multiplayer.zip) - The client to login into the server
* [Bridge plugin](https://cdn.gtanet.work/bridge-package.zip) - The plugin allowing use to use C# server-side
* [MySQL Server](https://dev.mysql.com/downloads/mysql/) - The database to store the data
* [.NET Core SDK](https://www.microsoft.com/net/download) - The SDK to develop C# resources
* Any client you want to access the MySQL database

**Note:** This project has only been tested under Windows environments

### Installing
1. Install the .msi file that comes into RAGE Multiplayer's .zip file
2. Execute the **updater.exe** located on the root folder where you installed RAGE Multiplayer
3. Unzip the Bridge plugin into the folder called **server-files** replacing the files if needed
4. Get all the files from this GitHub and place them into the same folder as before, replacing the files
5. Make sure your router has opened 22005 UDP port and 22006 TCP/IP
6. Open your MySQL client and execute the **wprp.sql** script under **server-files** folder
7. Import to Visual Studio the **WiredPlayers.csproj** file, located on the following path:
**%RAGEMP Installed folder%/server-files/bridge/resources/WiredPlayers/**
8. Change the database connection string under **Database.cs** located on the following path: 
**%RAGEMP Installed folder%/server-files/bridge/resources/WiredPlayers/database/**
```
You have to fill these fields with your database's connection information:

	private const String host = "";
	private const String user = "";
	private const String pass = "";
	private const String database = "";
```
9. In your database, execute the following query, replacing *MY_SOCIAL_CLUB_ACCOUNT* and *MY_PASSWORD* with your Rockstar's Social Club's account and a password you want to use to login the server, which can be different from the one used by the Social Club:
```
INSERT INTO accounts (socialName, password) VALUES ('MY_SOCIAL_CLUB_ACCOUNT', MD5('MY_PASSWORD'));
```
10. Make sure your solution has linked the **gtanetwork.api** and **MySql.Data** Nugets, if not, add them
11. On Visual Studio, clean and build the solution in order to generate the required **WiredPlayers.dll** library
12. Execute the **server.exe** located under the **server-files** folder
13. Log into your server and enjoy it


If you followed all this steps, you should be able to login with your newly registered account