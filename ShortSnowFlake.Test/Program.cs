using System;

namespace ShortSnowFlake.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            var idWoker = new ShortIdWorker(0);
            var id = idWoker.NextId();
            var translateId = idWoker.TranslateTimespan((long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds);
            Console.WriteLine(id);
            Console.WriteLine(translateId);
            Console.ReadKey();
        }
    }
}
