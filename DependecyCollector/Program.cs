using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace DependecyCollector
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.Out.WriteLine("Usage - DependecyCollector {assembly}");
                return;
            }
            Task.Run(()=>CopyAllDependeies(args[0])).Wait();
        }

        public static ConcurrentDictionary<Assembly, bool> traveredNodes = new ConcurrentDictionary<Assembly, bool>();

        private static async Task CopyAllDependeies(string path)
        {
            Assembly asm = LoadAssemblyFrom(path);
            Console.Out.WriteLine("start");

            if (asm != null)
            {
                string assmblyName = Path.GetFileNameWithoutExtension(asm.Location);
                Assembly[] dependetAsm= await  GetDependecies(asm,1024);
                if (!Directory.Exists(assmblyName))
                {
                    Directory.CreateDirectory(assmblyName);
                }
                foreach (var assembly in dependetAsm)
                {
                    AsyncCopy(assembly, asm,assmblyName);
                }
            }
        }

        private static async void AsyncCopy(Assembly assembly, Assembly asm, string assmblyName)
        {
            var sourceFileName = assembly.Location;
            var dst = Path.GetFileName(sourceFileName);
            var destFileName = Path.Combine(assmblyName, dst);
            Console.Out.WriteLine(string.Format("Copy {0} to {1}", sourceFileName, destFileName));
            File.Copy(sourceFileName, destFileName,true);
            await Task.FromResult(true);

        }

        private static async Task<Assembly[]> GetDependecies(Assembly asm, int maxLevel= Int32.MaxValue)
        {
            traveredNodes.TryAdd(asm, true);
            HashSet<Assembly> allAssemblies = new HashSet<Assembly>();
            if (maxLevel > 0)
            {
                foreach (AssemblyName referencedAssembly in asm.GetReferencedAssemblies())
                {
                    Assembly depenedency = LoadAssemblyByName(referencedAssembly);
                    if (depenedency != null)
                    {
                        allAssemblies.Add(depenedency);
                        if (!traveredNodes.ContainsKey(depenedency))
                        {
                            Assembly[] subDependecies = await GetDependecies(depenedency, maxLevel - 1);
                            foreach (var subDependecy in subDependecies)
                            {

                                allAssemblies.Add(subDependecy);
                            }
                        }
                        else
                        {
                            Console.WriteLine("allrady loaded depenedency {0}",depenedency.GetName().Name);
                        }
                    }
                }
            }
            return allAssemblies.ToArray();

        }

        private static Assembly LoadAssemblyByName(AssemblyName name)
        {
            try
            {
                return Assembly.Load(name);
            }
            catch (Exception e)
            {
                Console.WriteLine(string.Format("Failed to load {0} {1}", name, e.Message));
                return null;
            }
        } 
        
        private static Assembly LoadAssemblyFrom(string s)
        {
            try
            {
                return Assembly.LoadFrom(s);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return null;
            }
        }
    }
}