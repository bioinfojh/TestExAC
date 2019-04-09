using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Newtonsoft.Json;

namespace TestExAC
{
    public class ExACAnnotator : IVcfAnnotator
    {
        #region Members and Properties
        public static readonly string BaseBulkVarUrl = "http://exac.hms.harvard.edu/rest/bulk/variant";
        public static readonly string BaseVarUrl = "http://exac.hms.harvard.edu/rest/variant/";
        public static readonly string HttpVarUrl = "http://exac.broadinstitute.org/variant/";

        private bool useBulkMode = true;
        public int BulkLineLimit { get; set; }

        public bool BulkAnnotate
        {
            get { return this.useBulkMode; }
            set { this.useBulkMode = value; }
        }
        #endregion

        #region Constructor
        public ExACAnnotator() : this(1000)
        {
        }

        public ExACAnnotator(int limit)
        {
            BulkLineLimit = limit;
        }
        #endregion

        #region Static Functions
        public static ExACVariantAnnotation GetVarAnnotation(string chr, int start, string refAllele, string altAllele)
        {
            string queryContent = chr + "-" + start + "-" + refAllele + "-" + altAllele;
            return GetVarAnnotation(queryContent);
        }

        public static ExACVariantAnnotation GetVarAnnotation(string queryContent)
        {
            string queryString = BaseVarUrl + queryContent;
            using (var client = new WebClient())
            {
                string jsonString = client.DownloadString(queryString);
                ExACVariantAnnotation annot = JsonConvert.DeserializeObject<ExACVariantAnnotation>(jsonString);
                //dynamic usr = serializerDeserializeObject(jason);
                //SomeModel model = serializer.Deserialize<SomeModel>(json);
                // TODO: do something with the model
                return annot;
            }
        }

        public static Dictionary<string, ExACVariantAnnotation> GetBulkVarAnnotations(string[] queryContents)
        {
            //construct json post content
            string json = JsonConvert.SerializeObject(queryContents);
            string jsonResult = PostJson(BaseBulkVarUrl, json);
            Dictionary<string, ExACVariantAnnotation> annots = JsonConvert.DeserializeObject<Dictionary<string, ExACVariantAnnotation>>(jsonResult);
            return annots;
        }

