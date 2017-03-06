using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MagstripDecoder
{
    static class BitEx
    {
        public static int GetBits(this BitArray bitArray, int start, int length) => Enumerable.Range(start, length).Aggregate(0, (acc, bit) => (acc << 1) | (bitArray[bit] ? 1 : 0));

        public static void Reverse(this BitArray bitArray)
        {
            var length = bitArray.Length;
            var mid = (length / 2);

            for (var i = 0; i < mid; i++)
            {
                var bit = bitArray[i];
                bitArray[i] = bitArray[length - i - 1];
                bitArray[length - i - 1] = bit;
            }
        }

        //public static void Reverse<T>(this IList<T> array)
        //{
        //    var length = array.Count;
        //    var mid = (length / 2);

        //    for (var i = 0; i < mid; i++)
        //    {
        //        var bit = array[i];
        //        array[i] = array[length - i - 1];
        //        array[length - i - 1] = bit;
        //    }
        //}

    }

    internal class Program
    {

        private static void Decode(List<byte> bytes)
        {
            const string nlcFirstLetters = "0123456789ABCDEFGHIJKLMNOPQRSTUV";

            var bitarray = new BitArray(bytes.ToArray().Reverse().ToArray());
            //bitarray.Reverse();

            var originNlcPrefix = nlcFirstLetters[bitarray.GetBits(147, 5)];
            var originNlcSuffix = bitarray.GetBits(137, 10);
            var destinationNlcPrefix = nlcFirstLetters[bitarray.GetBits(132, 5)];
            var destinationNlcSuffix = bitarray.GetBits(122, 10);
            var rawRouteCode = bitarray.GetBits(111, 11);
            var rawLTOT = bitarray.GetBits(101, 10);
            var rawStatus = bitarray.GetBits(94, 7);
            var tktClass = bitarray.GetBits(93, 1);
            var dateMode = bitarray.GetBits(82, 11);
            var ownerShip = bitarray.GetBits(80, 2);

        }


        // example: 41AA1120D7004198C80008000180044026AB20

        private static void Main(string[] args)
        {   
            try
            {
                var myargs = args;
                if (args.Count() < 1)
                {
                    myargs = new []{ "41AA1120D7004198C80008000180044026AB20" };
                    //throw new Exception("You must supply at least one 19-byte (38 hex digits) string to decode");
                }
                
                // for each argument (we can supply any number of strings to decode)
                foreach (var stripebytes in myargs)
                {
                    if (stripebytes.Length != 38)
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
