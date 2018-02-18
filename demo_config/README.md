# Config setup

Configure ServiceStack using the .net core IConfiguration system

## About this demo

Inspect [Program.cs](Program.cs) for yourself to see how to configure ServiceStack using IConfiguration.
Settings for ServiceStack are stored in the [servicestack.json](servicestack.json) file.

## Run this demo

```shell
cd demo_config
dotnet restore
dotnet run # or dotnet watch
```

### App Settings

Visit [http://localhost:5000/settings.json](http://localhost:5000/settings.json)

- Notice the "ServiceStack:*" settings.
- Notice the "USER" environment variable that was overriden by the [servicestack.json](servicestack.json) file

### Typed HostConfig object

Visit [http://localhost:5000/config.json](http://localhost:5000/config.json)

This object is used to configure the ServiceStack AppHost
