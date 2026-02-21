-- =============================================================================
-- SCRIPT DE BASE DE DATOS - PedidosDB
-- SQL Server 2019+
-- =============================================================================

USE master;
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'PedidosDB')
    CREATE DATABASE PedidosDB;
GO

--USE PedidosDB;
GO

--  PedidoCabecera 
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name = 'PedidoCabecera' AND xtype = 'U')
BEGIN
    CREATE TABLE PedidoCabecera (
        Id        INT IDENTITY(1,1) NOT NULL,
        ClienteId INT               NOT NULL,
        Fecha     DATETIME2         NOT NULL CONSTRAINT DF_PedidoCabecera_Fecha DEFAULT (GETUTCDATE()),
        Total     DECIMAL(18,2)     NOT NULL,
        Usuario   NVARCHAR(100)     NOT NULL,
        CONSTRAINT PK_PedidoCabecera PRIMARY KEY (Id)
    );
    CREATE INDEX IX_PedidoCabecera_ClienteId ON PedidoCabecera (ClienteId);
    CREATE INDEX IX_PedidoCabecera_Fecha     ON PedidoCabecera (Fecha DESC);
    PRINT 'Tabla PedidoCabecera creada.';
END
GO

--  PedidoDetalle 
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name = 'PedidoDetalle' AND xtype = 'U')
BEGIN
    CREATE TABLE PedidoDetalle (
        Id         INT IDENTITY(1,1) NOT NULL,
        PedidoId   INT               NOT NULL,
        ProductoId INT               NOT NULL,
        Cantidad   INT               NOT NULL,
        Precio     DECIMAL(18,2)     NOT NULL,
        CONSTRAINT PK_PedidoDetalle PRIMARY KEY (Id),
        CONSTRAINT FK_PedidoDetalle_PedidoCabecera
            FOREIGN KEY (PedidoId) REFERENCES PedidoCabecera (Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_PedidoDetalle_PedidoId   ON PedidoDetalle (PedidoId);
    CREATE INDEX IX_PedidoDetalle_ProductoId ON PedidoDetalle (ProductoId);
    PRINT 'Tabla PedidoDetalle creada.';
END
GO

--  LogAuditoria 
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name = 'LogAuditoria' AND xtype = 'U')
BEGIN
    CREATE TABLE LogAuditoria (
        Id          INT IDENTITY(1,1) NOT NULL,
        Fecha       DATETIME2         NOT NULL CONSTRAINT DF_LogAuditoria_Fecha DEFAULT (GETUTCDATE()),
        Evento      NVARCHAR(100)     NOT NULL,
        Descripcion NVARCHAR(500)     NOT NULL,
        Usuario     NVARCHAR(100)     NULL,
        Nivel       NVARCHAR(10)      NOT NULL CONSTRAINT DF_LogAuditoria_Nivel DEFAULT ('INFO'),
        CONSTRAINT PK_LogAuditoria PRIMARY KEY (Id)
    );
    CREATE INDEX IX_LogAuditoria_Fecha ON LogAuditoria (Fecha DESC);
    CREATE INDEX IX_LogAuditoria_Nivel ON LogAuditoria (Nivel);
    PRINT 'Tabla LogAuditoria creada.';
END
GO

PRINT 'Script finalizado exitosamente. Base de datos PedidosDB lista.';
GO
