using System;
using System.Collections.Generic;
using System.IO;

namespace TestExAC
{
    class Program
    {
        static void Main(string[] args)
        {
            /*
            ExACAnnotator.GetVarAnnotation("1", 931393, "G", "T");
            ExACAnnotator.GetVarAnnotation("1-935222-C-A");
            ExACAnnotator.GetVarAnnotation("14-21853913-T-C");
            ExACAnnotator.GetVarAnnotation("22-46615746-A-G");
            ExACAnnotator.GetVarAnnotation("1-6475586-TC-GA");
            List<string> list = new List<string>();
            list.Add("1-931393-G-T");
            list.Add("14-21853913-T-C");
            list.Add("22-46615746-A-G");
            list.Add("1-6475586-TC-GA");
            ExACAnnotator.GetBulkVarAnnotations(list);*/
            if (args.Length != 2)
            {
                Console.WriteLine("Please use:\ndotnet run yourfile.vcf output.txt");
                return;
            }
            string vcfFile = args[0];
            if (!File.Exists(vcfFile))
            {
                Console.WriteLine("Error! Can not find vcf file to annotate: " + vcfFile);
                return;
            }
            string outputFile = args[1];
            IVcfAnnotator annot = new ExACAnnotator();
            VCFFile vf = new VCFFile(vcfFile);
            annot.AnnotateVcfFile(vf, outputFile);
            vf.Close();
        }
    }
}
