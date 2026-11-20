/* ============================================================
   RACEDAY EVENT MANAGEMENT SYSTEM
   PROG6212 - PROGRAMMING 2B
   POE PART 1 - SQL DATABASE SCRIPT

   Student Name: Varen Venketraju
   Student Number: ST10481447
   Module: PROG6212
   Assessment: Portfolio of Evidence  Part 1

   ============================================================ */

/* ============================================================
    CODE ATTRIBUTION

   Usage:
   Microsoft Learn documentation was consulted for guidance
   on SQL Server table creation, constraints, foreign keys,
   default values, identity columns and data integrity.

   Author:
   Microsoft

   Source:
   Microsoft Learn

   Title:
   CREATE TABLE (Transact-SQL)

   Type:
   Technical documentation / Transact-SQL reference

   Link:
   https://learn.microsoft.com/en-us/sql/t-sql/statements/create-table-transact-sql

   Date Accessed:
   19 September 2026

  
   ============================================================ */





-- Prevent SQL Server from displaying unnecessary row count messages.
SET NOCOUNT ON;
GO


/* ============================================================
   SECTION 1: CREATE THE RACEDAY DATABASE
   ============================================================ */

-- Check whether the RaceDay database already exists.
IF DB_ID(N'RaceDayDB') IS NULL
BEGIN
    -- Create the database only if it does not already exist.
    EXEC('CREATE DATABASE RaceDayDB');
END;
GO


-- Change the active database to RaceDayDB.
USE RaceDayDB;
GO


/* ============================================================
   SECTION 2: REMOVE EXISTING TABLES
   ============================================================ */

-- RESULT depends on ENROLMENT, so it must be removed first.
DROP TABLE IF EXISTS dbo.[RESULT];

-- ENROLMENT depends on USER, EVENT and CATEGORY.
DROP TABLE IF EXISTS dbo.[ENROLMENT];

-- CATEGORY depends on EVENT.
DROP TABLE IF EXISTS dbo.[CATEGORY];

-- EVENT depends on USER.
DROP TABLE IF EXISTS dbo.[EVENT];

-- USER depends on ROLE.
DROP TABLE IF EXISTS dbo.[USER];

-- ROLE does not depend on another RaceDay table.
DROP TABLE IF EXISTS dbo.[ROLE];
GO


/* ============================================================
   CODE ATTRIBUTION - CONSTRAINTS

   Usage:
   Guidance was used when implementing UNIQUE and CHECK
   constraints to protect the integrity of RaceDay data.

   Author:
   Microsoft

   Source:
   Microsoft Learn

   Title:
   Unique constraints and check constraints

   Type:
   Technical documentation

   Link:
   https://learn.microsoft.com/en-us/sql/relational-databases/tables/unique-constraints-and-check-constraints

   Date Accessed:
   19 September 2026
   ============================================================ */


/* ============================================================
   SECTION 3: CREATE ROLE TABLE
   ============================================================ */

-- ROLE stores the types of users that can use RaceDay.
CREATE TABLE dbo.[ROLE]
(
    -- ROLE_ID uniquely identifies each role.
    ROLE_ID INT IDENTITY(1,1) NOT NULL,

    -- ROLE_NAME stores values such as Organiser and Participant.
    ROLE_NAME VARCHAR(30) NOT NULL,

    -- Set ROLE_ID as the primary key.
    CONSTRAINT PK_ROLE
        PRIMARY KEY (ROLE_ID),

    -- Prevent duplicate role names.
    CONSTRAINT UQ_ROLE_ROLE_NAME
        UNIQUE (ROLE_NAME)
);
GO


/* ============================================================
   SECTION 4: CREATE USER TABLE
   ============================================================ */

