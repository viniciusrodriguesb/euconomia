using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infraestrutura.Migrations
{
    /// <inheritdoc />
    public partial class ModeloInicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "usuario",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                    SecurityStamp = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                    PhoneNumber = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuario", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimType = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                    ClaimValue = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "alerta",
                columns: table => new
                {
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<int>(type: "integer", nullable: false),
                    gravidade = table.Column<int>(type: "integer", nullable: false),
                    competencia = table.Column<DateOnly>(type: "date", nullable: false),
                    mensagem = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    referencia = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    lido_em = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_alerta", x => new { x.usuario_id, x.id });
                    table.CheckConstraint("ck_alerta_tipo_gravidade_competencia", "tipo BETWEEN 1 AND 8 AND gravidade BETWEEN 1 AND 2 AND extract(day from competencia) = 1");
                    table.ForeignKey(
                        name: "FK_alerta_usuario_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimType = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                    ClaimValue = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_usuario_UserId",
                        column: x => x.UserId,
                        principalTable: "usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    ProviderKey = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_usuario_UserId",
                        column: x => x.UserId,
                        principalTable: "usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_usuario_UserId",
                        column: x => x.UserId,
                        principalTable: "usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LoginProvider = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    Name = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    Value = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_usuario_UserId",
                        column: x => x.UserId,
                        principalTable: "usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "auditoria",
                columns: table => new
                {
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    entidade_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entidade = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    acao = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    antes = table.Column<string>(type: "jsonb", maxLength: 240, nullable: true),
                    depois = table.Column<string>(type: "jsonb", maxLength: 240, nullable: true),
                    criado_em = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_auditoria", x => new { x.usuario_id, x.id });
                    table.CheckConstraint("ck_auditoria_conteudo", "antes IS NOT NULL OR depois IS NOT NULL");
                    table.ForeignKey(
                        name: "FK_auditoria_usuario_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "categoria",
                columns: table => new
                {
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    inativado_em = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categoria", x => new { x.usuario_id, x.id });
                    table.ForeignKey(
                        name: "FK_categoria_usuario_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "configuracao_usuario",
                columns: table => new
                {
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    percentual_gasto_amarelo = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    percentual_gasto_vermelho = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    percentual_custos_fixos = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    percentual_comprometimento = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    percentual_limite_cartao = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    limite_transacoes_estabelecimento = table.Column<int>(type: "integer", nullable: false),
                    multiplicador_despesa_atipica = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    confirmar_ao_virar_mes = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_configuracao_usuario", x => x.usuario_id);
                    table.CheckConstraint("ck_configuracao_usuario_limites", "percentual_gasto_amarelo > 0 AND percentual_gasto_vermelho >= percentual_gasto_amarelo AND percentual_custos_fixos > 0 AND percentual_comprometimento > 0 AND percentual_limite_cartao > 0 AND limite_transacoes_estabelecimento > 0 AND multiplicador_despesa_atipica > 0");
                    table.ForeignKey(
                        name: "FK_configuracao_usuario_usuario_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "conta",
                columns: table => new
                {
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    saldo_inicial = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    data_saldo_inicial = table.Column<DateOnly>(type: "date", nullable: false),
                    inativado_em = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_conta", x => new { x.usuario_id, x.id });
                    table.ForeignKey(
                        name: "FK_conta_usuario_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "estabelecimento",
                columns: table => new
                {
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    nome_normalizado = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    limite_transacoes = table.Column<int>(type: "integer", nullable: true),
                    mesclado_em_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_estabelecimento", x => new { x.usuario_id, x.id });
                    table.CheckConstraint("ck_estabelecimento_limite", "limite_transacoes > 0");
                    table.ForeignKey(
                        name: "FK_estabelecimento_estabelecimento_usuario_id_mesclado_em_id",
                        columns: x => new { x.usuario_id, x.mesclado_em_id },
                        principalTable: "estabelecimento",
                        principalColumns: new[] { "usuario_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_estabelecimento_usuario_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "registro_idempotencia",
                columns: table => new
                {
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    chave_idempotencia = table.Column<Guid>(type: "uuid", nullable: false),
                    impressao_digital = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    resposta = table.Column<string>(type: "jsonb", maxLength: 240, nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_registro_idempotencia", x => new { x.usuario_id, x.id });
                    table.ForeignKey(
                        name: "FK_registro_idempotencia_usuario_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cartao",
                columns: table => new
                {
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    bandeira = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    limite = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    dia_fechamento = table.Column<int>(type: "integer", nullable: false),
                    dia_vencimento = table.Column<int>(type: "integer", nullable: false),
                    conta_pagamento_id = table.Column<Guid>(type: "uuid", nullable: false),
                    inativado_em = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cartao", x => new { x.usuario_id, x.id });
                    table.CheckConstraint("ck_cartao_limite_dias", "limite > 0 AND dia_fechamento BETWEEN 1 AND 31 AND dia_vencimento BETWEEN 1 AND 31");
                    table.ForeignKey(
                        name: "FK_cartao_conta_usuario_id_conta_pagamento_id",
                        columns: x => new { x.usuario_id, x.conta_pagamento_id },
                        principalTable: "conta",
                        principalColumns: new[] { "usuario_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_cartao_usuario_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "alias_estabelecimento",
                columns: table => new
                {
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    estabelecimento_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome_normalizado = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_alias_estabelecimento", x => new { x.usuario_id, x.id });
                    table.ForeignKey(
                        name: "FK_alias_estabelecimento_estabelecimento_usuario_id_estabeleci~",
                        columns: x => new { x.usuario_id, x.estabelecimento_id },
                        principalTable: "estabelecimento",
                        principalColumns: new[] { "usuario_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_alias_estabelecimento_usuario_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "compra_parcelada",
                columns: table => new
                {
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    cartao_id = table.Column<Guid>(type: "uuid", nullable: false),
                    valor_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_parcelas = table.Column<int>(type: "integer", nullable: false),
                    data_compra = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_compra_parcelada", x => new { x.usuario_id, x.id });
                    table.CheckConstraint("ck_compra_parcelada_valor_parcelas", "total_parcelas >= 2 AND valor_total >= total_parcelas * 0.01");
                    table.ForeignKey(
                        name: "FK_compra_parcelada_cartao_usuario_id_cartao_id",
                        columns: x => new { x.usuario_id, x.cartao_id },
                        principalTable: "cartao",
                        principalColumns: new[] { "usuario_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_compra_parcelada_usuario_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "regra_recorrente",
                columns: table => new
                {
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    regra_anterior_id = table.Column<Guid>(type: "uuid", nullable: true),
                    descricao = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    tipo = table.Column<int>(type: "integer", nullable: false),
                    valor_base = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    dia_do_mes = table.Column<int>(type: "integer", nullable: false),
                    competencia_inicio = table.Column<DateOnly>(type: "date", nullable: false),
                    competencia_fim = table.Column<DateOnly>(type: "date", nullable: true),
                    forma_pagamento = table.Column<int>(type: "integer", nullable: false),
                    conta_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cartao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    categoria_id = table.Column<Guid>(type: "uuid", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_regra_recorrente", x => new { x.usuario_id, x.id });
                    table.CheckConstraint("ck_regra_recorrente_periodo", "extract(day from competencia_inicio) = 1 AND (competencia_fim IS NULL OR (extract(day from competencia_fim) = 1 AND competencia_fim >= competencia_inicio))");
                    table.CheckConstraint("ck_regra_recorrente_tipo_forma", "tipo BETWEEN 1 AND 4 AND forma_pagamento BETWEEN 1 AND 6 AND (tipo = 4) = (forma_pagamento = 6) AND (forma_pagamento = 6) = (cartao_id IS NOT NULL)");
                    table.CheckConstraint("ck_regra_recorrente_valor_dia", "valor_base > 0 AND dia_do_mes BETWEEN 1 AND 31");
                    table.ForeignKey(
                        name: "FK_regra_recorrente_cartao_usuario_id_cartao_id",
                        columns: x => new { x.usuario_id, x.cartao_id },
                        principalTable: "cartao",
                        principalColumns: new[] { "usuario_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_regra_recorrente_categoria_usuario_id_categoria_id",
                        columns: x => new { x.usuario_id, x.categoria_id },
                        principalTable: "categoria",
                        principalColumns: new[] { "usuario_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_regra_recorrente_conta_usuario_id_conta_id",
                        columns: x => new { x.usuario_id, x.conta_id },
                        principalTable: "conta",
                        principalColumns: new[] { "usuario_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_regra_recorrente_regra_recorrente_usuario_id_regra_anterior~",
                        columns: x => new { x.usuario_id, x.regra_anterior_id },
                        principalTable: "regra_recorrente",
                        principalColumns: new[] { "usuario_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_regra_recorrente_usuario_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "excecao_recorrencia",
                columns: table => new
                {
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    regra_recorrente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    competencia = table.Column<DateOnly>(type: "date", nullable: false),
                    valor_override = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    ignorada = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_excecao_recorrencia", x => new { x.usuario_id, x.id });
                    table.CheckConstraint("ck_excecao_recorrencia_valor", "extract(day from competencia) = 1 AND ((ignorada AND valor_override IS NULL) OR (NOT ignorada AND valor_override IS NOT NULL AND valor_override > 0))");
                    table.ForeignKey(
                        name: "FK_excecao_recorrencia_regra_recorrente_usuario_id_regra_recor~",
                        columns: x => new { x.usuario_id, x.regra_recorrente_id },
                        principalTable: "regra_recorrente",
                        principalColumns: new[] { "usuario_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_excecao_recorrencia_usuario_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fatura",
                columns: table => new
                {
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    cartao_id = table.Column<Guid>(type: "uuid", nullable: false),
                    competencia = table.Column<DateOnly>(type: "date", nullable: false),
                    data_fechamento = table.Column<DateOnly>(type: "date", nullable: false),
                    data_vencimento = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    lancamento_pagamento_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fatura", x => new { x.usuario_id, x.id });
                    table.UniqueConstraint("AK_fatura_usuario_id_id_cartao_id", x => new { x.usuario_id, x.id, x.cartao_id });
                    table.CheckConstraint("ck_fatura_datas", "extract(day from competencia) = 1 AND date_trunc('month', data_fechamento)::date = competencia AND data_vencimento > data_fechamento");
                    table.CheckConstraint("ck_fatura_status", "status BETWEEN 1 AND 3 AND (status = 3) = (lancamento_pagamento_id IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_fatura_cartao_usuario_id_cartao_id",
                        columns: x => new { x.usuario_id, x.cartao_id },
                        principalTable: "cartao",
                        principalColumns: new[] { "usuario_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_fatura_usuario_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "lancamento",
                columns: table => new
                {
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<int>(type: "integer", nullable: false),
                    forma_pagamento = table.Column<int>(type: "integer", nullable: false),
                    valor = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    moeda = table.Column<string>(type: "char(3)", maxLength: 3, nullable: false),
                    data_lancamento = table.Column<DateOnly>(type: "date", nullable: false),
                    competencia = table.Column<DateOnly>(type: "date", nullable: false),
                    conta_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cartao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fatura_id = table.Column<Guid>(type: "uuid", nullable: true),
                    categoria_id = table.Column<Guid>(type: "uuid", nullable: true),
                    estabelecimento_id = table.Column<Guid>(type: "uuid", nullable: true),
                    descricao = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    compra_parcelada_id = table.Column<Guid>(type: "uuid", nullable: true),
                    numero_parcela = table.Column<int>(type: "integer", nullable: true),
                    total_parcelas = table.Column<int>(type: "integer", nullable: true),
                    regra_recorrente_id = table.Column<Guid>(type: "uuid", nullable: true),
                    lancamento_estornado_id = table.Column<Guid>(type: "uuid", nullable: true),
                    chave_idempotencia = table.Column<Guid>(type: "uuid", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lancamento", x => new { x.usuario_id, x.id });
                    table.CheckConstraint("ck_lancamento_categoria", "tipo NOT IN (1, 2, 3, 4) OR categoria_id IS NOT NULL");
                    table.CheckConstraint("ck_lancamento_estorno", "(tipo = 7) = (lancamento_estornado_id IS NOT NULL) AND lancamento_estornado_id IS DISTINCT FROM id");
                    table.CheckConstraint("ck_lancamento_parcelamento", "(compra_parcelada_id IS NULL AND numero_parcela IS NULL AND total_parcelas IS NULL) OR (tipo = 2 AND forma_pagamento = 6 AND compra_parcelada_id IS NOT NULL AND numero_parcela IS NOT NULL AND total_parcelas IS NOT NULL AND total_parcelas >= 2 AND numero_parcela BETWEEN 1 AND total_parcelas)");
                    table.CheckConstraint("ck_lancamento_tipo_forma", "tipo BETWEEN 1 AND 7 AND forma_pagamento BETWEEN 1 AND 6 AND ((forma_pagamento = 6 AND tipo IN (2, 7) AND cartao_id IS NOT NULL AND fatura_id IS NOT NULL) OR (forma_pagamento <> 6 AND ((tipo IN (6, 7) AND cartao_id IS NOT NULL AND fatura_id IS NOT NULL) OR (tipo <> 6 AND cartao_id IS NULL AND fatura_id IS NULL))))");
                    table.CheckConstraint("ck_lancamento_valor_competencia", "valor > 0 AND moeda ~ '^[A-Z]{3}$' AND extract(day from competencia) = 1");
                    table.ForeignKey(
                        name: "FK_lancamento_cartao_usuario_id_cartao_id",
                        columns: x => new { x.usuario_id, x.cartao_id },
                        principalTable: "cartao",
                        principalColumns: new[] { "usuario_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_lancamento_categoria_usuario_id_categoria_id",
                        columns: x => new { x.usuario_id, x.categoria_id },
                        principalTable: "categoria",
                        principalColumns: new[] { "usuario_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_lancamento_compra_parcelada_usuario_id_compra_parcelada_id",
                        columns: x => new { x.usuario_id, x.compra_parcelada_id },
                        principalTable: "compra_parcelada",
                        principalColumns: new[] { "usuario_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_lancamento_conta_usuario_id_conta_id",
                        columns: x => new { x.usuario_id, x.conta_id },
                        principalTable: "conta",
                        principalColumns: new[] { "usuario_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_lancamento_estabelecimento_usuario_id_estabelecimento_id",
                        columns: x => new { x.usuario_id, x.estabelecimento_id },
                        principalTable: "estabelecimento",
                        principalColumns: new[] { "usuario_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_lancamento_fatura_usuario_id_fatura_id",
                        columns: x => new { x.usuario_id, x.fatura_id },
                        principalTable: "fatura",
                        principalColumns: new[] { "usuario_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_lancamento_fatura_usuario_id_fatura_id_cartao_id",
                        columns: x => new { x.usuario_id, x.fatura_id, x.cartao_id },
                        principalTable: "fatura",
                        principalColumns: new[] { "usuario_id", "id", "cartao_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_lancamento_lancamento_usuario_id_lancamento_estornado_id",
                        columns: x => new { x.usuario_id, x.lancamento_estornado_id },
                        principalTable: "lancamento",
                        principalColumns: new[] { "usuario_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_lancamento_regra_recorrente_usuario_id_regra_recorrente_id",
                        columns: x => new { x.usuario_id, x.regra_recorrente_id },
                        principalTable: "regra_recorrente",
                        principalColumns: new[] { "usuario_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_lancamento_usuario_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_alerta_usuario_id_tipo_competencia_referencia",
                table: "alerta",
                columns: new[] { "usuario_id", "tipo", "competencia", "referencia" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_alias_estabelecimento_usuario_id_estabelecimento_id",
                table: "alias_estabelecimento",
                columns: new[] { "usuario_id", "estabelecimento_id" });

            migrationBuilder.CreateIndex(
                name: "IX_alias_estabelecimento_usuario_id_nome_normalizado",
                table: "alias_estabelecimento",
                columns: new[] { "usuario_id", "nome_normalizado" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_auditoria_usuario_id_entidade_id_criado_em",
                table: "auditoria",
                columns: new[] { "usuario_id", "entidade_id", "criado_em" });

            migrationBuilder.CreateIndex(
                name: "IX_cartao_usuario_id_conta_pagamento_id",
                table: "cartao",
                columns: new[] { "usuario_id", "conta_pagamento_id" });

            migrationBuilder.CreateIndex(
                name: "IX_categoria_usuario_id_nome",
                table: "categoria",
                columns: new[] { "usuario_id", "nome" },
                unique: true,
                filter: "inativado_em IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_compra_parcelada_usuario_id_cartao_id",
                table: "compra_parcelada",
                columns: new[] { "usuario_id", "cartao_id" });

            migrationBuilder.CreateIndex(
                name: "IX_conta_usuario_id_nome",
                table: "conta",
                columns: new[] { "usuario_id", "nome" },
                unique: true,
                filter: "inativado_em IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_estabelecimento_usuario_id_mesclado_em_id",
                table: "estabelecimento",
                columns: new[] { "usuario_id", "mesclado_em_id" });

            migrationBuilder.CreateIndex(
                name: "IX_estabelecimento_usuario_id_nome_normalizado",
                table: "estabelecimento",
                columns: new[] { "usuario_id", "nome_normalizado" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_excecao_recorrencia_usuario_id_regra_recorrente_id_competen~",
                table: "excecao_recorrencia",
                columns: new[] { "usuario_id", "regra_recorrente_id", "competencia" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fatura_usuario_id_cartao_id_competencia",
                table: "fatura",
                columns: new[] { "usuario_id", "cartao_id", "competencia" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fatura_usuario_id_competencia",
                table: "fatura",
                columns: new[] { "usuario_id", "competencia" });

            migrationBuilder.CreateIndex(
                name: "IX_fatura_usuario_id_lancamento_pagamento_id",
                table: "fatura",
                columns: new[] { "usuario_id", "lancamento_pagamento_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_lancamento_usuario_id_cartao_id",
                table: "lancamento",
                columns: new[] { "usuario_id", "cartao_id" });

            migrationBuilder.CreateIndex(
                name: "IX_lancamento_usuario_id_categoria_id_competencia",
                table: "lancamento",
                columns: new[] { "usuario_id", "categoria_id", "competencia" });

            migrationBuilder.CreateIndex(
                name: "IX_lancamento_usuario_id_chave_idempotencia",
                table: "lancamento",
                columns: new[] { "usuario_id", "chave_idempotencia" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_lancamento_usuario_id_competencia_id",
                table: "lancamento",
                columns: new[] { "usuario_id", "competencia", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_lancamento_usuario_id_compra_parcelada_id_numero_parcela",
                table: "lancamento",
                columns: new[] { "usuario_id", "compra_parcelada_id", "numero_parcela" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_lancamento_usuario_id_conta_id_data_lancamento",
                table: "lancamento",
                columns: new[] { "usuario_id", "conta_id", "data_lancamento" });

            migrationBuilder.CreateIndex(
                name: "IX_lancamento_usuario_id_data_lancamento_id",
                table: "lancamento",
                columns: new[] { "usuario_id", "data_lancamento", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_lancamento_usuario_id_estabelecimento_id_competencia",
                table: "lancamento",
                columns: new[] { "usuario_id", "estabelecimento_id", "competencia" });

            migrationBuilder.CreateIndex(
                name: "IX_lancamento_usuario_id_fatura_id_cartao_id",
                table: "lancamento",
                columns: new[] { "usuario_id", "fatura_id", "cartao_id" });

            migrationBuilder.CreateIndex(
                name: "IX_lancamento_usuario_id_lancamento_estornado_id",
                table: "lancamento",
                columns: new[] { "usuario_id", "lancamento_estornado_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_lancamento_usuario_id_regra_recorrente_id_competencia",
                table: "lancamento",
                columns: new[] { "usuario_id", "regra_recorrente_id", "competencia" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_registro_idempotencia_usuario_id_chave_idempotencia",
                table: "registro_idempotencia",
                columns: new[] { "usuario_id", "chave_idempotencia" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_regra_recorrente_usuario_id_cartao_id",
                table: "regra_recorrente",
                columns: new[] { "usuario_id", "cartao_id" });

            migrationBuilder.CreateIndex(
                name: "IX_regra_recorrente_usuario_id_categoria_id",
                table: "regra_recorrente",
                columns: new[] { "usuario_id", "categoria_id" });

            migrationBuilder.CreateIndex(
                name: "IX_regra_recorrente_usuario_id_competencia_inicio",
                table: "regra_recorrente",
                columns: new[] { "usuario_id", "competencia_inicio" });

            migrationBuilder.CreateIndex(
                name: "IX_regra_recorrente_usuario_id_conta_id",
                table: "regra_recorrente",
                columns: new[] { "usuario_id", "conta_id" });

            migrationBuilder.CreateIndex(
                name: "IX_regra_recorrente_usuario_id_regra_anterior_id",
                table: "regra_recorrente",
                columns: new[] { "usuario_id", "regra_anterior_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "usuario",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "usuario",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_fatura_lancamento_usuario_id_lancamento_pagamento_id",
                table: "fatura",
                columns: new[] { "usuario_id", "lancamento_pagamento_id" },
                principalTable: "lancamento",
                principalColumns: new[] { "usuario_id", "id" },
                onDelete: ReferentialAction.Restrict);
            migrationBuilder.Sql("""
                CREATE FUNCTION rejeitar_mutacao_historico() RETURNS trigger
                LANGUAGE plpgsql AS $$
                BEGIN
                    RAISE EXCEPTION 'O histórico de % é imutável.', TG_TABLE_NAME USING ERRCODE = '55000';
                END;
                $$;

                CREATE TRIGGER lancamento_imutavel
                BEFORE UPDATE OR DELETE OR TRUNCATE ON lancamento
                FOR EACH STATEMENT EXECUTE FUNCTION rejeitar_mutacao_historico();

                CREATE TRIGGER auditoria_imutavel
                BEFORE UPDATE OR DELETE OR TRUNCATE ON auditoria
                FOR EACH STATEMENT EXECUTE FUNCTION rejeitar_mutacao_historico();

                CREATE VIEW total_fatura AS
                SELECT f.usuario_id, f.id AS fatura_id,
                       COALESCE(SUM(CASE l.tipo WHEN 2 THEN l.valor WHEN 7 THEN -l.valor END), 0) AS total
                FROM fatura f
                LEFT JOIN lancamento l ON l.usuario_id = f.usuario_id AND l.fatura_id = f.id AND l.forma_pagamento = 6
                GROUP BY f.usuario_id, f.id;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP VIEW total_fatura; DROP TRIGGER lancamento_imutavel ON lancamento; DROP TRIGGER auditoria_imutavel ON auditoria; DROP FUNCTION rejeitar_mutacao_historico();");
            migrationBuilder.DropForeignKey(
                name: "FK_cartao_usuario_usuario_id",
                table: "cartao");

            migrationBuilder.DropForeignKey(
                name: "FK_categoria_usuario_usuario_id",
                table: "categoria");

            migrationBuilder.DropForeignKey(
                name: "FK_compra_parcelada_usuario_usuario_id",
                table: "compra_parcelada");

            migrationBuilder.DropForeignKey(
                name: "FK_conta_usuario_usuario_id",
                table: "conta");

            migrationBuilder.DropForeignKey(
                name: "FK_estabelecimento_usuario_usuario_id",
                table: "estabelecimento");

            migrationBuilder.DropForeignKey(
                name: "FK_fatura_usuario_usuario_id",
                table: "fatura");

            migrationBuilder.DropForeignKey(
                name: "FK_lancamento_usuario_usuario_id",
                table: "lancamento");

            migrationBuilder.DropForeignKey(
                name: "FK_regra_recorrente_usuario_usuario_id",
                table: "regra_recorrente");

            migrationBuilder.DropForeignKey(
                name: "FK_lancamento_estabelecimento_usuario_id_estabelecimento_id",
                table: "lancamento");

            migrationBuilder.DropForeignKey(
                name: "FK_cartao_conta_usuario_id_conta_pagamento_id",
                table: "cartao");

            migrationBuilder.DropForeignKey(
                name: "FK_lancamento_conta_usuario_id_conta_id",
                table: "lancamento");

            migrationBuilder.DropForeignKey(
                name: "FK_regra_recorrente_conta_usuario_id_conta_id",
                table: "regra_recorrente");

            migrationBuilder.DropForeignKey(
                name: "FK_compra_parcelada_cartao_usuario_id_cartao_id",
                table: "compra_parcelada");

            migrationBuilder.DropForeignKey(
                name: "FK_fatura_cartao_usuario_id_cartao_id",
                table: "fatura");

            migrationBuilder.DropForeignKey(
                name: "FK_lancamento_cartao_usuario_id_cartao_id",
                table: "lancamento");

            migrationBuilder.DropForeignKey(
                name: "FK_regra_recorrente_cartao_usuario_id_cartao_id",
                table: "regra_recorrente");

            migrationBuilder.DropForeignKey(
                name: "FK_lancamento_regra_recorrente_usuario_id_regra_recorrente_id",
                table: "lancamento");

            migrationBuilder.DropForeignKey(
                name: "FK_fatura_lancamento_usuario_id_lancamento_pagamento_id",
                table: "fatura");

            migrationBuilder.DropTable(
                name: "alerta");

            migrationBuilder.DropTable(
                name: "alias_estabelecimento");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "auditoria");

            migrationBuilder.DropTable(
                name: "configuracao_usuario");

            migrationBuilder.DropTable(
                name: "excecao_recorrencia");

            migrationBuilder.DropTable(
                name: "registro_idempotencia");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "usuario");

            migrationBuilder.DropTable(
                name: "estabelecimento");

            migrationBuilder.DropTable(
                name: "conta");

            migrationBuilder.DropTable(
                name: "cartao");

            migrationBuilder.DropTable(
                name: "regra_recorrente");

            migrationBuilder.DropTable(
                name: "lancamento");

            migrationBuilder.DropTable(
                name: "categoria");

            migrationBuilder.DropTable(
                name: "compra_parcelada");

            migrationBuilder.DropTable(
                name: "fatura");
        }
    }
}
