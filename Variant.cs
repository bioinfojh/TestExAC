using System.Collections.Generic;

namespace TestExAC
{
    public class Variant
    {
        public int allele_count { get; set; }
        public int pos { get; set; }
        public List<List<List<double>>> genotype_depths { get; set; }
        public Dictionary<string, string> quality_metrics { get; set; }
        public string variant_id { get; set; }
        public string alt { get; set; }
        public Dictionary<string, int> pop_homs { get; set; }
        public Dictionary<string, int> pop_acs { get; set; }
        public double allele_freq { get; set; }
        public List<List<List<double>>> genotype_qualities { get; set; }
        public List<Dictionary<string, string>> vep_annotations { get; set; }
        public string rsid { get; set; }
        public string @ref { get; set; }
        public long xpos { get; set; }
        public double site_quality { get; set; }
        public List<string> orig_alt_alleles { get; set; }
        public List<string> genes { get; set; }
        public int hom_count { get; set; }
        public string chrom { get; set; }
        public long xstart { get; set; }
        public int allele_num { get; set; }
        public Dictionary<string, int> pop_ans { get; set; }
        public string filter { get; set; }
        public long xstop { get; set; }
        public List<string> transcripts { get; set; }
    }
}