-- USER stores both Organisers and Participants.
CREATE TABLE dbo.[USER]
(
    -- USER_ID uniquely identifies each RaceDay user.
    USER_ID INT IDENTITY(1,1) NOT NULL,

    -- ROLE_ID links the user to one record in ROLE.
    ROLE_ID INT NOT NULL,

    -- Store the user's first name.
    FIRST_NAME VARCHAR(60) NOT NULL,

    -- Store the user's surname.
    LAST_NAME VARCHAR(60) NOT NULL,

    -- Store the user's email address.
    EMAIL VARCHAR(150) NOT NULL,

    -- Store the user's hashed password instead of plain text.
    PASSWORD_HASH VARCHAR(255) NOT NULL,

    -- Store a contact telephone number.
    PHONE_NUMBER VARCHAR(20) NOT NULL,

    -- Store the user's date of birth.
    DATE_OF_BIRTH DATE NOT NULL,

    -- This field can later contain the Azure profile image URL.
    PROFILE_IMAGE_URL VARCHAR(500) NULL,

    -- Record when the user account was created.
    CREATED_AT DATETIME2(0) NOT NULL
        CONSTRAINT DF_USER_CREATED_AT
        DEFAULT SYSDATETIME(),

    -- Set USER_ID as the primary key.
    CONSTRAINT PK_USER
        PRIMARY KEY (USER_ID),

    -- Each email address may only belong to one account.
    CONSTRAINT UQ_USER_EMAIL
        UNIQUE (EMAIL),

    -- ROLE_ID must reference a valid role.
    CONSTRAINT FK_USER_ROLE
        FOREIGN KEY (ROLE_ID)
        REFERENCES dbo.[ROLE](ROLE_ID)
);
GO


/* ============================================================
   SECTION 5: CREATE EVENT TABLE
   ============================================================ */

-- EVENT stores all running, walking and cycling events.
CREATE TABLE dbo.[EVENT]
(
    -- EVENT_ID uniquely identifies each event.
    EVENT_ID INT IDENTITY(1,1) NOT NULL,

    -- ORGANISER_ID stores the user responsible for the event.
    ORGANISER_ID INT NOT NULL,

    -- Store the event name.
    NAME VARCHAR(150) NOT NULL,

    -- Store a description of the event.
    DESCRIPTION VARCHAR(1000) NOT NULL,

    -- Store the event date and starting time.
    EVENT_DATE DATETIME2(0) NOT NULL,

    -- Store where the event takes place.
    LOCATION VARCHAR(200) NOT NULL,

    -- Store the event distance in kilometres.
    DISTANCE_KM DECIMAL(6,2) NOT NULL,

    -- Store whether the event is a Run, Walk or Cycle.
    EVENT_TYPE VARCHAR(20) NOT NULL,

    -- This field can later contain the Azure banner image URL.
    BANNER_IMAGE_URL VARCHAR(500) NULL,

    -- Record when the event was created.
    CREATED_AT DATETIME2(0) NOT NULL
        CONSTRAINT DF_EVENT_CREATED_AT
        DEFAULT SYSDATETIME(),

    -- Set EVENT_ID as the primary key.
    CONSTRAINT PK_EVENT
        PRIMARY KEY (EVENT_ID),

    -- The Organiser must already exist in the USER table.
    CONSTRAINT FK_EVENT_ORGANISER
        FOREIGN KEY (ORGANISER_ID)
        REFERENCES dbo.[USER](USER_ID),

    -- An event distance must always be greater than zero.
    CONSTRAINT CK_EVENT_DISTANCE
        CHECK (DISTANCE_KM > 0),

    -- Restrict EVENT_TYPE to the three types specified by RaceDay.
    CONSTRAINT CK_EVENT_TYPE
        CHECK
        (
            EVENT_TYPE IN ('Run', 'Walk', 'Cycle')
        )
);
GO


/* ============================================================
   SECTION 6: CREATE CATEGORY TABLE
   ============================================================ */

