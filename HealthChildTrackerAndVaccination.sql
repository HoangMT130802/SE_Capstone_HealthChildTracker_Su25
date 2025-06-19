-- Tạo Database
CREATE DATABASE HealthChildTrackerAndVaccination;
GO

USE HealthChildTrackerAndVaccination;
GO

-- Tạo bảng Account
CREATE TABLE Account (
    account_id INT IDENTITY(1,1) PRIMARY KEY,
    accountName NVARCHAR(255) NOT NULL,
    password NCHAR(255) NOT NULL,
    email NCHAR(255) NOT NULL,
    role NVARCHAR(255) NOT NULL,
    status BIT NOT NULL,
    created_at DATETIME NOT NULL,
    updated_at DATETIME NOT NULL
);

-- Tạo bảng Member
CREATE TABLE Member (
    MemberID INT IDENTITY(1,1) PRIMARY KEY,
    AccountID INT NOT NULL,
    FullName NVARCHAR(255) NOT NULL,
    PhoneNumber NVARCHAR(255) NOT NULL,
    Address NVARCHAR(255),
    CreatedAt DATETIME NOT NULL,
    UpdatedAt DATETIME NOT NULL,
    CONSTRAINT FK_Member_Account FOREIGN KEY (AccountID) REFERENCES Account(account_id)
);

-- Tạo bảng VaccinationFacility
CREATE TABLE VaccinationFacility (
    FacilityId INT IDENTITY(1,1) PRIMARY KEY,
    FacilityName NVARCHAR(255) NOT NULL,
    LicenseNumber INT NOT NULL,
    Address NVARCHAR(255) NOT NULL,
    Phone INT NOT NULL,
    Email NVARCHAR(255) NOT NULL,
    Description BIGINT NOT NULL,
    Status BIGINT NOT NULL,
    CreatedAt BIGINT NOT NULL,
    UpdatedAt BIGINT NOT NULL
);

-- Tạo bảng FacilityStaff
CREATE TABLE FacilityStaff (
    staff_id INT IDENTITY(1,1) PRIMARY KEY,
    account_id INT NOT NULL,
    facility_id INT NOT NULL,
    full_name NVARCHAR(255) NOT NULL,
    phone INT,
    email NVARCHAR(255),
    position NVARCHAR(255) NOT NULL,
    description NVARCHAR(MAX),
    status BIT NOT NULL,
    created_at DATETIME NOT NULL,
    updated_at DATETIME NOT NULL,
    CONSTRAINT FK_FacilityStaff_Account FOREIGN KEY (account_id) REFERENCES Account(account_id),
    CONSTRAINT FK_FacilityStaff_Facility FOREIGN KEY (facility_id) REFERENCES VaccinationFacility(FacilityId)
);

-- Tạo bảng DoctorProfile
CREATE TABLE DoctorProfile (
    doctor_id INT PRIMARY KEY,
    age INT NOT NULL,
    specialization NVARCHAR(255) NOT NULL,
    certifications NVARCHAR(MAX),
    university NVARCHAR(255),
    bio NVARCHAR(MAX),
    created_at DATETIME NOT NULL,
    updated_at DATETIME NOT NULL,
    CONSTRAINT FK_DoctorProfile_FacilityStaff FOREIGN KEY (doctor_id) REFERENCES FacilityStaff(staff_id)
);

-- Tạo bảng Child
CREATE TABLE Child (
    child_id INT IDENTITY(1,1) PRIMARY KEY,
    MemberID INT NOT NULL,
    FullName NVARCHAR(255) NOT NULL,
    birth_date DATETIME NOT NULL,
    gender NCHAR(255) NOT NULL,
    BloodType NVARCHAR(255) NOT NULL,
    AllergiesNotes NVARCHAR(255),
    MedicalHistory NVARCHAR(255),
    Status BIT NOT NULL,
    CreatedAt DATETIME NOT NULL,
    UpdateAt DATETIME NOT NULL,
    CONSTRAINT FK_Child_Member FOREIGN KEY (MemberID) REFERENCES Member(MemberID)
);

