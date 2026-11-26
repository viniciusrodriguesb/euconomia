using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infraestrutura.Migrations
{
    /// <inheritdoc />
    public partial class PreferenciasERecorrencias : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_regra_recorrente_periodo",
                table: "regra_recorrente");

            migrationBuilder.AddColumn<int>(
                name: "alertas_desativados",
                table: "configuracao_usuario",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateOnly>(
                name: "confirmacao_automatica_desde",
                table: "configuracao_usuario",
                type: "date",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_regra_recorrente_periodo",
                table: "regra_recorrente",
                sql: "extract(day from competencia_inicio) = 1 AND (competencia_fim IS NULL OR (extract(day from competencia_fim) = 1 AND competencia_fim >= (competencia_inicio - interval '1 month')::date))");

            migrationBuilder.CreateIndex(
                name: "IX_configuracao_usuario_usuario_id_confirmacao_automatica_desde",
                table: "configuracao_usuario",
                columns: new[] { "usuario_id", "confirmacao_automatica_desde" },
                filter: "confirmar_ao_virar_mes AND confirmacao_automatica_desde IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_configuracao_usuario_alertas",
                table: "configuracao_usuario",
                sql: "alertas_desativados BETWEEN 0 AND 255");

            migrationBuilder.AddCheckConstraint(
                name: "ck_configuracao_usuario_confirmacao",
                table: "configuracao_usuario",
                sql: "confirmacao_automatica_desde IS NULL OR (confirmar_ao_virar_mes AND extract(day from confirmacao_automatica_desde) = 1)");

            migrationBuilder.Sql("""
                ALTER TABLE lancamento ADD CONSTRAINT ck_lancamento_valor_finito CHECK (valor <> 'NaN'::numeric);
                ALTER TABLE conta ADD CONSTRAINT ck_conta_saldo_finito CHECK (saldo_inicial <> 'NaN'::numeric);
                ALTER TABLE cartao ADD CONSTRAINT ck_cartao_limite_finito CHECK (limite <> 'NaN'::numeric);
                ALTER TABLE compra_parcelada ADD CONSTRAINT ck_compra_valor_finito CHECK (valor_total <> 'NaN'::numeric);
                ALTER TABLE regra_recorrente ADD CONSTRAINT ck_regra_valor_finito CHECK (valor_base <> 'NaN'::numeric);
                ALTER TABLE excecao_recorrencia ADD CONSTRAINT ck_excecao_valor_finito CHECK (valor_override <> 'NaN'::numeric);

                CREATE FUNCTION validar_vinculos_lancamento() RETURNS trigger
                LANGUAGE plpgsql AS $$
                DECLARE
                    original lancamento%ROWTYPE;
                    compra compra_parcelada%ROWTYPE;
                BEGIN
                    IF NEW.tipo = 7 THEN
                        SELECT * INTO original FROM lancamento
                        WHERE usuario_id = NEW.usuario_id AND id = NEW.lancamento_estornado_id;
                        IF NOT FOUND OR original.tipo = 7 OR NEW.data_lancamento < original.data_lancamento
                           OR (NEW.valor, NEW.moeda, NEW.competencia, NEW.forma_pagamento, NEW.conta_id,
                               NEW.cartao_id, NEW.fatura_id, NEW.categoria_id, NEW.estabelecimento_id)
                           IS DISTINCT FROM
                              (original.valor, original.moeda, original.competencia, original.forma_pagamento, original.conta_id,
                               original.cartao_id, original.fatura_id, original.categoria_id, original.estabelecimento_id)
                        THEN
                            RAISE EXCEPTION 'O estorno precisa reproduzir os dados financeiros do lançamento original.' USING ERRCODE = '23514';
                        END IF;
                    END IF;
                    IF NEW.compra_parcelada_id IS NOT NULL THEN
                        SELECT * INTO compra FROM compra_parcelada
                        WHERE usuario_id = NEW.usuario_id AND id = NEW.compra_parcelada_id;
                        IF NOT FOUND OR (NEW.cartao_id, NEW.data_lancamento, NEW.total_parcelas)
                            IS DISTINCT FROM (compra.cartao_id, compra.data_compra, compra.total_parcelas)
                        THEN
                            RAISE EXCEPTION 'Os dados da parcela divergem da compra.' USING ERRCODE = '23514';
                        END IF;
                    END IF;
                    RETURN NEW;
                END;
                $$;
                CREATE TRIGGER lancamento_vinculos
                BEFORE INSERT ON lancamento FOR EACH ROW EXECUTE FUNCTION validar_vinculos_lancamento();

                CREATE FUNCTION conferir_compra_completa() RETURNS trigger
                LANGUAGE plpgsql AS $$
                DECLARE
                    soma numeric;
                    quantidade bigint;
                BEGIN
                    SELECT sum(valor), count(*) INTO soma, quantidade FROM lancamento
                    WHERE usuario_id = NEW.usuario_id AND compra_parcelada_id = NEW.id;
                    IF quantidade <> NEW.total_parcelas OR soma IS DISTINCT FROM NEW.valor_total THEN
                        RAISE EXCEPTION 'A quantidade e a soma das parcelas precisam coincidir com a compra.' USING ERRCODE = '23514';
                    END IF;
                    RETURN NULL;
                END;
                $$;
                CREATE CONSTRAINT TRIGGER compra_completa
                AFTER INSERT ON compra_parcelada DEFERRABLE INITIALLY DEFERRED
                FOR EACH ROW EXECUTE FUNCTION conferir_compra_completa();

                CREATE TRIGGER compra_imutavel
                BEFORE UPDATE OR DELETE OR TRUNCATE ON compra_parcelada
                FOR EACH STATEMENT EXECUTE FUNCTION rejeitar_mutacao_historico();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER compra_imutavel ON compra_parcelada;
                DROP TRIGGER compra_completa ON compra_parcelada;
                DROP FUNCTION conferir_compra_completa();
                DROP TRIGGER lancamento_vinculos ON lancamento;
                DROP FUNCTION validar_vinculos_lancamento();
                ALTER TABLE lancamento DROP CONSTRAINT ck_lancamento_valor_finito;
                ALTER TABLE conta DROP CONSTRAINT ck_conta_saldo_finito;
                ALTER TABLE cartao DROP CONSTRAINT ck_cartao_limite_finito;
                ALTER TABLE compra_parcelada DROP CONSTRAINT ck_compra_valor_finito;
                ALTER TABLE regra_recorrente DROP CONSTRAINT ck_regra_valor_finito;
                ALTER TABLE excecao_recorrencia DROP CONSTRAINT ck_excecao_valor_finito;
                """);

            migrationBuilder.DropCheckConstraint(
                name: "ck_regra_recorrente_periodo",
                table: "regra_recorrente");

            migrationBuilder.DropIndex(
                name: "IX_configuracao_usuario_usuario_id_confirmacao_automatica_desde",
                table: "configuracao_usuario");

            migrationBuilder.DropCheckConstraint(
                name: "ck_configuracao_usuario_alertas",
                table: "configuracao_usuario");

            migrationBuilder.DropCheckConstraint(
                name: "ck_configuracao_usuario_confirmacao",
                table: "configuracao_usuario");

            migrationBuilder.DropColumn(
                name: "alertas_desativados",
                table: "configuracao_usuario");

            migrationBuilder.DropColumn(
                name: "confirmacao_automatica_desde",
                table: "configuracao_usuario");

            migrationBuilder.AddCheckConstraint(
                name: "ck_regra_recorrente_periodo",
                table: "regra_recorrente",
                sql: "extract(day from competencia_inicio) = 1 AND (competencia_fim IS NULL OR (extract(day from competencia_fim) = 1 AND competencia_fim >= competencia_inicio))");
        }
    }
}
