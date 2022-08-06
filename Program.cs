using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace MissingDependencies
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            var dir = args[0];
            Directory.SetCurrentDirectory(dir);

            var allFiles = Directory.GetFiles(dir, "*.dll");

            var dotnetAssemblies = allFiles
                .Select(AnalyzeFile)
                .Where(f => f != null)
                .ToList();

            // A dictionary of the files in "my" folder for quick lookup
            var myfiles = dotnetAssemblies.Select(i => i.Name).AsEnumerable().ToDictionary(i => i, i => 0);

            // A list of DLLs we can find, but not in our folder (i.e. "system DLLs") that we can ignore
            var systemDlls = new List<string>();

            var notFoundDlls = new List<string>();


            foreach (var assembly in dotnetAssemblies)
            {
                foreach (var referencedAssembly in assembly.Refs)
                {
                    if (notFoundDlls.Contains(referencedAssembly))
                        continue;

                    if (systemDlls.Contains(referencedAssembly))
                        continue;

                    if (myfiles.ContainsKey(referencedAssembly))
                    {
                        myfiles[referencedAssembly]++;
                    }
                    else
                    {
                        try
                        {
                            var x = Assembly.ReflectionOnlyLoad(referencedAssembly);
                            if (x != null)
                            {
                                systemDlls.Add(referencedAssembly);
                            }
                            else
                            {
                            }
                        }
                        catch (FileNotFoundException)

                        {
                            notFoundDlls.Add(referencedAssembly);
                        }
                    }
                }
            }

            notFoundDlls.Sort();

            var notthere = new List<string>();
            var versionmismatch = new List<string>();


            foreach (var d in notFoundDlls)
            {
                var f = ExtractFilenamefromReference(d);
                if (File.Exists(f + ".dll"))
                {
                    versionmismatch.Add(d);
                }
                else
                {
                    notthere.Add(d);
                }
            }

            Console.WriteLine("Version mismatch");
            foreach (var ss in versionmismatch)
            {
                Console.WriteLine(ss);
            }

            Console.WriteLine("Missing assembly");
            foreach (var ss in notthere)
            {
                Console.WriteLine(ss);
            }
        }


        private static string ExtractFilenamefromReference(string r)
        {
            var z = r.Split(',');
            return z[0];
        }

        private static MyAssemblyInfo AnalyzeFile(string path)
        {
            try
            {
                var a = Assembly.ReflectionOnlyLoadFrom(path);
                var l = new MyAssemblyInfo
                {
                    Name = a.FullName,
                    Refs = a.GetReferencedAssemblies().Select(x => x.FullName).ToList()
                };
                return l;
            }
            catch (BadImageFormatException)
            {
                return null;
            }
        }
    }

    internal class MyAssemblyInfo
    {
        public string Name { get; set; }

        public IEnumerable<string> Refs { get; set; }
    }
}