-- Tạo bảng Disease
CREATE TABLE Disease (
    DiseaseID INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(MAX),
    Symptoms NVARCHAR(MAX),
    Treatment NVARCHAR(MAX),
    CreatedAt DATETIME NOT NULL,
    UpdatedAt DATETIME NOT NULL
);

-- Tạo bảng Vaccine
CREATE TABLE Vaccine (
    VaccineId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(255) NOT NULL,
    Description NVARCHAR(255) NOT NULL,
    Manufacturer NVARCHAR(255) NOT NULL,
    Category NVARCHAR(255) NOT NULL,
    AgeGroup NVARCHAR(255) NOT NULL,
    NumberOfDoses INT NOT NULL,
    MinIntervalBetweenDoses INT NOT NULL,
    SideEffects NVARCHAR(255),
    Contraindications NVARCHAR(255) NOT NULL,
    Price DECIMAL(8,2) NOT NULL,
    Status NVARCHAR(255) NOT NULL,
    CreatedAt DATETIME NOT NULL,
    UpdatedAt DATETIME NOT NULL
);

-- Tạo bảng VaccineDisease
CREATE TABLE VaccineDisease (
    VaccineDiseaseID INT IDENTITY(1,1) PRIMARY KEY,
    DiseaseID INT NOT NULL,
    VaccineID INT NOT NULL,
    CreatedAt DATETIME NOT NULL,
    UpdatedAt DATETIME NOT NULL,
    CONSTRAINT FK_VaccineDisease_Disease FOREIGN KEY (DiseaseID) REFERENCES Disease(DiseaseID),
    CONSTRAINT FK_VaccineDisease_Vaccine FOREIGN KEY (VaccineID) REFERENCES Vaccine(VaccineId)
);

-- Tạo bảng VaccineTemplate
CREATE TABLE VaccineTemplate (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    DiseaseId INT NOT NULL,
    PeriodFrom TIME NOT NULL,
    PeriodTo TIME NOT NULL,
    Description NVARCHAR(255) NOT NULL,
    IsRequired BIT NOT NULL,
    Priority NVARCHAR(255) NOT NULL,
    CreatedAt DATETIME NOT NULL,
    UpdatedAt DATETIME NOT NULL,
    CONSTRAINT FK_VaccineTemplate_Disease FOREIGN KEY (DiseaseId) REFERENCES Disease(DiseaseID)
);

-- Tạo bảng ScheduleSlot
CREATE TABLE ScheduleSlot (
    SlotId INT IDENTITY(1,1) PRIMARY KEY,
    SlotTime NVARCHAR(50) NOT NULL,
    MaxCapacity INT NOT NULL,
    BookedCount INT NOT NULL,
    Status NVARCHAR(50) NOT NULL,
    CreatedAt DATETIME NOT NULL,
    UpdatedAt DATETIME NOT NULL
);

-- Tạo bảng AppointmentSchedule
CREATE TABLE AppointmentSchedule (
    ScheduleId INT IDENTITY(1,1) PRIMARY KEY,
    FacilityId INT NOT NULL,
    SlotId INT NOT NULL,
    Date DATE NOT NULL,
    BookedCount INT DEFAULT 0,
    Status NVARCHAR(50) NOT NULL,
    CreatedAt DATETIME NOT NULL,
    UpdatedAt DATETIME NOT NULL,
    CONSTRAINT FK_AppointmentSchedule_Facility FOREIGN KEY (FacilityId) REFERENCES VaccinationFacility(FacilityId)
);

-- Tạo bảng Order
CREATE TABLE [Order] (
    OrderID INT IDENTITY(1,1) PRIMARY KEY,
    MemberId INT NOT NULL,
    PackageId INT NOT NULL,
    OrderDate DATETIME NOT NULL,
    TotalAmount DECIMAL(10,2) NOT NULL,
    Status NVARCHAR(255) NOT NULL,
    CreatedAt DATETIME NOT NULL,
    UpdatedAt DATETIME NOT NULL,
    CONSTRAINT FK_Order_Member FOREIGN KEY (MemberId) REFERENCES Member(MemberID)
);

