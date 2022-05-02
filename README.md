# Pastebook Version

## Install Packages For Client

```bash
npm i
```
## Install Packages For Server

```bash
dotnet add package BCrypt.Net-Next
dotnet add package System.Data.SqlClient
dotnet add package System.IdentityModel.Tokens.Jwt --version 6.15.0
```
## Usage
In the server directory, export an the SQL Database Connection String
```bash
export DB_CONNECTION_STRING="Server=<Server>;Database=<Database>;User Id=<User>;Password=<Password>;"
```
then run using the following command
```bash
dotnet run
```
Run the client
```bash
ng start
```
then access the app on [http://localhost:4200/](http://localhost:4200/)
## Contributing
Charles, Chock, Camille, JK from
Pointwest
