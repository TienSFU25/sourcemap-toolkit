using System;
using System.Collections.Generic;
using System.IO;

namespace SourcemapToolkit.SourcemapParser
{
    public class SourceMapTree : SourceMap
    {
        /// <summary>
        /// The name of the generated file to which this source map corresponds
        ///// </summary>
        //public string File;

        ////public int FileIndex;
        //// full path to the source map
        public string FullPath;

        ///// <summary>
        ///// The raw, unparsed mappings entry of the soure map
        ///// </summary>
        //public string Mappings;

        ///// <summary>
        ///// The list of source files that were the inputs used to generate this output file
        ///// </summary>
        //public List<string> Sources;

        ///// <summary>
        ///// A list of known original names for entries in this file
        ///// </summary>
        //public List<string> Names;

        // public Dictionary<Tuple<int, int>, Tuple<int?, int?, int?, int?>> RawParsedMappings;

        // each line can have multiple mappings
        public Dictionary<int, List<RawParsedMapping>> RawParsedMappings;

        public List<SourceMapTree> SmSources;

        public bool IsOriginalSource;

        public string RawContent;

        public void addRawMappingAtPosition(int generatedLine, int generatedColumn, int originalLine, int originalColumn, int sourceFileIndex)
        {
            if (this.RawParsedMappings == null)
            {
                this.RawParsedMappings = new Dictionary<int, List<RawParsedMapping>>();
            }

            if (!this.RawParsedMappings.ContainsKey(generatedLine))
            {
                this.RawParsedMappings.Add(generatedLine, new List<RawParsedMapping>());
            }

            List<RawParsedMapping> thisLine;

            this.RawParsedMappings.TryGetValue(generatedLine, out thisLine);

            var rpm = new RawParsedMapping
            {
                GenSrcCol = generatedColumn,
                OrigSrcLine = originalLine,
                OrigSrcCol = originalColumn,
                OrigFileIndex = sourceFileIndex
            };

            thisLine.Add(rpm);
        }

        public Tuple<string, int, int> findSource(int lineNumber, int? colNumber)
        {
            var sources = this.SmSources;

            if (this.IsOriginalSource)
            {
                int c = colNumber.HasValue ? colNumber.Value : 0;

                return Tuple.Create(this.FullPath, lineNumber, c);
            }

            List<RawParsedMapping> mappingsForLine;

            // stupid one based lines
            this.RawParsedMappings.TryGetValue(lineNumber, out mappingsForLine);

            if (mappingsForLine == null)
            {
                //throw new Exception("wtf?");
                return null;
            }

            SourceMapTree childTree;

            Func<RawParsedMapping, int?, Tuple<string, int, int>> findSourceByMapping = (RawParsedMapping mapping, int? origColNumber) =>
            {
                if (!mapping.OrigFileIndex.HasValue)
                {
                    throw new Exception("wtf??????");
                }

                int origLineNumber = mapping.OrigSrcLine.Value;
                int origFileIndex = mapping.OrigFileIndex.Value;

                childTree = sources[origFileIndex];
                return childTree.findSource(origLineNumber, origColNumber);
            };

            Func<Tuple<string, int, int>> defaultToLines = () =>
            {
                // fall back to line mappings
                var defaultMapping = mappingsForLine[0];
                return findSourceByMapping(defaultMapping, null);
            };

            if (!colNumber.HasValue)
            {
                return defaultToLines();
            }

            foreach (var mapping in mappingsForLine)
            {
                var generatedCodeColumn = mapping.GenSrcCol;

                if (generatedCodeColumn > colNumber)
                {
                    break;
                }

                if (generatedCodeColumn == colNumber)
                {
                    return findSourceByMapping(mapping, colNumber);
                }
            }

            // fall back to line mappings
            return defaultToLines();
        }

        // write this properly plz
        public List<string> findAllSources()
        {
            var sources = new List<string>();

            Action<SourceMapTree> bfs = null;

            bfs = (smt) =>
            {
                sources.Add(smt.FullPath);

                if (smt.SmSources != null)
                {
                    foreach (var child in smt.SmSources)
                    {
                        bfs(child);
                    }
                }
            };

            bfs(this);

            return sources;
        }

        public SourceMap ToSourceMap()
        {
            // must have the file protocol
            //var sources = this.sourcesContentByPath
            //    .Select(fileInfo => fileInfo.Item1).ToList();

            var sources = this.findAllSources();

            // copy first level attributes
            SourceMap sm = new SourceMap()
            {
                File = this.File,
                Names = this.Names,
                Sources = sources,
                Version = 3
            };

            var mappings = new List<MappingEntry>();

            var tree = this;

            //var tempPath = @"C:\Users\tivu\Documents\Debug\fuctup.txt";

            //using (StreamWriter sw = System.IO.File.CreateText(tempPath))
            //{
                // each is a line
                foreach (var rawParsedMapping in tree.RawParsedMappings)
                {
                    var generatedLineNumber = rawParsedMapping.Key;
                    var mappingsForLine = rawParsedMapping.Value;

                    foreach (var mapping in mappingsForLine)
                    {
                        var generatedColumnNumber = mapping.GenSrcCol;
                        var originalAllTheWay = tree.findSource(generatedLineNumber, generatedColumnNumber);

                        if (originalAllTheWay == null)
                        {
                            //sw.WriteLine("Cannot find source for line {0}, column {1}", generatedLineNumber, generatedColumnNumber);
                            continue;
                        }

                        var filePath = originalAllTheWay.Item1;

                        var s1 = new SourcePosition { ZeroBasedLineNumber = generatedLineNumber, ZeroBasedColumnNumber = generatedColumnNumber };
                        var d1 = new SourcePosition { ZeroBasedLineNumber = originalAllTheWay.Item2, ZeroBasedColumnNumber = originalAllTheWay.Item3 };

                        var m1 = new MappingEntry { GeneratedSourcePosition = s1, OriginalSourcePosition = d1, OriginalFileName = filePath };
                        mappings.Add(m1);
                    }
                }

                sm.ParsedMappings = mappings;

                return sm;
            }
        //}
    }
}
