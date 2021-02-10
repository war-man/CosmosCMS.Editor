CREATE TABLE [dbo].[Layouts] (
    [Id]                       INT            IDENTITY (1, 1) NOT NULL,
    [LayoutName]               NVARCHAR (128) NULL,
    [Notes]                    NVARCHAR (MAX) NULL,
    [Head]                     NVARCHAR (MAX) NULL,
    [BodyHtmlAttributes]       NVARCHAR (256) NULL,
    [BodyHeaderHtmlAttributes] NVARCHAR (256) NULL,
    [HtmlHeader]               NVARCHAR (MAX) NULL,
    [FooterHtmlAttributes]     NVARCHAR (256) NULL,
    [FooterHtmlContent]        NVARCHAR (MAX) NULL,
    [PostFooterBlock]          NVARCHAR (MAX) NULL,
    [IsDefault]                BIT            DEFAULT (CONVERT([bit],(0))) NOT NULL,
    CONSTRAINT [PK_Layouts] PRIMARY KEY CLUSTERED ([Id] ASC)
);

