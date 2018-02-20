# Base setup

Basic setup for analyzing the order of operations in ServiceStack

## About this demo

Inspect [Program.cs](Program.cs) for yourself. This is mostly an analysis of how all the AppHost overrides allow you to manipulate the request/response objects.

Refer to the documentation at http://docs.servicestack.net/order-of-operations for the details.

## Run this demo

```shell
cd demo_order_of_operations
dotnet restore
dotnet run # or dotnet watch
```

Analyze the "orderofoperations.txt" file at the root of the project (the data returned in the browser is incomplete, as it doesn't include behaviors after the response has already been written).
