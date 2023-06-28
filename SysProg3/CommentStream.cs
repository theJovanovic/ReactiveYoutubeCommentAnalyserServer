using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using SysProg3.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;

namespace SysProg3;
public class CommentStream : IObservable<CommentsData>
{

    private readonly Subject<CommentsData> CommentSubject;
    private static Dictionary<string, CommentsCache> Cache = new Dictionary<string, CommentsCache>();
    private static AutoResetEvent ClearCacheSignal = new AutoResetEvent(false);
    private static readonly int MAX_NUMBER_OF_ITEMS_IN_CACHE = 3;
    private static readonly object lockObject = new object();

    public CommentStream()
    {
        CommentSubject = new Subject<CommentsData>();

        _ = Task.Run(() =>
        {
            while (true)
            {
                ClearCacheSignal.WaitOne();

                lock (lockObject)
                {
                    var keyWithMinCommentCount = Cache
                        .OrderBy(kv => kv.Value.Comments.Count)
                        .FirstOrDefault().Key;
                    if (keyWithMinCommentCount != null)
                    {
                        var videoTitle = Cache[keyWithMinCommentCount].Title;
                        var commentsNum = Cache[keyWithMinCommentCount].Comments.Count;
                        Cache.Remove(keyWithMinCommentCount);
                        Console.WriteLine($"- Cache item '{videoTitle}' ({commentsNum} comments) was deleted.");
                    }
                }
            }
        });
    }

    public void FetchComments(string clientKey, List<string> videoIDs)
    {
        foreach (var videoID in videoIDs)
        {
            _ = Task.Run(async () =>
            {
                //Console.WriteLine("Processing: " + Thread.CurrentThread.ManagedThreadId);
                CommentsData commentsData = new CommentsData();
                commentsData.Key = clientKey;

                var videoTitle = CommentStreamManager.GetVideoTitle(videoID);
                if (videoTitle == null)
                {
                    Console.WriteLine($"- Video with given ID doesn't exist. (ID: '{videoID}')");
                    commentsData.Title = "Not found";
                    commentsData.Comments = new List<string>();
                    CommentSubject.OnNext(commentsData);
                    return;
                }
              
                lock (lockObject)
                {
                    if (Cache.ContainsKey(videoID))
                    {
                        Console.WriteLine($"- Video data found in cache. (Video name: '{videoTitle}')");
                        commentsData.Title = videoTitle;
                        commentsData.Comments = Cache[videoID].Comments;
                        CommentSubject.OnNext(commentsData);
                        return;
                    }
                }

                var comments = CommentStreamManager.GetVideoComments(videoID);
                await Console.Out.WriteLineAsync($"- Comments succesfuly fetched. (Video name: '{videoTitle}')");

                commentsData.Title = videoTitle;
                commentsData.Comments = comments!;
                CommentSubject.OnNext(commentsData);

                if (videoTitle == "Not found" && comments.Count == 0)
                    return;

                lock (lockObject)
                {
                    if (!Cache.ContainsKey(videoID))
                    {
                        CommentsCache commentsCache = new CommentsCache();
                        commentsCache.Title = videoTitle;
                        commentsCache.Comments = comments;

                        var itemWithLeastComments = Cache
                            .OrderBy(x => x.Value.Comments.Count)
                            .FirstOrDefault();

                        if (Cache.Count == MAX_NUMBER_OF_ITEMS_IN_CACHE - 1)
                        {
                            if (comments.Count > itemWithLeastComments.Value.Comments.Count)
                            {
                                ClearCacheSignal.Set();
                                Cache.Add(videoID, commentsCache);
                                Console.Out.WriteLine($"- Comments are cached. (Video name: '{videoTitle}')");
                            }
                        }
                        else
                        {
                            Cache.Add(videoID, commentsCache);
                            Console.Out.WriteLine($"- Comments are cached. (Video name: '{videoTitle}')");
                        }
                    }
                }
            });
        }
    }

    public IDisposable Subscribe(IObserver<CommentsData> observer)
    {
        //Console.WriteLine("Subscribe: " + Thread.CurrentThread.ManagedThreadId);
        return CommentSubject.Subscribe(observer);
    }
}
