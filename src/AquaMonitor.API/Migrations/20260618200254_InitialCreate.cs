using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AquaMonitor.API.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AM_SENSORES",
                columns: table => new
                {
                    ID = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    NOME = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: false),
                    LOCALIZACAO = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    TIPO = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: false),
                    LIMITE_CONSUMO = table.Column<double>(type: "BINARY_DOUBLE", nullable: false),
                    ATIVO = table.Column<bool>(type: "NUMBER(1)", nullable: false, defaultValue: true),
                    CRIADO_EM = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false, defaultValueSql: "SYSDATE")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AM_SENSORES", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "AM_USUARIOS",
                columns: table => new
                {
                    ID = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    NOME = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: false),
                    EMAIL = table.Column<string>(type: "NVARCHAR2(150)", maxLength: 150, nullable: false),
                    SENHA_HASH = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    PERFIL = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: false),
                    CRIADO_EM = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false, defaultValueSql: "SYSDATE")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AM_USUARIOS", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "AM_ALERTAS",
                columns: table => new
                {
                    ID = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    SENSOR_ID = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    TIPO = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: false),
                    DESCRICAO = table.Column<string>(type: "NVARCHAR2(500)", maxLength: 500, nullable: false),
                    NIVEL = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: false),
                    RESOLVIDO = table.Column<bool>(type: "NUMBER(1)", nullable: false, defaultValue: false),
                    CRIADO_EM = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false, defaultValueSql: "SYSDATE"),
                    RESOLVIDO_EM = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AM_ALERTAS", x => x.ID);
                    table.ForeignKey(
                        name: "FK_AM_ALERTAS_AM_SENSORES_SENSOR_ID",
                        column: x => x.SENSOR_ID,
                        principalTable: "AM_SENSORES",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AM_LEITURAS",
                columns: table => new
                {
                    ID = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    SENSOR_ID = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    CONSUMO_LITROS = table.Column<double>(type: "BINARY_DOUBLE", nullable: false),
                    PRESSAO_BAR = table.Column<double>(type: "BINARY_DOUBLE", nullable: true),
                    TEMPERATURA_CELSIUS = table.Column<double>(type: "BINARY_DOUBLE", nullable: true),
                    REGISTRADO_EM = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false, defaultValueSql: "SYSDATE"),
                    VAZAMENTO_DETECTADO = table.Column<bool>(type: "NUMBER(1)", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AM_LEITURAS", x => x.ID);
                    table.ForeignKey(
                        name: "FK_AM_LEITURAS_AM_SENSORES_SENSOR_ID",
                        column: x => x.SENSOR_ID,
                        principalTable: "AM_SENSORES",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AM_ALERTAS_RESOLVIDO",
                table: "AM_ALERTAS",
                column: "RESOLVIDO");

            migrationBuilder.CreateIndex(
                name: "IX_AM_ALERTAS_SENSOR_ID",
                table: "AM_ALERTAS",
                column: "SENSOR_ID");

            migrationBuilder.CreateIndex(
                name: "IX_AM_LEITURAS_REGISTRADO_EM",
                table: "AM_LEITURAS",
                column: "REGISTRADO_EM");

            migrationBuilder.CreateIndex(
                name: "IX_AM_LEITURAS_SENSOR_ID",
                table: "AM_LEITURAS",
                column: "SENSOR_ID");

            migrationBuilder.CreateIndex(
                name: "IX_AM_SENSORES_NOME",
                table: "AM_SENSORES",
                column: "NOME");

            migrationBuilder.CreateIndex(
                name: "IX_AM_USUARIOS_EMAIL",
                table: "AM_USUARIOS",
                column: "EMAIL",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AM_ALERTAS");

            migrationBuilder.DropTable(
                name: "AM_LEITURAS");

            migrationBuilder.DropTable(
                name: "AM_USUARIOS");

            migrationBuilder.DropTable(
                name: "AM_SENSORES");
        }
    }
}
