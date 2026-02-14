using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Prolab_4.Core.Caching;
using Prolab_4.Core.Exceptions;
using Prolab_4.Core.Logging;
using Prolab_4.Core.Resilience;
using Prolab_4.Core.Security;
using Prolab_4.Core.Testing;
using Prolab_4.Models;
using Prolab_4.Services;

namespace Prolab_4.Tests
{
    #region Durak Tests

    [TestClass(Description = "Durak model testleri")]
    public class DurakTests
    {
        [TestMethod(Description = "Durak oluşturma testi")]
        public void Durak_Create_ShouldHaveDefaultValues()
        {
            var durak = new Durak();

            Assert.IsNotNull(durak.Baglantilar);
            Assert.IsEmpty(durak.Baglantilar);
            Assert.IsFalse(durak.SonDurak);
        }

        [TestMethod(Description = "Durak bağlantı ekleme testi")]
        public void Durak_AddBaglanti_ShouldAddToBaglantilar()
        {
            var durak = new Durak { Id = "D1", Ad = "Merkez" };
            var baglanti = new DurakBaglantisi { HedefDurakId = "D2" };

            durak.Baglantilar.Add(baglanti);

            Assert.AreEqual(1, durak.Baglantilar.Count);
            Assert.AreEqual("D2", durak.Baglantilar[0].HedefDurakId);
        }

        [TestCase("D1", "Merkez")]
        [TestCase("D2", "İstasyon")]
        [TestCase("D3", "Terminal")]
        public void Durak_SetProperties_ShouldReturnCorrectValues(string id, string ad)
        {
            var durak = new Durak { Id = id, Ad = ad };

            Assert.AreEqual(id, durak.Id);
            Assert.AreEqual(ad, durak.Ad);
        }
    }

    #endregion

    #region Cache Tests

    [TestClass(Description = "Cache sistemi testleri")]
    public class CacheTests
    {
        private AdvancedMemoryCache _cache;

        [TestSetup]
        public void Setup()
        {
            _cache = new AdvancedMemoryCache(maxSizeMB: 10);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _cache?.Dispose();
        }

        [TestMethod(Description = "Cache set ve get testi")]
        public void Cache_SetAndGet_ShouldReturnValue()
        {
            _cache.Set("testKey", "testValue");

            var result = _cache.TryGet<string>("testKey", out var value);

            Assert.IsTrue(result);
            Assert.AreEqual("testValue", value);
        }

        [TestMethod(Description = "Cache miss testi")]
        public void Cache_GetNonExistent_ShouldReturnFalse()
        {
            var result = _cache.TryGet<string>("nonexistent", out var value);

            Assert.IsFalse(result);
            Assert.IsNull(value);
        }

        [TestMethod(Description = "Cache remove testi")]
        public void Cache_Remove_ShouldRemoveValue()
        {
            _cache.Set("key", "value");
            _cache.Remove("key");

            var result = _cache.TryGet<string>("key", out _);

            Assert.IsFalse(result);
        }

        [TestMethod(Description = "Cache clear testi")]
        public void Cache_Clear_ShouldRemoveAllValues()
        {
            _cache.Set("key1", "value1");
            _cache.Set("key2", "value2");

            _cache.Clear();

            Assert.IsFalse(_cache.TryGet<string>("key1", out _));
            Assert.IsFalse(_cache.TryGet<string>("key2", out _));
        }

        [TestMethod(Description = "Cache prefix remove testi")]
        public void Cache_RemoveByPrefix_ShouldRemoveMatchingKeys()
        {
            _cache.Set("route:A:B", "value1");
            _cache.Set("route:C:D", "value2");
            _cache.Set("other:key", "value3");

            _cache.RemoveByPrefix("route:");

            Assert.IsFalse(_cache.TryGet<string>("route:A:B", out _));
            Assert.IsFalse(_cache.TryGet<string>("route:C:D", out _));
            Assert.IsTrue(_cache.TryGet<string>("other:key", out _));
        }