-- CATEGORY stores age-based and distance-based event categories.
CREATE TABLE dbo.[CATEGORY]
(
    -- CATEGORY_ID uniquely identifies each category.
    CATEGORY_ID INT IDENTITY(1,1) NOT NULL,

    -- EVENT_ID identifies the event that owns the category.
    EVENT_ID INT NOT NULL,

    -- Store the display name of the category.
    CATEGORY_NAME VARCHAR(100) NOT NULL,

    -- Identify whether this is an Age or Distance category.
    CATEGORY_TYPE VARCHAR(20) NOT NULL,

    -- Store the minimum age when the category uses age limits.
    MIN_AGE INT NULL,

    -- Store the maximum age when the category uses age limits.
    MAX_AGE INT NULL,

    -- Store the distance when the category is distance-based.
    DISTANCE_KM DECIMAL(6,2) NULL,

    -- Set CATEGORY_ID as the primary key.
    CONSTRAINT PK_CATEGORY
        PRIMARY KEY (CATEGORY_ID),

    -- Every category must belong to a valid event.
    CONSTRAINT FK_CATEGORY_EVENT
        FOREIGN KEY (EVENT_ID)
        REFERENCES dbo.[EVENT](EVENT_ID),

    -- Prevent duplicate category names within the same event.
    CONSTRAINT UQ_CATEGORY_EVENT_NAME
        UNIQUE (EVENT_ID, CATEGORY_NAME),

    -- Only the category types used by RaceDay are accepted.
    CONSTRAINT CK_CATEGORY_TYPE
        CHECK
        (
            CATEGORY_TYPE IN ('Age', 'Distance')
        ),

    -- Prevent an invalid age range.
    CONSTRAINT CK_CATEGORY_AGE_RANGE
        CHECK
        (
            MIN_AGE IS NULL
            OR MAX_AGE IS NULL
            OR MIN_AGE <= MAX_AGE
        ),

    -- If a distance is provided, it must be greater than zero.
    CONSTRAINT CK_CATEGORY_DISTANCE
        CHECK
        (
            DISTANCE_KM IS NULL
            OR DISTANCE_KM > 0
        )
);
GO


/* ============================================================
   SECTION 7: CREATE ENROLMENT TABLE
   ============================================================ */

-- ENROLMENT records a Participant entering an Event.
CREATE TABLE dbo.[ENROLMENT]
(
    -- ENROLMENT_ID uniquely identifies each enrolment.
    ENROLMENT_ID INT IDENTITY(1,1) NOT NULL,

    -- PARTICIPANT_ID identifies the participating USER.
    PARTICIPANT_ID INT NOT NULL,

    -- EVENT_ID identifies the selected RaceDay event.
    EVENT_ID INT NOT NULL,

    -- CATEGORY_ID identifies the participant's selected category.
    CATEGORY_ID INT NOT NULL,

    -- Store when the participant enrolled.
    ENROLMENT_DATE DATETIME2(0) NOT NULL
        CONSTRAINT DF_ENROLMENT_DATE
        DEFAULT SYSDATETIME(),

    -- Store the current state of the enrolment.
    STATUS VARCHAR(20) NOT NULL
        CONSTRAINT DF_ENROLMENT_STATUS
        DEFAULT 'Pending',

    -- Set ENROLMENT_ID as the primary key.
    CONSTRAINT PK_ENROLMENT
        PRIMARY KEY (ENROLMENT_ID),

    -- PARTICIPANT_ID must reference an existing USER.
    CONSTRAINT FK_ENROLMENT_PARTICIPANT
        FOREIGN KEY (PARTICIPANT_ID)
        REFERENCES dbo.[USER](USER_ID),

    -- EVENT_ID must reference an existing EVENT.
    CONSTRAINT FK_ENROLMENT_EVENT
        FOREIGN KEY (EVENT_ID)
        REFERENCES dbo.[EVENT](EVENT_ID),

    -- CATEGORY_ID must reference an existing CATEGORY.
    CONSTRAINT FK_ENROLMENT_CATEGORY
        FOREIGN KEY (CATEGORY_ID)
        REFERENCES dbo.[CATEGORY](CATEGORY_ID),

    -- Prevent a Participant from entering the same event twice.
    CONSTRAINT UQ_ENROLMENT_PARTICIPANT_EVENT
        UNIQUE (PARTICIPANT_ID, EVENT_ID),

    -- Restrict enrolment status to valid system values.
    CONSTRAINT CK_ENROLMENT_STATUS
        CHECK
        (
            STATUS IN
            (
                'Pending',
                'Confirmed',
                'Cancelled',
                'Completed'
            )
        )
);
GO


/* ============================================================
   SECTION 8: CREATE RESULT TABLE
   ============================================================ */

