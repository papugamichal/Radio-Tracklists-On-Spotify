using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using RadioTracklistsOnSpotify.Services.DataSourceService;
using RadioTracklistsOnSpotify.Services.DataSourceService.Abstraction;
using RadioTracklistsOnSpotify.Services.DataSourceService.Configuration;

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
        public async Task GetPlaylistFor_FixedDate_ReturnsNotEmptyList()
        {
            //Arrange
            var date = new DateTime(2022, 02, 24);

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