        [TestMethod(Description = "Cache GetOrCreate testi")]
        public async Task Cache_GetOrCreate_ShouldCallFactoryOnce()
        {
            int callCount = 0;

            var value1 = await _cache.GetOrCreateAsync("key", async () =>
            {
                callCount++;
                return await Task.FromResult("value");
            });

            var value2 = await _cache.GetOrCreateAsync("key", async () =>
            {
                callCount++;
                return await Task.FromResult("value");
            });

            Assert.AreEqual(1, callCount);
            Assert.AreEqual("value", value1);
            Assert.AreEqual("value", value2);
        }

        [TestMethod(Description = "Cache statistics testi")]
        public void Cache_Statistics_ShouldTrackHitsAndMisses()
        {
            _cache.Set("key", "value");

            _cache.TryGet<string>("key", out _);     // Hit
            _cache.TryGet<string>("key", out _);     // Hit
            _cache.TryGet<string>("missing", out _); // Miss

            var stats = _cache.GetStatistics();

            Assert.AreEqual(2, stats.HitCount);
            Assert.AreEqual(1, stats.MissCount);
        }
    }

    #endregion

    #region Cache Key Builder Tests

    [TestClass(Description = "Cache key builder testleri")]
    public class CacheKeyBuilderTests
    {
        [TestMethod]
        public void Build_WithMultipleParts_ShouldJoinWithColon()
        {
            var key = CacheKeyBuilder.Build("part1", "part2", "part3");

            Assert.AreEqual("part1:part2:part3", key);
        }

        [TestMethod]
        public void Build_WithNullPart_ShouldIncludeNullString()
        {
            var key = CacheKeyBuilder.Build("part1", null, "part3");

            Assert.AreEqual("part1:null:part3", key);
        }

        [TestMethod]
        public void ForRoute_ShouldBuildCorrectKey()
        {
            var key = CacheKeyBuilder.ForRoute("durak1", "durak2", "ogrenci");

            Assert.AreEqual("route:durak1:durak2:ogrenci", key);
        }

        [TestMethod]
        public void ForDurak_ShouldBuildCorrectKey()
        {
            var key = CacheKeyBuilder.ForDurak("merkez");

            Assert.AreEqual("durak:merkez", key);
        }
    }

    #endregion

    #region Circuit Breaker Tests

    [TestClass(Description = "Circuit breaker testleri")]
    public class CircuitBreakerTests
    {
        [TestMethod(Description = "Başarılı işlemde devre kapalı kalmalı")]
        public async Task CircuitBreaker_SuccessfulCall_ShouldStayClosed()
        {
            var breaker = new CircuitBreaker(failureThreshold: 3);

            var result = await breaker.ExecuteAsync(() => Task.FromResult("success"));

            Assert.AreEqual("success", result);
            Assert.AreEqual(CircuitState.Closed, breaker.State);
        }

        [TestMethod(Description = "Threshold aşılınca devre açılmalı")]
        public async Task CircuitBreaker_ExceedThreshold_ShouldOpen()
        {
            var breaker = new CircuitBreaker(failureThreshold: 2);

            for (int i = 0; i < 2; i++)
            {
                try
                {
                    await breaker.ExecuteAsync<int>(() => throw new Exception("fail"));
                }
                catch { }
            }

            Assert.AreEqual(CircuitState.Open, breaker.State);
        }

        [TestMethod(Description = "Devre açıkken exception fırlatmalı")]
        public async Task CircuitBreaker_WhenOpen_ShouldThrowException()
        {
            var breaker = new CircuitBreaker(failureThreshold: 1);

            try { await breaker.ExecuteAsync<int>(() => throw new Exception("fail")); } catch { }

            await Assert.ThrowsAsync<CircuitBreakerOpenException>(async () =>
            {
                await breaker.ExecuteAsync(() => Task.FromResult(1));
            });
        }
    }

    #endregion

    #region Retry Policy Tests

