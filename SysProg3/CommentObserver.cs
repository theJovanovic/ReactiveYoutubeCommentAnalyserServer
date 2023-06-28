using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reactive.Concurrency;
using System.Text;
using System.Threading.Tasks;
using SysProg3.Structures;

namespace SysProg3;
public class CommentObserver : IObserver<CommentsData>
{

    public List<CommentsData> CommentsData = new List<CommentsData>();
    private CountdownEvent Counter;

    public CommentObserver(ref CountdownEvent countdownEvent)
    {
        Counter = countdownEvent;
    }

    public void OnNext(CommentsData commentsData)
    {
        Console.WriteLine("OnNext: " + Thread.CurrentThread.ManagedThreadId);
        CommentsData.Add(commentsData);
        Counter.Signal();
    }

    public void OnCompleted()
    {
        throw new NotImplementedException();
    }

    public void OnError(Exception error)
    {
        throw new NotImplementedException();
    }
}
