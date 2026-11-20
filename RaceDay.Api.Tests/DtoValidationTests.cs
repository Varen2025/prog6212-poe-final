
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RaceDay.Api.DTOs;

namespace RaceDay.Api.Tests
{
    /* ============================================================
       RACEDAY EVENT MANAGEMENT SYSTEM
       PROG6212 - POE PART 2
       AUTOMATED DTO VALIDATION TESTS
       ============================================================ */

    /* ============================================================
       Code Attribution

       TITLE: Unit Testing C# with MSTest
       AUTHOR: Microsoft
       DATE: n.d.
       AVAILABLE:
       https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-mstest-intro
       DATE ACCESSED: 19 October 2026

       Usage: Referenced for creating automated MSTest
       tests using TestClass, TestMethod and DataRow.

       ============================================================ */

    /* ============================================================
       Code Attribution

       TITLE: Validator.TryValidateObject Method
       AUTHOR: Microsoft
       DATE: n.d.
       AVAILABLE:
       https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations.validator.tryvalidateobject
       DATE ACCESSED: 19 October 2026

       Usage: Referenced for testing validation attributes
       and custom validation rules in RaceDay DTOs.

       ============================================================ */

    // Contains automated tests for RaceDay request validation.
    [TestClass]
    public class DtoValidationTests
    {
        /* ========================================================
           SECTION 1: VALIDATION HELPER
           ======================================================== */

        // Check whether a request object passes its validation.
        private static bool IsValid(object model)
        {
            // Create a list to store validation errors.
            var results = new List<ValidationResult>();

            // Prepare the validation context.
            var context = new ValidationContext(model);

            // Check all properties and custom validation rules.
            return Validator.TryValidateObject(
                model,
                context,
                results,
                validateAllProperties: true);
        }


        /* ========================================================
           SECTION 2: EVENT VALIDATION TESTS
           ======================================================== */

        // Create a valid event for use in different tests.
        private static EventRequestDto CreateValidEvent()
        {
            return new EventRequestDto
            {
                Name = "Durban Summer 10K",

                Description =
                    "A community running event in Durban.",

                EventDate = DateTime.Now.AddDays(30),

                Location = "Durban Beachfront",

                DistanceKm = 10.00m,

                EventType = "Run"
            };
        }


        // TEST 1: A correctly completed event should be valid.
        [TestMethod]
        public void ValidEvent_ShouldPassValidation()
        {
            // Arrange
            EventRequestDto request = CreateValidEvent();

            // Act
            bool result = IsValid(request);

            // Assert
            Assert.IsTrue(result);
        }


        // TEST 2: An event must have a name.
        [TestMethod]
        public void EmptyEventName_ShouldFailValidation()
        {
            // Arrange
            EventRequestDto request = CreateValidEvent();
            request.Name = "";

            // Act
            bool result = IsValid(request);

            // Assert
            Assert.IsFalse(result);
        }


        // TEST 3: Only Run, Walk and Cycle are permitted.
        // MSTest executes one test for each invalid value.
        [TestMethod]
        [DataRow("Swim")]
        [DataRow("Football")]
        [DataRow("Drive")]
        public void InvalidEventType_ShouldFailValidation(
            string eventType)
        {
            // Arrange
            EventRequestDto request = CreateValidEvent();
            request.EventType = eventType;

            // Act
            bool result = IsValid(request);

            // Assert
            Assert.IsFalse(result);
        }


        // TEST 4: An event distance must be positive.
        [TestMethod]
        public void ZeroEventDistance_ShouldFailValidation()
        {
            // Arrange
            EventRequestDto request = CreateValidEvent();
            request.DistanceKm = 0;

            // Act
            bool result = IsValid(request);

            // Assert
            Assert.IsFalse(result);
        }


        /* ========================================================
           SECTION 3: CATEGORY VALIDATION TESTS
           ======================================================== */

        // TEST 5: A valid age category should pass validation.
        [TestMethod]
        public void ValidAgeCategory_ShouldPassValidation()
        {
            // Arrange
            var request = new CategoryRequestDto
            {
                CategoryName = "Adults 18-29",
                CategoryType = "Age",
                MinAge = 18,
                MaxAge = 29,
                DistanceKm = null
            };

            // Act
            bool result = IsValid(request);

            // Assert
            Assert.IsTrue(result);
        }


        // TEST 6: A valid distance category should be accepted.
        [TestMethod]
        public void ValidDistanceCategory_ShouldPassValidation()
        {
            // Arrange
            var request = new CategoryRequestDto
            {
                CategoryName = "5K Fun Run",
                CategoryType = "Distance",
                MinAge = null,
                MaxAge = null,
                DistanceKm = 5.00m
            };

            // Act
            bool result = IsValid(request);

            // Assert
            Assert.IsTrue(result);
        }


        // TEST 7: Age categories require a maximum age.
        [TestMethod]
        public void AgeCategoryWithoutMaximumAge_ShouldFail()
        {
            // Arrange
            var request = new CategoryRequestDto
            {
                CategoryName = "Junior Category",
                CategoryType = "Age",
                MinAge = 12,
                MaxAge = null,
                DistanceKm = null
            };

            // Act
            bool result = IsValid(request);

            // Assert
            Assert.IsFalse(result);
        }


        // TEST 8: Minimum age cannot exceed maximum age.
        [TestMethod]
        public void InvalidAgeRange_ShouldFailValidation()
        {
            // Arrange
            var request = new CategoryRequestDto
            {
                CategoryName = "Invalid Age Category",
                CategoryType = "Age",
                MinAge = 40,
                MaxAge = 20,
                DistanceKm = null
            };

            // Act
            bool result = IsValid(request);

            // Assert
            Assert.IsFalse(result);
        }


