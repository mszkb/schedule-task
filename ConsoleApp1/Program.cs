using System;
using System.Threading;
using System.Linq;
using System.Collections.Generic;
using Timer = System.Timers.Timer;


// TASK:
// Change the Scheduler class such that it will ensure that SchedulerTask.Calculate() is executed regularly for each SchedulerTask
// with an interval that is as close as possible to the ScheulderTask.Interval.
//
// IMPORTANT: The accuracy of actual execution intervals should degrade gracefully when a *large number of tasks* is specified.
// Try to avoid 100% total CPU utilization (i.e. not all CPU cores should be at 100% all the time).
//
// you can assume:
//  - 8 CPU cores are available
//  - SchedulerTask.Calculate() takes up near 100% CPU utilization of a single core for the duration of the function call

public sealed class SchedulerTask // this class should not be changed
{
    public TimeSpan Interval { get; }
    public int TaskId { get; }
	
    private static int nextId = 0;

    public SchedulerTask()
    {
        this.Interval = TimeSpan.FromSeconds(Random.Shared.Next(1, 10)); // run every 1 to 10 seconds
        this.TaskId = nextId++;
    }

    public void Calculate()
    {
        var startTime = DateTime.UtcNow;
        Console.WriteLine($"{startTime.ToLongTimeString()}.{startTime.Millisecond} - ID: {TaskId} - Interval: {Interval.TotalSeconds}");
		
        var endTime = startTime + TimeSpan.FromMilliseconds(Random.Shared.Next(500, 1500)); // run for at least 0.5, max 1.5 seconds
        while (DateTime.UtcNow < endTime); // keep busy
    }

    static public IEnumerable<SchedulerTask> GetTasks() {
        while (true) {
            yield return new SchedulerTask();
        }
    }
}

public class Scheduler
{
    private List<SchedulerTask> Tasks;
    private List<System.Threading.Timer> Timers;
    
    // Semaphores are a good way to limit the number of concurrent tasks
    private SemaphoreSlim semaphore;
    private readonly int _cpuCores;

    public Scheduler(List<SchedulerTask> tasks, int cpuCores = 8)
    {
        this.Tasks = tasks;
        this.Timers = new List<System.Threading.Timer>();
        this.semaphore = new SemaphoreSlim(cpuCores);
        this._cpuCores = cpuCores;
    }

    public void Start()
    {
        if (_cpuCores < 1) throw new ArgumentException("CPU cores must be at least 1");
        if (Tasks.Count < 1) throw new ArgumentException("At least one task must be specified");
        
        foreach (var task in Tasks)
        {
            // graceful degrade based on the number of tasks and cpu cores
            // the more tasks, the less accurate the intervals
            var interval = task.Interval.TotalMilliseconds / (Tasks.Count / _cpuCores);
            var randomStartTime = Random.Shared.Next(0, (int)interval);
            
            var timer = new System.Threading.Timer(async _ =>
            {
                await semaphore.WaitAsync();

                try
                {
                    var randomInterval = interval + RandomNess(interval);
                    
                    await Task.Delay(TimeSpan.FromMilliseconds(randomInterval));
                    task.Calculate();
                    await Task.Delay(TimeSpan.FromMilliseconds(randomInterval));
                }
                finally
                {
                    // finally is always executed, even if an exception is thrown
                    // this ensures that no semaphore is left locked (Semaphore Leak)
                    semaphore.Release();
                }
            }, null, TimeSpan.FromMilliseconds(randomStartTime), TimeSpan.FromMilliseconds(interval));

            Timers.Add(timer);
        }
    }

    private double RandomNess(double interval)
    {
        return Random.Shared.Next(0, (int)interval + 100);
    }

    /// <summary>
    /// Helper function to release all resources of the timers
    /// </summary>
    public void Stop()
    {
        foreach (var timer in Timers)
        {
            timer.Dispose();
        }
    }
}

public class Program
{
    public static void Main()
    {
        // get 1000 items from the generator
        var tasks = SchedulerTask.GetTasks().Take(1000).ToList();
        var cpuCores = 8;
        
        var scheduler = new Scheduler(tasks, cpuCores);
        scheduler.Start();

        // prevent the program from exiting immediately
        Console.ReadLine();

        scheduler.Stop();
    }
}