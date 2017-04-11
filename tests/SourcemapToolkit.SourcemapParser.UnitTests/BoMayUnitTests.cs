using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SourcemapToolkit.SourcemapParser.UnitTests
{
    [TestClass]
    public class BoMayUnitTests
    {
        [TestMethod]
        [DeploymentItem("somefile.txt", "targetFolder")]
        public void TestYourDad()
        {
            string testData = File.ReadAllText(@"targetFolder\somefile.txt");
            Assert.AreEqual(1, 1);
        }
    }
}