    [TestClass(Description = "Retry policy testleri")]
    public class RetryPolicyTests
    {
        [TestMethod(Description = "Başarılı işlemde retry olmamalı")]
        public async Task RetryPolicy_SuccessfulCall_ShouldNotRetry()
        {
            int callCount = 0;
            var policy = new RetryPolicyBuilder()
                .WithMaxRetries(3)
                .Build();

            var result = await policy.ExecuteAsync(() =>
            {
                callCount++;
                return Task.FromResult("success");
            });

            Assert.AreEqual(1, callCount);
            Assert.AreEqual("success", result);
        }

        [TestMethod(Description = "Geçici hata sonrası retry yapmalı")]
        public async Task RetryPolicy_TransientFailure_ShouldRetry()
        {
            int callCount = 0;
            var policy = new RetryPolicyBuilder()
                .WithMaxRetries(3)
                .WithInitialDelay(TimeSpan.FromMilliseconds(1))
                .Build();

            var result = await policy.ExecuteAsync(() =>
            {
                callCount++;
                if (callCount < 3)
                    throw new Exception("transient");
                return Task.FromResult("success");
            });

            Assert.AreEqual(3, callCount);
            Assert.AreEqual("success", result);
        }

        [TestMethod(Description = "Maksimum retry aşılınca exception fırlatmalı")]
        public async Task RetryPolicy_ExceedMaxRetries_ShouldThrow()
        {
            var policy = new RetryPolicyBuilder()
                .WithMaxRetries(2)
                .WithInitialDelay(TimeSpan.FromMilliseconds(1))
                .Build();

            await Assert.ThrowsAsync<RetryExhaustedException>(async () =>
            {
                await policy.ExecuteAsync<int>(() => throw new Exception("always fails"));
            });
        }
    }

    #endregion

    #region Input Validation Tests

    [TestClass(Description = "Input validation testleri")]
    public class InputValidationTests
    {
        [TestCase("test@example.com", true)]
        [TestCase("user.name@domain.org", true)]
        [TestCase("invalid-email", false)]
        [TestCase("@domain.com", false)]
        [TestCase("", false)]
        public void IsValidEmail_ShouldValidateCorrectly(string email, bool expected)
        {
            var result = InputValidator.IsValidEmail(email);
            Assert.AreEqual(expected, result);
        }

        [TestCase("abc123", true)]
        [TestCase("ABC", true)]
        [TestCase("123", true)]
        [TestCase("abc-123", false)]
        [TestCase("abc 123", false)]
        [TestCase("", false)]
        public void IsAlphanumeric_ShouldValidateCorrectly(string input, bool expected)
        {
            var result = InputValidator.IsAlphanumeric(input);
            Assert.AreEqual(expected, result);
        }

        [TestCase(41.0, 29.0, true)]  // Istanbul
        [TestCase(0.0, 0.0, true)]     // Equator/Greenwich
        [TestCase(-91.0, 0.0, false)]  // Invalid latitude
        [TestCase(0.0, 181.0, false)]  // Invalid longitude
        public void IsValidCoordinate_ShouldValidateCorrectly(double lat, double lon, bool expected)
        {
            var result = InputValidator.IsValidCoordinate(lat, lon);
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void Validate_WithRules_ShouldReturnCorrectResult()
        {
            var result = InputValidator.Validate("abc",
                new RequiredRule<string>("Name"),
                new StringLengthRule(2, 10, "Name"));

            Assert.IsTrue(result.IsValid);
        }

        [TestMethod]
        public void Validate_WithFailingRules_ShouldReturnErrors()
        {
            var result = InputValidator.Validate("",
                new RequiredRule<string>("Name"));

            Assert.IsFalse(result.IsValid);
            Assert.IsNotEmpty(result.Errors);
        }
    }

    #endregion

    #region Rate Limiter Tests

    [TestClass(Description = "Rate limiter testleri")]
    public class RateLimiterTests : IDisposable
    {
        private AdvancedRateLimiter _limiter;

        [TestSetup]
        public void Setup()
        {
            _limiter = new AdvancedRateLimiter(maxRequests: 3, windowSize: TimeSpan.FromMinutes(1));
        }

        [TestMethod]
        public void RateLimiter_WithinLimit_ShouldAllow()
        {
            Assert.IsTrue(_limiter.IsAllowed("user1"));
            Assert.IsTrue(_limiter.IsAllowed("user1"));
            Assert.IsTrue(_limiter.IsAllowed("user1"));
        }

