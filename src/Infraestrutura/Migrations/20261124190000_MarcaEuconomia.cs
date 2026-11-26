using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Infraestrutura.Migrations;

[DbContext(typeof(FinanceiroDb))]
[Migration("20261124190000_MarcaEuconomia")]
public sealed class MarcaEuconomia : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            INSERT INTO "AspNetUserTokens" ("UserId", "LoginProvider", "Name", "Value")
            SELECT "UserId", 'Euconomia', "Name", "Value"
            FROM "AspNetUserTokens"
            WHERE "LoginProvider" = 'Prumo' AND "Name" = 'RecuperacaoSenha'
            ON CONFLICT ("UserId", "LoginProvider", "Name") DO NOTHING;

            DELETE FROM "AspNetUserTokens"
            WHERE "LoginProvider" = 'Prumo' AND "Name" = 'RecuperacaoSenha';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            INSERT INTO "AspNetUserTokens" ("UserId", "LoginProvider", "Name", "Value")
            SELECT "UserId", 'Prumo', "Name", "Value"
            FROM "AspNetUserTokens"
            WHERE "LoginProvider" = 'Euconomia' AND "Name" = 'RecuperacaoSenha'
            ON CONFLICT ("UserId", "LoginProvider", "Name") DO NOTHING;

            DELETE FROM "AspNetUserTokens"
            WHERE "LoginProvider" = 'Euconomia' AND "Name" = 'RecuperacaoSenha';
            """);
    }
}
