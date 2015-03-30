using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

using Thumbalizr;

namespace Example
{
    class Program
    {
        static void Main(string[] args)
        {
            string key = ""; // Insert your API key
            string url = @"http://thumbalizr.com/"; // enter your URL
            int wait = 10; // number of seconds to wait between 2 screenshots

            Client client = new Client(key);
            Result screenshot = client.Screenshot(url);

            int max = 20;
            int count = 0;

            while (screenshot.Status == Status.Processing && count < max)
            {
                count++;

                Thread.Sleep(wait * 1000);

                screenshot = client.Screenshot(url);
            }

            if (screenshot.Status == Status.Processing)
            {
                Console.WriteLine("Screenshot is not finished");
                return;
            }

            if (screenshot.Status == Status.Error)
            {
                Console.WriteLine("Screenshot failed:");
                Console.WriteLine(screenshot.Error);
                return;
            }

            // Sccreenshot is finished
            string filename = screenshot.Save();

            if (filename == String.Empty)
            {
                Console.WriteLine("Screenshot could NotFiniteNumberException be saved to disk");
                return;
            }

            Console.WriteLine("Screenshot was saved to " + filename);
            
        }
    }
}