-- RESULT stores the performance recorded for an enrolment.
CREATE TABLE dbo.[RESULT]
(
    -- RESULT_ID uniquely identifies each result.
    RESULT_ID INT IDENTITY(1,1) NOT NULL,

    -- ENROLMENT_ID connects the result to one enrolment.
    ENROLMENT_ID INT NOT NULL,

    -- Store the participant's finish time.
    FINISH_TIME TIME(0) NOT NULL,

    -- Store the participant's finishing position.
    FINISHING_POSITION INT NOT NULL,

    -- Store whether the result is visible to the Participant.
    IS_PUBLISHED BIT NOT NULL
        CONSTRAINT DF_RESULT_IS_PUBLISHED
        DEFAULT 0,

    -- Record when a published result was made available.
    PUBLISHED_AT DATETIME2(0) NULL,

    -- Set RESULT_ID as the primary key.
    CONSTRAINT PK_RESULT
        PRIMARY KEY (RESULT_ID),

    -- A result must belong to an existing enrolment.
    CONSTRAINT FK_RESULT_ENROLMENT
        FOREIGN KEY (ENROLMENT_ID)
        REFERENCES dbo.[ENROLMENT](ENROLMENT_ID),

    -- One enrolment can have a maximum of one result.
    CONSTRAINT UQ_RESULT_ENROLMENT
        UNIQUE (ENROLMENT_ID),

    -- A finishing position cannot be zero or negative.
    CONSTRAINT CK_RESULT_POSITION
        CHECK
        (
            FINISHING_POSITION > 0
        ),

    -- If the result is published, a publication date is required.
    CONSTRAINT CK_RESULT_PUBLISHED
        CHECK
        (
            (IS_PUBLISHED = 0 AND PUBLISHED_AT IS NULL)
            OR
            (IS_PUBLISHED = 1 AND PUBLISHED_AT IS NOT NULL)
        )
);
GO


/* ============================================================
   SECTION 9: INSERT ROLE SEED DATA
   ============================================================ */

-- Add the Organiser role used by users who manage events.
INSERT INTO dbo.[ROLE]
(
    ROLE_NAME
)
VALUES
(
    'Organiser'
);


-- Add the Participant role used by users who enter events.
INSERT INTO dbo.[ROLE]
(
    ROLE_NAME
)
VALUES
(
    'Participant'
);
GO


/* ============================================================
   CODE ATTRIBUTION - PASSWORD HASHING

   Usage:
   Microsoft Learn documentation was consulted for guidance
   on the HASHBYTES function and the SHA2_256 algorithm used
   to generate example password hashes for seeded users.

   Author:
   Microsoft

   Source:
   Microsoft Learn

   Title:
   HASHBYTES (Transact-SQL)

   Type:
   Technical documentation / Transact-SQL reference

   Link:
   https://learn.microsoft.com/en-us/sql/t-sql/functions/hashbytes-transact-sql

   Date Accessed:
   19 September 2026

   ============================================================ */


/* ============================================================
   SECTION 10: INSERT USER SEED DATA
   ============================================================ */

-- Add the first Organiser.
INSERT INTO dbo.[USER]
(
    ROLE_ID,
    FIRST_NAME,
    LAST_NAME,
    EMAIL,
    PASSWORD_HASH,
    PHONE_NUMBER,
    DATE_OF_BIRTH,
    PROFILE_IMAGE_URL
)
VALUES
(
    (
        SELECT ROLE_ID
        FROM dbo.[ROLE]
        WHERE ROLE_NAME = 'Organiser'
    ),

    'Ayanda',

    'Mkhize',

    'ayanda.mkhize@raceday.co.za',

    CONVERT
    (
        VARCHAR(64),
        HASHBYTES
        (
            'SHA2_256',
            'RaceDay@123'
        ),
        2
    ),

    '0825550101',

    '1990-04-12',

    NULL
);


-- Add the second Organiser.
INSERT INTO dbo.[USER]
(
    ROLE_ID,
    FIRST_NAME,
    LAST_NAME,
    EMAIL,
    PASSWORD_HASH,
    PHONE_NUMBER,
    DATE_OF_BIRTH,
    PROFILE_IMAGE_URL
)
VALUES
(
    (
        SELECT ROLE_ID
        FROM dbo.[ROLE]
        WHERE ROLE_NAME = 'Organiser'
    ),

    'Priya',

    'Naidoo',

    'priya.naidoo@raceday.co.za',

    CONVERT
    (
        VARCHAR(64),
        HASHBYTES
        (
            'SHA2_256',
            'RaceDay@456'
        ),
        2
    ),

    '0835550102',

    '1987-11-03',

    NULL
);


