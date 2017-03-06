using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MagstripDecoder
{
    internal class Program
    {
        private static void Decode(List<Byte> bytes)
        {

        }


        // example: 41AA1120D7004198C80008000180044026AB20

        private static void Main(string[] args)
        {   
            try
            {
                if (args.Count() < 1)
                {
                    throw new Exception("You must supply at least one 19-byte (38 hex digits) string to decode");
                }
                
                // for each argument (we can supply any number of strings to decode)
                foreach (var stripebytes in args)
                {
                    if (stripebytes.Length != 19)
                    {
                        throw new Exception("Each string must be 38 characters long");
                    }
                    var byteArray = Enumerable.Range(0, stripebytes.Length / 2)
                         .Select(x => Convert.ToByte(stripebytes.Substring(x * 2, 2), 16))
                         .ToList();
                    Decode(byteArray);
                }

            }
            catch (Exception ex)
            {
                var codeBase = System.Reflection.Assembly.GetExecutingAssembly().CodeBase;
                var progname = Path.GetFileNameWithoutExtension(codeBase);
                Console.Error.WriteLine(progname + ": Error: " + ex.Message);
            }

        }
    }
}
