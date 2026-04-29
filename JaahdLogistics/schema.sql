CREATE TABLE IF NOT EXISTS Users (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Username TEXT UNIQUE NOT NULL,
    PasswordHash TEXT NOT NULL,
    Role TEXT NOT NULL, -- Admin, ProjectManager, ProcurementManager, FinanceManager, Storekeeper, HeadOfAssociation
    FullName TEXT,
    Position TEXT,
    SignatureImage BLOB
);

CREATE TABLE IF NOT EXISTS Projects (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Code TEXT NOT NULL,
    Year INTEGER NOT NULL
);

CREATE TABLE IF NOT EXISTS BudgetLines (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ProjectId INTEGER NOT NULL,
    Code TEXT NOT NULL, -- e.g., 1.1
    Name TEXT NOT NULL DEFAULT '',
    Description TEXT,
    Unit TEXT,
    Quantity DECIMAL(18, 2) NOT NULL DEFAULT 0,
    UnitPrice DECIMAL(18, 2) NOT NULL DEFAULT 0,
    TotalAmount DECIMAL(18, 2) NOT NULL,
    Currency TEXT NOT NULL, -- USD, YER
    FOREIGN KEY (ProjectId) REFERENCES Projects(Id)
);

CREATE TABLE IF NOT EXISTS PurchaseRequisitions (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    PRNumber TEXT UNIQUE NOT NULL,
    ProjectId INTEGER NOT NULL,
    RequesterId INTEGER NOT NULL,
    Date DATETIME DEFAULT CURRENT_TIMESTAMP,
    Justification TEXT,
    Currency TEXT NOT NULL,
    ExchangeRate DECIMAL(18, 4) DEFAULT 1.0,
    Status TEXT DEFAULT 'Pending', -- Pending, LogisticsApproved, FinanceApproved, FinalApproved, Rejected
    Version INTEGER DEFAULT 1,
    LastModified DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (ProjectId) REFERENCES Projects(Id),
    FOREIGN KEY (RequesterId) REFERENCES Users(Id)
);

CREATE TABLE IF NOT EXISTS PRItems (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    PRId INTEGER NOT NULL,
    BudgetLineId INTEGER NOT NULL,
    Description TEXT NOT NULL,
    Unit TEXT,
    Quantity DECIMAL(18, 2) NOT NULL,
    UnitPrice DECIMAL(18, 2) NOT NULL,
    FOREIGN KEY (PRId) REFERENCES PurchaseRequisitions(Id),
    FOREIGN KEY (BudgetLineId) REFERENCES BudgetLines(Id)
);

CREATE TABLE IF NOT EXISTS RFQs (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    RFQNumber TEXT UNIQUE NOT NULL,
    PRId INTEGER NOT NULL,
    Date DATETIME DEFAULT CURRENT_TIMESTAMP,
    ClosingDate DATETIME,
    Terms TEXT,
    FOREIGN KEY (PRId) REFERENCES PurchaseRequisitions(Id)
);

CREATE TABLE IF NOT EXISTS BidAnalyses (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    RFQId INTEGER NOT NULL,
    Date DATETIME DEFAULT CURRENT_TIMESTAMP,
    RecommendedBidderId INTEGER,
    Justification TEXT,
    Status TEXT DEFAULT 'Pending', -- Pending, LogisticsApproved, FinanceApproved, PMApproved, FinalApproved, Rejected
    Version INTEGER DEFAULT 1,
    LastModified DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (RFQId) REFERENCES RFQs(Id)
);

CREATE TABLE IF NOT EXISTS Bidders (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    BidAnalysisId INTEGER NOT NULL,
    Name TEXT NOT NULL,
    Address TEXT,
    Contact TEXT,
    Email TEXT,
    FOREIGN KEY (BidAnalysisId) REFERENCES BidAnalyses(Id)
);

CREATE TABLE IF NOT EXISTS BidItems (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    BidderId INTEGER NOT NULL,
    Description TEXT NOT NULL,
    Unit TEXT,
    Quantity DECIMAL(18, 2) NOT NULL,
    UnitPrice DECIMAL(18, 2) NOT NULL,
    FOREIGN KEY (BidderId) REFERENCES Bidders(Id)
);

