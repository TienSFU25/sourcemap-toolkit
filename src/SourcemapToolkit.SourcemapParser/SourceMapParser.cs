using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace SourcemapToolkit.SourcemapParser
{
    public class SourceMapParser
    {
        private readonly MappingsListParser _mappingsListParser;

        public SourceMapParser()
        {
            _mappingsListParser = new MappingsListParser();
        }

        /// <summary>
        /// Parses a stream representing a source map into a SourceMap object.
        /// </summary>
        public SourceMap ParseSourceMap(StreamReader sourceMapStream)
        {
            if (sourceMapStream == null)
            {
                return null;
            }
            using (JsonTextReader jsonTextReader = new JsonTextReader(sourceMapStream))
            {
                JsonSerializer serializer = new JsonSerializer();

                SourceMap result = serializer.Deserialize<SourceMap>(jsonTextReader);
                result.ParsedMappings = _mappingsListParser.ParseMappings(result.Mappings, result.Names, result.Sources);
                sourceMapStream.Close();
                return result;
            }
        }

        public SourceMapTree ParseRecursive(string filepath)
        {
            var thing = _mappingsListParser.MyParseMappings(filepath);
            return thing;
        }

        //public SourceMapTreeRoot ComposeSourceMaps(List<SourceMapTreeRoot> sourceMaps)
        //{
        //    var thing = _mappingsListParser.MyParseMappings(filepath);
        //    return thing;
        //}
    }
}
