using System.Net;
using System;
using System.Text;
using Google.Apis.YouTube.v3;
using Google.Apis.Services;
using System.Reactive.Subjects;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Reactive.Linq;
using Google.Apis.YouTube.v3.Data;
using System.Net.Http.Headers;
using SysProg3.Structures;
using edu.stanford.nlp.classify;
using edu.stanford.nlp.parser.nndep;
using edu.stanford.nlp.ie.crf;
using System.Reactive.Concurrency;
using System.Reactive;

namespace SysProg3;

class Program
{
    public static async Task Main(string[] args)
    {
        Server server = new Server();
        server.Start("http://localhost:8080/");
    }
}
