using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ValueTaskTest
{
    class Program
    {
        static async ValueTask<int> SampleAsync()
        {
            var r = await new ValueTask<int>(Task.FromResult(123));
            return r;
        }

        static void Main(string[] args)
        {
            var t = SampleAsync();
            var r = t.Result;
        }
    }
}
