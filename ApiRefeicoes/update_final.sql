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

-- Tabelas Auxiliares
CREATE TABLE [Departamentos] (
    [Id] int NOT NULL IDENTITY,
    [Nome] nvarchar(100) NOT NULL,
    [DepartamentoGenerico] nvarchar(100) NULL,
    CONSTRAINT [PK_Departamentos] PRIMARY KEY ([Id])
);

CREATE TABLE [Funcoes] (
    [Id] int NOT NULL IDENTITY,
    [Nome] nvarchar(100) NOT NULL,
    [CONSTRAINT] [PK_Funcoes] PRIMARY KEY ([Id])
);

CREATE TABLE [Usuarios] (
    [Id] int NOT NULL IDENTITY,
    [Username] nvarchar(max) NOT NULL,
    [PasswordHash] nvarchar(max) NOT NULL,
    [Role] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Usuarios] PRIMARY KEY ([Id])
);

-- Tabela Colaboradores (SEM BIOMETRIA)
CREATE TABLE [Colaboradores] (
    [Id] int NOT NULL IDENTITY,
    [Nome] nvarchar(100) NOT NULL,
    [CartaoPonto] nvarchar(20) NOT NULL,
    [Foto] varbinary(max) NULL,
    [FuncaoId] int NOT NULL,
    [DepartamentoId] int NOT NULL,
    [Ativo] bit NOT NULL,
    [PersonId] uniqueidentifier NULL,
    [AcessoCafeDaManha] bit NOT NULL,
    [AcessoAlmoco] bit NOT NULL,
    [AcessoJanta] bit NOT NULL,
    [AcessoCeia] bit NOT NULL,
    -- [Biometria] Removido conforme solicitado
    [DeviceIdentifier] nvarchar(max) NULL,
    CONSTRAINT [PK_Colaboradores] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Colaboradores_Departamentos_DepartamentoId] FOREIGN KEY ([DepartamentoId]) REFERENCES [Departamentos] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Colaboradores_Funcoes_FuncaoId] FOREIGN KEY ([FuncaoId]) REFERENCES [Funcoes] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Dispositivos] (
    [Id] int NOT NULL IDENTITY,
    [DeviceIdentifier] nvarchar(max) NOT NULL,
    [Nome] nvarchar(max) NOT NULL,
    [UltimoLogin] datetime2 NOT NULL,
    [IsAtivo] bit NOT NULL,
    [UsuarioId] int NOT NULL,
    CONSTRAINT [PK_Dispositivos] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Dispositivos_Usuarios_UsuarioId] FOREIGN KEY ([UsuarioId]) REFERENCES [Usuarios] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [ParadasDeFabrica] (
    [Id] int NOT NULL IDENTITY,
    [Parada] bit NULL,
    [DataParada] datetime2 NULL,
    [UsuarioId] int NULL,
    CONSTRAINT [PK_ParadasDeFabrica] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ParadasDeFabrica_Usuarios_UsuarioId] FOREIGN KEY ([UsuarioId]) REFERENCES [Usuarios] ([Id])
);

-- Tabela RegistroRefeicoes (COM CAMPO RefeicaoExcedente)
CREATE TABLE [RegistroRefeicoes] (
    [Id] int NOT NULL IDENTITY,
    [ColaboradorId] int NOT NULL,
    [DataHoraRegistro] datetime2 NOT NULL,
    [TipoRefeicao] nvarchar(50) NOT NULL,
    [NomeColaborador] nvarchar(100) NOT NULL,
    [NomeDepartamento] nvarchar(100) NOT NULL,
    [DepartamentoGenerico] nvarchar(100) NULL,
    [NomeFuncao] nvarchar(100) NOT NULL,
    [ValorRefeicao] decimal(18,2) NOT NULL,
    [ParadaDeFabrica] bit NOT NULL,
    [RefeicaoExcedente] bit NOT NULL DEFAULT 0, -- Campo Novo
    CONSTRAINT [PK_RegistroRefeicoes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RegistroRefeicoes_Colaboradores_ColaboradorId] FOREIGN KEY ([ColaboradorId]) REFERENCES [Colaboradores] ([Id]) ON DELETE CASCADE
);

-- Índices
CREATE INDEX [IX_Colaboradores_DepartamentoId] ON [Colaboradores] ([DepartamentoId]);
CREATE INDEX [IX_Colaboradores_FuncaoId] ON [Colaboradores] ([FuncaoId]);
CREATE INDEX [IX_Dispositivos_UsuarioId] ON [Dispositivos] ([UsuarioId]);
CREATE INDEX [IX_ParadasDeFabrica_UsuarioId] ON [ParadasDeFabrica] ([UsuarioId]);
CREATE INDEX [IX_RegistroRefeicoes_ColaboradorId] ON [RegistroRefeicoes] ([ColaboradorId]);

COMMIT;
GO