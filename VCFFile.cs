using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;

namespace TestExAC
{
    public class VCFFile
    {
        #region Members and Properties
        private string name;
        private string filename;
        private bool gzip = false;
        private bool writeMode = false;
        private Stream stream;
        private StreamWriter vcfWriter;
        private StreamReader vcfReader;
        private long firstLinePosition;
        private VcfHeader header;

        public VcfHeader HeaderLine
        {
            get { return header; }
        }

        #endregion

        #region Constructor
        public VCFFile(string file)
        {
            this.filename = file;
            this.name = Path.GetFileNameWithoutExtension(file);
            if (file.EndsWith(".gz", StringComparison.CurrentCultureIgnoreCase))
            {
                this.gzip = true;
            }
            else
                this.gzip = false;
            this.Load(file);
        }

        public VCFFile(string file, bool isGzipFile) : this(file, isGzipFile, false)
        {
        }

        public VCFFile(string file, bool isGzipFile, bool writeMode)
        {
            this.filename = file;
            this.name = Path.GetFileNameWithoutExtension(file);
            this.gzip = isGzipFile;
            this.writeMode = writeMode;

            if (!this.writeMode)
                this.Load(file);
            else
            {
                this.PreWrite(file); // prepare to generate a new VCF
            }
        }

        #endregion

        private bool Load(string fileName)
        {
            if (this.gzip) //I should use BGZF stream for compressed vcf.gz, use standard gzip reader here for demo purpose
            {
                this.stream = new GZipStream(File.OpenRead(fileName), CompressionMode.Decompress);
            }
            else
                this.stream = File.OpenRead(fileName);
            this.vcfReader = new StreamReader(this.stream);
            //read header of vcf file
            header = InternalReadHeader(this.vcfReader);
            this.firstLinePosition = this.stream.Position;
            return true;
        }

        private bool PreWrite(string outputFile)
        {
            throw new NotImplementedException("VCF write function to be implemented");
        }

        private static VcfHeader InternalReadHeader(StreamReader reader)
        {
            VcfHeader header = null;
            string cline = null;
            while ((cline = reader.ReadLine()) != null)
            {
                string line = cline.Trim();
                if (line.StartsWith("##"))
                {
                    if (line.StartsWith("##INFO=<"))
                    {
                        //parse info line, to be implemented
                    }
                    else if (line.StartsWith("##FORMAT=<"))
                    {
                        //parse format line, to be implemented
                    }
                    else if (line.StartsWith("##FILTER=<"))
                    {
                        //parse filter line, to be implemented
                    }
                    else if (line.StartsWith("##ALT=<"))
                    {
                        //parse alt line, to be implemented 
                    }
                    else
                    {
                        //parse other meta information line, to be implemented                        
                    }
                }
                else if (line.StartsWith("#CHROM"))
                {
                    header = new VcfHeader(line.Split('\t'));
                    break;
                }
                else if (header == null && !line.StartsWith("#"))
                {
                    throw new Exception("Invalid VCF file. Header was not found before the data lines.");
                }
                else
                    break;
            }

            if (header == null)
            {
                throw new Exception("Invalid VCF file: header line was not found.");
            }

            return header;
        }

        public void Close()
        {
            if (this.vcfReader != null)
            {
                this.vcfReader.Close();
                this.vcfReader = null;
            }
            if (this.vcfWriter != null)
            {
                this.vcfWriter.Close();
                this.vcfWriter = null;
            }
            if (this.stream != null)
            {
                stream.Close();
                this.stream = null;
            }
        }

        public void Traverse(VcfLineAction action)
        {
            lock (this)
            {
                stream.Position = this.firstLinePosition;
                long index = 0;
                string row = null;
                while ((row = this.vcfReader.ReadLine()) != null)
                {
                    if (action(row, index++))
                        break;

                }
            }
        }

        public void Traverse(int multiLineLimit, VcfMultiLineAction action)
        {
            lock (this)
            {
                stream.Position = this.firstLinePosition;
                long index = 0;
                string row = null;
                List<string> lines = new List<string>();
                while ((row = this.vcfReader.ReadLine()) != null)
                {
                    lines.Add(row);
                    index++;
                    if (lines.Count < multiLineLimit)
                    {
                        continue;
                    }
                    if (action(lines.ToArray()))
                        break;
                    lines.Clear();
                }
                if (lines.Count > 0)
                {
                    action(lines.ToArray());
                }
            }
        }

