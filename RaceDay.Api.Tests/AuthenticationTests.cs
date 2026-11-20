
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RaceDay.Api.Controllers;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Tests
{
    /* ============================================================
       RACEDAY EVENT MANAGEMENT SYSTEM
       PROG6212 - POE PART 2
       REGISTRATION, LOGIN AND PASSWORD TESTS
       ============================================================ */

    /* ============================================================
       Code Attribution

       TITLE: Unit Testing C# with MSTest
       AUTHOR: Microsoft
       DATE: n.d.
       AVAILABLE:
       https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-mstest-intro
       DATE ACCESSED: 19 October 2026

       Usage: Referenced for writing automated authentication
       tests using MSTest assertions and test methods.

       ============================================================ */

    /* ============================================================
       Code Attribution

       TITLE: Testing with EF Core In-Memory
       AUTHOR: Microsoft
       DATE: n.d.
       AVAILABLE:
       https://learn.microsoft.com/en-us/ef/core/providers/in-memory/
       DATE ACCESSED: 19 October 2026

       Usage: Referenced for creating an isolated database
       for testing RaceDay controller operations.

       ============================================================ */

    /* ============================================================
       Code Attribution

       TITLE: PasswordHasher<TUser> Class
       AUTHOR: Microsoft
       DATE: n.d.
       AVAILABLE:
       https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.identity.passwordhasher-1
       DATE ACCESSED: 19 October 2026

       Usage: Referenced for testing password hashing
       and password verification.

       ============================================================ */

    /* ============================================================
       Code Attribution

       TITLE: Session and State Management in ASP.NET Core
       AUTHOR: Microsoft
       DATE: n.d.
       AVAILABLE:
       https://learn.microsoft.com/en-us/aspnet/core/fundamentals/app-state
       DATE ACCESSED: 19 October 2026

       Usage: Referenced for testing the session values
       created after successful RaceDay login.

       ============================================================ */

    [TestClass]
    public class AuthenticationTests
    {
        /* ========================================================
           SECTION 1: CREATE ISOLATED TEST DATABASE
           ======================================================== */

        // Create a fresh in-memory database for each test.
        // This prevents test data from being saved to
        // the actual RaceDayPart2DB SQL Server database.
        private static RaceDayDbContext CreateTestDatabase()
        {
            // Generate a unique database name for test isolation.
            string databaseName =
                "RaceDayAuthTests_" + Guid.NewGuid();

            // Configure Entity Framework Core to use memory.
            var options =
                new DbContextOptionsBuilder<RaceDayDbContext>()
                    .UseInMemoryDatabase(databaseName)
                    .Options;

            // Create the RaceDay database context.
            var context = new RaceDayDbContext(options);

            // Create the tables and apply EF Core model seed data.
            context.Database.EnsureCreated();

            // Ensure the Organiser role is available for testing.
            if (!context.Roles.Any(
                role => role.RoleName == "Organiser"))
            {
                context.Roles.Add(new Role
                {
                    RoleId = 1,
                    RoleName = "Organiser"
                });
            }

            // Ensure the Participant role is available.
            if (!context.Roles.Any(
                role => role.RoleName == "Participant"))
            {
                context.Roles.Add(new Role
                {
                    RoleId = 2,
                    RoleName = "Participant"
                });
            }

            // Save any missing test roles.
            context.SaveChanges();

            return context;
        }


        /* ========================================================
           SECTION 2: CREATE PASSWORD SERVICE
           ======================================================== */

        // Use the real RaceDay password service.
        // This avoids replacing password hashing with fake logic.
        private static IPasswordService CreatePasswordService()
        {
            // Create Microsoft's Identity password hasher.
            var hasher = new PasswordHasher<User>();

            // Pass it to the existing RaceDay service.
            return new PasswordService(hasher);
        }


        /* ========================================================
           SECTION 3: SIMULATE A TEST SESSION
           ======================================================== */

        // Authentication tests run without an actual browser.
        // This class supplies the session operations required
        // by AuthController during login.
        private sealed class TestSession : ISession
        {
            // Store session values using an in-memory dictionary.
            private readonly Dictionary<string, byte[]> _values =
                new Dictionary<string, byte[]>();

            // Give the test session an identifier.
            public string Id { get; } =
                Guid.NewGuid().ToString();

            // The session is always available during tests.
            public bool IsAvailable => true;

            // Return the names of stored session entries.
            public IEnumerable<string> Keys => _values.Keys;

            // Remove all values when login clears the session.
            public void Clear()
            {
                _values.Clear();
            }

            // Remove a specific session entry.
            public void Remove(string key)
            {
                _values.Remove(key);
            }

            // Save a session value.
            public void Set(string key, byte[] value)
            {
                _values[key] = value;
            }

            // Retrieve a value from the test session.
            public bool TryGetValue(
                string key,
                out byte[]? value)
            {
                return _values.TryGetValue(key, out value);
            }

            // There is no external session store to load.
            public Task LoadAsync(
                CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }

            // No external storage operation is required.
            public Task CommitAsync(
                CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }
        }


        // Provide the mock session through ASP.NET Core features.
        private sealed class TestSessionFeature : ISessionFeature
        {
            public ISession Session { get; set; } =
                new TestSession();
        }


        /* ========================================================
           SECTION 4: CREATE AUTH CONTROLLER
           ======================================================== */

        // Create the real controller with test dependencies.
        private static AuthController CreateController(
            RaceDayDbContext context,
            ISession? session = null)
        {
            // Use the actual password service.
            IPasswordService passwordService =
                CreatePasswordService();

            // Create the existing RaceDay AuthController.
            var controller = new AuthController(
                context,
                passwordService);

            // Create an HTTP context for controller testing.
            var httpContext = new DefaultHttpContext();

            // Attach a simulated server-side session.
            httpContext.Features.Set<ISessionFeature>(
                new TestSessionFeature
                {
                    Session = session ?? new TestSession()
                });

            // Attach the HTTP context to the controller.
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            return controller;
        }


        /* ========================================================
           SECTION 5: CREATE TEST REGISTRATION REQUEST
           ======================================================== */

        // Reuse consistent fictional account information.
        private static RegisterRequestDto CreateRegistrationRequest()
        {
            return new RegisterRequestDto
            {
                FirstName = "Test",

                LastName = "Participant",

                Email = "mstest.participant@example.com",

                Password = "RaceDayTest2026!",

                PhoneNumber = "0825550100",

                DateOfBirth = new DateOnly(2002, 6, 15),

                Role = "Participant"
            };
        }


        /* ========================================================
           SECTION 6: SUCCESSFUL REGISTRATION
           ======================================================== */

        // TEST 1: A new Participant should be registered.
        [TestMethod]
        public async Task Register_ValidParticipant_ShouldCreateAccount()
        {
            // Arrange
            using var context = CreateTestDatabase();

            AuthController controller =
                CreateController(context);

            RegisterRequestDto request =
                CreateRegistrationRequest();

            // Act
            var response = await controller.Register(
                request,
                CancellationToken.None);

            // Assert
            var result = response.Result as ObjectResult
                ?? throw new AssertFailedException(
                    "Expected an HTTP object response.");

            Assert.AreEqual(
                StatusCodes.Status201Created,
                result.StatusCode);

            // Confirm the account was saved.
            User? account = await context.Users
                .FirstOrDefaultAsync(
                    user => user.Email == request.Email);

            Assert.IsNotNull(account);

            Assert.AreEqual(
                request.FirstName,
                account.FirstName);

            Assert.AreEqual(
                request.Email,
                account.Email);

            // The original password must not be stored.
            Assert.AreNotEqual(
                request.Password,
                account.PasswordHash);
        }


        /* ========================================================
           SECTION 7: DUPLICATE EMAIL REGISTRATION
           ======================================================== */

        // TEST 2: A duplicate email should be rejected.
        [TestMethod]
        public async Task Register_DuplicateEmail_ShouldReturn409()
        {
            // Arrange
            using var context = CreateTestDatabase();

            AuthController controller =
                CreateController(context);

            RegisterRequestDto request =
                CreateRegistrationRequest();

            // Register the account for the first time.
            await controller.Register(
                request,
                CancellationToken.None);

            // Act
            // Try registering the same account again.
            var response = await controller.Register(
                request,
                CancellationToken.None);

            // Assert
            var result = response.Result as ObjectResult
                ?? throw new AssertFailedException(
                    "Expected an HTTP object response.");

            Assert.AreEqual(
                StatusCodes.Status409Conflict,
                result.StatusCode);

            // Only one account should exist for the email.
            int accountCount = await context.Users.CountAsync(
                user => user.Email == request.Email);

            Assert.AreEqual(1, accountCount);
        }


        /* ========================================================
           SECTION 8: SUCCESSFUL LOGIN
           ======================================================== */

        // TEST 3: Correct credentials should allow login.
        [TestMethod]
        public async Task Login_ValidCredentials_ShouldReturn200()
        {
            // Arrange
            using var context = CreateTestDatabase();

            var session = new TestSession();

            AuthController controller =
                CreateController(context, session);

            RegisterRequestDto registration =
                CreateRegistrationRequest();

            // Create an account before attempting login.
            await controller.Register(
                registration,
                CancellationToken.None);

            var login = new LoginRequestDto
            {
                Email = registration.Email,
                Password = registration.Password
            };

            // Act
            var response = await controller.Login(
                login,
                CancellationToken.None);

            // Assert
            var result = response.Result as ObjectResult
                ?? throw new AssertFailedException(
                    "Expected an HTTP object response.");

            Assert.AreEqual(
                StatusCodes.Status200OK,
                result.StatusCode);

            // Verify the controller stored the user ID.
            int? sessionUserId =
                session.GetInt32("UserId");

            Assert.IsTrue(
                sessionUserId.HasValue &&
                sessionUserId.Value > 0);

            // Verify the Participant role was stored.
            Assert.AreEqual(
                "Participant",
                session.GetString("Role"));
        }


        /* ========================================================
           SECTION 9: INCORRECT PASSWORD
           ======================================================== */

        // TEST 4: An incorrect password should reject login.
        [TestMethod]
        public async Task Login_WrongPassword_ShouldReturn401()
        {
            // Arrange
            using var context = CreateTestDatabase();

            var session = new TestSession();

            AuthController controller =
                CreateController(context, session);

            RegisterRequestDto registration =
                CreateRegistrationRequest();

            // Create a registered Participant account.
            await controller.Register(
                registration,
                CancellationToken.None);

            // Submit an incorrect password.
            var login = new LoginRequestDto
            {
                Email = registration.Email,
                Password = "IncorrectPassword123!"
            };

            // Act
            var response = await controller.Login(
                login,
                CancellationToken.None);

            // Assert
            var result = response.Result as ObjectResult
                ?? throw new AssertFailedException(
                    "Expected an HTTP object response.");

            Assert.AreEqual(
                StatusCodes.Status401Unauthorized,
                result.StatusCode);

            // Incorrect login must not create a session.
            Assert.IsNull(
                session.GetInt32("UserId"));
        }


        /* ========================================================
           SECTION 10: NONEXISTENT ACCOUNT
           ======================================================== */

        // TEST 5: A user who does not exist cannot log in.
        [TestMethod]
        public async Task Login_UnknownEmail_ShouldReturn401()
        {
            // Arrange
            using var context = CreateTestDatabase();

            var session = new TestSession();

            AuthController controller =
                CreateController(context, session);

            var login = new LoginRequestDto
            {
                Email = "unknown@example.com",
                Password = "RaceDayTest2026!"
            };

            // Act
            var response = await controller.Login(
                login,
                CancellationToken.None);

            // Assert
            var result = response.Result as ObjectResult
                ?? throw new AssertFailedException(
                    "Expected an HTTP object response.");

            Assert.AreEqual(
                StatusCodes.Status401Unauthorized,
                result.StatusCode);

            Assert.IsNull(
                session.GetInt32("UserId"));
        }


        /* ========================================================
           SECTION 11: VALID PASSWORD VERIFICATION
           ======================================================== */

        // TEST 6: A correct password should match its hash.
        [TestMethod]
        public void PasswordService_CorrectPassword_ShouldVerify()
        {
            // Arrange
            IPasswordService passwordService =
                CreatePasswordService();

            var user = new User();

            string password = "RaceDayTest2026!";

            // Hash the original password.
            string passwordHash =
                passwordService.HashPassword(user, password);

            // Act
            PasswordVerificationResult result =
                passwordService.VerifyPassword(
                    user,
                    passwordHash,
                    password);

            // Assert
            Assert.AreNotEqual(
                PasswordVerificationResult.Failed,
                result);
        }


        /* ========================================================
           SECTION 12: INVALID PASSWORD VERIFICATION
           ======================================================== */

        // TEST 7: A wrong password must not match the hash.
        [TestMethod]
        public void PasswordService_WrongPassword_ShouldFail()
        {
            // Arrange
            IPasswordService passwordService =
                CreatePasswordService();

            var user = new User();

            string passwordHash =
                passwordService.HashPassword(
                    user,
                    "RaceDayTest2026!");

            // Act
            PasswordVerificationResult result =
                passwordService.VerifyPassword(
                    user,
                    passwordHash,
                    "WrongPassword2026!");

            // Assert
            Assert.AreEqual(
                PasswordVerificationResult.Failed,
                result);
        }


        /* ========================================================
           SECTION 13: PASSWORD SALTING
           ======================================================== */

        // TEST 8: Repeated passwords should have different hashes.
        [TestMethod]
        public void PasswordService_SamePassword_ShouldUseDifferentSalt()
        {
            // Arrange
            IPasswordService passwordService =
                CreatePasswordService();

            var user = new User();

            const string password = "RaceDayTest2026!";

            // Act
            string firstHash =
                passwordService.HashPassword(user, password);

            string secondHash =
                passwordService.HashPassword(user, password);

            // Assert
            // Identity creates a random salt for each hash.
            Assert.AreNotEqual(firstHash, secondHash);

            // Both hashes must still verify correctly.
            Assert.AreNotEqual(
                PasswordVerificationResult.Failed,
                passwordService.VerifyPassword(
                    user,
                    firstHash,
                    password));

            Assert.AreNotEqual(
                PasswordVerificationResult.Failed,
                passwordService.VerifyPassword(
                    user,
                    secondHash,
                    password));
        }
    }
}
