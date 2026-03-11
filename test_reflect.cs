using System;
using System.Reflection;
using System.Linq;

namespace Teste
{
    class Program
    {
        static void Main(string[] args)
        {
            var asm = Assembly.LoadFrom(@"c:\Welber\2022\GitHubDesktop\corte_cor_ag\bin\Debug\net8.0\Unimake.Business.DFe.dll");
            var types = asm.GetTypes().Where(t => t.Name.Contains("Parametro") && t.Namespace != null && (t.Namespace.Contains("NFSe") || t.Namespace.Contains("NACIONAL"))).ToList();
            
            foreach(var t in types)
            {
                Console.WriteLine(t.FullName);
            }
        }
    }
}