-- Tạo bảng VaccinationAppointment
CREATE TABLE VaccinationAppointment (
    AppointmentId INT IDENTITY(1,1) PRIMARY KEY,
    ChildId INT NOT NULL,
    ScheduleId INT NOT NULL,
    OrderId INT,
    Status NVARCHAR(255) NOT NULL,
    Note NVARCHAR(255) NOT NULL,
    CreatedAt DATETIME NOT NULL,
    UpdatedAt DATETIME NOT NULL,
    CONSTRAINT FK_VaccinationAppointment_Child FOREIGN KEY (ChildId) REFERENCES Child(child_id),
    CONSTRAINT FK_VaccinationAppointment_Schedule FOREIGN KEY (ScheduleId) REFERENCES AppointmentSchedule(ScheduleId),
    CONSTRAINT FK_VaccinationAppointment_Order FOREIGN KEY (OrderId) REFERENCES [Order](OrderID)
);

-- Tạo bảng ChildVaccineProfile
CREATE TABLE ChildVaccineProfile (
    VaccineProfileId INT IDENTITY(1,1) PRIMARY KEY,
    ChildId INT NOT NULL,
    DiseaseId INT NOT NULL,
    AppointmentId INT NOT NULL,
    VaccineId INT NOT NULL,
    DoseNum INT NOT NULL,
    ExpectedDate DATE NOT NULL,
    ActualDate DATE NOT NULL,
    Status NVARCHAR(255) NOT NULL,
    IsRequired BIT NOT NULL,
    Priority NVARCHAR(255) NOT NULL,
    CreatedAt BIGINT NOT NULL,
    UpdatedAt BIGINT NOT NULL,
    CONSTRAINT FK_ChildVaccineProfile_Child FOREIGN KEY (ChildId) REFERENCES Child(child_id),
    CONSTRAINT FK_ChildVaccineProfile_Disease FOREIGN KEY (DiseaseId) REFERENCES Disease(DiseaseID),
    CONSTRAINT FK_ChildVaccineProfile_Appointment FOREIGN KEY (AppointmentId) REFERENCES VaccinationAppointment(AppointmentId),
    CONSTRAINT FK_ChildVaccineProfile_Vaccine FOREIGN KEY (VaccineId) REFERENCES Vaccine(VaccineId)
);

-- Tạo bảng VaccinationAppointmentDetail
CREATE TABLE VaccinationAppointmentDetail (
    DetailID INT IDENTITY(1,1) PRIMARY KEY,
    AppointmentID INT NOT NULL,
    VaccineId INT NOT NULL,
    VaccinationDate DATE NOT NULL,
    DoseNumber NVARCHAR(50),
    Notes NVARCHAR(MAX),
    CreatedAt DATETIME NOT NULL,
    UpdatedAt DATETIME NOT NULL,
    CONSTRAINT FK_VaccinationAppointmentDetail_Appointment FOREIGN KEY (AppointmentID) REFERENCES VaccinationAppointment(AppointmentId),
    CONSTRAINT FK_VaccinationAppointmentDetail_Vaccine FOREIGN KEY (VaccineId) REFERENCES Vaccine(VaccineId)
);

-- Tạo bảng HealthSurvey
CREATE TABLE HealthSurvey (
    SurveyID INT IDENTITY(1,1) PRIMARY KEY,
    Title NVARCHAR(255) NOT NULL,
    Description NVARCHAR(MAX),
    StartDate DATE NOT NULL,
    EndDate DATE NOT NULL,
    Status NVARCHAR(50) NOT NULL,
    CreatedAt DATETIME NOT NULL,
    UpdatedAt DATETIME NOT NULL
);

-- Tạo bảng SurveyQuestion
CREATE TABLE SurveyQuestion (
    QuestionID INT IDENTITY(1,1) PRIMARY KEY,
    QuestionText NVARCHAR(MAX) NOT NULL,
    QuestionType NVARCHAR(50) NOT NULL,
    SurveyId INT NOT NULL,
    IsRequired BIT NOT NULL,
    CreatedAt DATETIME NOT NULL,
    UpdatedAt DATETIME NOT NULL,
    CONSTRAINT FK_SurveyQuestion_Survey FOREIGN KEY (SurveyId) REFERENCES HealthSurvey(SurveyID)
);

