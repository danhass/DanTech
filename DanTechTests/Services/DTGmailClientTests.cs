using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.AspNetCore.Http;
using System.Linq;
using DanTech.Data;
using Google.Apis.Oauth2.v2.Data;
using DanTechTests.Data;
using DanTech.Services;
using System;
using Microsoft.Extensions.Configuration;
using System.Net;

namespace DanTechTests
{
    [TestClass]
    public class DTGmailClientTests
    {
        [TestMethod()]
        public void DTGmailClient_InstantiateAndPing()
        {
            //Arrange
            var client = new DTGmailClient(DTTestOrganizer.InitConfiguration());


            //Act
            var result = client.Send();

            //Assert
            Assert.IsTrue(result, "Cound not call default Send() method on Gmail client");
        }
    }
}
