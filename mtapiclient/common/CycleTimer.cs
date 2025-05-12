using System;
using System.Threading;

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
        timer.Change(off_time, Timeout.Infinite);
        await WaitForever();        
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
        }
        else
        {
            isOn = true;
            timer.Change(on_time, Timeout.Infinite);
        }
    }
}
