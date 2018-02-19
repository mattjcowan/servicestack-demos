# Base setup

RiotJS SSR demo using ServiceStack and NodeServices

## About this demo

Leverages [NodeServices](https://github.com/aspnet/JavaScriptServices/tree/dev/src/Microsoft.AspNetCore.NodeServices#microsoftaspnetcorenodeservices) to render RiotJS tag on the server. After the tag is rendered, the client fetches updated data via AJAX and refreshes the control on the client-side.

Subsequent interactions are done on the client (so only SSR right now on the first request) but the client keeps the server up to date via ajax calls.

## Run this demo

Install riotjs library

```shell
cd demo_riotjs_ssr
npm install

# alternatively, you could just do it from scratch
npm init -y
npm install riot@3.9.0
```

Now run the dotnet app

```shell
dotnet restore
dotnet run # or dotnet watch
```

## Notes

In non-SSR mode (http://localhost:5000/non-ssr), when viewing the source, the `todo` tag is not populated. The client populates it when it does an ajax request for the tag.

![](/images/2018-02-18-21-12-12.png)

In SSR mode (http://localhost:5000/ssr), when viewing the source in the browser, you'll see the `todo` tag is fully populated from the server. On the server, the title is set to "This is populated from server", to prove the point. The client subsequently fetches data to get the latest and updates the client, changing the title to "This is populated from the client". You see a very quick flicker as the title changes when you load the page.

![](/images/2018-02-18-21-08-37.png)

