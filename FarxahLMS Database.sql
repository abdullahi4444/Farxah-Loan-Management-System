/* =========================================================
   DATABASE CREATION
   ========================================================= */
CREATE DATABASE FarxahLMS;
GO
USE FarxahLMS;
GO


/* =========================================================
   TABLE: USERS (Admin & Loan Officer)
   ========================================================= */
CREATE TABLE Users (
    UserID INT IDENTITY(1,1) PRIMARY KEY,
    Username VARCHAR(50) NOT NULL UNIQUE,
    PasswordHash VARCHAR(255) NOT NULL,
    Role VARCHAR(20) NOT NULL CHECK (Role IN ('Admin', 'LoanOfficer')),
    CreatedAt DATETIME DEFAULT GETDATE()
);
GO

select * from Users;



/* =========================================================
   TABLE: CUSTOMERS
   ========================================================= */
CREATE TABLE Customers (
    CustomerID INT IDENTITY(1,1) PRIMARY KEY,
    FullName VARCHAR(100) NOT NULL,
    Phone VARCHAR(15) NOT NULL,
    Address VARCHAR(150),
    CreatedDate DATETIME DEFAULT GETDATE()
);
GO

-- ---------------------------
ALTER TABLE Customers 
ADD CONSTRAINT UQ_Customers_FullName_Phone UNIQUE (FullName, Phone);


/* =========================================================
   TABLE: LOANS 
   ========================================================= */
CREATE TABLE Loans (
    LoanID INT IDENTITY(1,1) PRIMARY KEY,
    CustomerID INT NOT NULL,
    LoanAmount DECIMAL(10,2) NOT NULL CHECK (LoanAmount > 0),
    LoanDate DATE NOT NULL,
    DueDate DATE NOT NULL,
    Status VARCHAR(20) DEFAULT 'Active',

    CONSTRAINT FK_Loans_Customers
        FOREIGN KEY (CustomerID) REFERENCES Customers(CustomerID)
);
GO


SELECT COUNT(*) 
    FROM Loans 
    WHERE Status = 'Overdue';


/* =========================================================
   TABLE: PAYMENTS
   ========================================================= */
CREATE TABLE Payments (
    PaymentID INT IDENTITY(1,1) PRIMARY KEY,
    LoanID INT NOT NULL,
    PaidAmount DECIMAL(10,2) NOT NULL CHECK (PaidAmount > 0),
    PaymentDate DATE DEFAULT GETDATE(),
    RecordedBy INT NOT NULL,

    CONSTRAINT FK_Payments_Loans
        FOREIGN KEY (LoanID) REFERENCES Loans(LoanID),

    CONSTRAINT FK_Payments_Users
        FOREIGN KEY (RecordedBy) REFERENCES Users(UserID)
);
GO

select * from payments


/* =========================================================
   SAMPLE ADMIN USER (FOR TESTING)
   ========================================================= */
INSERT INTO Users (Username, PasswordHash, Role)
VALUES ('admin', '123', 'Admin');
GO
INSERT INTO Users (Username, PasswordHash, Role)
VALUES ('LoanOfficer', '123hashed', 'LoanOfficer');
GO

select * from Users;
select * from Customers;
select * from Loans;
select * from Payments;



SELECT 
    c.CustomerID,
    c.FullName,
    c.Phone,
    c.Address,
    CONVERT(VARCHAR(10), c.CreatedDate, 103) AS [Registration Date],

    ISNULL(loans.TotalLoan, 0) - ISNULL(payments.TotalPaid, 0) AS Balance

FROM Customers c
LEFT JOIN (
    SELECT CustomerID, SUM(LoanAmount) AS TotalLoan
    FROM Loans
    GROUP BY CustomerID
) loans ON c.CustomerID = loans.CustomerID

LEFT JOIN (
    SELECT l.CustomerID, SUM(p.PaidAmount) AS TotalPaid
    FROM Payments p
    INNER JOIN Loans l ON p.LoanID = l.LoanID
    GROUP BY l.CustomerID
) payments ON c.CustomerID = payments.CustomerID

ORDER BY c.FullName;


CREATE VIEW vw_CustomerBalance AS
SELECT 
    -- Customer Information
    c.CustomerID,
    c.FullName,
    c.Phone,
    c.Address,
    c.CreatedDate,
    
    -- Loan Information
    ISNULL(SUM(l.LoanAmount), 0) AS TotalLoanAmount,
    
    -- Payment Information
    ISNULL(SUM(p.PaidAmount), 0) AS TotalPaidAmount,
    
    -- Balance Calculation
    ISNULL(SUM(l.LoanAmount), 0) - ISNULL(SUM(p.PaidAmount), 0) AS Balance,
    
    -- Additional useful fields
    COUNT(DISTINCT l.LoanID) AS NumberOfLoans,
    COUNT(DISTINCT p.PaymentID) AS NumberOfPayments,
    
    -- Status based on balance
    CASE 
        WHEN ISNULL(SUM(l.LoanAmount), 0) - ISNULL(SUM(p.PaidAmount), 0) > 0 THEN 'Pending'
        WHEN ISNULL(SUM(l.LoanAmount), 0) - ISNULL(SUM(p.PaidAmount), 0) = 0 THEN 'Paid'
        ELSE 'Overpaid'
    END AS PaymentStatus,
    
    -- Last payment date
    MAX(p.PaymentDate) AS LastPaymentDate
    
FROM Customers c
LEFT JOIN Loans l ON c.CustomerID = l.CustomerID
LEFT JOIN Payments p ON l.LoanID = p.LoanID
GROUP BY c.CustomerID, c.FullName, c.Phone, c.Address, c.CreatedDate;
GO


SELECT * FROM vw_CustomerBalance ORDER BY Balance DESC;







CREATE OR ALTER VIEW vw_CustomerBalance2 AS
SELECT 
    c.CustomerID,
    c.FullName,
    c.Phone,
    c.Address,
    c.CreatedDate,
    
    -- Total loan amount per customer
    ISNULL((
        SELECT SUM(LoanAmount) 
        FROM Loans 
        WHERE CustomerID = c.CustomerID
    ), 0) AS TotalLoanAmount,
    
    -- Total paid amount per customer
    ISNULL((
        SELECT SUM(p.PaidAmount) 
        FROM Payments p
        INNER JOIN Loans l ON p.LoanID = l.LoanID
        WHERE l.CustomerID = c.CustomerID
    ), 0) AS TotalPaidAmount,
    
    -- Balance (Total Loan - Total Paid)
    ISNULL((
        SELECT SUM(LoanAmount) 
        FROM Loans 
        WHERE CustomerID = c.CustomerID
    ), 0) - 
    ISNULL((
        SELECT SUM(p.PaidAmount) 
        FROM Payments p
        INNER JOIN Loans l ON p.LoanID = l.LoanID
        WHERE l.CustomerID = c.CustomerID
    ), 0) AS Balance
FROM Customers c;

SELECT * FROM vw_CustomerBalance2 ORDER BY Balance DESC;
