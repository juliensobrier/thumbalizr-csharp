using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.Drawing;
using Thumbalizr;

namespace Tests
{
    [TestClass]
    public class UnitTest1
    {
        private Client client;

        [TestInitialize]
        public void SetUp()
        {
            client = new Client("", true);
        }

        [TestMethod]
        public void TestScreenshotDefault()
        {
            string url = @"http://www.google.com/";
            Result screenshot = client.Screenshot(url);

            Assert.AreEqual(url, screenshot.Url);
            Assert.AreEqual(Status.Finished, screenshot.Status);

            Assert.AreEqual(200, screenshot.Thumbnail.Width);
            Assert.AreEqual(Encoding.Jpg, screenshot.Encoding);
        }

        [TestMethod]
        public void TestScreenshotOverrride()
        {
            string url = @"http://www.google.com/";
            Result screenshot = client.Screenshot(url, 2000, 100, Encoding.Png, Mode.Page, true, 500, 25000, 25000);

            Assert.AreEqual(url, screenshot.Url);
            Assert.AreEqual(Status.Finished, screenshot.Status);
            
            // Override user options
            Assert.AreEqual(Encoding.Jpg, screenshot.Encoding, "Override image encoding");
            Assert.AreEqual(300, screenshot.Thumbnail.Width);
        }


    }
}
