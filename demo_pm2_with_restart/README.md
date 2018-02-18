# Run ServiceStack with PM2 and restart app on demand

Run ServiceStack with PM2. Clicking on a "Restart" url, will stop the application and 'cause PM2 to restart the app.

You can use this configuration if you need to reload the environment in a dynamic plugin management type of application for example.

***Note:** There is a brief period of downtime (usually seconds) as the app recycles*

## About this demo

Inspect [Program.cs](Program.cs) for yourself to see how to configure ServiceStack.

## Run this demo

Make sure the project builds

```shell
dotnet restore
dotnet build
```

### Install pm2

The easiest way is to install pm2 with the node package manager.
You'll need [NodeJS](https://nodejs.org) installed on your machine.

```shell
npm install -g pm2
```

### Run the app in pm2

```shell
pm2 start --name demoApp dotnet -- run
```

If you were to use this setup in production, you'd publish the project and point dotnet to the actual `demo.dll` file instead of using `dotnet run`.

### Run and restart the app in the browser

Now visit http://localhost:5000, you should see something like

![](/images/2018-02-17-18-22-54.png)

Refresh several times, and the RuntimeId will stay the same, as it's a static readonly variable.

Now click on the **Restart** link, and wait a second or two.

Refresh the page, and the RuntimeId will have changed. Try it several times.

Go back to the terminal, and check the PM2 status

```shell
pm2 status
```

You'll see that the process has restarted a number of times (3 times for example in the image below).

![](/images/2018-02-17-18-27-18.png)

### Restart the process in pm2

You can restart the demo app process in the terminal

```shell
pm2 restart demoApp
```

### Stop and remove the demo app from pm2

Once you're done with this demo, stop and delete the demo app from pm2.

```shell
pm2 stop demoApp
pm2 delete demoApp
```

### (Optional) Uninstall pm2

```shell
pm2 kill # kill all pm2 processes
npm uninstall -g pm2
```