        public static string PostJson(string url, string jsonPostString)
        {
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "POST";
            try
            {
                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    //string json = "{\"user\":\"test\"," +"\"password\":\"bla\"}";
                    streamWriter.Write(jsonPostString);
                    streamWriter.Flush();
                    streamWriter.Close();
                }

                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    return streamReader.ReadToEnd();
                }
            }
            catch (WebException ex)
            {
                WebResponse errorResponse = ex.Response;
                using (Stream responseStream = errorResponse.GetResponseStream())
                {
                    StreamReader reader = new StreamReader(responseStream, System.Text.Encoding.GetEncoding("utf-8"));
                    String errorText = reader.ReadToEnd();
                    // log errorText if needed
                }
                throw ex;
            }
        }
        #endregion

        public IVarAnnotation AnnotateVar(string query)
        {
            return ExACAnnotator.GetVarAnnotation(query);
        }

        public Dictionary<string, IVarAnnotation> BulkAnnotateVars(string[] queries)
        {
            Dictionary<string, ExACVariantAnnotation> dict = ExACAnnotator.GetBulkVarAnnotations(queries);
            Dictionary<string, IVarAnnotation> outputs = new Dictionary<string, IVarAnnotation>();
            foreach (KeyValuePair<string, ExACVariantAnnotation> pair in dict)
            {
                outputs[pair.Key] = pair.Value;
            }
            return outputs;
        }

        public void AnnotateVcfFile(VCFFile file, string outputFile)
        {
            string headerLine = "#CHROM\tPOS\tID\tREF\tALT\tTYPE\tDP\tRO\tAO\tARO\t" +
        "ExAC_Consequence\tExAC_Transcripts\tExAC_rsID\tExAC_AlleleCount\tExAC_AlleleNumber\tExAC_HomozygousNumber\tExAC_AlleleFrequence\tExACBrowser_Link";
            using (StreamWriter writer = new StreamWriter(outputFile))
            {
                writer.WriteLine(headerLine);
                if (useBulkMode)
                {
                    file.Traverse(BulkLineLimit, delegate (string[] lines)
                    {
                        try
                        {
                            List<string> queries = new List<string>();
                            Dictionary<string, string> outputinfo1 = new Dictionary<string, string>();
                            foreach (string line in lines)
                            {
                                string[] vcfvalues = line.Split('\t');
                                string chrom = vcfvalues[0].Trim();
                                string pos = vcfvalues[1].Trim();
                                string id = vcfvalues[2].Trim();
                                string refAllele = vcfvalues[3].Trim();
                                string altAllele = vcfvalues[4].Trim();
                                string info = vcfvalues[7].Trim();
                                Dictionary<string, string> dict = VCFFile.ParseInfoLine(info);
                                string type = dict.ContainsKey("TYPE") ? dict["TYPE"] : ".";
                                string dp = dict.ContainsKey("DP") ? dict["DP"] : ".";
                                string ro = dict.ContainsKey("RO") ? dict["RO"] : ".";
                                string ao = dict.ContainsKey("AO") ? dict["AO"] : ".";

                                string[] alts = altAllele.Split(',');
                                string[] aos = ao.Split(',');
                                string[] altTypes = type.Split(',');
                                float[] aros = new float[alts.Length];
                                for (int i = 0; i < alts.Length; i++)
                                {
                                    string altAl = alts[i];
                                    float aro = float.Parse(aos[i]) / float.Parse(ro);

                                    bool modifyAllele = false;
                                    string newRefAl = refAllele;
                                    string newAltAl = altAl;
                                    int movepos = 0;
                                    if (refAllele.Length > 1 && altAl.Length > 1)
                                    {
                                        modifyAllele = ModifyAlleleForQuery(refAllele, altAl, out newRefAl, out newAltAl, out movepos);
                                    }
                                    int origpos = int.Parse(pos);
                                    int newpos = (movepos > 0)? origpos+movepos : origpos;
                                    string query = (modifyAllele) ? chrom + "-" + newpos + "-" + newRefAl + "-" + newAltAl : chrom + "-" + pos + "-" + refAllele + "-" + altAl;
                                    
                                    queries.Add(query);
                                    string info1 = chrom + "\t" + pos + "\t" + id + "\t" + refAllele + "\t" + altAl
                                    + "\t" + altTypes[i] + "\t" + dp + "\t" + ro + "\t" + aos[i] + "\t" + aro;
                                    outputinfo1[query] = info1;
                                }
                            }
                            Dictionary<string, IVarAnnotation> annots = this.BulkAnnotateVars(queries.ToArray());

                            foreach (KeyValuePair<string, string> pair in outputinfo1)
                            {
                                if (annots.ContainsKey(pair.Key))
                                {
                                    IVarAnnotation annot = annots[pair.Key];
                                    if (annot == null)
                                    {
                                        Console.WriteLine("annotation is empty for: " + pair.Key);
                                        continue;
                                    }
                                    else
                                    {
                                        string httpLink = HttpVarUrl + pair.Key;
                                        writer.WriteLine(pair.Value + "\t" + annot.ToString() + "\t" + httpLink);
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("annotation is empty for: " + pair.Key);
                                    continue;

                                }
                            }
                            return false;
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                        }
                        return true;
                    });
                }
                else
                {
                    file.Traverse(delegate (string line, long index)
                    {
                        try
                        {
                            string[] vcfvalues = line.Split('\t');
                            string chrom = vcfvalues[0].Trim();
                            string pos = vcfvalues[1].Trim();
                            string id = vcfvalues[2].Trim();
                            string refAllele = vcfvalues[3].Trim();
                            string altAllele = vcfvalues[4].Trim();

                            string info = vcfvalues[7].Trim();
                            Dictionary<string, string> dict = VCFFile.ParseInfoLine(info);
                            string type = dict.ContainsKey("TYPE") ? dict["TYPE"] : ".";
                            string dp = dict.ContainsKey("DP") ? dict["DP"] : ".";
                            string ro = dict.ContainsKey("RO") ? dict["RO"] : ".";
                            string ao = dict.ContainsKey("AO") ? dict["AO"] : ".";


                            if (altAllele.Contains(','))
                            {
                                string[] alts = altAllele.Split(',');
                                string[] aos = ao.Split(',');
                                string[] altTypes = type.Split(',');
                                for (int i = 0; i < alts.Length; i++)
                                {
                                    string altAl = alts[i];
                                    bool modifyAllele = false;
                                    string newRefAl = refAllele;
                                    string newAltAl = altAl;
                                    int movepos = 0;
                                    if (refAllele.Length > 1 && altAl.Length > 1)
                                    {
                                        modifyAllele = ModifyAlleleForQuery(refAllele, altAl, out newRefAl, out newAltAl, out movepos);
                                    }
                                    int origpos = int.Parse(pos);
                                    int newpos = (movepos > 0)? origpos + movepos : origpos;
                                    string query = (modifyAllele) ? chrom + "-" + newpos + "-" + newRefAl + "-" + newAltAl : chrom + "-" + pos + "-" + refAllele + "-" + altAl;
                                    IVarAnnotation annot = this.AnnotateVar(query);
                                    if (annot == null)
                                    {
                                        continue;
                                    }
                                    else
                                    {
                                        float aro = float.Parse(aos[i]) / float.Parse(ro);
                                        string info1 = chrom + "\t" + pos + "\t" + id + "\t" + refAllele + "\t" + altAl
                                        + "\t" + altTypes[i] + "\t" + dp + "\t" + ro + "\t" + aos[i] + "\t" + aro;
                                        string httpLink = HttpVarUrl + query;
                                        writer.WriteLine(info1 + "\t" + annot.ToString() + "\t" + httpLink);
                                    }
                                }
                            }
                            else
                            {
                                float aro = float.Parse(ao) / float.Parse(ro);
                                bool modifyAllele = false;
                                string newRefAl = refAllele;
                                string newAltAl = altAllele;
                                int movepos =0;
                                if (refAllele.Length > 1 && altAllele.Length > 1)
                                {
                                    modifyAllele = ModifyAlleleForQuery(refAllele, altAllele, out newRefAl, out newAltAl, out movepos);
                                }
                                int origpos = int.Parse(pos);
                                int newpos = (movepos > 0)? origpos + movepos : origpos;
                                string query = (modifyAllele) ? chrom + "-" + newpos + "-" + newRefAl + "-" + newAltAl : chrom + "-" + pos + "-" + refAllele + "-" + altAllele;
                                IVarAnnotation annot = this.AnnotateVar(query);
                                string info1 = chrom + "\t" + pos + "\t" + id + "\t" + refAllele + "\t" + altAllele
                                        + "\t" + type + "\t" + dp + "\t" + ro + "\t" + ao + "\t" + aro;
                                string httpLink = HttpVarUrl + query;
                                writer.WriteLine(info1 + "\t" + annot.ToString() + "\t" + httpLink);
                            }
                            return false;
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                        }
                        return true;
                    });
                }
            }
        }

        /// <summary>
        /// remove the suffix and prefix that are the same between reference allele and alternative allele
        /// </summary>
        /// <param name="refAllele"></param>
        /// <param name="altAl"></param>
        /// <param name="newRefAl"></param>
        /// <param name="newAltAl"></param>
        /// <returns></returns>
        private bool ModifyAlleleForQuery(string refAllele, string altAl, out string newRefAl, out string newAltAl, out int movepos)
        {
            bool modify = false;
            movepos = 0;
            //remove suffix
            newRefAl = refAllele;
            newAltAl = altAl;
            int limit = Math.Min(refAllele.Length, altAl.Length);
            int count = 0;
            for (int i = 1; i < limit; i++) //leave at least 1 nt
            {
                if (refAllele[refAllele.Length - i] == altAl[altAl.Length - i])
                {
                    count++;
                    continue;
                }
                else
                    break;
            }
            if (count > 0)
            {
                newRefAl = refAllele.Substring(0, refAllele.Length - count);
                newAltAl = altAl.Substring(0, altAl.Length - count);
                modify = true;
            }
            // remove prefix
            if(newRefAl.Length > 1 && newAltAl.Length >1) 
            {
                limit = Math.Min(newRefAl.Length, newAltAl.Length)-1;
                count = 0;
                for (int i = 0; i < limit; i++) //leave at least 1 nt
                {
                    if (newRefAl[i] == newAltAl[i])
                    {
                        count++;
                        continue;
                    }
                    else
                        break;
                }
                if (count > 0)
                {
                    newRefAl = newRefAl.Substring(count, newRefAl.Length - count);
                    newAltAl = newAltAl.Substring(count, newAltAl.Length - count);
                    movepos= count;
                    modify = true;
                }
            }
            return modify;
        }
    }
}