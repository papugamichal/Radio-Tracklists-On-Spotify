using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using RadioNowySwiatAutomatedPlaylist.Services.DataSourceService;
using RadioNowySwiatAutomatedPlaylist.Services.DataSourceService.Abstraction;
using RadioNowySwiatAutomatedPlaylist.Services.DataSourceService.Configuration;

namespace AutomatedPlaylist.Tests.Services
{
    [TestFixture]
    public class Radio357DataSourceServiceTests
    {
        private IDataSourceService _sut;

        [SetUp]
        public void Setup()
        {
            var logger = Mock.Of<ILogger<Radio357DataSourceService>>();
            var options = Options.Create(new DataSourceOptions
            {
                PlaylistEndpoint = "https://www.odsluchane.eu/szukaj.php?r=390&date=",
                DateFormat = "dd-MM-yyyy"
            });

            _sut = new Radio357DataSourceService(logger, options);
        }

        [Test]
        public async Task GetPlaylistFor_Today_ReturnsNotEmptyList()
        {
            //Arrange
            var date = DateTime.Today;

            //Act
            var result = await _sut.GetPlaylistFor(date);

            //Assert
            Assert.That(result, Is.Not.Empty);
        }

        [Test]
        public async Task GetPlaylistFor_Tomorrow_ReturnsEmptyList()
        {
            //Arrange
            var date = DateTime.Today.AddDays(1);

            //Act
            var result = await _sut.GetPlaylistFor(date);

            //Assert
            Assert.That(result, Is.Empty);
        }
    }
}