-- Tạo bảng SurveyAnswer
CREATE TABLE SurveyAnswer (
    AnswerID INT IDENTITY(1,1) PRIMARY KEY,
    QuestionID INT NOT NULL,
    AnswerText NVARCHAR(MAX) NOT NULL,
    IsCorrect BIT NOT NULL,
    CreatedAt DATETIME NOT NULL,
    UpdatedAt DATETIME NOT NULL,
    CONSTRAINT FK_SurveyAnswer_Question FOREIGN KEY (QuestionID) REFERENCES SurveyQuestion(QuestionID)
);

-- Tạo bảng AppointmentSurvey
CREATE TABLE AppointmentSurvey (
    SurveyID INT IDENTITY(1,1) PRIMARY KEY,
    AppointmentID INT NOT NULL,
    QuestionID INT NOT NULL,
    AnswerID INT,
    AnswerText NVARCHAR(MAX),
    CreatedAt DATETIME NOT NULL,
    UpdatedAt DATETIME NOT NULL,
    CONSTRAINT FK_AppointmentSurvey_Appointment FOREIGN KEY (AppointmentID) REFERENCES VaccinationAppointment(AppointmentId),
    CONSTRAINT FK_AppointmentSurvey_Question FOREIGN KEY (QuestionID) REFERENCES SurveyQuestion(QuestionID),
    CONSTRAINT FK_AppointmentSurvey_Answer FOREIGN KEY (AnswerID) REFERENCES SurveyAnswer(AnswerID)
);

-- Tạo bảng OrderDetail
CREATE TABLE OrderDetail (
    OrderDetailID INT IDENTITY(1,1) PRIMARY KEY,
    OrderID INT NOT NULL,
    VaccineID INT NOT NULL,
    RemainingQuantity INT NOT NULL,
    Price DECIMAL(10,2) NOT NULL,
    CreatedAt DATETIME NOT NULL,
    UpdatedAt DATETIME NOT NULL,
    CONSTRAINT FK_OrderDetail_Order FOREIGN KEY (OrderID) REFERENCES [Order](OrderID),
    CONSTRAINT FK_OrderDetail_Vaccine FOREIGN KEY (VaccineID) REFERENCES Vaccine(VaccineId)
);

-- Tạo bảng Transaction
CREATE TABLE [Transaction] (
    Transaction_id INT IDENTITY(1,1) PRIMARY KEY,
    TransactionType INT NOT NULL,
    DocNo INT NOT NULL,
    amount DECIMAL(8,2) NOT NULL,
    PaymentMethod NVARCHAR(255) NOT NULL,
    TransactionCode NVARCHAR(255) NOT NULL,
    Description NVARCHAR(255) NOT NULL,
    created_at DATE NOT NULL
);

-- Tạo bảng FacilityVaccine
CREATE TABLE FacilityVaccine (
    FacilityVaccineId INT IDENTITY(1,1) PRIMARY KEY,
    FacilityId INT NOT NULL,
    VaccineId INT NOT NULL,
    Price DECIMAL(8,2) NOT NULL,
    AvailableQuantity INT NOT NULL,
    BatchNumber INT NOT NULL,
    ExpiryDate DATE NOT NULL,
    ImportDate DATE NOT NULL,
    Status NVARCHAR(255) NOT NULL,
    CreatedAt DATETIME NOT NULL,
    UpdatedAt DATETIME NOT NULL,
    CONSTRAINT FK_FacilityVaccine_Facility FOREIGN KEY (FacilityId) REFERENCES VaccinationFacility(FacilityId),
    CONSTRAINT FK_FacilityVaccine_Vaccine FOREIGN KEY (VaccineId) REFERENCES Vaccine(VaccineId)
);