-- Add the first Participant.
INSERT INTO dbo.[USER]
(
    ROLE_ID,
    FIRST_NAME,
    LAST_NAME,
    EMAIL,
    PASSWORD_HASH,
    PHONE_NUMBER,
    DATE_OF_BIRTH,
    PROFILE_IMAGE_URL
)
VALUES
(
    (
        SELECT ROLE_ID
        FROM dbo.[ROLE]
        WHERE ROLE_NAME = 'Participant'
    ),

    'Thando',

    'Dlamini',

    'thando.dlamini@example.com',

    CONVERT
    (
        VARCHAR(64),
        HASHBYTES
        (
            'SHA2_256',
            'Runner@123'
        ),
        2
    ),

    '0845550103',

    '2001-06-18',

    NULL
);


-- Add the second Participant.
INSERT INTO dbo.[USER]
(
    ROLE_ID,
    FIRST_NAME,
    LAST_NAME,
    EMAIL,
    PASSWORD_HASH,
    PHONE_NUMBER,
    DATE_OF_BIRTH,
    PROFILE_IMAGE_URL
)
VALUES
(
    (
        SELECT ROLE_ID
        FROM dbo.[ROLE]
        WHERE ROLE_NAME = 'Participant'
    ),

    'Naledi',

    'Khumalo',

    'naledi.khumalo@example.com',

    CONVERT
    (
        VARCHAR(64),
        HASHBYTES
        (
            'SHA2_256',
            'Runner@456'
        ),
        2
    ),

    '0715550104',

    '1998-02-25',

    NULL
);
GO


/* ============================================================
   SECTION 11: INSERT EVENT SEED DATA
   ============================================================ */

-- Add a running event organised by Ayanda.
INSERT INTO dbo.[EVENT]
(
    ORGANISER_ID,
    NAME,
    DESCRIPTION,
    EVENT_DATE,
    LOCATION,
    DISTANCE_KM,
    EVENT_TYPE,
    BANNER_IMAGE_URL
)
VALUES
(
    (
        SELECT USER_ID
        FROM dbo.[USER]
        WHERE EMAIL = 'ayanda.mkhize@raceday.co.za'
    ),

    'Durban Sunrise 10K',

    'A community road running event along the Durban beachfront.',

    '2026-09-06 06:00:00',

    'Durban Beachfront, KwaZulu-Natal',

    10.00,

    'Run',

    NULL
);


-- Add a cycling event organised by Priya.
INSERT INTO dbo.[EVENT]
(
    ORGANISER_ID,
    NAME,
    DESCRIPTION,
    EVENT_DATE,
    LOCATION,
    DISTANCE_KM,
    EVENT_TYPE,
    BANNER_IMAGE_URL
)
VALUES
(
    (
        SELECT USER_ID
        FROM dbo.[USER]
        WHERE EMAIL = 'priya.naidoo@raceday.co.za'
    ),

    'Umhlanga Cycle Challenge',

    'A coastal cycling event for recreational and experienced riders.',

    '2026-09-13 07:00:00',

    'Umhlanga, KwaZulu-Natal',

    40.00,

    'Cycle',

    NULL
);


-- Add a community walking event organised by Ayanda.
INSERT INTO dbo.[EVENT]
(
    ORGANISER_ID,
    NAME,
    DESCRIPTION,
    EVENT_DATE,
    LOCATION,
    DISTANCE_KM,
    EVENT_TYPE,
    BANNER_IMAGE_URL
)
VALUES
(
    (
        SELECT USER_ID
        FROM dbo.[USER]
        WHERE EMAIL = 'ayanda.mkhize@raceday.co.za'
    ),

    'Riverside Community Walk',

    'A family-friendly community walk promoting health and activity.',

    '2026-10-03 08:00:00',

    'Durban North, KwaZulu-Natal',

    5.00,

    'Walk',

    NULL
);
GO


/* ============================================================
   SECTION 12: INSERT CATEGORY SEED DATA
   ============================================================ */

-- Add a 10km distance category to the running event.
INSERT INTO dbo.[CATEGORY]
(
    EVENT_ID,
    CATEGORY_NAME,
    CATEGORY_TYPE,
    MIN_AGE,
    MAX_AGE,
    DISTANCE_KM
)
VALUES
(
    (
        SELECT EVENT_ID
        FROM dbo.[EVENT]
        WHERE NAME = 'Durban Sunrise 10K'
    ),

    'Open 10km',

    'Distance',

    NULL,

    NULL,

    10.00
);


