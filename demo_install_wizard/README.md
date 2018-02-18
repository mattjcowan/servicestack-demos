# Install wizard setup

Create an install wizard that configures your environment on first use

## About this demo

For this demo to work, you'll need a tool that can automatically restart the app when the wizard is run.
I suggest [you use PM2](../demo_pm2_with_restart) since it's super simple to setup.

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

Now visit http://localhost:5000/some_random_route, you should see something like the following.

![](/images/2018-02-18-01-26-10.png)

Now, setup a connection string (the easiest is to just use SqLite as shown in the image above).

Upon submit, it will save the settings, and tell you if you were successful or not.

If you were not successful, you'll see something like:

![](/images/2018-02-18-01-33-37.png)

If you WERE successful, you'll see something like:

![](/images/2018-02-18-01-34-19.png)

And once the environment is back up and running (which PM2 will do for you), the wizard will redirect you to the landing page.
The app does this by polling for an /api/ping endpoint, and waiting till the endpoint is alive once more...

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