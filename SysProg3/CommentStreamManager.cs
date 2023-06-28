using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SysProg3;
public class CommentStreamManager
{
    private static readonly YouTubeService YoutubeService = new YouTubeService(new BaseClientService.Initializer()
    {
        ApiKey = "AIzaSyDRl-ca4DxM_6-P5XB-oqIzVbqeWFEGWzk",
        ApplicationName = "YouTube API NER"
    }
    );

    public static string? GetVideoTitle(string videoId)
    {
        var videoRequest = YoutubeService.Videos.List("snippet");
        videoRequest.Id = videoId;
        var videoResponse = videoRequest.Execute();

        if (videoResponse.Items.Count > 0)
        {
            var video = videoResponse.Items[0];
            var videoTitle = video.Snippet.Title;
            return videoTitle;
        }

        return null;
    }

    public static List<string>? GetVideoComments(string videoId)
    {
        string? nextPageToken = null;
        var comments = new List<string>();
        var commentThreadsRequest = YoutubeService.CommentThreads.List("snippet, replies");
        commentThreadsRequest.VideoId = videoId;

        do
        {
            commentThreadsRequest.PageToken = nextPageToken;
            var commentThreadsResponse = commentThreadsRequest.Execute();

            foreach (var commentThread in commentThreadsResponse.Items)
            {
                var commentSnippet = commentThread.Snippet.TopLevelComment;
                comments.Add(commentSnippet.Snippet.TextOriginal);

                if (commentThread.Replies != null)
                {
                    var replies = commentThread.Replies.Comments;
                    foreach (var reply in replies)
                    {
                        comments.Add(reply.Snippet.TextOriginal);
                    }
                }
            }

            nextPageToken = commentThreadsResponse.NextPageToken;

        } while (!string.IsNullOrEmpty(nextPageToken));

        return comments;
    }
}