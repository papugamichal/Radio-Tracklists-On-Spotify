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
    public class RadioNowySwiatDataSourceServiceTests
    {
        private IDataSourceService _sut;

        [SetUp]
        public void Setup()
        {
            var logger = Mock.Of<ILogger<RadioNowySwiatDataSourceService>>();
            var options = Options.Create(new DataSourceOptions
            {
                PlaylistEndpoint = "https://nowyswiat.online/playlista/?dzien=",
                DateFormat = "yyyy-MM-dd"
            });

            _sut = new RadioNowySwiatDataSourceService(logger, options);
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
