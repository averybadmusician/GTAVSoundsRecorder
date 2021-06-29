using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsonParser
{
    class Program
    {
        const int LOG_EVERY = 1000;
        const int WRITE_EVERY = 5000;
        const string INPUT = "input.json";
        const string OUTPUT = "output.txt";
        const string LOG_DIVIDER = "================================";

        static void Main(string[] args)
        {
            Console.WriteLine(Encoding.UTF8.GetByteCount(new string('1', 350)));
            Console.ReadKey();
            return;

            string output = "";
            int count = 0;
            bool first = true;

            Log("Starting");
            if (File.Exists(OUTPUT)) File.Delete(OUTPUT);
            var data = JsonConvert.DeserializeObject<dynamic>(File.ReadAllText(INPUT));
            Log("Parsed");
            foreach (var voice in data)
            {
                string voiceName = voice.Name.ToString();
                foreach (var speech in voice.Speeches)
                {
                    string speechName = speech.Name.ToString();
                    string speechIndex = speech.Index.ToString();
                    string formatted = $"{voiceName},{speechName},{speechIndex}";
                    if (first) first = false;
                    else output += Environment.NewLine;
                    output += formatted;
                    count++;
                    if (count % LOG_EVERY == 0)
                        Console.WriteLine($"{count}: {formatted}");
                    if (count % WRITE_EVERY == 0)
                    {
                        Log("Writing");
                        File.AppendAllText(OUTPUT, output);
                        output = "";
                    }
                }
            }
            File.AppendAllText(OUTPUT, output);
            Log("Done");
            Console.ReadKey();
        }
        static void Log(string message) => Console.WriteLine(LOG_DIVIDER + "    " + message.ToUpperInvariant() + "    " + LOG_DIVIDER);
    }
}