        [TestMethod]
        public void RateLimiter_ExceedLimit_ShouldBlock()
        {
            _limiter.IsAllowed("user1");
            _limiter.IsAllowed("user1");
            _limiter.IsAllowed("user1");

            Assert.IsFalse(_limiter.IsAllowed("user1"));
        }

        [TestMethod]
        public void RateLimiter_DifferentUsers_ShouldTrackSeparately()
        {
            for (int i = 0; i < 3; i++)
            {
                _limiter.IsAllowed("user1");
            }

            // user2 should still be allowed
            Assert.IsTrue(_limiter.IsAllowed("user2"));
        }

        [TestMethod]
        public void RateLimiter_GetInfo_ShouldReturnCorrectInfo()
        {
            _limiter.IsAllowed("user1");
            _limiter.IsAllowed("user1");

            var info = _limiter.GetInfo("user1");

            Assert.IsTrue(info.IsAllowed);
            Assert.AreEqual(1, info.RemainingRequests);
        }

        public void Dispose()
        {
            _limiter?.Dispose();
        }
    }

    #endregion

    #region Encryption Tests

    [TestClass(Description = "Encryption testleri")]
    public class EncryptionTests
    {
        [TestMethod]
        public void Encrypt_AndDecrypt_ShouldReturnOriginal()
        {
            var original = "sensitive data 123";

            var encrypted = EncryptionHelper.Encrypt(original);
            var decrypted = EncryptionHelper.Decrypt(encrypted);

            Assert.AreNotEqual(original, encrypted);
            Assert.AreEqual(original, decrypted);
        }

        [TestMethod]
        public void Encrypt_SameInput_ShouldProduceSameOutput()
        {
            var input = "test";

            var result1 = EncryptionHelper.Encrypt(input);
            var result2 = EncryptionHelper.Encrypt(input);

            Assert.AreEqual(result1, result2);
        }

        [TestMethod]
        public void HashPassword_ShouldVerifyCorrectly()
        {
            var password = "mySecurePassword123";

            var hash = EncryptionHelper.HashPassword(password, out var salt);
            var isValid = EncryptionHelper.VerifyPassword(password, hash, salt);

            Assert.IsTrue(isValid);
        }

        [TestMethod]
        public void HashPassword_WrongPassword_ShouldNotVerify()
        {
            var password = "correct";
            var wrongPassword = "wrong";

            var hash = EncryptionHelper.HashPassword(password, out var salt);
            var isValid = EncryptionHelper.VerifyPassword(wrongPassword, hash, salt);

            Assert.IsFalse(isValid);
        }

        [TestMethod]
        public void ComputeHash_SameInput_ShouldProduceSameOutput()
        {
            var input = "test input";

            var hash1 = EncryptionHelper.ComputeHash(input);
            var hash2 = EncryptionHelper.ComputeHash(input);

            Assert.AreEqual(hash1, hash2);
        }

        [TestMethod]
        public void ComputeHash_DifferentInput_ShouldProduceDifferentOutput()
        {
            var hash1 = EncryptionHelper.ComputeHash("input1");
            var hash2 = EncryptionHelper.ComputeHash("input2");

            Assert.AreNotEqual(hash1, hash2);
        }

        [TestMethod]
        public void GenerateSecureToken_ShouldReturnUniqueValues()
        {
            var token1 = EncryptionHelper.GenerateSecureToken();
            var token2 = EncryptionHelper.GenerateSecureToken();

            Assert.AreNotEqual(token1, token2);
        }
    }

    #endregion

    #region Security Utils Tests

    [TestClass(Description = "Security utils testleri")]
    public class SecurityUtilsTests
    {
        [TestMethod]
        public void HtmlEncode_ShouldEncodeSpecialCharacters()
        {
            var input = "<script>alert('xss')</script>";
            var encoded = SecurityUtils.HtmlEncode(input);

            Assert.IsFalse(encoded.Contains("<script>"));
            Assert.Contains(encoded, "&lt;");
        }

