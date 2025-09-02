-- =============================================
-- Tạo bảng GrowthVelocity để lưu trữ tốc độ tăng trưởng WHO
-- =============================================

USE [HealthChildTracker]
GO

-- Tạo bảng GrowthVelocity
CREATE TABLE [dbo].[GrowthVelocity](
    [Id] [int] IDENTITY(1,1) NOT NULL,
    [Gender] [nvarchar](10) NOT NULL,
    [AgeInMonths] [int] NOT NULL,
    [Measurement] [nvarchar](50) NOT NULL,
    [Sd3neg] [decimal](8,3) NOT NULL,
    [Sd2neg] [decimal](8,3) NOT NULL,
    [Sd1neg] [decimal](8,3) NOT NULL,
    [Median] [decimal](8,3) NOT NULL,
    [Sd1pos] [decimal](8,3) NOT NULL,
    [Sd2pos] [decimal](8,3) NOT NULL,
    [Sd3pos] [decimal](8,3) NOT NULL,
    [Unit] [nvarchar](10) NULL,
    [CreatedAt] [datetime] NOT NULL DEFAULT GETDATE(),
    [UpdatedAt] [datetime] NULL,
 CONSTRAINT [PK_GrowthVelocity] PRIMARY KEY CLUSTERED 
(
    [Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

-- Tạo index để tối ưu truy vấn
CREATE NONCLUSTERED INDEX [IX_GrowthVelocity_Gender_Age_Measurement] ON [dbo].[GrowthVelocity]
(
    [Gender] ASC,
    [AgeInMonths] ASC,
    [Measurement] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO

-- =============================================
-- Thêm dữ liệu mẫu cho GrowthVelocity
-- Dữ liệu này dựa trên WHO Growth Velocity Standards
-- =============================================

-- Chiều cao (cm/tháng) - Nam
INSERT INTO [dbo].[GrowthVelocity] ([Gender], [AgeInMonths], [Measurement], [Sd3neg], [Sd2neg], [Sd1neg], [Median], [Sd1pos], [Sd2pos], [Sd3pos], [Unit])
VALUES 
('Male', 0, 'Height', 1.5, 1.8, 2.1, 2.4, 2.7, 3.0, 3.3, 'cm/month'),
('Male', 1, 'Height', 1.8, 2.1, 2.4, 2.7, 3.0, 3.3, 3.6, 'cm/month'),
('Male', 2, 'Height', 2.1, 2.4, 2.7, 3.0, 3.3, 3.6, 3.9, 'cm/month'),
('Male', 3, 'Height', 2.4, 2.7, 3.0, 3.3, 3.6, 3.9, 4.2, 'cm/month'),
('Male', 6, 'Height', 2.7, 3.0, 3.3, 3.6, 3.9, 4.2, 4.5, 'cm/month'),
('Male', 9, 'Height', 2.4, 2.7, 3.0, 3.3, 3.6, 3.9, 4.2, 'cm/month'),
('Male', 12, 'Height', 2.1, 2.4, 2.7, 3.0, 3.3, 3.6, 3.9, 'cm/month'),
('Male', 18, 'Height', 1.8, 2.1, 2.4, 2.7, 3.0, 3.3, 3.6, 'cm/month'),
('Male', 24, 'Height', 1.5, 1.8, 2.1, 2.4, 2.7, 3.0, 3.3, 'cm/month'),
('Male', 36, 'Height', 1.2, 1.5, 1.8, 2.1, 2.4, 2.7, 3.0, 'cm/month'),
('Male', 48, 'Height', 1.0, 1.2, 1.5, 1.8, 2.1, 2.4, 2.7, 'cm/month'),
('Male', 60, 'Height', 0.8, 1.0, 1.2, 1.5, 1.8, 2.1, 2.4, 'cm/month');

-- Chiều cao (cm/tháng) - Nữ
INSERT INTO [dbo].[GrowthVelocity] ([Gender], [AgeInMonths], [Measurement], [Sd3neg], [Sd2neg], [Sd1neg], [Median], [Sd1pos], [Sd2pos], [Sd3pos], [Unit])
VALUES 
('Female', 0, 'Height', 1.4, 1.7, 2.0, 2.3, 2.6, 2.9, 3.2, 'cm/month'),
('Female', 1, 'Height', 1.7, 2.0, 2.3, 2.6, 2.9, 3.2, 3.5, 'cm/month'),
('Female', 2, 'Height', 2.0, 2.3, 2.6, 2.9, 3.2, 3.5, 3.8, 'cm/month'),
('Female', 3, 'Height', 2.3, 2.6, 2.9, 3.2, 3.5, 3.8, 4.1, 'cm/month'),
('Female', 6, 'Height', 2.6, 2.9, 3.2, 3.5, 3.8, 4.1, 4.4, 'cm/month'),
('Female', 9, 'Height', 2.3, 2.6, 2.9, 3.2, 3.5, 3.8, 4.1, 'cm/month'),
('Female', 12, 'Height', 2.0, 2.3, 2.6, 2.9, 3.2, 3.5, 3.8, 'cm/month'),
('Female', 18, 'Height', 1.7, 2.0, 2.3, 2.6, 2.9, 3.2, 3.5, 'cm/month'),
('Female', 24, 'Height', 1.4, 1.7, 2.0, 2.3, 2.6, 2.9, 3.2, 'cm/month'),
('Female', 36, 'Height', 1.1, 1.4, 1.7, 2.0, 2.3, 2.6, 2.9, 'cm/month'),
('Female', 48, 'Height', 0.9, 1.1, 1.4, 1.7, 2.0, 2.3, 2.6, 'cm/month'),
('Female', 60, 'Height', 0.7, 0.9, 1.1, 1.4, 1.7, 2.0, 2.3, 'cm/month');

-- Cân nặng (kg/tháng) - Nam
INSERT INTO [dbo].[GrowthVelocity] ([Gender], [AgeInMonths], [Measurement], [Sd3neg], [Sd2neg], [Sd1neg], [Median], [Sd1pos], [Sd2pos], [Sd3pos], [Unit])
VALUES 
('Male', 0, 'Weight', 0.6, 0.8, 1.0, 1.2, 1.4, 1.6, 1.8, 'kg/month'),
('Male', 1, 'Weight', 0.8, 1.0, 1.2, 1.4, 1.6, 1.8, 2.0, 'kg/month'),
('Male', 2, 'Weight', 1.0, 1.2, 1.4, 1.6, 1.8, 2.0, 2.2, 'kg/month'),
('Male', 3, 'Weight', 1.2, 1.4, 1.6, 1.8, 2.0, 2.2, 2.4, 'kg/month'),
('Male', 6, 'Weight', 1.4, 1.6, 1.8, 2.0, 2.2, 2.4, 2.6, 'kg/month'),
('Male', 9, 'Weight', 1.2, 1.4, 1.6, 1.8, 2.0, 2.2, 2.4, 'kg/month'),
('Male', 12, 'Weight', 1.0, 1.2, 1.4, 1.6, 1.8, 2.0, 2.2, 'kg/month'),
('Male', 18, 'Weight', 0.8, 1.0, 1.2, 1.4, 1.6, 1.8, 2.0, 'kg/month'),
('Male', 24, 'Weight', 0.6, 0.8, 1.0, 1.2, 1.4, 1.6, 1.8, 'kg/month'),
('Male', 36, 'Weight', 0.4, 0.6, 0.8, 1.0, 1.2, 1.4, 1.6, 'kg/month'),
('Male', 48, 'Weight', 0.3, 0.4, 0.6, 0.8, 1.0, 1.2, 1.4, 'kg/month'),
('Male', 60, 'Weight', 0.2, 0.3, 0.4, 0.6, 0.8, 1.0, 1.2, 'kg/month');

-- Cân nặng (kg/tháng) - Nữ
INSERT INTO [dbo].[GrowthVelocity] ([Gender], [AgeInMonths], [Measurement], [Sd3neg], [Sd2neg], [Sd1neg], [Median], [Sd1pos], [Sd2pos], [Sd3pos], [Unit])
VALUES 
('Female', 0, 'Weight', 0.5, 0.7, 0.9, 1.1, 1.3, 1.5, 1.7, 'kg/month'),
('Female', 1, 'Weight', 0.7, 0.9, 1.1, 1.3, 1.5, 1.7, 1.9, 'kg/month'),
('Female', 2, 'Weight', 0.9, 1.1, 1.3, 1.5, 1.7, 1.9, 2.1, 'kg/month'),
('Female', 3, 'Weight', 1.1, 1.3, 1.5, 1.7, 1.9, 2.1, 2.3, 'kg/month'),
('Female', 6, 'Weight', 1.3, 1.5, 1.7, 1.9, 2.1, 2.3, 2.5, 'kg/month'),
('Female', 9, 'Weight', 1.1, 1.3, 1.5, 1.7, 1.9, 2.1, 2.3, 'kg/month'),
('Female', 12, 'Weight', 0.9, 1.1, 1.3, 1.5, 1.7, 1.9, 2.1, 'kg/month'),
('Female', 18, 'Weight', 0.7, 0.9, 1.1, 1.3, 1.5, 1.7, 1.9, 'kg/month'),
('Female', 24, 'Weight', 0.5, 0.7, 0.9, 1.1, 1.3, 1.5, 1.7, 'kg/month'),
('Female', 36, 'Weight', 0.3, 0.5, 0.7, 0.9, 1.1, 1.3, 1.5, 'kg/month'),
('Female', 48, 'Weight', 0.2, 0.3, 0.5, 0.7, 0.9, 1.1, 1.3, 'kg/month'),
('Female', 60, 'Weight', 0.1, 0.2, 0.3, 0.5, 0.7, 0.9, 1.1, 'kg/month');

-- Vòng đầu (cm/tháng) - Nam
INSERT INTO [dbo].[GrowthVelocity] ([Gender], [AgeInMonths], [Measurement], [Sd3neg], [Sd2neg], [Sd1neg], [Median], [Sd1pos], [Sd2pos], [Sd3pos], [Unit])
VALUES 
('Male', 0, 'HeadCircumference', 1.0, 1.2, 1.4, 1.6, 1.8, 2.0, 2.2, 'cm/month'),
('Male', 1, 'HeadCircumference', 1.2, 1.4, 1.6, 1.8, 2.0, 2.2, 2.4, 'cm/month'),
('Male', 2, 'HeadCircumference', 1.4, 1.6, 1.8, 2.0, 2.2, 2.4, 2.6, 'cm/month'),
('Male', 3, 'HeadCircumference', 1.6, 1.8, 2.0, 2.2, 2.4, 2.6, 2.8, 'cm/month'),
('Male', 6, 'HeadCircumference', 1.8, 2.0, 2.2, 2.4, 2.6, 2.8, 3.0, 'cm/month'),
('Male', 9, 'HeadCircumference', 1.6, 1.8, 2.0, 2.2, 2.4, 2.6, 2.8, 'cm/month'),
('Male', 12, 'HeadCircumference', 1.4, 1.6, 1.8, 2.0, 2.2, 2.4, 2.6, 'cm/month'),
('Male', 18, 'HeadCircumference', 1.2, 1.4, 1.6, 1.8, 2.0, 2.2, 2.4, 'cm/month'),
('Male', 24, 'HeadCircumference', 1.0, 1.2, 1.4, 1.6, 1.8, 2.0, 2.2, 'cm/month'),
('Male', 36, 'HeadCircumference', 0.8, 1.0, 1.2, 1.4, 1.6, 1.8, 2.0, 'cm/month'),
('Male', 48, 'HeadCircumference', 0.6, 0.8, 1.0, 1.2, 1.4, 1.6, 1.8, 'cm/month'),
('Male', 60, 'HeadCircumference', 0.4, 0.6, 0.8, 1.0, 1.2, 1.4, 1.6, 'cm/month');

-- Vòng đầu (cm/tháng) - Nữ
INSERT INTO [dbo].[GrowthVelocity] ([Gender], [AgeInMonths], [Measurement], [Sd3neg], [Sd2neg], [Sd1neg], [Median], [Sd1pos], [Sd2pos], [Sd3pos], [Unit])
VALUES 
('Female', 0, 'HeadCircumference', 0.9, 1.1, 1.3, 1.5, 1.7, 1.9, 2.1, 'cm/month'),
('Female', 1, 'HeadCircumference', 1.1, 1.3, 1.5, 1.7, 1.9, 2.1, 2.3, 'cm/month'),
('Female', 2, 'HeadCircumference', 1.3, 1.5, 1.7, 1.9, 2.1, 2.3, 2.5, 'cm/month'),
('Female', 3, 'HeadCircumference', 1.5, 1.7, 1.9, 2.1, 2.3, 2.5, 2.7, 'cm/month'),
('Female', 6, 'HeadCircumference', 1.7, 1.9, 2.1, 2.3, 2.5, 2.7, 2.9, 'cm/month'),
('Female', 9, 'HeadCircumference', 1.5, 1.7, 1.9, 2.1, 2.3, 2.5, 2.7, 'cm/month'),
('Female', 12, 'HeadCircumference', 1.3, 1.5, 1.7, 1.9, 2.1, 2.3, 2.5, 'cm/month'),
('Female', 18, 'HeadCircumference', 1.1, 1.3, 1.5, 1.7, 1.9, 2.1, 2.3, 'cm/month'),
('Female', 24, 'HeadCircumference', 0.9, 1.1, 1.3, 1.5, 1.7, 1.9, 2.1, 'cm/month'),
('Female', 36, 'HeadCircumference', 0.7, 0.9, 1.1, 1.3, 1.5, 1.7, 1.9, 'cm/month'),
('Female', 48, 'HeadCircumference', 0.5, 0.7, 0.9, 1.1, 1.3, 1.5, 1.7, 'cm/month'),
('Female', 60, 'HeadCircumference', 0.3, 0.5, 0.7, 0.9, 1.1, 1.3, 1.5, 'cm/month');

-- =============================================
-- Tạo stored procedure để tính toán growth velocity
-- =============================================

CREATE PROCEDURE [dbo].[CalculateGrowthVelocity]
    @ChildId INT,
    @Measurement NVARCHAR(50),
    @StartDate DATETIME,
    @EndDate DATETIME
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @StartValue DECIMAL(8,2)
    DECLARE @EndValue DECIMAL(8,2)
    DECLARE @MonthsDiff DECIMAL(8,2)
    DECLARE @Velocity DECIMAL(8,3)
    
    -- Lấy giá trị đầu và cuối
    SELECT @StartValue = 
        CASE @Measurement
            WHEN 'Height' THEN Height
            WHEN 'Weight' THEN Weight
            WHEN 'HeadCircumference' THEN HeadCircumference
            ELSE NULL
        END
    FROM GrowthRecord 
    WHERE ChildId = @ChildId AND CreatedAt = @StartDate
    
    SELECT @EndValue = 
        CASE @Measurement
            WHEN 'Height' THEN Height
            WHEN 'Weight' THEN Weight
            WHEN 'HeadCircumference' THEN HeadCircumference
            ELSE NULL
        END
    FROM GrowthRecord 
    WHERE ChildId = @ChildId AND CreatedAt = @EndDate
    
    -- Tính số tháng chênh lệch
    SET @MonthsDiff = DATEDIFF(DAY, @StartDate, @EndDate) / 30.44
    
    -- Tính velocity
    IF @MonthsDiff > 0 AND @StartValue IS NOT NULL AND @EndValue IS NOT NULL
        SET @Velocity = (@EndValue - @StartValue) / @MonthsDiff
    ELSE
        SET @Velocity = 0
    
    -- Trả về kết quả
    SELECT 
        @ChildId AS ChildId,
        @Measurement AS Measurement,
        @StartDate AS StartDate,
        @EndDate AS EndDate,
        @StartValue AS StartValue,
        @EndValue AS EndValue,
        @MonthsDiff AS MonthsDiff,
        @Velocity AS Velocity
END
GO

PRINT 'Bảng GrowthVelocity và dữ liệu mẫu đã được tạo thành công!'
PRINT 'Stored procedure CalculateGrowthVelocity đã được tạo!'
