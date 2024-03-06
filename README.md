# Scheduler Project

This project is a simple task scheduler written in C#.
It is designed to execute a large number of tasks at regular intervals, while managing CPU utilization effectively.

The program is designed to avoid 100% cpu utilization by using SemaphoreSlim to limit the number of tasks that can run concurrently.
You can increase the number of CPU cores, see Usage section below.

## Installation

To run the program: clone the repository and fire up a terminal. The project is written in C# and targets [.NET Core 8.0](https://dotnet.microsoft.com/en-us/download/dotnet/8.0).

````bash
dotnet restore
dotnet run
````

## Usage

To use the scheduler, create a list of `SchedulerTask` objects and pass it to the `Scheduler` constructor. Then call the `Start` method to start executing the tasks. Here is an example:

```csharp
var tasks = SchedulerTask.GetTasks().Take(100).ToList();
var scheduler = new Scheduler(tasks);
scheduler.Start();
```

To stop executing the tasks, call the `Stop` method:

```csharp
scheduler.Stop();
```

You can increase the number of concurrent tasks in the Program class.
Internally it will increase the size of the SemaphoreSlim object to allow more tasks to run concurrently.

```csharp
var cpuCores = 8;
var scheduler = new Scheduler(tasks, cpuCores);
```

If you increase the number of concurrent tasks, you may see more CPU utilization.

## Contributing

Contributions are welcome. Please open an issue to discuss your ideas before making a pull request.

## License

This project is licensed under the ISC License.