        // TEST 9: Distance categories cannot include age limits.
        [TestMethod]
        public void DistanceCategoryWithAgeLimits_ShouldFail()
        {
            // Arrange
            var request = new CategoryRequestDto
            {
                CategoryName = "10K Category",
                CategoryType = "Distance",
                MinAge = 18,
                MaxAge = 35,
                DistanceKm = 10.00m
            };

            // Act
            bool result = IsValid(request);

            // Assert
            Assert.IsFalse(result);
        }


        /* ========================================================
           SECTION 4: ENROLMENT VALIDATION TESTS
           ======================================================== */

        // TEST 10: Valid event and category IDs are accepted.
        [TestMethod]
        public void ValidEnrolmentRequest_ShouldPassValidation()
        {
            // Arrange
            var request = new EnrolmentRequestDto
            {
                EventId = 1,
                CategoryId = 2
            };

            // Act
            bool result = IsValid(request);

            // Assert
            Assert.IsTrue(result);
        }


        // TEST 11: An event ID cannot be zero.
        [TestMethod]
        public void EnrolmentWithZeroEventId_ShouldFail()
        {
            // Arrange
            var request = new EnrolmentRequestDto
            {
                EventId = 0,
                CategoryId = 2
            };

            // Act
            bool result = IsValid(request);

            // Assert
            Assert.IsFalse(result);
        }


        // TEST 12: A category ID cannot be zero.
        [TestMethod]
        public void EnrolmentWithZeroCategoryId_ShouldFail()
        {
            // Arrange
            var request = new EnrolmentRequestDto
            {
                EventId = 1,
                CategoryId = 0
            };

            // Act
            bool result = IsValid(request);

            // Assert
            Assert.IsFalse(result);
        }


        /* ========================================================
           SECTION 5: ENROLMENT STATUS VALIDATION
           ======================================================== */

        // TEST 13: All four database statuses are accepted.
        // Each DataRow creates a separate automated test case.
        [TestMethod]
        [DataRow("Pending")]
        [DataRow("Confirmed")]
        [DataRow("Cancelled")]
        [DataRow("Completed")]
        public void ValidEnrolmentStatus_ShouldPass(
            string status)
        {
            // Arrange
            var request = new UpdateEnrolmentStatusDto
            {
                Status = status
            };

            // Act
            bool result = IsValid(request);

            // Assert
            Assert.IsTrue(result);
        }


        // TEST 14: An unsupported status must be rejected.
        [TestMethod]
        public void InvalidEnrolmentStatus_ShouldFail()
        {
            // Arrange
            var request = new UpdateEnrolmentStatusDto
            {
                Status = "Approved"
            };

            // Act
            bool result = IsValid(request);

            // Assert
            Assert.IsFalse(result);
        }


        /* ========================================================
           SECTION 6: RACE RESULT VALIDATION
           ======================================================== */

        // Create a valid race result for use in multiple tests.
        private static ResultRequestDto CreateValidResult()
        {
            return new ResultRequestDto
            {
                EnrolmentId = 1,

                FinishTime = new TimeOnly(0, 52, 14),

                FinishingPosition = 3,

                IsPublished = false
            };
        }


        // TEST 15: A valid result should pass validation.
        [TestMethod]
        public void ValidRaceResult_ShouldPassValidation()
        {
            // Arrange
            ResultRequestDto request = CreateValidResult();

            // Act
            bool result = IsValid(request);

            // Assert
            Assert.IsTrue(result);
        }


        // TEST 16: A finishing time cannot be zero.
        [TestMethod]
        public void ZeroFinishTime_ShouldFailValidation()
        {
            // Arrange
            ResultRequestDto request = CreateValidResult();
            request.FinishTime = TimeOnly.MinValue;

            // Act
            bool result = IsValid(request);

            // Assert
            Assert.IsFalse(result);
        }


        // TEST 17: A finishing time must be supplied.
        [TestMethod]
        public void MissingFinishTime_ShouldFailValidation()
        {
            // Arrange
            ResultRequestDto request = CreateValidResult();
            request.FinishTime = null;

            // Act
            bool result = IsValid(request);

            // Assert
            Assert.IsFalse(result);
        }


        // TEST 18: Finishing position must be positive.
        [TestMethod]
        public void ZeroFinishingPosition_ShouldFail()
        {
            // Arrange
            ResultRequestDto request = CreateValidResult();
            request.FinishingPosition = 0;

            // Act
            bool result = IsValid(request);

            // Assert
            Assert.IsFalse(result);
        }


        /* ========================================================
           SECTION 7: PROFILE VALIDATION TESTS
           ======================================================== */

        // TEST 19: Correct profile information is accepted.
        [TestMethod]
        public void ValidProfile_ShouldPassValidation()
        {
            // Arrange
            var request = new UpdateProfileRequestDto
            {
                FirstName = "Test",

                LastName = "Participant",

                PhoneNumber = "0825550100",

                DateOfBirth = new DateOnly(2002, 6, 15)
            };

            // Act
            bool result = IsValid(request);

            // Assert
            Assert.IsTrue(result);
        }


        // TEST 20: A short phone number is invalid.
        [TestMethod]
        public void ShortPhoneNumber_ShouldFailValidation()
        {
            // Arrange
            var request = new UpdateProfileRequestDto
            {
                FirstName = "Test",

                LastName = "Participant",

                PhoneNumber = "123",

                DateOfBirth = new DateOnly(2002, 6, 15)
            };

            // Act
            bool result = IsValid(request);

            // Assert
            Assert.IsFalse(result);
        }
    }
}