-- Tạo bảng GrowthRecord
CREATE TABLE GrowthRecord (
    RecordId INT IDENTITY(1,1) PRIMARY KEY,
    child_id INT NOT NULL,
    height DECIMAL(8,2) NOT NULL,
    weight DECIMAL(8,2) NOT NULL,
    BMI DECIMAL(8,2) NOT NULL,
    HeadCircumference DECIMAL(8,2) NOT NULL,
    Note NVARCHAR(255) NOT NULL,
    Created_at DATETIME NOT NULL,
    Updated_at DATETIME NOT NULL,
    CONSTRAINT FK_GrowthRecord_Child FOREIGN KEY (child_id) REFERENCES Child(child_id)
);

-- Tạo bảng DailyRecord
CREATE TABLE DailyRecord (
    DailyRecordId INT IDENTITY(1,1) PRIMARY KEY,
    ChildId INT NOT NULL,
    RecordDate DATE NOT NULL,
    MilkAmount INT NOT NULL,
    FeedingTimes INT NOT NULL,
    DiaperChanges INT NOT NULL,
    SleepHours DECIMAL(8,2) NOT NULL,
    Note NVARCHAR(255) NOT NULL,
    CreatedAt DATETIME NOT NULL,
    UpdatedAt DATETIME NOT NULL,
    CONSTRAINT FK_DailyRecord_Child FOREIGN KEY (ChildId) REFERENCES Child(child_id)
);

-- Tạo bảng GrowthStandard
CREATE TABLE GrowthStandard (
    id INT IDENTITY(1,1) PRIMARY KEY,
    Gender NVARCHAR(255) NOT NULL,
    AgeInMonths INT NOT NULL,
    Measurement NVARCHAR(255) NOT NULL,
    SD3neg DECIMAL(8,2) NOT NULL,
    SD2neg DECIMAL(8,2) NOT NULL,
    SD1neg DECIMAL(8,2) NOT NULL,
    Median DECIMAL(8,2) NOT NULL,
    SD1pos DECIMAL(8,2) NOT NULL,
    SD2pos DECIMAL(8,2) NOT NULL,
    SD3pos DECIMAL(8,2) NOT NULL
);

-- Tạo bảng Membership
CREATE TABLE Membership (
    MembershipId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(255) NOT NULL,
    Description NVARCHAR(255) NOT NULL,
    Duration INT NOT NULL,
    Price DECIMAL(8,2) NOT NULL,
    Benefits NVARCHAR(255) NOT NULL,
    Status BIT NOT NULL,
    CreatedAt DATETIME NOT NULL,
    UpdatedAt DATETIME NOT NULL
);

-- Tạo bảng UserMembership
CREATE TABLE UserMembership (
    UserMembershipId INT IDENTITY(1,1) PRIMARY KEY,
    AccountID INT NOT NULL,
    MembershipId INT NOT NULL,
    StartDate DATETIME NOT NULL,
    EndDate DATETIME NOT NULL,
    Status BIT NOT NULL,
    RemainingConsultations BIGINT NOT NULL,
    LastRenewalDate DATE NOT NULL,
    CONSTRAINT FK_UserMembership_Account FOREIGN KEY (AccountID) REFERENCES Account(account_id),
    CONSTRAINT FK_UserMembership_Membership FOREIGN KEY (MembershipId) REFERENCES Membership(MembershipId)
);

-- Tạo bảng FacilityMembership
CREATE TABLE FacilityMembership (
    FacilityMembershipId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(255) NOT NULL,
    Description NVARCHAR(MAX) NOT NULL,
    Duration INT NOT NULL,
    Price DECIMAL(8,2) NOT NULL,
    Benefits NVARCHAR(MAX) NOT NULL,
    Status BIT NOT NULL,
    CreatedAt DATETIME NOT NULL,
    UpdatedAt DATETIME NOT NULL
);

