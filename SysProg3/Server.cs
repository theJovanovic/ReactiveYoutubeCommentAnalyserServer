using edu.stanford.nlp.ie.crf;
using SysProg3.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SysProg3;
internal class Server
{

    private readonly CommentStream CommentStream = new CommentStream();
    private CRFClassifier Classifier;

    public void Start(string domain) //http://localhost:8080/
    {
        Console.WriteLine("Loading NER models. Plaese wait.");
        LoadNerModels();

        HttpListener listener = new HttpListener();
        listener.Prefixes.Add(domain);
        listener.Start();
        Console.WriteLine($"\nListening for requests on: {domain}");

        while (true)
        {
            HttpListenerContext context = listener.GetContext();

            if (context.Request.HttpMethod != "GET")
            {
                context.Response.StatusCode = 405;
                SendResponse(context.Response, "Error 405 - Method Not Allowed");
            }

            if (context.Request.RawUrl == "/favicon.ico")
                continue;

            _ = Task.Run(() => { _ = ProcessRequest(context); });
        }
    }

    private void LoadNerModels()
    {
        var jarRoot = @"..\..\..\stanford-ner-2018-10-16";
        var classifiersDirectory = jarRoot + @"\classifiers";
        Classifier = CRFClassifier.getClassifierNoExceptions(
            classifiersDirectory + @"\english.muc.7class.distsim.crf.ser.gz");
    }
 
    private async Task ProcessRequest(HttpListenerContext context)
    {

        await Console.Out.WriteLineAsync($"\nTask started.");

        string query = context.Request.RawUrl;

        if (query == "/")
        {
            await Console.Out.WriteLineAsync("- Empty query string.");
            context.Response.StatusCode = 400;
            SendResponse(context.Response, "Error: Empty query string");
            return;
        }

        List<string> videoIDs = query.Substring(1).Split("&").ToList();
        CountdownEvent counter = new CountdownEvent(videoIDs.Count);

        string clientKey = GenerateRandomString(10);
        CommentObserver clientObserver = new CommentObserver(ref counter);

        var subscription = CommentStream
            .Where(commentsData => commentsData.Key == clientKey)
            .Select(commentsData =>
            {
                //Console.WriteLine("Select: " + Thread.CurrentThread.ManagedThreadId);
                List<string> classifiedComments = new List<string>();
                for (int i = 0; i < commentsData.Comments.Count; i++)
                {
                    classifiedComments.Add(Classifier.classifyToString(commentsData.Comments[i]));
                }
                CommentsData classifiedData = new CommentsData();
                classifiedData.Key = commentsData.Key;
                classifiedData.Title = commentsData.Title;
                classifiedData.Comments = classifiedComments;
                return classifiedData;
            })
            .SubscribeOn(Scheduler.Default)
            .ObserveOn(Scheduler.Default)
            .Subscribe(clientObserver);
            
        CommentStream.FetchComments(clientKey, videoIDs);

        counter.Wait();

        string responseString = GenerateResponseString(clientObserver.CommentsData);
        context.Response.StatusCode = 200;
        SendResponse(context.Response, responseString);

        subscription.Dispose();
    }

    private string GenerateRandomString(int length)
    {
        const string allowedCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();

        var result = new StringBuilder(length);
        for (int i = 0; i < length; i++)
        {
            int index = random.Next(0, allowedCharacters.Length);
            result.Append(allowedCharacters[index]);
        }

        return result.ToString();
    }

    private string GenerateResponseString(List<CommentsData> commentsData)
    {
        StringBuilder responseString = new StringBuilder();
        foreach (var video in commentsData)
        {
            responseString.Append(
                        $"Naziv videa: {video.Title}\n" +
                        $"Broj komentara: {video.Comments.Count}\n");
            for (int i = 0; i < video.Comments.Count; i++)
            {
                responseString.Append($"\t{i + 1} - {video.Comments[i]}\n");
            }
            responseString.Append("\n\n\n");
        }
        return responseString.ToString();
    }

    private void SendResponse(HttpListenerResponse response, string responseString)
    {
        byte[] buffer = Encoding.UTF8.GetBytes(responseString);
        response.ContentLength64 = buffer.Length;
        response.ContentType = "text/plain; charset=utf-8";
        Stream output = response.OutputStream;
        output.Write(buffer, 0, buffer.Length);
        output.Close();
    }
}