CREATE TABLE IF NOT EXISTS PurchaseOrders (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    PONumber TEXT UNIQUE NOT NULL,
    PRId INTEGER NOT NULL,
    ProjectId INTEGER NOT NULL DEFAULT 0,
    BidAnalysisId INTEGER,
    VendorId INTEGER, -- Points to Bidders(Id)
    Date DATETIME DEFAULT CURRENT_TIMESTAMP,
    Terms TEXT,
    Status TEXT DEFAULT 'Pending', -- Pending, LogisticsApproved, FinanceApproved, PMApproved, FinalApproved, Rejected
    Version INTEGER DEFAULT 1,
    LastModified DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (PRId) REFERENCES PurchaseRequisitions(Id),
    FOREIGN KEY (BidAnalysisId) REFERENCES BidAnalyses(Id),
    FOREIGN KEY (VendorId) REFERENCES Bidders(Id)
);

CREATE TABLE IF NOT EXISTS POItems (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    POId INTEGER NOT NULL,
    Description TEXT NOT NULL,
    Unit TEXT,
    Quantity DECIMAL(18, 2) NOT NULL,
    UnitPrice DECIMAL(18, 2) NOT NULL,
    FOREIGN KEY (POId) REFERENCES PurchaseOrders(Id)
);

CREATE TABLE IF NOT EXISTS GoodsReceivingNotes (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    GRNNumber TEXT UNIQUE NOT NULL,
    POId INTEGER NOT NULL,
    Date DATETIME DEFAULT CURRENT_TIMESTAMP,
    ReceiverId INTEGER NOT NULL, -- Storekeeper
    Status TEXT DEFAULT 'Completed',
    FOREIGN KEY (POId) REFERENCES PurchaseOrders(Id),
    FOREIGN KEY (ReceiverId) REFERENCES Users(Id)
);

CREATE TABLE IF NOT EXISTS GRNItems (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    GRNId INTEGER NOT NULL,
    POItemId INTEGER NOT NULL,
    ReceivedQuantity DECIMAL(18, 2) NOT NULL,
    AcceptedQuantity DECIMAL(18, 2) NOT NULL,
    RejectedQuantity DECIMAL(18, 2) NOT NULL,
    RejectReason TEXT,
    FOREIGN KEY (GRNId) REFERENCES GoodsReceivingNotes(Id),
    FOREIGN KEY (POItemId) REFERENCES POItems(Id)
);

CREATE TABLE IF NOT EXISTS ThreeWayMatch (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    POId INTEGER NOT NULL,
    GRNId INTEGER NOT NULL,
    InvoiceNumber TEXT,
    InvoiceDetails TEXT,
    InvoiceScan BLOB,
    Date DATETIME DEFAULT CURRENT_TIMESTAMP,
    Status TEXT DEFAULT 'Pending', -- Pending, LogisticsApproved, FinanceApproved, PMApproved
    FOREIGN KEY (POId) REFERENCES PurchaseOrders(Id),
    FOREIGN KEY (GRNId) REFERENCES GoodsReceivingNotes(Id)
);

CREATE TABLE IF NOT EXISTS Inventory (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ItemDescription TEXT NOT NULL,
    ProjectId INTEGER NOT NULL,
    CurrentQuantity DECIMAL(18, 2) NOT NULL,
    Unit TEXT,
    FOREIGN KEY (ProjectId) REFERENCES Projects(Id)
);

CREATE TABLE IF NOT EXISTS Settings (
    Id INTEGER PRIMARY KEY CHECK (Id = 1),
    AssociationName TEXT,
    Address TEXT,
    ContactInfo TEXT,
    LogoImage BLOB,
    PRTerms TEXT,
    RFQTerms TEXT,
    POTerms TEXT
);

CREATE TABLE IF NOT EXISTS Approvals (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    EntityType TEXT NOT NULL, -- PR, BidAnalysis, PO, ThreeWayMatch
    EntityId INTEGER NOT NULL,
    UserId INTEGER NOT NULL,
    Status TEXT NOT NULL, -- Approved, Rejected
    ApprovalDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (UserId) REFERENCES Users(Id)
);
