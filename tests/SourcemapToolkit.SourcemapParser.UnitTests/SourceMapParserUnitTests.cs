using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SourcemapToolkit.SourcemapParser.UnitTests
{
    [TestClass]
    public class SourceMapParserUnitTests
    {
        [TestMethod]
        public void ParseSourceMap_NullInputStream_ReturnsNull()
        {
            // Arrange
            SourceMapParser sourceMapParser = new SourceMapParser();
            StreamReader input = null;

            // Act
            SourceMap output = sourceMapParser.ParseSourceMap(input);

            // Assert
            Assert.IsNull(output);
        }

        [TestMethod]
        public void ParseSourceMap_SimpleSourceMap_CorrectlyParsed()
        {
            // Arrange
            SourceMapParser sourceMapParser = new SourceMapParser();
            string input = "{ \"version\":3, \"file\":\"CommonIntl\", \"lineCount\":65, \"mappings\":\"AACAA,aAAA,CAAc\", \"sources\":[\"input/CommonIntl.js\"], \"names\":[\"CommonStrings\",\"afrikaans\"]}";

            // Act
            SourceMap output = sourceMapParser.ParseSourceMap(UnitTestUtils.StreamReaderFromString(input));

            // Assert
            Assert.AreEqual(3, output.Version);
            Assert.AreEqual("CommonIntl", output.File);
            Assert.AreEqual("AACAA,aAAA,CAAc", output.Mappings);
            Assert.AreEqual(1, output.Sources.Count);
            Assert.AreEqual("input/CommonIntl.js", output.Sources[0]);
            Assert.AreEqual(2, output.Names.Count);
            Assert.AreEqual("CommonStrings", output.Names[0]);
            Assert.AreEqual("afrikaans", output.Names[1]);
        }

        public void ParseRecursive_SimpleSourceMap_CorrectlyParsed()
        {
            // Arrange
            SourceMapParser sourceMapParser = new SourceMapParser();
            string input = "{ \"version\":3, \"file\":\"parent.js\", \"mappings\":\";;AACA;AACA\", \"sources\":[\"child1.js\"]}";

            // Act
            SourceMap output = sourceMapParser.ParseRecursive(UnitTestUtils.StreamReaderFromString(input));

            // Assert
            Assert.AreEqual(4, output.ParsedMappings.Count);
        }

        public void ParseRecursive_SourceMapIncompleteSources_CorrectlyParsed()
        {
            // Arrange
            SourceMapParser sourceMapParser = new SourceMapParser();
            string input = "{ \"version\":3, \"file\":\"parent.js\", \"mappings\":\";AAAA;ACAA\", \"sources\":[\"child1.js\", \"child2.js\"]}";

            // Act
            SourceMap output = sourceMapParser.ParseRecursive(UnitTestUtils.StreamReaderFromString(input));

            // Assert
            Assert.AreEqual(3, output.ParsedMappings.Count);
        }
    }
}
