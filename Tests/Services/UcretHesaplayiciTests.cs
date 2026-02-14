using Prolab_4.Services;
using Xunit;
using FluentAssertions;

namespace Prolab_4.Tests.Services
{
    /// <summary>
    /// UcretHesaplayici testleri.
    /// </summary>
    public class UcretHesaplayiciTests
    {
        private readonly UcretHesaplayici _hesaplayici;

        public UcretHesaplayiciTests()
        {
            _hesaplayici = new UcretHesaplayici();
        }

        #region Genel Ücret Hesaplama Tests

        [Theory]
        [InlineData("Otobus", 10.0)]
        [InlineData("Tramvay", 7.5)]
        [InlineData("Taksi", 50.0)]
        public void GetUcret_Genel_ReturnsCorrectFee(string aracTipi, double expectedUcret)
        {
            // Arrange
            var yolcuTipi = "Genel";
            double mesafe = 1.0;

            // Act
            var result = _hesaplayici.Hesapla(aracTipi, mesafe, yolcuTipi);

            // Assert
            result.Should().BeApproximately(expectedUcret, 0.01);
        }

        #endregion

        #region Öğrenci İndirimi Tests

        [Theory]
        [InlineData("Otobus", 5.0)]  // %50 indirim
        [InlineData("Tramvay", 3.75)] // %50 indirim
        public void GetUcret_Ogrenci_AppliesDiscount(string aracTipi, double expectedUcret)
        {
            // Arrange
            var yolcuTipi = "Ogrenci";
            double mesafe = 1.0;

            // Act
            var result = _hesaplayici.Hesapla(aracTipi, mesafe, yolcuTipi);

            // Assert
            result.Should().BeApproximately(expectedUcret, 0.01);
        }

        #endregion

        #region Yaşlı İndirimi Tests

        [Theory]
        [InlineData("Otobus", 7.0)]   // %30 indirim
        [InlineData("Tramvay", 5.25)] // %30 indirim
        public void GetUcret_Yasli_AppliesDiscount(string aracTipi, double expectedUcret)
        {
            // Arrange
            var yolcuTipi = "Yasli";
            double mesafe = 1.0;

            // Act
            var result = _hesaplayici.Hesapla(aracTipi, mesafe, yolcuTipi);

            // Assert
            result.Should().BeApproximately(expectedUcret, 0.01);
        }

        #endregion

        #region Taksi Mesafe Hesabı Tests

        [Theory]
        [InlineData(1.0, 50.0)]
        [InlineData(2.0, 100.0)]
        [InlineData(5.0, 250.0)]
        public void GetUcret_Taksi_CalculatesBasedOnDistance(double mesafe, double expectedUcret)
        {
            // Arrange
            var aracTipi = "Taksi";
            var yolcuTipi = "Genel";

            // Act
            var result = _hesaplayici.Hesapla(aracTipi, mesafe, yolcuTipi);

            // Assert
            result.Should().BeApproximately(expectedUcret, 0.01);
        }

        #endregion

        #region Yürümek Ücretsiz Tests

        [Theory]
        [InlineData("Genel")]
        [InlineData("Ogrenci")]
        [InlineData("Yasli")]
        public void GetUcret_Yurumek_AlwaysFree(string yolcuTipi)
        {
            // Arrange
            var aracTipi = "Yurumek";
            double mesafe = 1.0;

            // Act
            var result = _hesaplayici.Hesapla(aracTipi, mesafe, yolcuTipi);

            // Assert
            result.Should().Be(0);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void GetUcret_ZeroDistance_ReturnsZeroForTaksi()
        {
            // Arrange
            var aracTipi = "Taksi";
            double mesafe = 0;
            var yolcuTipi = "Genel";

            // Act
            var result = _hesaplayici.Hesapla(aracTipi, mesafe, yolcuTipi);

            // Assert
            result.Should().Be(0);
        }

        [Fact]
        public void GetUcret_UnknownVehicle_ReturnsZero()
        {
            // Arrange
            var aracTipi = "BilinmeyenArac";
            double mesafe = 1.0;
            var yolcuTipi = "Genel";

            // Act
            var result = _hesaplayici.Hesapla(aracTipi, mesafe, yolcuTipi);

            // Assert
            result.Should().Be(0);
        }

        #endregion
    }
}
