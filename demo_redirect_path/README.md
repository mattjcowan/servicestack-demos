# Base setup

Use the ASPNET Core rewrite capabilities to control ServiceStack routes and endpoints.

## About this demo

Leverages [URL Rewriting Middleware in ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/url-rewriting) to serve a ServiceStack API on different endpoints.

I find this helpful when for example I need to develop an API that runs both as:

- https://api.mydomain.com
- https://mydomain.com/api
s
## Run this demo

Now run the dotnet app

```shell
dotnet restore
dotnet run # or dotnet watch
```

## Notes

The ServiceStack implementation is running at the "ROOT" path of the website, but using these rewrite capabilities, the ServiceStack api is served from "/api" and the Swagger documentation is served from "/docs".

The following URLs render the same thing:

Mapped URL | ServiceStack URL
--- | ---
/api | /metadata
/docs | /swagger-ui/
/api/{anything} | /{servicestack_route}