        public static Dictionary<string, string> ParseInfoLine(string info)
        {
            string[] values = info.Split(';');
            Dictionary<string, string> output = new Dictionary<string, string>();
            foreach (string infovalue in values)
            {
                string[] temp = infovalue.Split('=');
                if (temp.Length != 2)
                {
                    throw new Exception("Fail to parse info field: " + infovalue);
                }
                output[temp[0]] = temp[1];
            }
            return output;
        }
    }

    public class VcfHeader
    {
        #region Static members
        public static VcfHeader Parse(string line)
        {
            string[] sv = line.Split('\t');
            for (int i = 0; i < sv.Length; i++)
            {
                if (sv[i] == null)
                {
                    throw new System.Exception("VCF header line is not in right format");
                }
                sv[i] = sv[i].Trim();
            }
            return new VcfHeader(sv);
        }

        public static readonly string[] FixedColumns = { "#CHROM", "POS", "ID", "REF", "ALT", "QUAL", "Filter", "INFO", "FORMAT" };
        #endregion

        #region Members and properties
        private string[] fields_;
        private bool hasFormat_;
        private int sampleCount_;

        /// <summary>
        /// Get field count
        /// </summary>
        public int FieldCount
        {
            get { return fields_.Length; }
        }

        /// <summary>
        /// Has FORMAT field?
        /// </summary>
        public bool HasFormat
        {
            get { return hasFormat_; }
        }

        /// <summary>
        /// Get sample count (can be 0)
        /// </summary>
        public int SampleCount
        {
            get { return sampleCount_; }
        }

        /// <summary>
        /// Get/set field 
        /// </summary>
        /// <param name="fieldIndex">Field index</param>
        /// <returns>Field</returns>
        public string this[int fieldIndex]
        {
            get { return this.fields_[fieldIndex]; }
            set { this.fields_[fieldIndex] = value; }
        }
        #endregion

        #region Constructors
        public VcfHeader(string[] fields)
        {
            if (fields.Length < 8)
                throw new Exception("VCF header line must contain at least 8 fixed columns. ");
            if (fields[0] != "#CHROM")
                throw new Exception("Invalid VCF header: the first field must be #CHROM.");
            if (fields[1] != "POS")
                throw new Exception("Invalid VCF header: the second field must be POS.");
            if (fields[2] != "ID")
                throw new Exception("Invalid VCF header: the third field must be ID.");
            if (fields[3] != "REF")
                throw new Exception("Invalid VCF header: the fourth field must be REF.");
            if (fields[4] != "ALT")
                throw new Exception("Invalid VCF header: the fifth field must be ALT.");
            if (fields[5] != "QUAL")
                throw new Exception("Invalid VCF header: the sixth field must be QUAL.");
            if (fields[6] != "FILTER")
                throw new Exception("Invalid VCF header: the seventh field must be FILTER.");
            if (fields[7] != "INFO")
                throw new Exception("Invalid VCF header: the eight field must be INFO.");

            this.fields_ = fields;
            this.hasFormat_ = fields.Length > 8;
            if (this.hasFormat_ && fields[8] != "FORMAT")
                throw new Exception("Invalid VCF header: the ninth field must be FORMAT.");
            this.sampleCount_ = Math.Max(0, fields_.Length - 9);
        }
        #endregion

        #region Functions

        public VcfHeader Subset(string[] sampleIDs)
        {
            string[] fields = new string[9 + sampleIDs.Length];
            for (int i = 0; i < 8; i++)
            {
                fields[i] = this.fields_[i];
            }
            int pos = 8;
            fields[pos++] = "FORMAT";
            for (int i = 0; i < sampleIDs.Length; i++)
            {
                fields[pos++] = sampleIDs[i];
            }
            return new VcfHeader(fields);
        }

        public string[] GetFirst8Fields()
        {
            string[] outArray = new string[8];
            Array.Copy(this.fields_, outArray, 8);
            return outArray;
        }

        public string GetSampleField(int sampleIndex)
        {
            return this.fields_[9 + sampleIndex];
        }

        public string[] GetSampleNames()
        {
            string[] sampleNames = new string[sampleCount_];
            for (int i = 0; i < sampleCount_; i++)
            {
                sampleNames[i] = this.GetSampleField(i);
            }
            return sampleNames;
        }

        public void SetSampleNames(string[] sampleFields)
        {
            if (sampleFields.Length != sampleCount_)
                throw new Exception("Invalid sample fields. Length is not the same.");

            for (int i = 0; i < sampleCount_; i++)
            {
                this.fields_[i + 9] = sampleFields[i];
            }
        }

        public override string ToString()
        {
            return string.Join("\t", this.fields_);
        }
        #endregion
    }

    public delegate bool VcfLineAction(string line, long index);
    public delegate bool VcfMultiLineAction(string[] lines);
}