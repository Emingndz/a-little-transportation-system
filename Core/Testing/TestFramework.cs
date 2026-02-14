using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Prolab_4.Core.Logging;

namespace Prolab_4.Core.Testing
{
    #region Attributes

    /// <summary>
    /// Test sınıfını işaretler.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class TestClassAttribute : Attribute
    {
        public string Description { get; set; }
    }

    /// <summary>
    /// Test metodunu işaretler.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class TestMethodAttribute : Attribute
    {
        public string Description { get; set; }
        public int TimeoutMs { get; set; } = 30000;
    }

    /// <summary>
    /// Test öncesi setup.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class TestSetupAttribute : Attribute { }

    /// <summary>
    /// Test sonrası cleanup.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class TestCleanupAttribute : Attribute { }

    /// <summary>
    /// Tüm testlerden önce.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class TestClassSetupAttribute : Attribute { }

    /// <summary>
    /// Tüm testlerden sonra.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class TestClassCleanupAttribute : Attribute { }

    /// <summary>
    /// Parametreli test.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class TestCaseAttribute : Attribute
    {
        public object[] Parameters { get; }
        public string Name { get; set; }

        public TestCaseAttribute(params object[] parameters)
        {
            Parameters = parameters;
        }
    }

    /// <summary>
    /// Skip test.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class SkipAttribute : Attribute
    {
        public string Reason { get; }

        public SkipAttribute(string reason = null)
        {
            Reason = reason;
        }
    }

    /// <summary>
    /// Test kategorisi.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class TestCategoryAttribute : Attribute
    {
        public string Category { get; }

        public TestCategoryAttribute(string category)
        {
            Category = category;
        }
    }

    #endregion

    #region Test Results

    public enum TestStatus
    {
        Passed,
        Failed,
        Skipped,
        Error
    }

    public class TestResult
    {
        public string TestName { get; set; }
        public string ClassName { get; set; }
        public TestStatus Status { get; set; }
        public TimeSpan Duration { get; set; }
        public string ErrorMessage { get; set; }
        public string StackTrace { get; set; }
        public List<string> Output { get; set; } = new List<string>();

        public override string ToString()
        {
            var icon = Status switch
            {
                TestStatus.Passed => "✓",
                TestStatus.Failed => "✗",
                TestStatus.Skipped => "⊘",
                TestStatus.Error => "!",
                _ => "?"
            };

            return $"[{icon}] {ClassName}.{TestName} ({Duration.TotalMilliseconds:F0}ms)";
        }
    }

    public class TestRunSummary
    {
        public List<TestResult> Results { get; } = new List<TestResult>();
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan TotalDuration => EndTime - StartTime;

        public int TotalTests => Results.Count;
        public int PassedTests => Results.Count(r => r.Status == TestStatus.Passed);
        public int FailedTests => Results.Count(r => r.Status == TestStatus.Failed);
        public int SkippedTests => Results.Count(r => r.Status == TestStatus.Skipped);
        public int ErrorTests => Results.Count(r => r.Status == TestStatus.Error);

        public bool AllPassed => FailedTests == 0 && ErrorTests == 0;

        public void PrintSummary()
        {
            Console.WriteLine("\n" + new string('=', 60));
            Console.WriteLine("TEST RESULTS SUMMARY");
            Console.WriteLine(new string('=', 60));

            foreach (var result in Results)
            {
                var color = result.Status switch
                {
                    TestStatus.Passed => ConsoleColor.Green,
                    TestStatus.Failed => ConsoleColor.Red,
                    TestStatus.Skipped => ConsoleColor.Yellow,
                    TestStatus.Error => ConsoleColor.Magenta,
                    _ => ConsoleColor.Gray
                };

                Console.ForegroundColor = color;
                Console.WriteLine(result.ToString());

                if (!string.IsNullOrEmpty(result.ErrorMessage))
                {
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine($"   Error: {result.ErrorMessage}");
                }

                Console.ResetColor();
            }

            Console.WriteLine(new string('-', 60));
            Console.WriteLine($"Total: {TotalTests} | Passed: {PassedTests} | Failed: {FailedTests} | Skipped: {SkippedTests} | Errors: {ErrorTests}");
            Console.WriteLine($"Duration: {TotalDuration.TotalSeconds:F2}s");
            Console.WriteLine(new string('=', 60));
        }
    }