-- Tạo bảng FacilityMembershipSubscription
CREATE TABLE FacilityMembershipSubscription (
    SubscriptionId INT IDENTITY(1,1) PRIMARY KEY,
    FacilityId INT NOT NULL,
    FacilityMembershipId INT NOT NULL,
    StartDate DATETIME NOT NULL,
    EndDate DATETIME NOT NULL,
    Status BIT NOT NULL,
    CreatedAt DATETIME NOT NULL,
    UpdatedAt DATETIME NOT NULL,
    CONSTRAINT FK_FacilityMembershipSubscription_Facility FOREIGN KEY (FacilityId) REFERENCES VaccinationFacility(FacilityId),
    CONSTRAINT FK_FacilityMembershipSubscription_Membership FOREIGN KEY (FacilityMembershipId) REFERENCES FacilityMembership(FacilityMembershipId)
);

-- Tạo bảng FacilityRatings
CREATE TABLE FacilityRatings (
    RatingId INT IDENTITY(1,1) PRIMARY KEY,
    FacilityId INT NOT NULL,
    MemberId INT NOT NULL,
    Rating INT NOT NULL,
    Comment NVARCHAR(MAX),
    ServiceQuality INT NOT NULL,
    FacilityCleanliness INT NOT NULL,
    StaffAttitude INT NOT NULL,
    CreatedAt DATETIME NOT NULL,
    UpdatedAt DATETIME NOT NULL,
    CONSTRAINT FK_FacilityRatings_Facility FOREIGN KEY (FacilityId) REFERENCES VaccinationFacility(FacilityId),
    CONSTRAINT FK_FacilityRatings_Member FOREIGN KEY (MemberId) REFERENCES Member(MemberID)
);

-- Tạo bảng Blog
CREATE TABLE Blog (
    BlogId INT IDENTITY(1,1) PRIMARY KEY,
    Title NVARCHAR(255) NOT NULL,
    Content NVARCHAR(MAX) NOT NULL,
    Image NVARCHAR(MAX),
    Category NVARCHAR(255) NOT NULL,
    Status NVARCHAR(255) NOT NULL,
    CreatedAt DATETIME NOT NULL,
    UpdatedAt DATETIME NOT NULL
);

-- Tạo bảng VaccinePackage
CREATE TABLE VaccinePackage (
    PackageId INT IDENTITY(1,1) PRIMARY KEY,
    FacilityId INT NOT NULL,
    Name NVARCHAR(255) NOT NULL,
    Description NVARCHAR(MAX) NOT NULL,
    Duration INT NOT NULL,
    Price DECIMAL(8,2) NOT NULL,
    Status NVARCHAR(50) NOT NULL,
    CreatedAt DATETIME NOT NULL,
    UpdatedAt DATETIME NOT NULL,
    CONSTRAINT FK_VaccinePackage_Facility FOREIGN KEY (FacilityId) REFERENCES VaccinationFacility(FacilityId)
);

-- Tạo bảng PackageVaccine
CREATE TABLE PackageVaccine (
    PackageVaccineId INT IDENTITY(1,1) PRIMARY KEY,
    PackageId INT NOT NULL,
    VaccineId INT NOT NULL,
    Quantity INT NOT NULL,
    CreatedAt DATETIME NOT NULL,
    UpdatedAt DATETIME NOT NULL,
    CONSTRAINT FK_PackageVaccine_Package FOREIGN KEY (PackageId) REFERENCES VaccinePackage(PackageId),
    CONSTRAINT FK_PackageVaccine_Vaccine FOREIGN KEY (VaccineId) REFERENCES Vaccine(VaccineId)
);

-- Thêm Foreign Key cho Order.PackageId
ALTER TABLE [Order] 
ADD CONSTRAINT FK_Order_Package 
FOREIGN KEY (PackageId) REFERENCES VaccinePackage(PackageId);

-- Thêm Foreign Key cho AppointmentSchedule.SlotId
ALTER TABLE AppointmentSchedule 
ADD CONSTRAINT FK_AppointmentSchedule_Slot 
FOREIGN KEY (SlotId) REFERENCES ScheduleSlot(SlotId);

GO

PRINT 'Database HealthChildTrackerAndVaccination đã được tạo thành công với tất cả các bảng và relationships!'; 