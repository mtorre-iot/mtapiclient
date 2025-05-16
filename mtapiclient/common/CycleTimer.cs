using System;
using System.Threading;
using mtapiclient.classes;

namespace mtapiclient.common;
public class CycleTimer
{
    private Timer timer {get; set;}
    private int on_time {get; set;}
    private int off_time {get; set;}
    public bool isOn {get; set;} = false;

    public CycleTimer ()
    {
        timer = new Timer(OnTimedEvent);
    }
    public async void Start(int on_time, int off_time)
    {
        this.on_time = on_time;
        this.off_time = off_time;
        if (on_time > 0 && off_time > 0)
        {
            timer.Change(off_time, Timeout.Infinite);
            Logger.write(logLevel.warning, $"Initial OFF time: {off_time} milliseconds");
            await WaitForever();
        }
        else
        {
            isOn = true; // Force it on
            Logger.write(logLevel.warning, "$CycleTimer is disabled. Thread Ended.");
        }
    }

    private Task WaitForever()
    {
        var tcs = new TaskCompletionSource<bool>();
        return tcs.Task;
    }

    private void OnTimedEvent(Object state)
    {
        if (isOn)
        {
            isOn = false;
            timer.Change(off_time, Timeout.Infinite); 
            Logger.write(logLevel.warning, $"OFF time: {off_time} milliseconds");
        }
        else
        {
            isOn = true;
            timer.Change(on_time, Timeout.Infinite);
            Logger.write(logLevel.warning, $"ON time: {on_time} milliseconds");
        }
    }
}
