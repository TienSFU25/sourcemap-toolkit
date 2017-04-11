using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace SourcemapToolkit.SourcemapParser
{
    /// <summary>
    /// Corresponds to a single parsed entry in the source map mapping string that is used internally by the parser.
    /// The public API exposes the MappingEntry object, which is more useful to consumers of the library.
    /// </summary>
    public class NumericMappingEntry
    {
        /// <summary>
        /// The zero-based line number in the generated code that corresponds to this mapping segment.
        /// </summary>
        public int GeneratedLineNumber;

        /// <summary>
        /// The zero-based column number in the generated code that corresponds to this mapping segment.
        /// </summary>
        public int GeneratedColumnNumber;

        /// <summary>
        /// The zero-based index into the sources array that corresponds to this mapping segment.
        /// </summary>
        public int? OriginalSourceFileIndex;

        /// <summary>
        /// The zero-based line number in the source code that corresponds to this mapping segment.
        /// </summary>
        public int? OriginalLineNumber;

        /// <summary>
        /// The zero-based line number in the source code that corresponds to this mapping segment.
        /// </summary>
        public int? OriginalColumnNumber;

        /// <summary>
        /// The zero-based index into the names array that can be used to identify names associated with this object.
        /// </summary>
        public int? OriginalNameIndex;

        public static SourceMap makeShit()
        {
            var sm = new SourceMap();

            var s1 = new SourcePosition { ZeroBasedColumnNumber = 0, ZeroBasedLineNumber = 1 };
            var d1 = new SourcePosition { ZeroBasedColumnNumber = 4, ZeroBasedLineNumber = 5 };
            var originalFile = "whatever.ts";
            var originalName = "whocares";

            var m1 = new MappingEntry { GeneratedSourcePosition = d1, OriginalSourcePosition = s1, OriginalFileName = originalFile, OriginalName = originalName };
            var m2 = new MappingEntry { GeneratedSourcePosition = d1, OriginalSourcePosition = s1, OriginalFileName = originalFile, OriginalName = originalName };

            var mappings = new List<MappingEntry> { m1, m2 };

            sm.ParsedMappings = mappings;

            return sm;
        }

        public MappingEntry ToMappingEntry(List<string> names, List<string> sources)
        {
            MappingEntry result = new MappingEntry
            {
                GeneratedSourcePosition = new SourcePosition
                {
                    ZeroBasedColumnNumber = GeneratedColumnNumber,
                    ZeroBasedLineNumber = GeneratedLineNumber
                }
            };

            if (OriginalColumnNumber.HasValue && OriginalLineNumber.HasValue)
            {
                result.OriginalSourcePosition = new SourcePosition
                {
                    ZeroBasedColumnNumber = OriginalColumnNumber.Value,
                    ZeroBasedLineNumber = OriginalLineNumber.Value
                };
            }

            if (OriginalNameIndex.HasValue)
            {
                try
                {
                    result.OriginalName = names[OriginalNameIndex.Value];
                }
                catch (IndexOutOfRangeException e)
                {
                    throw new IndexOutOfRangeException("Source map contains original name index that is outside the range of the provided names array" ,e);
                }

            }

            if (OriginalSourceFileIndex.HasValue)
            {
                try
                {
                    result.OriginalFileName = sources[OriginalSourceFileIndex.Value];
                }
                catch (IndexOutOfRangeException e)
                {
                    throw new IndexOutOfRangeException("Source map contains original source index that is outside the range of the provided sources array", e);
                }
            }

            return result;
        }
    }

    /// <summary>
    /// The various fields within a segment of the Mapping parser are relative to the previous value we parsed.
    /// This class tracks this state throughout the parsing process.
    /// </summary>
    internal struct MappingsParserState
    {
        public readonly int CurrentGeneratedLineNumber;
        public readonly int CurrentGeneratedColumnBase;
        public readonly int SourcesListIndexBase;
        public readonly int OriginalSourceStartingLineBase;
        public readonly int OriginalSourceStartingColumnBase;
        public readonly int NamesListIndexBase;

        public MappingsParserState(MappingsParserState previousMappingsParserState = new MappingsParserState(),
            int? newGeneratedLineNumber = null,
            int? newGeneratedColumnBase = null,
            int? newSourcesListIndexBase = null,
            int? newOriginalSourceStartingLineBase = null,
            int? newOriginalSourceStartingColumnBase = null,
            int? newNamesListIndexBase = null)
        {
            CurrentGeneratedLineNumber = newGeneratedLineNumber ?? previousMappingsParserState.CurrentGeneratedLineNumber;
            CurrentGeneratedColumnBase = newGeneratedColumnBase ?? previousMappingsParserState.CurrentGeneratedColumnBase;
            SourcesListIndexBase = newSourcesListIndexBase ?? previousMappingsParserState.SourcesListIndexBase;
            OriginalSourceStartingLineBase = newOriginalSourceStartingLineBase ?? previousMappingsParserState.OriginalSourceStartingLineBase;
            OriginalSourceStartingColumnBase = newOriginalSourceStartingColumnBase ?? previousMappingsParserState.OriginalSourceStartingColumnBase;
            NamesListIndexBase = newNamesListIndexBase ?? previousMappingsParserState.NamesListIndexBase;
        }
    }

    /// <summary>
    /// One of the entries of the V3 source map is a base64 VLQ encoded string providing metadata about a particular line of generated code.
    /// This class is responsible for converting this string into a more friendly format.
    /// </summary>
    internal class MappingsListParser
    {
        /// <summary>
        /// Parses a single "segment" of the mapping field for a source map. A segment describes one piece of code in the generated source.
        /// In the mapping string "AAaAA,CAACC;", AAaAA and CAACC are both segments. This method assumes the segments have already been decoded
        /// from Base64 VLQ into a list of integers.
        /// </summary>
        /// <param name="segmentFields">The integer values for the segment fields</param>
        /// <param name="mappingsParserState">The current state of the state variables for the parser</param>
        /// <returns></returns>
        internal NumericMappingEntry ParseSingleMappingSegment(List<int> segmentFields, MappingsParserState mappingsParserState)
        {
            if (segmentFields == null)
            {
                throw new ArgumentNullException(nameof(segmentFields));
            }

            if (segmentFields.Count == 0 || segmentFields.Count == 2 || segmentFields.Count == 3)
            {
                throw new ArgumentOutOfRangeException(nameof(segmentFields));
            }

            NumericMappingEntry numericMappingEntry = new NumericMappingEntry
            {
                GeneratedLineNumber = mappingsParserState.CurrentGeneratedLineNumber,
                GeneratedColumnNumber = mappingsParserState.CurrentGeneratedColumnBase + segmentFields[0]
            };

            /*
             *	The following description was taken from the Sourcemap V3 spec https://docs.google.com/document/d/1U1RGAehQwRypUTovF1KRlpiOFze0b-_2gc6fAH0KY0k/mobilebasic?pref=2&pli=1
             *  The Sourcemap V3 spec is under a Creative Commons Attribution-ShareAlike 3.0 Unported License. https://creativecommons.org/licenses/by-sa/3.0/
             *
             *  Each VLQ segment has 1, 4, or 5 variable length fields.
             *  The fields in each segment are:
             *  1. The zero-based starting column of the line in the generated code that the segment represents.
             *     If this is the first field of the first segment, or the first segment following a new generated line(“;”),
             *     then this field holds the whole base 64 VLQ.Otherwise, this field contains a base 64 VLQ that is relative to
             *     the previous occurrence of this field.Note that this is different than the fields below because the previous
             *     value is reset after every generated line.
             *  2. If present, an zero - based index into the “sources” list.This field is a base 64 VLQ relative to the previous
             *     occurrence of this field, unless this is the first occurrence of this field, in which case the whole value is represented.
             *  3. If present, the zero-based starting line in the original source represented. This field is a base 64 VLQ relative to the
             *     previous occurrence of this field, unless this is the first occurrence of this field, in which case the whole value is
             *     represented.Always present if there is a source field.
             *  4. If present, the zero - based starting column of the line in the source represented.This field is a base 64 VLQ relative to
             *     the previous occurrence of this field, unless this is the first occurrence of this field, in which case the whole value is
             *     represented.Always present if there is a source field.
             *  5. If present, the zero - based index into the “names” list associated with this segment.This field is a base 64 VLQ relative
             *     to the previous occurrence of this field, unless this is the first occurrence of this field, in which case the whole value
             *     is represented.
             */
            if (segmentFields.Count > 1)
            {
                numericMappingEntry.OriginalSourceFileIndex = mappingsParserState.SourcesListIndexBase + segmentFields[1];
                numericMappingEntry.OriginalLineNumber = mappingsParserState.OriginalSourceStartingLineBase + segmentFields[2];
                numericMappingEntry.OriginalColumnNumber = mappingsParserState.OriginalSourceStartingColumnBase + segmentFields[3];
            }

            if (segmentFields.Count >= 5)
            {
                numericMappingEntry.OriginalNameIndex = mappingsParserState.NamesListIndexBase + segmentFields[4];
            }

            return numericMappingEntry;
        }

        /// <summary>
        /// Top level API that should be called for decoding the MappingsString element. It will convert the string containing Base64
        /// VLQ encoded segments into a list of MappingEntries.
        /// </summary>
        internal List<MappingEntry> ParseMappings(string mappingString, List<string> names, List<string> sources)
        {
            List<MappingEntry> mappingEntries = new List<MappingEntry>();
            MappingsParserState currentMappingsParserState = new MappingsParserState();

            // The V3 source map format calls for all Base64 VLQ segments to be seperated by commas.
            // Each line of generated code is separated using semicolons. The count of semicolons encountered gives the current line number.
            string[] lines = mappingString.Split(';');

            for (int lineNumber = 0; lineNumber < lines.Length; lineNumber += 1)
            {
                // The only value that resets when encountering a semicolon is the starting column.
                currentMappingsParserState = new MappingsParserState(currentMappingsParserState, newGeneratedLineNumber:lineNumber, newGeneratedColumnBase: 0);
                string[] segmentsForLine = lines[lineNumber].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string segment in segmentsForLine)
                {
                    NumericMappingEntry numericMappingEntry = ParseSingleMappingSegment(Base64VlqDecoder.Decode(segment), currentMappingsParserState);
                    mappingEntries.Add(numericMappingEntry.ToMappingEntry(names, sources));

                    // Update the current MappingParserState based on the generated MappingEntry
                    currentMappingsParserState = new MappingsParserState(currentMappingsParserState,
                        newGeneratedColumnBase: numericMappingEntry.GeneratedColumnNumber,
                        newSourcesListIndexBase: numericMappingEntry.OriginalSourceFileIndex,
                        newOriginalSourceStartingLineBase: numericMappingEntry.OriginalLineNumber,
                        newOriginalSourceStartingColumnBase: numericMappingEntry.OriginalColumnNumber,
                        newNamesListIndexBase: numericMappingEntry.OriginalNameIndex);
                }
            }
            return mappingEntries;
        }

        private string MagicallyFindMap(string rawContent)
        {
            Regex r = new Regex(@"\/\/# sourceMappingURL=([0-z\.]*\.map)");

            var matches = r.Matches(rawContent);

            if (matches.Count == 0)
            {
                return null;
            }

            List<string> paths = new List<string>();

            foreach (var m in matches)
            {
                var mm = (Match)m;
                var capture = mm.Groups[1].Value;

                paths.Add(capture);
            }

            // only match we care about
            var matchy = paths.Last();
            return matchy;
        }

        private string resolveFullPath(string dir, string file)
        {
            // thanks windows paths
            if (Path.IsPathRooted(file))
            {
                return file;
            }

            return Path.GetFullPath(dir + file);
        }

        internal SourceMapTree MyParseMappings(string fullPath)
        {
            string fileName = Path.GetFileName(fullPath);
            string fileDir = Path.GetDirectoryName(fullPath) + "//";

            string rawContent = File.ReadAllText(fullPath);
            string mapFileName = MagicallyFindMap(rawContent);

            var basic = new SourceMapTree
            {
                FullPath = fullPath,
                IsOriginalSource = true,
                File = fileName,
                RawContent = rawContent
            };

            if (mapFileName == null)
            {
                return basic;
            }

            // parse and read the map file
            string mapFullPath = resolveFullPath(fileDir, mapFileName);

            //if (Directory.Exists(mapFullPath))
            //{
                using (StreamReader sourceMapStream = new StreamReader(mapFullPath))
                {
                    using (JsonTextReader jsonTextReader = new JsonTextReader(sourceMapStream))
                    {
                        JsonSerializer serializer = new JsonSerializer();

                        SourceMap sm_ = serializer.Deserialize<SourceMap>(jsonTextReader);

                        SourceMapTree sm = new SourceMapTree();
                        //sm.File = sm_.File;

                        sm.File = resolveFullPath(fileDir, sm_.File);

                        sm.Mappings = sm_.Mappings;
                        sm.Sources = sm_.Sources;
                        sm.Names = sm_.Names;
                        sm.RawParsedMappings = new Dictionary<int, List<RawParsedMapping>>();
                        sm.SmSources = new List<SourceMapTree>();
                        sm.IsOriginalSource = false;

                        // need to read the stuff in from sm.File
                        string fullFilePath = sm.File;
                        sm.FullPath = fullFilePath;

                        if (Directory.Exists(sm.File))
                        {
                            sm.RawContent = File.ReadAllText(fullFilePath);
                        }

                        MappingsParserState currentMappingsParserState = new MappingsParserState();

                        string mappingString = sm.Mappings;
                        List<string> names = sm.Names;
                        List<string> sources = sm.Sources;

                        // The V3 source map format calls for all Base64 VLQ segments to be seperated by commas.
                        // Each line of generated code is separated using semicolons. The count of semicolons encountered gives the current line number.
                        string[] lines = mappingString.Split(';');

                        for (int lineNumber = 0; lineNumber < lines.Length; lineNumber += 1)
                        {
                            // The only value that resets when encountering a semicolon is the starting column.
                            currentMappingsParserState = new MappingsParserState(currentMappingsParserState, newGeneratedLineNumber: lineNumber, newGeneratedColumnBase: 0);
                            string[] segmentsForLine = lines[lineNumber].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                            foreach (string segment in segmentsForLine)
                            {
                                NumericMappingEntry nme = ParseSingleMappingSegment(Base64VlqDecoder.Decode(segment), currentMappingsParserState);
                                var src = Tuple.Create(nme.GeneratedLineNumber, nme.GeneratedColumnNumber);
                                var dst = Tuple.Create(nme.OriginalLineNumber, nme.OriginalColumnNumber, nme.OriginalSourceFileIndex, nme.OriginalNameIndex);

                                var rawParsedMapping = new RawParsedMapping
                                {
                                    GenSrcCol = nme.GeneratedColumnNumber,
                                    OrigSrcLine = nme.OriginalLineNumber,
                                    OrigSrcCol = nme.OriginalColumnNumber,
                                    OrigFileIndex = nme.OriginalSourceFileIndex,
                                    OrigNameIndex = nme.OriginalNameIndex
                                };

                                // if list not there add it
                                List<RawParsedMapping> mappingList;
                                sm.RawParsedMappings.TryGetValue(nme.GeneratedLineNumber, out mappingList);

                                if (mappingList == null)
                                {
                                    mappingList = new List<RawParsedMapping>();
                                    sm.RawParsedMappings[nme.GeneratedLineNumber] = mappingList;
                                }

                                // add mapping
                                sm.RawParsedMappings[nme.GeneratedLineNumber].Add(rawParsedMapping);

                                // Update the current MappingParserState based on the generated MappingEntry
                                currentMappingsParserState = new MappingsParserState(currentMappingsParserState,
                                    newGeneratedColumnBase: nme.GeneratedColumnNumber,
                                    newSourcesListIndexBase: nme.OriginalSourceFileIndex,
                                    newOriginalSourceStartingLineBase: nme.OriginalLineNumber,
                                    newOriginalSourceStartingColumnBase: nme.OriginalColumnNumber,
                                    newNamesListIndexBase: nme.OriginalNameIndex);
                            }
                        }

                        var s = sm.Sources;
                        for (var i = 0; i < s.Count; i++)
                        {
                            var sourceFileName = s[i];
                            var sourceFilePath = resolveFullPath(fileDir, sourceFileName);
                            SourceMapTree childMap = MyParseMappings(sourceFilePath);
                            sm.SmSources.Add(childMap);
                        }

                        return sm;
                    }
                }
            //} else
            //{
            //    return basic;
            //}
        }
    }
}
