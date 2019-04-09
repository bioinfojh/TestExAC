using System.Collections.Generic;

namespace TestExAC
{
    public interface IVcfAnnotator
    {
        IVarAnnotation AnnotateVar(string query);
        Dictionary<string, IVarAnnotation> BulkAnnotateVars(string[] queries);
        void AnnotateVcfFile(VCFFile file, string outputFile);
    }
}