-- Add an Under 20 age category to the running event.
INSERT INTO dbo.[CATEGORY]
(
    EVENT_ID,
    CATEGORY_NAME,
    CATEGORY_TYPE,
    MIN_AGE,
    MAX_AGE,
    DISTANCE_KM
)
VALUES
(
    (
        SELECT EVENT_ID
        FROM dbo.[EVENT]
        WHERE NAME = 'Durban Sunrise 10K'
    ),

    'Under 20',

    'Age',

    13,

    19,

    NULL
);


-- Add a 40km category to the cycling event.
INSERT INTO dbo.[CATEGORY]
(
    EVENT_ID,
    CATEGORY_NAME,
    CATEGORY_TYPE,
    MIN_AGE,
    MAX_AGE,
    DISTANCE_KM
)
VALUES
(
    (
        SELECT EVENT_ID
        FROM dbo.[EVENT]
        WHERE NAME = 'Umhlanga Cycle Challenge'
    ),

    '40km Cycle',

    'Distance',

    NULL,

    NULL,

    40.00
);


-- Add a Senior age category to the cycling event.
INSERT INTO dbo.[CATEGORY]
(
    EVENT_ID,
    CATEGORY_NAME,
    CATEGORY_TYPE,
    MIN_AGE,
    MAX_AGE,
    DISTANCE_KM
)
VALUES
(
    (
        SELECT EVENT_ID
        FROM dbo.[EVENT]
        WHERE NAME = 'Umhlanga Cycle Challenge'
    ),

    'Senior Rider',

    'Age',

    50,

    100,

    NULL
);


-- Add a 5km distance category to the walking event.
INSERT INTO dbo.[CATEGORY]
(
    EVENT_ID,
    CATEGORY_NAME,
    CATEGORY_TYPE,
    MIN_AGE,
    MAX_AGE,
    DISTANCE_KM
)
VALUES
(
    (
        SELECT EVENT_ID
        FROM dbo.[EVENT]
        WHERE NAME = 'Riverside Community Walk'
    ),

    '5km Walk',

    'Distance',

    NULL,

    NULL,

    5.00
);


-- Add a general age category to the walking event.
INSERT INTO dbo.[CATEGORY]
(
    EVENT_ID,
    CATEGORY_NAME,
    CATEGORY_TYPE,
    MIN_AGE,
    MAX_AGE,
    DISTANCE_KM
)
VALUES
(
    (
        SELECT EVENT_ID
        FROM dbo.[EVENT]
        WHERE NAME = 'Riverside Community Walk'
    ),

    'Family Open',

    'Age',

    5,

    100,

    NULL
);
GO


/* ============================================================
   SECTION 13: INSERT ENROLMENT SEED DATA
   ============================================================ */

-- Enrol Thando in the Durban Sunrise 10K.
INSERT INTO dbo.[ENROLMENT]
(
    PARTICIPANT_ID,
    EVENT_ID,
    CATEGORY_ID,
    ENROLMENT_DATE,
    STATUS
)
VALUES
(
    (
        SELECT USER_ID
        FROM dbo.[USER]
        WHERE EMAIL = 'thando.dlamini@example.com'
    ),

    (
        SELECT EVENT_ID
        FROM dbo.[EVENT]
        WHERE NAME = 'Durban Sunrise 10K'
    ),

    (
        SELECT CATEGORY_ID
        FROM dbo.[CATEGORY]
        WHERE CATEGORY_NAME = 'Open 10km'
    ),

    '2026-08-20 10:30:00',

    'Completed'
);


-- Enrol Naledi in the Umhlanga Cycle Challenge.
INSERT INTO dbo.[ENROLMENT]
(
    PARTICIPANT_ID,
    EVENT_ID,
    CATEGORY_ID,
    ENROLMENT_DATE,
    STATUS
)
VALUES
(
    (
        SELECT USER_ID
        FROM dbo.[USER]
        WHERE EMAIL = 'naledi.khumalo@example.com'
    ),

    (
        SELECT EVENT_ID
        FROM dbo.[EVENT]
        WHERE NAME = 'Umhlanga Cycle Challenge'
    ),

    (
        SELECT CATEGORY_ID
        FROM dbo.[CATEGORY]
        WHERE CATEGORY_NAME = '40km Cycle'
    ),

    '2026-08-28 14:15:00',

    'Completed'
);


