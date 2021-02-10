CREATE TABLE [dbo].[ArticleLogs] (
    [Id]             INT            IDENTITY (1, 1) NOT NULL,
    [ArticleId]      INT            NOT NULL,
    [IdentityUserId] NVARCHAR (450) NULL,
    [ActivityNotes]  NVARCHAR (MAX) NULL,
    [DateTimeStamp]  DATETIME2 (7)  NOT NULL,
    CONSTRAINT [PK_ArticleLogs] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_ArticleLogs_Articles_ArticleId] FOREIGN KEY ([ArticleId]) REFERENCES [dbo].[Articles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ArticleLogs_AspNetUsers_IdentityUserId] FOREIGN KEY ([IdentityUserId]) REFERENCES [dbo].[AspNetUsers] ([Id])
);


GO
CREATE NONCLUSTERED INDEX [IX_ArticleLogs_IdentityUserId]
    ON [dbo].[ArticleLogs]([IdentityUserId] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_ArticleLogs_ArticleId]
    ON [dbo].[ArticleLogs]([ArticleId] ASC);

