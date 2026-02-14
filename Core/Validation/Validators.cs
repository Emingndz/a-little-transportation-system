using Prolab_4.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Prolab_4.Core.Validation
{
    /// <summary>
    /// Doğrulama sonucu
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid => !Errors.Any();
        public List<ValidationError> Errors { get; } = new List<ValidationError>();

        public void AddError(string propertyName, string errorMessage)
        {
            Errors.Add(new ValidationError(propertyName, errorMessage));
        }

        public static ValidationResult Success() => new ValidationResult();

        public static ValidationResult Fail(string propertyName, string errorMessage)
        {
            var result = new ValidationResult();
            result.AddError(propertyName, errorMessage);
            return result;
        }

        public string GetErrorSummary()
        {
            return string.Join(Environment.NewLine, Errors.Select(e => $"• {e.PropertyName}: {e.ErrorMessage}"));
        }
    }

    /// <summary>
    /// Tek bir doğrulama hatası
    /// </summary>
    public class ValidationError
    {
        public string PropertyName { get; }
        public string ErrorMessage { get; }

        public ValidationError(string propertyName, string errorMessage)
        {
            PropertyName = propertyName;
            ErrorMessage = errorMessage;
        }
    }

    /// <summary>
    /// Rota talebi için DTO
    /// </summary>
    public class RotaTalebi
    {
        public string BaslangicDurakId { get; set; }
        public string HedefDurakId { get; set; }
        public double? KullaniciEnlem { get; set; }
        public double? KullaniciBoylam { get; set; }
        public string YolcuTipi { get; set; }
        public string OdemeYontemi { get; set; }
    }

    /// <summary>
    /// Rota talebi doğrulayıcı
    /// </summary>
    public class RotaTalebiValidator
    {
        public ValidationResult Validate(RotaTalebi talep)
        {
            var result = new ValidationResult();

            if (talep == null)
            {
                result.AddError("Talep", "Rota talebi boş olamaz.");
                return result;
            }

            // Başlangıç kontrolü
            if (string.IsNullOrWhiteSpace(talep.BaslangicDurakId) && 
                (!talep.KullaniciEnlem.HasValue || !talep.KullaniciBoylam.HasValue))
            {
                result.AddError("Başlangıç", "Başlangıç durağı veya konum belirtilmelidir.");
            }

            // Hedef kontrolü
            if (string.IsNullOrWhiteSpace(talep.HedefDurakId))
            {
                result.AddError("Hedef", "Hedef durak belirtilmelidir.");
            }

            // Aynı durak kontrolü
            if (!string.IsNullOrWhiteSpace(talep.BaslangicDurakId) && 
                talep.BaslangicDurakId == talep.HedefDurakId)
            {
                result.AddError("Duraklar", "Başlangıç ve hedef durak aynı olamaz.");
            }

            // Koordinat doğrulama
            if (talep.KullaniciEnlem.HasValue)
            {
                if (talep.KullaniciEnlem < -90 || talep.KullaniciEnlem > 90)
                {
                    result.AddError("Enlem", "Enlem -90 ile 90 arasında olmalıdır.");
                }
            }

            if (talep.KullaniciBoylam.HasValue)
            {
                if (talep.KullaniciBoylam < -180 || talep.KullaniciBoylam > 180)
                {
                    result.AddError("Boylam", "Boylam -180 ile 180 arasında olmalıdır.");
                }
            }

            // Yolcu tipi kontrolü
            var gecerliYolcuTipleri = new[] { "Genel", "Öğrenci", "Yaşlı" };
            if (!string.IsNullOrWhiteSpace(talep.YolcuTipi) && 
                !gecerliYolcuTipleri.Contains(talep.YolcuTipi))
            {
                result.AddError("YolcuTipi", $"Geçersiz yolcu tipi. Geçerli değerler: {string.Join(", ", gecerliYolcuTipleri)}");
            }

            // Ödeme yöntemi kontrolü
            var gecerliOdemeYontemleri = new[] { "Nakit", "KentKart", "KrediKarti" };
            if (!string.IsNullOrWhiteSpace(talep.OdemeYontemi) && 
                !gecerliOdemeYontemleri.Contains(talep.OdemeYontemi))
            {
                result.AddError("OdemeYontemi", $"Geçersiz ödeme yöntemi. Geçerli değerler: {string.Join(", ", gecerliOdemeYontemleri)}");
            }

            return result;
        }
    }

    /// <summary>
    /// Durak doğrulayıcı
    /// </summary>
    public class DurakValidator
    {
        public ValidationResult Validate(Durak durak)
        {
            var result = new ValidationResult();

            if (durak == null)
            {
                result.AddError("Durak", "Durak boş olamaz.");
                return result;
            }

            if (string.IsNullOrWhiteSpace(durak.Id))
            {
                result.AddError("Id", "Durak ID'si boş olamaz.");
            }

            if (string.IsNullOrWhiteSpace(durak.Ad))
            {
                result.AddError("Ad", "Durak adı boş olamaz.");
            }

            if (durak.Enlem < -90 || durak.Enlem > 90)
            {
                result.AddError("Enlem", "Enlem -90 ile 90 arasında olmalıdır.");
            }

            if (durak.Boylam < -180 || durak.Boylam > 180)
            {
                result.AddError("Boylam", "Boylam -180 ile 180 arasında olmalıdır.");
            }

            var gecerliTurler = new[] { "bus", "tram" };
            if (!string.IsNullOrWhiteSpace(durak.Tur) && !gecerliTurler.Contains(durak.Tur))
            {
                result.AddError("Tur", $"Geçersiz durak türü. Geçerli değerler: {string.Join(", ", gecerliTurler)}");
            }

            return result;
        }
    }

    /// <summary>
    /// Koordinat doğrulayıcı (İzmit bölgesi için)
    /// </summary>
    public class KoordinatValidator
    {
        // İzmit bölgesi sınırları (yaklaşık)
        private const double MIN_ENLEM = 40.6;
        private const double MAX_ENLEM = 41.0;
        private const double MIN_BOYLAM = 29.7;
        private const double MAX_BOYLAM = 30.2;

        public ValidationResult ValidateIzmitBolgesi(double enlem, double boylam)
        {
            var result = new ValidationResult();

            if (enlem < MIN_ENLEM || enlem > MAX_ENLEM)
            {
                result.AddError("Enlem", $"Enlem İzmit bölgesi dışında ({MIN_ENLEM} - {MAX_ENLEM}).");
            }

            if (boylam < MIN_BOYLAM || boylam > MAX_BOYLAM)
            {
                result.AddError("Boylam", $"Boylam İzmit bölgesi dışında ({MIN_BOYLAM} - {MAX_BOYLAM}).");
            }

            return result;
        }
    }
}
