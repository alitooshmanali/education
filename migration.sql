IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [Folders] (
    [Id] bigint NOT NULL IDENTITY,
    [Name] nvarchar(200) NOT NULL,
    [ParentFolderId] bigint NULL,
    [RelativePath] nvarchar(1000) NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [UpdatedAt] datetimeoffset NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Folders] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Folders_Folders_ParentFolderId] FOREIGN KEY ([ParentFolderId]) REFERENCES [Folders] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Users] (
    [Id] bigint NOT NULL IDENTITY,
    [Username] nvarchar(100) NOT NULL,
    [PasswordHash] nvarchar(500) NOT NULL,
    [Role] int NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [UpdatedAt] datetimeoffset NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
);

CREATE TABLE [Videos] (
    [Id] bigint NOT NULL IDENTITY,
    [FileName] nvarchar(260) NOT NULL,
    [DisplayName] nvarchar(260) NOT NULL,
    [RelativePath] nvarchar(1000) NOT NULL,
    [StorageKey] nvarchar(1000) NOT NULL,
    [FolderId] bigint NOT NULL,
    [FileExtension] nvarchar(20) NOT NULL,
    [FileSize] bigint NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [UpdatedAt] datetimeoffset NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Videos] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Videos_Folders_FolderId] FOREIGN KEY ([FolderId]) REFERENCES [Folders] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AuditLogs] (
    [Id] bigint NOT NULL IDENTITY,
    [UserId] bigint NULL,
    [Action] nvarchar(200) NOT NULL,
    [EntityType] nvarchar(200) NOT NULL,
    [EntityId] bigint NULL,
    [Description] nvarchar(2000) NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [UpdatedAt] datetimeoffset NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AuditLogs_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE SET NULL
);

CREATE TABLE [Permissions] (
    [Id] bigint NOT NULL IDENTITY,
    [UserId] bigint NOT NULL,
    [FolderId] bigint NOT NULL,
    [PermissionType] int NOT NULL,
    [CreatedByUserId] bigint NULL,
    [RevokedAt] datetimeoffset NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [UpdatedAt] datetimeoffset NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Permissions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Permissions_Folders_FolderId] FOREIGN KEY ([FolderId]) REFERENCES [Folders] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Permissions_Users_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [Users] ([Id]),
    CONSTRAINT [FK_Permissions_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [UserDevices] (
    [Id] bigint NOT NULL IDENTITY,
    [UserId] bigint NOT NULL,
    [DeviceIdentifier] nvarchar(200) NOT NULL,
    [FirstSeenAt] datetimeoffset NOT NULL,
    [LastSeenAt] datetimeoffset NOT NULL,
    [RevokedAt] datetimeoffset NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [UpdatedAt] datetimeoffset NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_UserDevices] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_UserDevices_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AuthenticationSessions] (
    [Id] bigint NOT NULL IDENTITY,
    [UserId] bigint NOT NULL,
    [UserDeviceId] bigint NOT NULL,
    [SessionKey] nvarchar(200) NOT NULL,
    [LastSeenAt] datetimeoffset NOT NULL,
    [RevokedAt] datetimeoffset NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [UpdatedAt] datetimeoffset NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_AuthenticationSessions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AuthenticationSessions_UserDevices_UserDeviceId] FOREIGN KEY ([UserDeviceId]) REFERENCES [UserDevices] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_AuthenticationSessions_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [VideoWatchSessions] (
    [Id] bigint NOT NULL IDENTITY,
    [UserId] bigint NOT NULL,
    [VideoId] bigint NOT NULL,
    [UserDeviceId] bigint NOT NULL,
    [AuthenticationSessionId] bigint NULL,
    [StartedAt] datetimeoffset NOT NULL,
    [LastActivityAt] datetimeoffset NOT NULL,
    [EndedAt] datetimeoffset NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [UpdatedAt] datetimeoffset NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_VideoWatchSessions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_VideoWatchSessions_AuthenticationSessions_AuthenticationSessionId] FOREIGN KEY ([AuthenticationSessionId]) REFERENCES [AuthenticationSessions] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_VideoWatchSessions_UserDevices_UserDeviceId] FOREIGN KEY ([UserDeviceId]) REFERENCES [UserDevices] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_VideoWatchSessions_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_VideoWatchSessions_Videos_VideoId] FOREIGN KEY ([VideoId]) REFERENCES [Videos] ([Id]) ON DELETE CASCADE
);

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAt', N'IsActive', N'PasswordHash', N'Role', N'UpdatedAt', N'Username') AND [object_id] = OBJECT_ID(N'[Users]'))
    SET IDENTITY_INSERT [Users] ON;
INSERT INTO [Users] ([Id], [CreatedAt], [IsActive], [PasswordHash], [Role], [UpdatedAt], [Username])
VALUES (CAST(1 AS bigint), '2026-01-01T00:00:00.0000000+00:00', CAST(1 AS bit), N'obLD1OX2BxgpOktcbX6PkA==.6/kqMtBYfE1zdN0uyJkQqVPW+JELO19hgW1GZi3TFMk=', 1, '2026-01-01T00:00:00.0000000+00:00', N'a.tooshmalani');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAt', N'IsActive', N'PasswordHash', N'Role', N'UpdatedAt', N'Username') AND [object_id] = OBJECT_ID(N'[Users]'))
    SET IDENTITY_INSERT [Users] OFF;

CREATE INDEX [IX_AuditLogs_UserId] ON [AuditLogs] ([UserId]);

CREATE INDEX [IX_AuthenticationSessions_UserDeviceId] ON [AuthenticationSessions] ([UserDeviceId]);

CREATE UNIQUE INDEX [IX_AuthenticationSessions_UserId_SessionKey] ON [AuthenticationSessions] ([UserId], [SessionKey]);

CREATE INDEX [IX_Folders_ParentFolderId] ON [Folders] ([ParentFolderId]);

CREATE INDEX [IX_Permissions_CreatedByUserId] ON [Permissions] ([CreatedByUserId]);

CREATE INDEX [IX_Permissions_FolderId] ON [Permissions] ([FolderId]);

CREATE INDEX [IX_Permissions_UserId] ON [Permissions] ([UserId]);

CREATE UNIQUE INDEX [IX_Permissions_UserId_FolderId] ON [Permissions] ([UserId], [FolderId]);

CREATE INDEX [IX_UserDevices_UserId] ON [UserDevices] ([UserId]);

CREATE UNIQUE INDEX [IX_UserDevices_UserId_DeviceIdentifier] ON [UserDevices] ([UserId], [DeviceIdentifier]);

CREATE UNIQUE INDEX [IX_Users_Username] ON [Users] ([Username]);

CREATE INDEX [IX_Videos_FolderId] ON [Videos] ([FolderId]);

CREATE INDEX [IX_VideoWatchSessions_AuthenticationSessionId] ON [VideoWatchSessions] ([AuthenticationSessionId]);

CREATE INDEX [IX_VideoWatchSessions_UserDeviceId] ON [VideoWatchSessions] ([UserDeviceId]);

CREATE INDEX [IX_VideoWatchSessions_UserId] ON [VideoWatchSessions] ([UserId]);

CREATE INDEX [IX_VideoWatchSessions_VideoId] ON [VideoWatchSessions] ([VideoId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260901100434_InitialCreate', N'9.0.0');

COMMIT;
GO

