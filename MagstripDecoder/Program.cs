using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace MagstripDecoder
{
    static class BitEx
    {
        /// <summary>
        /// Gets an RSPS3000 field from a BitArray - this is a BitArray extension method and should be called using bitArray.GetField
        /// </summary>
        /// <param name="bitArray">The bit array (should be 152 bits long: RSP data + LUL data)</param>
        /// <param name="start">The start position according to RSPS3000 (the lowest bit number e.g. for Route code this should be 135)</param>
        /// <param name="length">The number of bits to read specified in RSPS3000: (e.g. for Route code this should be 11)</param>
        /// <returns>The bitfield read.</returns>
        public static int GetBitField(this BitArray bitArray, int start, int length) 
        {
            var result = Enumerable.Range(bitArray.Length - start - length + 24, length).Aggregate(0, (acc, bit) => (acc << 1) | (bitArray[bit] ? 1 : 0));
            return result;
        }

        /// <summary>
        /// Reverses an entire BitArray end to end.
        /// </summary>
        /// <param name="bitArray">The bit array to be reversed</param>
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

        /// <summary>
        /// Reverse the bits in a byte using the 64 bit and and multiply method
        /// </summary>
        /// <param name="b">the input byte</param>
        /// <returns>the reversed byte</returns>
        public static byte ReverseBits(byte b)
        {
            return (byte)(((b * 0x80200802ul) & 0x0884422110ul) * 0x0101010101ul >> 32);
        }

        /// <summary>
        /// Dumps a bit array to the output window in groups of eight bits
        /// </summary>
        /// <param name="ba"></param>
        public static void DumpBitArray(BitArray ba)
        {
            int count = 0;
            foreach (bool b in ba)
            {
                System.Diagnostics.Debug.Write(b ? "1" : "0");
                if (count++ % 8 == 7)
                {
                    System.Diagnostics.Debug.Write(" ");
                }
            }
            System.Diagnostics.Debug.WriteLine("");
        }

    }

    public class Program
    {
        private static void Decode(List<byte> bytes)
        {
            const string nlcFirstLetters = "0123456789ABCDEFGHIJKLMNOPQRSTUV";

            for (var i = 0; i < bytes.Count; i++)
            {
                bytes[i] = BitEx.ReverseBits(bytes[i]);
            }

            var bitarray = new BitArray(bytes.ToArray());
            BitEx.DumpBitArray(bitarray);

            var originNlcPrefix = nlcFirstLetters[bitarray.GetBitField(171, 5)];
            var originNlcSuffix = bitarray.GetBitField(161, 10);
            var destinationNlcPrefix = nlcFirstLetters[bitarray.GetBitField(156, 5)];
            var destinationNlcSuffix = bitarray.GetBitField(146, 10);
            var rawRouteCode = bitarray.GetBitField(135, 11);
            var rawLTOT = bitarray.GetBitField(125, 10);
            var rawStatus = bitarray.GetBitField(118, 7);
            var tktClass = bitarray.GetBitField(117, 1);
            var dateMode = bitarray.GetBitField(116, 11);
            var ownerShip = bitarray.GetBitField(104, 2);

            var ltot = "" + "ABCDEFGHIJKLMNOPQRSTUVWXYZ"[(rawLTOT & 0x3E0) >> 5];
            ltot += "ABCDEFGHIJKLMNOPQRSTUVWXYZ"[rawLTOT & 0x1F];

            Console.WriteLine($"LTOT is {ltot}");
            if (capriDict.TryGetValue(ltot, out var ttlist))
            {
                foreach (var tt in ttlist)
                {
                    if (ttypeDict.TryGetValue(tt, out var description))
                    {
                        Console.WriteLine($"ticket type: {tt} description: {description}");
                    }
                    else
                    {
                        Console.WriteLine($"ticket type: {tt} - unknown ticket type");
                    }
                }
            }

//            var startValidity = bitarray.GetBitField()
            var endOfValidity = bitarray.GetBitField(33, 9);

            // DateTime basedate = new DateTime(2015, 7, 15);
            DateTime basedate = new DateTime(2016, 6, 12);
            DateTime endDate = basedate + TimeSpan.FromDays(endOfValidity);
            Console.WriteLine($"End Date is {endDate:dd MMM yyyy}");

            Console.WriteLine($"End of validity is {endOfValidity}");
        }

        class TicketType
        {
            public string ThreeLetterCode { get; set; }
            public string TicketTypeName { get; set; }
        }

        static Dictionary<string, List<string>> capriDict = new Dictionary<string, List<string>>();
        static Dictionary<string, string> ttypeDict;

        static void ReadRJISTTY()
        {
            var files = Directory.GetFiles(@"o:\", "*.tty");
            var result = files.Where(name => Regex.Match(name, @"RJFAF[0-9][0-9][0-9]\.tty", RegexOptions.IgnoreCase).Success);
            if (result.Count() != 1)
            {
                throw new Exception($@"There must be exactly one .tty file in the RJIS (O:\) folder - found {result.Count()}");
            }

            using (var streamReader = new StreamReader(result.Single()))
            {
                string line;
                while ((line = streamReader.ReadLine()) != null)
                {
                    if (!string.IsNullOrWhiteSpace(line) && line[0] != '/')
                    {
                        var ticketCode = line.Substring(1, 3);

                        // first two characters of capri-code:
                        var capriCode2 = line.Substring(99, 2);

                        if (capriDict.ContainsKey(capriCode2))
                        {
                            capriDict[capriCode2].Add(ticketCode);
                        }
                        else
                        {
                            capriDict[capriCode2] = new List<string> { ticketCode };
                        }
                    }
                }
            }

            var idmsTTFilename = @"l:\TicketTypesRefData.xml";
            var xdoc = XDocument.Load(idmsTTFilename);

            ttypeDict = (from ticketType in xdoc.Element("TicketTypesReferenceData").Elements("TicketType")
                          select new
                          {
                              code = (string)ticketType.Element("Code"),
                              name = (string)ticketType.Element("OJPDisplayName")
                          }).ToDictionary(x=>x.code, x=>x.name);

            var ttresult = ttypeDict["SOR"];

            System.Diagnostics.Debug.WriteLine("Done");
        }


        // example: 41AA1120D7004198C80008000180044026AB20

        private static void Main(string[] args)
        {   
            try
            {
                ReadRJISTTY();
                var myargs = args;
                if (args.Count() < 1)
                {
                    //myargs = new []{ "A231446000064008CC0E80000000044025670C" };
                    myargs = new []{ "A231446000064008CC0E800000000440256717" };
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
