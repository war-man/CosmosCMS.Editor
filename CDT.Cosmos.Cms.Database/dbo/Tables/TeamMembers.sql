CREATE TABLE [dbo].[TeamMembers] (
    [Id]       INT            IDENTITY (1, 1) NOT NULL,
    [TeamRole] INT            NOT NULL,
    [TeamId]   INT            NOT NULL,
    [UserId]   NVARCHAR (450) NULL,
    CONSTRAINT [PK_TeamMembers] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_TeamMembers_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers] ([Id]),
    CONSTRAINT [FK_TeamMembers_Teams_TeamId] FOREIGN KEY ([TeamId]) REFERENCES [dbo].[Teams] ([Id]) ON DELETE CASCADE
);


GO
CREATE NONCLUSTERED INDEX [IX_TeamMembers_UserId]
    ON [dbo].[TeamMembers]([UserId] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_TeamMembers_TeamId]
    ON [dbo].[TeamMembers]([TeamId] ASC);