-- Enrol Thando in the upcoming Riverside Community Walk.
INSERT INTO dbo.[ENROLMENT]
(
    PARTICIPANT_ID,
    EVENT_ID,
    CATEGORY_ID,
    ENROLMENT_DATE,
    STATUS
)
VALUES
(
    (
        SELECT USER_ID
        FROM dbo.[USER]
        WHERE EMAIL = 'thando.dlamini@example.com'
    ),

    (
        SELECT EVENT_ID
        FROM dbo.[EVENT]
        WHERE NAME = 'Riverside Community Walk'
    ),

    (
        SELECT CATEGORY_ID
        FROM dbo.[CATEGORY]
        WHERE CATEGORY_NAME = '5km Walk'
    ),

    '2026-09-18 09:45:00',

    'Confirmed'
);
GO


/* ============================================================
   SECTION 14: INSERT RESULT SEED DATA
   ============================================================ */

-- Record Thando's result for the Durban Sunrise 10K.
INSERT INTO dbo.[RESULT]
(
    ENROLMENT_ID,
    FINISH_TIME,
    FINISHING_POSITION,
    IS_PUBLISHED,
    PUBLISHED_AT
)
VALUES
(
    (
        SELECT ENROLMENT_ID
        FROM dbo.[ENROLMENT]
        WHERE PARTICIPANT_ID =
        (
            SELECT USER_ID
            FROM dbo.[USER]
            WHERE EMAIL = 'thando.dlamini@example.com'
        )
        AND EVENT_ID =
        (
            SELECT EVENT_ID
            FROM dbo.[EVENT]
            WHERE NAME = 'Durban Sunrise 10K'
        )
    ),

    '00:52:14',

    47,

    1,

    '2026-09-06 11:00:00'
);


-- Record Naledi's result for the Umhlanga Cycle Challenge.
INSERT INTO dbo.[RESULT]
(
    ENROLMENT_ID,
    FINISH_TIME,
    FINISHING_POSITION,
    IS_PUBLISHED,
    PUBLISHED_AT
)
VALUES
(
    (
        SELECT ENROLMENT_ID
        FROM dbo.[ENROLMENT]
        WHERE PARTICIPANT_ID =
        (
            SELECT USER_ID
            FROM dbo.[USER]
            WHERE EMAIL = 'naledi.khumalo@example.com'
        )
        AND EVENT_ID =
        (
            SELECT EVENT_ID
            FROM dbo.[EVENT]
            WHERE NAME = 'Umhlanga Cycle Challenge'
        )
    ),

    '01:34:27',

    18,

    1,

    '2026-09-13 13:00:00'
);
GO


/* ============================================================
   SECTION 15: VERIFY AND DISPLAY THE SEEDED DATA
   ============================================================ */

-- Display all system roles.
SELECT *
FROM dbo.[ROLE];


-- Display the Organiser and Participant accounts.
SELECT *
FROM dbo.[USER];


-- Display all RaceDay events.
SELECT *
FROM dbo.[EVENT];


-- Display the categories belonging to each event.
SELECT *
FROM dbo.[CATEGORY];


-- Display all Participant enrolments.
SELECT *
FROM dbo.[ENROLMENT];


-- Display all recorded results.
SELECT *
FROM dbo.[RESULT];
GO


/* ============================================================
   SECTION 16: OPTIONAL RELATIONSHIP VERIFICATION
   ============================================================ */

SELECT
    U.FIRST_NAME + ' ' + U.LAST_NAME AS PARTICIPANT_NAME,
    E.NAME AS EVENT_NAME,
    C.CATEGORY_NAME,
    EN.STATUS,
    R.FINISH_TIME,
    R.FINISHING_POSITION
FROM dbo.[ENROLMENT] AS EN

INNER JOIN dbo.[USER] AS U
    ON EN.PARTICIPANT_ID = U.USER_ID

INNER JOIN dbo.[EVENT] AS E
    ON EN.EVENT_ID = E.EVENT_ID

INNER JOIN dbo.[CATEGORY] AS C
    ON EN.CATEGORY_ID = C.CATEGORY_ID

LEFT JOIN dbo.[RESULT] AS R
    ON EN.ENROLMENT_ID = R.ENROLMENT_ID

ORDER BY
    E.EVENT_DATE,
    PARTICIPANT_NAME;
GO


/* ============================================================
   END OF RACEDAY DATABASE SCRIPT
   ============================================================ */

PRINT 'RaceDay database created and seeded successfully.';
GO