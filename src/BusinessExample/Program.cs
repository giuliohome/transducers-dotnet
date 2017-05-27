using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TransducersNet;

namespace BusinessExample
{

    class Program
    {
        private static IEnumerable<string> asStream(string[] iterable)
        {
            foreach (var item in iterable)
            {
                yield return item;
            }
        }
        static void Main(string[] args)
        {
            string logContents = @"1a2ddc2, 5f2b932
f1a543f, 5890595
3abe124, bd11537
f1a543f, 5f2b932
f1a543f, bd11537
f1a543f, 5890595
1a2ddc2, bd11537
1a2ddc2, 5890595
3abe124, 5f2b932
f1a543f, 5f2b932
f1a543f, bd11537
f1a543f, 5890595
1a2ddc2, 5f2b932
1a2ddc2, bd11537
1a2ddc2, 5890595";

            var streamOfLines = asStream(logContents.Replace("\r", "").Split('\n'));
            var xform = Composer<string>
                .Map(x => x.Split(new string[] { ", " }, StringSplitOptions.None))
                .Map(sortedTransformation)
                .Filter(x => !x.skip)
                .Compose();

            var output = Transducer
                .Transduce(xform, collectTransaction, new List<Transaction>(), streamOfLines);

            //.ToList(xform, streamOfLines);
            //.Transduce(xform, collectUser, new Dictionary<string, List<string>>(), streamOfLines);

            foreach (var item in output)
            {
                //Console.WriteLine(item.Key +": " +  String.Join("->", item.Value));
                //Console.WriteLine(item.Take(1).FirstOrDefault() + ": " + String.Join("->", item.Skip(1)));
                //Console.WriteLine(item.User + ": " + item.From + " -> " + item.To);

                Console.WriteLine(item.total + ": " + item.From + " -> " + item.To);
            }

            var stats = output.Aggregate(new List<Transaction>(),
                    (acc, val) =>
                    {
                        var curr = acc.FirstOrDefault()?.total ?? 0;
                        if (val.total == curr)
                        {
                            acc.Add(val);
                        }
                        if (val.total > curr)
                        {
                            acc = new List<Transaction>() { val };
                        }
                        return acc;
                    }
                    );
            Console.WriteLine("STATS");
            foreach (var item in stats)
            {
                Console.WriteLine(item.total + ": " + item.From + " -> " + item.To);
            }
            Console.ReadLine();
        }

        private static Func<Dictionary<string, List<string>>, string[], Dictionary<string, List<string>>> collectUser =
            (result, value) =>
            {
                string key = userKey(value);
                List<string> list;
                if (result.ContainsKey(key))
                {
                    list = result[key];
                }
                else
                {
                    list = new List<string>() { "user: " + key };
                    result.Add(key, list);
                }
                list.Add(userVal(value));
                return result;
            };

        private static Func<List<Transaction>, UserTransaction, List<Transaction>> collectTransaction =
            (result, value) =>
            {
                var found = result.FirstOrDefault(t => t.From.Equals(value.From) && t.To.Equals(value.To));
                if (found == null)
                {
                    result.Add(new Transaction() { From = value.From, To = value.To, total = 1 });
                }
                else
                {
                    found.total++;
                }
                return result;
            };

        private static string userKey(string[] value)
        {
            return value[0];
        }
        private static string userVal(string[] value)
        {
            return value[1];
        }
        private static Dictionary<string, List<string>> userMap = new Dictionary<string, List<string>>();
        private static UserTransaction sortedTransformation(string[] val)
        {
            string key = userKey(val);
            List<string> list;
            if (userMap.ContainsKey(key))
            {
                list = userMap[key];
                list.Add(userVal(val));
                var tra = new UserTransaction() { User = list[0], From = list[1], To = list[2] };
                list.RemoveAt(1);
                return tra;
            }
            else
            {
                list = new List<string>() { "user: " + key };
                userMap.Add(key, list);
                list.Add(userVal(val));
                return new UserTransaction() { skip = true };
            }
        }
    }
    public class UserTransaction
    {
        public bool skip;
        public string User;
        public string From;
        public string To;
    }
    public class Transaction
    {
        public int total;
        public string From;
        public string To;
    }
}