        [TestCase("SELECT * FROM users", true)]
        [TestCase("DROP TABLE users", true)]
        [TestCase("normal input", false)]
        [TestCase("this is fine", false)]
        public void IsPotentialSqlInjection_ShouldDetectCorrectly(string input, bool expected)
        {
            var result = SecurityUtils.IsPotentialSqlInjection(input);
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void SanitizePath_ShouldRemoveTraversalPatterns()
        {
            var input = "../../../etc/passwd";
            var sanitized = SecurityUtils.SanitizePath(input);

            Assert.IsFalse(sanitized.Contains(".."));
        }

        [TestMethod]
        public void SecureCompare_EqualStrings_ShouldReturnTrue()
        {
            Assert.IsTrue(SecurityUtils.SecureCompare("secret", "secret"));
        }

        [TestMethod]
        public void SecureCompare_DifferentStrings_ShouldReturnFalse()
        {
            Assert.IsFalse(SecurityUtils.SecureCompare("secret1", "secret2"));
        }

        [TestMethod]
        public void SecureCompare_DifferentLengths_ShouldReturnFalse()
        {
            Assert.IsFalse(SecurityUtils.SecureCompare("short", "longer"));
        }
    }

    #endregion

    #region Result Pattern Tests

    [TestClass(Description = "Result pattern testleri")]
    public class ResultPatternTests
    {
        [TestMethod]
        public void Result_Success_ShouldBeSuccess()
        {
            var result = Result<int>.Success(42);

            Assert.IsTrue(result.IsSuccess);
            Assert.IsFalse(result.IsFailure);
            Assert.AreEqual(42, result.Value);
        }

        [TestMethod]
        public void Result_Failure_ShouldBeFailure()
        {
            var result = Result<int>.Failure("error message");

            Assert.IsFalse(result.IsSuccess);
            Assert.IsTrue(result.IsFailure);
            Assert.AreEqual("error message", result.Error);
        }

        [TestMethod]
        public void Result_Map_ShouldTransformValue()
        {
            var result = Result<int>.Success(10);

            var mapped = result.Map(x => x * 2);

            Assert.IsTrue(mapped.IsSuccess);
            Assert.AreEqual(20, mapped.Value);
        }

        [TestMethod]
        public void Result_Map_OnFailure_ShouldReturnFailure()
        {
            var result = Result<int>.Failure("error");

            var mapped = result.Map(x => x * 2);

            Assert.IsTrue(mapped.IsFailure);
            Assert.AreEqual("error", mapped.Error);
        }
    }

    #endregion

    #region Test Runner

    /// <summary>
    /// Tüm testleri çalıştırır.
    /// </summary>
    public static class TestsRunner
    {
        public static async Task<TestRunSummary> RunAllTestsAsync()
        {
            var runner = new TestRunner()
                .AddTestClass<DurakTests>()
                .AddTestClass<CacheTests>()
                .AddTestClass<CacheKeyBuilderTests>()
                .AddTestClass<CircuitBreakerTests>()
                .AddTestClass<RetryPolicyTests>()
                .AddTestClass<InputValidationTests>()
                .AddTestClass<RateLimiterTests>()
                .AddTestClass<EncryptionTests>()
                .AddTestClass<SecurityUtilsTests>()
                .AddTestClass<ResultPatternTests>();

            return await runner.RunAsync();
        }

        public static async Task<TestRunSummary> RunTestsByCategory(string category)
        {
            var runner = new TestRunner()
                .AddTestClass<DurakTests>()
                .AddTestClass<CacheTests>()
                .AddTestClass<CacheKeyBuilderTests>()
                .AddTestClass<CircuitBreakerTests>()
                .AddTestClass<RetryPolicyTests>()
                .AddTestClass<InputValidationTests>()
                .AddTestClass<RateLimiterTests>()
                .AddTestClass<EncryptionTests>()
                .AddTestClass<SecurityUtilsTests>()
                .AddTestClass<ResultPatternTests>()
                .FilterByCategory(category);

            return await runner.RunAsync();
        }
    }

    #endregion
}