    #endregion

    #region Assertions

    /// <summary>
    /// Test doğrulama sınıfı.
    /// </summary>
    public static class Assert
    {
        public static void IsTrue(bool condition, string message = null)
        {
            if (!condition)
            {
                throw new AssertionException(message ?? "Expected true but was false");
            }
        }

        public static void IsFalse(bool condition, string message = null)
        {
            if (condition)
            {
                throw new AssertionException(message ?? "Expected false but was true");
            }
        }

        public static void AreEqual<T>(T expected, T actual, string message = null)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw new AssertionException(
                    message ?? $"Expected: {expected}\nActual: {actual}");
            }
        }

        public static void AreNotEqual<T>(T notExpected, T actual, string message = null)
        {
            if (EqualityComparer<T>.Default.Equals(notExpected, actual))
            {
                throw new AssertionException(
                    message ?? $"Expected not to be: {notExpected}\nBut was: {actual}");
            }
        }

        public static void IsNull(object obj, string message = null)
        {
            if (obj != null)
            {
                throw new AssertionException(message ?? $"Expected null but was: {obj}");
            }
        }

        public static void IsNotNull(object obj, string message = null)
        {
            if (obj == null)
            {
                throw new AssertionException(message ?? "Expected non-null value but was null");
            }
        }

        public static void IsEmpty<T>(IEnumerable<T> collection, string message = null)
        {
            if (collection.Any())
            {
                throw new AssertionException(message ?? $"Expected empty collection but had {collection.Count()} items");
            }
        }

        public static void IsNotEmpty<T>(IEnumerable<T> collection, string message = null)
        {
            if (!collection.Any())
            {
                throw new AssertionException(message ?? "Expected non-empty collection but was empty");
            }
        }

        public static void Contains<T>(IEnumerable<T> collection, T item, string message = null)
        {
            if (!collection.Contains(item))
            {
                throw new AssertionException(message ?? $"Collection does not contain: {item}");
            }
        }

        public static void DoesNotContain<T>(IEnumerable<T> collection, T item, string message = null)
        {
            if (collection.Contains(item))
            {
                throw new AssertionException(message ?? $"Collection should not contain: {item}");
            }
        }

        public static void Contains(string haystack, string needle, string message = null)
        {
            if (!haystack.Contains(needle))
            {
                throw new AssertionException(message ?? $"String does not contain: '{needle}'");
            }
        }

        public static TException Throws<TException>(Action action, string message = null) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException ex)
            {
                return ex;
            }
            catch (Exception ex)
            {
                throw new AssertionException(
                    message ?? $"Expected {typeof(TException).Name} but got {ex.GetType().Name}: {ex.Message}");
            }

            throw new AssertionException(
                message ?? $"Expected {typeof(TException).Name} but no exception was thrown");
        }

        public static async Task<TException> ThrowsAsync<TException>(Func<Task> action, string message = null) where TException : Exception
        {
            try
            {
                await action();
            }
            catch (TException ex)
            {
                return ex;
            }
            catch (Exception ex)
            {
                throw new AssertionException(
                    message ?? $"Expected {typeof(TException).Name} but got {ex.GetType().Name}: {ex.Message}");
            }

            throw new AssertionException(
                message ?? $"Expected {typeof(TException).Name} but no exception was thrown");
        }

        public static void DoesNotThrow(Action action, string message = null)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                throw new AssertionException(
                    message ?? $"Expected no exception but got {ex.GetType().Name}: {ex.Message}");
            }
        }

        public static void IsInstanceOf<T>(object obj, string message = null)
        {
            if (!(obj is T))
            {
                throw new AssertionException(
                    message ?? $"Expected instance of {typeof(T).Name} but was {obj?.GetType().Name ?? "null"}");
            }
        }

        public static void IsGreaterThan<T>(T actual, T minimum, string message = null) where T : IComparable<T>
        {
            if (actual.CompareTo(minimum) <= 0)
            {
                throw new AssertionException(
                    message ?? $"Expected {actual} to be greater than {minimum}");
            }
        }

        public static void IsLessThan<T>(T actual, T maximum, string message = null) where T : IComparable<T>
        {
            if (actual.CompareTo(maximum) >= 0)
            {
                throw new AssertionException(
                    message ?? $"Expected {actual} to be less than {maximum}");
            }
        }

        public static void IsInRange<T>(T actual, T min, T max, string message = null) where T : IComparable<T>
        {
            if (actual.CompareTo(min) < 0 || actual.CompareTo(max) > 0)
            {
                throw new AssertionException(
                    message ?? $"Expected {actual} to be in range [{min}, {max}]");
            }
        }

        public static void AreApproximatelyEqual(double expected, double actual, double tolerance = 0.0001, string message = null)
        {
            if (Math.Abs(expected - actual) > tolerance)
            {
                throw new AssertionException(
                    message ?? $"Expected {expected} ± {tolerance} but was {actual}");
            }
        }

        public static void Fail(string message = null)
        {
            throw new AssertionException(message ?? "Test failed");
        }

        public static void Pass()
        {
            // Testin başarılı olduğunu belirtmek için
        }
    }

    public class AssertionException : Exception
    {
        public AssertionException(string message) : base(message) { }
    }

    #endregion

    #region Test Runner

    /// <summary>
    /// Test çalıştırıcı.
    /// </summary>
    public class TestRunner
    {
        private readonly List<Type> _testClasses;
        private readonly TestRunSummary _summary;
        private string _categoryFilter;
        private bool _stopOnFirstFailure;

        public TestRunner()
        {
            _testClasses = new List<Type>();
            _summary = new TestRunSummary();
        }

        public TestRunner AddTestClass<T>() where T : class
        {
            _testClasses.Add(typeof(T));
            return this;
        }

        public TestRunner AddTestClassesFromAssembly(Assembly assembly)
        {
            var testClasses = assembly.GetTypes()
                .Where(t => t.GetCustomAttribute<TestClassAttribute>() != null);

            _testClasses.AddRange(testClasses);
            return this;
        }

        public TestRunner FilterByCategory(string category)
        {
            _categoryFilter = category;
            return this;
        }

        public TestRunner StopOnFirstFailure()
        {
            _stopOnFirstFailure = true;
            return this;
        }

        public async Task<TestRunSummary> RunAsync()
        {
            _summary.StartTime = DateTime.Now;

            foreach (var testClass in _testClasses)
            {
                if (testClass.GetCustomAttribute<SkipAttribute>() != null)
                {
                    SkipAllTestsInClass(testClass);
                    continue;
                }

                await RunTestClassAsync(testClass);

                if (_stopOnFirstFailure && _summary.FailedTests > 0)
                {
                    break;
                }
            }

            _summary.EndTime = DateTime.Now;
            return _summary;
        }

        private async Task RunTestClassAsync(Type testClass)
        {
            object instance = null;

            try
            {
                instance = Activator.CreateInstance(testClass);

                // Class setup
                var classSetup = testClass.GetMethods()
                    .FirstOrDefault(m => m.GetCustomAttribute<TestClassSetupAttribute>() != null);

                if (classSetup != null)
                {
                    await InvokeMethodAsync(instance, classSetup);
                }

                // Get test methods
                var testMethods = testClass.GetMethods()
                    .Where(m => m.GetCustomAttribute<TestMethodAttribute>() != null)
                    .Where(m => ShouldRunTest(m))
                    .ToList();

                foreach (var method in testMethods)
                {
                    var testCases = method.GetCustomAttributes<TestCaseAttribute>().ToList();

                    if (testCases.Any())
                    {
                        // Parametreli testler
                        foreach (var testCase in testCases)
                        {
                            var testName = testCase.Name ?? $"{method.Name}({string.Join(", ", testCase.Parameters)})";
                            await RunSingleTestAsync(testClass, instance, method, testName, testCase.Parameters);
                        }
                    }
                    else
                    {
                        // Normal test
                        await RunSingleTestAsync(testClass, instance, method, method.Name);
                    }

                    if (_stopOnFirstFailure && _summary.FailedTests > 0)
                    {
                        break;
                    }
                }

                // Class cleanup
                var classCleanup = testClass.GetMethods()
                    .FirstOrDefault(m => m.GetCustomAttribute<TestClassCleanupAttribute>() != null);

                if (classCleanup != null)
                {
                    await InvokeMethodAsync(instance, classCleanup);
                }
            }
            finally
            {
                (instance as IDisposable)?.Dispose();
            }
        }

        private async Task RunSingleTestAsync(Type testClass, object instance, MethodInfo method, string testName, object[] parameters = null)
        {
            var result = new TestResult
            {
                ClassName = testClass.Name,
                TestName = testName
            };

            var skip = method.GetCustomAttribute<SkipAttribute>();
            if (skip != null)
            {
                result.Status = TestStatus.Skipped;
                result.ErrorMessage = skip.Reason;
                _summary.Results.Add(result);
                return;
            }

            var testAttr = method.GetCustomAttribute<TestMethodAttribute>();
            var sw = Stopwatch.StartNew();

            try
            {
                // Setup
                var setup = testClass.GetMethods()
                    .FirstOrDefault(m => m.GetCustomAttribute<TestSetupAttribute>() != null);

                if (setup != null)
                {
                    await InvokeMethodAsync(instance, setup);
                }

                // Test with timeout
                var timeoutTask = Task.Delay(testAttr.TimeoutMs);
                var testTask = InvokeMethodAsync(instance, method, parameters);

                var completed = await Task.WhenAny(testTask, timeoutTask);

                if (completed == timeoutTask)
                {
                    throw new TimeoutException($"Test timed out after {testAttr.TimeoutMs}ms");
                }

                await testTask;

                result.Status = TestStatus.Passed;

                // Cleanup
                var cleanup = testClass.GetMethods()
                    .FirstOrDefault(m => m.GetCustomAttribute<TestCleanupAttribute>() != null);

                if (cleanup != null)
                {
                    await InvokeMethodAsync(instance, cleanup);
                }
            }
            catch (AssertionException ex)
            {
                result.Status = TestStatus.Failed;
                result.ErrorMessage = ex.Message;
                result.StackTrace = ex.StackTrace;
            }
            catch (TargetInvocationException ex) when (ex.InnerException is AssertionException inner)
            {
                result.Status = TestStatus.Failed;
                result.ErrorMessage = inner.Message;
                result.StackTrace = inner.StackTrace;
            }
            catch (Exception ex)
            {
                result.Status = TestStatus.Error;
                result.ErrorMessage = ex.InnerException?.Message ?? ex.Message;
                result.StackTrace = ex.InnerException?.StackTrace ?? ex.StackTrace;
            }
            finally
            {
                sw.Stop();
                result.Duration = sw.Elapsed;
                _summary.Results.Add(result);
            }
        }

        private async Task InvokeMethodAsync(object instance, MethodInfo method, object[] parameters = null)
        {
            var result = method.Invoke(instance, parameters);

            if (result is Task task)
            {
                await task;
            }
        }

        private bool ShouldRunTest(MethodInfo method)
        {
            if (string.IsNullOrEmpty(_categoryFilter))
            {
                return true;
            }

            var category = method.GetCustomAttribute<TestCategoryAttribute>();
            return category?.Category == _categoryFilter;
        }

        private void SkipAllTestsInClass(Type testClass)
        {
            var skip = testClass.GetCustomAttribute<SkipAttribute>();
            var testMethods = testClass.GetMethods()
                .Where(m => m.GetCustomAttribute<TestMethodAttribute>() != null);

            foreach (var method in testMethods)
            {
                _summary.Results.Add(new TestResult
                {
                    ClassName = testClass.Name,
                    TestName = method.Name,
                    Status = TestStatus.Skipped,
                    ErrorMessage = skip?.Reason ?? "Class skipped"
                });
            }
        }
    }

    #endregion

    #region Mocking

    /// <summary>
    /// Basit mock framework.
    /// </summary>
    public class Mock<T> where T : class
    {
        private readonly Dictionary<string, object> _returns;
        private readonly Dictionary<string, int> _callCounts;
        private readonly Dictionary<string, List<object[]>> _callHistory;
        private readonly Dictionary<string, Action<object[]>> _callbacks;

        public Mock()
        {
            _returns = new Dictionary<string, object>();
            _callCounts = new Dictionary<string, int>();
            _callHistory = new Dictionary<string, List<object[]>>();
            _callbacks = new Dictionary<string, Action<object[]>>();
        }

        public MockSetup<T, TResult> Setup<TResult>(Expression<Func<T, TResult>> expression)
        {
            var methodName = GetMethodName(expression);
            return new MockSetup<T, TResult>(this, methodName);
        }

        public MockSetup<T> Setup(Expression<Action<T>> expression)
        {
            var methodName = GetMethodName(expression);
            return new MockSetup<T>(this, methodName);
        }

        public void Verify<TResult>(Expression<Func<T, TResult>> expression, Times times = null)
        {
            var methodName = GetMethodName(expression);
            var count = _callCounts.GetValueOrDefault(methodName, 0);

            times ??= Times.AtLeastOnce();

            if (!times.Verify(count))
            {
                throw new AssertionException(
                    $"Method '{methodName}' was called {count} times but expected {times}");
            }
        }

        public void Verify(Expression<Action<T>> expression, Times times = null)
        {
            var methodName = GetMethodName(expression);
            var count = _callCounts.GetValueOrDefault(methodName, 0);

            times ??= Times.AtLeastOnce();

            if (!times.Verify(count))
            {
                throw new AssertionException(
                    $"Method '{methodName}' was called {count} times but expected {times}");
            }
        }

        public T Object => CreateProxy();

        internal void SetReturn(string methodName, object value)
        {
            _returns[methodName] = value;
        }

        internal void SetCallback(string methodName, Action<object[]> callback)
        {
            _callbacks[methodName] = callback;
        }

        internal object Invoke(string methodName, object[] args)
        {
            _callCounts[methodName] = _callCounts.GetValueOrDefault(methodName, 0) + 1;

            if (!_callHistory.TryGetValue(methodName, out var history))
            {
                history = new List<object[]>();
                _callHistory[methodName] = history;
            }
            history.Add(args);

            if (_callbacks.TryGetValue(methodName, out var callback))
            {
                callback(args);
            }

            return _returns.GetValueOrDefault(methodName);
        }

        private T CreateProxy()
        {
            // Basit proxy - gerçek uygulamada Castle.DynamicProxy kullanılır
            // Burada interface'ler için basit bir yaklaşım
            return default;
        }

        private string GetMethodName(Expression expression)
        {
            if (expression is LambdaExpression lambda)
            {
                if (lambda.Body is MethodCallExpression methodCall)
                {
                    return methodCall.Method.Name;
                }
            }
            return "Unknown";
        }
    }

    public class MockSetup<T, TResult> where T : class
    {
        private readonly Mock<T> _mock;
        private readonly string _methodName;

        public MockSetup(Mock<T> mock, string methodName)
        {
            _mock = mock;
            _methodName = methodName;
        }

        public Mock<T> Returns(TResult value)
        {
            _mock.SetReturn(_methodName, value);
            return _mock;
        }

        public Mock<T> Returns(Func<TResult> factory)
        {
            _mock.SetCallback(_methodName, _ => _mock.SetReturn(_methodName, factory()));
            return _mock;
        }

        public Mock<T> Throws<TException>() where TException : Exception, new()
        {
            _mock.SetCallback(_methodName, _ => throw new TException());
            return _mock;
        }

        public Mock<T> Callback(Action<object[]> callback)
        {
            _mock.SetCallback(_methodName, callback);
            return _mock;
        }
    }

    public class MockSetup<T> where T : class
    {
        private readonly Mock<T> _mock;
        private readonly string _methodName;

        public MockSetup(Mock<T> mock, string methodName)
        {
            _mock = mock;
            _methodName = methodName;
        }

        public Mock<T> Throws<TException>() where TException : Exception, new()
        {
            _mock.SetCallback(_methodName, _ => throw new TException());
            return _mock;
        }

        public Mock<T> Callback(Action callback)
        {
            _mock.SetCallback(_methodName, _ => callback());
            return _mock;
        }
    }

    public class Times
    {
        private readonly Func<int, bool> _verifier;
        private readonly string _description;

        private Times(Func<int, bool> verifier, string description)
        {
            _verifier = verifier;
            _description = description;
        }

        public bool Verify(int count) => _verifier(count);

        public override string ToString() => _description;

        public static Times Never() => new Times(c => c == 0, "never");
        public static Times Once() => new Times(c => c == 1, "once");
        public static Times Exactly(int count) => new Times(c => c == count, $"exactly {count} times");
        public static Times AtLeast(int count) => new Times(c => c >= count, $"at least {count} times");
        public static Times AtMost(int count) => new Times(c => c <= count, $"at most {count} times");
        public static Times AtLeastOnce() => new Times(c => c >= 1, "at least once");
        public static Times Between(int min, int max) => new Times(c => c >= min && c <= max, $"between {min} and {max} times");
    }

    #endregion

    #region Test Data

    /// <summary>
    /// Test verisi oluşturucu.
    /// </summary>
    public class TestDataBuilder<T> where T : class, new()
    {
        private readonly T _instance;

        public TestDataBuilder()
        {
            _instance = new T();
        }

        public TestDataBuilder<T> With<TProperty>(Expression<Func<T, TProperty>> property, TProperty value)
        {
            var memberExpression = property.Body as MemberExpression;
            var propertyInfo = memberExpression?.Member as PropertyInfo;

            propertyInfo?.SetValue(_instance, value);
            return this;
        }

        public T Build() => _instance;

        public static T Random()
        {
            var instance = new T();
            var props = typeof(T).GetProperties().Where(p => p.CanWrite);
            var random = new Random();

            foreach (var prop in props)
            {
                var value = GenerateRandomValue(prop.PropertyType, random);
                if (value != null)
                {
                    prop.SetValue(instance, value);
                }
            }

            return instance;
        }

        private static object GenerateRandomValue(Type type, Random random)
        {
            if (type == typeof(string))
                return Guid.NewGuid().ToString("N").Substring(0, 8);
            if (type == typeof(int))
                return random.Next(1, 1000);
            if (type == typeof(double))
                return random.NextDouble() * 100;
            if (type == typeof(decimal))
                return (decimal)(random.NextDouble() * 100);
            if (type == typeof(bool))
                return random.Next(2) == 1;
            if (type == typeof(DateTime))
                return DateTime.Now.AddDays(random.Next(-365, 365));
            if (type == typeof(Guid))
                return Guid.NewGuid();

            return null;
        }
    }

    #endregion
}
