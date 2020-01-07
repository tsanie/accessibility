using System;
using System.IO;
using System.Text;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PackTool
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: <PackTool.exe> <File|Directory>");
                return;
            }

            var path = args[0];

            // pack
            if (Directory.Exists(path))
            {
                path = new DirectoryInfo(path).FullName;
                var files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories)
                    .Where(p => Path.GetFileName(p) != ".DS_Store")
                    //.OrderBy(p => p.Substring(path.Length + 1).IndexOf('/') > 0 ? "_" : p)
                    .ToArray();
                var outfile = path + ".pack";
                if (File.Exists(outfile))
                {
                    File.Delete(outfile);
                }
                using (var stream = File.Create(outfile))
                {
                    var array = new object[files.Length];
                    for (var i = 0; i < files.Length; i++)
                    {
                        var file = files[i];
                        var name = file.Substring(path.Length + 1);
                        var start = stream.Position;
                        var data = File.ReadAllBytes(file);
                        var crc32 = Force.Crc32.Crc32Algorithm.Compute(data);

                        stream.Write(data, 0, data.Length);

                        array[i] = new
                        {
                            bundleName = name,
                            nStart = start,
                            nLength = data.Length,
                            hashKey = crc32.ToString()
                        };
                    }
                    var obj = new
                    {
                        lstPackNode = array
                    };
                    var text = JsonConvert.SerializeObject(obj);
                    Console.WriteLine(text);
                    var d = Encoding.UTF8.GetBytes(text);
                    stream.Write(d);
                    stream.WriteInt(d.Length);
                    stream.Flush();
                }

                Console.ReadKey(true);
            }

            // extract
            else if (File.Exists(path))
            {
                using (var stream = File.OpenRead(path))
                {
                    stream.Seek(-4, SeekOrigin.End);
                    int length = stream.ReadInt();

                    stream.Seek(-4 - length, SeekOrigin.End);
                    var data = stream.ReadBytes(length);
                    var json = (JToken)JsonConvert.DeserializeObject(Encoding.UTF8.GetString(data));
                    var list = (JArray)json["lstPackNode"];

                    var directory = Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path));
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                    for (var i = 0; i < list.Count; i++)
                    {
                        var node = list[i];
                        var name = (string)node["bundleName"];
                        Console.Write(name);
                        var f = Path.Combine(directory, name);
                        var start = (long)node["nStart"];
                        var l = (int)node["nLength"];

                        var dir = Path.GetDirectoryName(f);
                        if (!Directory.Exists(dir))
                        {
                            Directory.CreateDirectory(dir);
                        }
                        stream.Seek(start, SeekOrigin.Begin);
                        var d = stream.ReadBytes(l);
                        File.WriteAllBytes(f, d);
                        Console.WriteLine(" ... extracted.");
                    }
                }

                Console.ReadKey(true);
            }

            // error
            else
            {
                Console.WriteLine("Invalid file/directory.");
            }
        }
    }

    static class Extension
    {
        public static int ReadInt(this Stream stream)
        {
            var data = new byte[4];
            stream.Read(data, 0, 4);
            return BitConverter.ToInt32(data);
        }

        public static void WriteInt(this Stream stream, int value)
        {
            var data = BitConverter.GetBytes(value);
            stream.Write(data, 0, data.Length);
        }

        public static byte[] ReadBytes(this Stream stream, int length)
        {
            var data = new byte[length];
            stream.Read(data, 0, length);
            return data;
        }
    }
}
