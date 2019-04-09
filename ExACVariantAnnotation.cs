using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Newtonsoft.Json;

namespace TestExAC
{
    public class ExACVariantAnnotation : IVarAnnotation
    {
        #region Static 
        private static string[] csq_order =
        {
            "transcript_ablation",
            "splice_acceptor_variant",
            "splice_donor_variant",
            "stop_gained",
            "frameshift_variant",
            "stop_lost",
            "start_lost",  // new in v81
            "initiator_codon_variant",  // deprecated
            "transcript_amplification",
            "inframe_insertion",
            "inframe_deletion",
            "missense_variant",
            "protein_altering_variant",  // new in v79
            "splice_region_variant",
            "incomplete_terminal_codon_variant",
            "stop_retained_variant",
            "synonymous_variant",
            "coding_sequence_variant",
            "mature_miRNA_variant",
            "5_prime_UTR_variant",
            "3_prime_UTR_variant",
            "non_coding_transcript_exon_variant",
            "non_coding_exon_variant",  // deprecated
            "intron_variant",
            "NMD_transcript_variant",
            "non_coding_transcript_variant",
            "nc_transcript_variant",  // deprecated
            "upstream_gene_variant",
            "downstream_gene_variant",
            "TFBS_ablation",
            "TFBS_amplification",
            "TF_binding_site_variant",
            "regulatory_region_ablation",
            "regulatory_region_amplification",
            "feature_elongation",
            "regulatory_region_variant",
            "feature_truncation",
            "intergenic_variant",
            ""
        };
        #endregion

        #region Member and Properties
        public Dictionary<string, object> consequence { get; set; }
        public List<Dictionary<string, string>> base_coverage { get; set; }
        public Variant variant { get; set; }
        public Dictionary<string, string> metrics { get; set; }
        public bool any_covered { get; set; }
        #endregion

        #region Constructor
        public ExACVariantAnnotation()
        {
        }
        #endregion        

        public string GetAnnotation()
        {
            return this.ToString();
        }

        public override string ToString()
        {
            if (variant == null) // should not happen, return an empty object instead
            {
                return ".\t.\t.\t.\t.\t.\t.";
            }
            string type = FindWorstConsequence(consequence);
            string transcriptdetail = ".";
            if (type != ".")
            {
                string cqGeneString = consequence[type].ToString();
                List<string> transcripts = new List<string>();
                Dictionary<string, List<Dictionary<string, string>>> geneinfo = JsonConvert.DeserializeObject<Dictionary<string, List<Dictionary<string, string>>>>(cqGeneString);
                foreach (KeyValuePair<string, List<Dictionary<string, string>>> pair in geneinfo)
                {
                    string geneID = pair.Key;
                    foreach (Dictionary<string, string> detailAnnotation in pair.Value)
                    {
                        string geneName = detailAnnotation.ContainsKey("SYMBOL") ? detailAnnotation["SYMBOL"] : ".";
                        string transcript = detailAnnotation.ContainsKey("Feature") ? detailAnnotation["Feature"] : ".";
                        transcripts.Add(geneName + " - " + transcript);
                    }
                }
                transcriptdetail = string.Join(";", transcripts.ToArray());
            }
            string rsid = (variant.rsid ==null)? "." : variant.rsid;
            int count = variant.allele_count;
            int num = variant.allele_num;
            double freq = variant.allele_freq;
            int homoCount = variant.hom_count;
            //string genes = variant.genes;
            return type + "\t" + transcriptdetail + "\t" + rsid + "\t" + count + "\t" + num + "\t" + homoCount + "\t" + freq;
        }

        private string FindWorstConsequence(Dictionary<string, object> consequence)
        {
            string type = ".";
            if (consequence != null && consequence.Count > 0)
            {
                int worstIndex = 999;
                string worstcq = null; // default
                foreach (string cq in consequence.Keys)
                {
                    int index = Array.IndexOf(csq_order, cq);
                    if (index < 0) // should not happen
                    {
                        Console.WriteLine("Can not find " + cq + " in internal consequence list, consider update the software ...");
                        if (string.IsNullOrEmpty(worstcq))
                        {
                            worstcq = cq;
                        }
                        continue;
                    }
                    else
                    {
                        if (index < worstIndex)
                        {
                            worstcq = cq;
                            worstIndex = index;
                        }
                    }
                }
                type = worstcq;
            }
            return type;
        }
    }
}