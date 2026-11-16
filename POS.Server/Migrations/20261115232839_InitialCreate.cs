using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POS.Server.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.CreateTable(
                name: "almacen",
                schema: "dbo",
                columns: table => new
                {
                    id_almacen = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    tipo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ciudad = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    direccion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_almacen", x => x.id_almacen);
                });

            migrationBuilder.CreateTable(
                name: "cliente",
                schema: "dbo",
                columns: table => new
                {
                    id_cliente = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    razon_social = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    nit = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    telefono = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    direccion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cliente", x => x.id_cliente);
                });

            migrationBuilder.CreateTable(
                name: "hito",
                schema: "dbo",
                columns: table => new
                {
                    id_hito = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nombre = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    orden = table.Column<byte>(type: "tinyint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hito", x => x.id_hito);
                });

            migrationBuilder.CreateTable(
                name: "producto",
                schema: "dbo",
                columns: table => new
                {
                    id_producto = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    sku = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    peso_unitario_kg = table.Column<decimal>(type: "decimal(10,3)", precision: 10, scale: 3, nullable: false),
                    volumen_unitario_m3 = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_producto", x => x.id_producto);
                });

            migrationBuilder.CreateTable(
                name: "rol",
                schema: "dbo",
                columns: table => new
                {
                    id_rol = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nombre = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rol", x => x.id_rol);
                });

            migrationBuilder.CreateTable(
                name: "tipo_carga",
                schema: "dbo",
                columns: table => new
                {
                    id_tipo_carga = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nombre = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tipo_carga", x => x.id_tipo_carga);
                });

            migrationBuilder.CreateTable(
                name: "tipo_incidencia",
                schema: "dbo",
                columns: table => new
                {
                    id_tipo_incidencia = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nombre = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tipo_incidencia", x => x.id_tipo_incidencia);
                });

            migrationBuilder.CreateTable(
                name: "vehiculo",
                schema: "dbo",
                columns: table => new
                {
                    id_vehiculo = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    placa = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    tipo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    capacidad_kg = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    capacidad_m3 = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Disponible")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vehiculo", x => x.id_vehiculo);
                });

            migrationBuilder.CreateTable(
                name: "inventario",
                schema: "dbo",
                columns: table => new
                {
                    id_almacen = table.Column<int>(type: "int", nullable: false),
                    id_producto = table.Column<int>(type: "int", nullable: false),
                    stock_actual = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventario", x => new { x.id_almacen, x.id_producto });
                    table.ForeignKey(
                        name: "FK_inventario_almacen_id_almacen",
                        column: x => x.id_almacen,
                        principalSchema: "dbo",
                        principalTable: "almacen",
                        principalColumn: "id_almacen");
                    table.ForeignKey(
                        name: "FK_inventario_producto_id_producto",
                        column: x => x.id_producto,
                        principalSchema: "dbo",
                        principalTable: "producto",
                        principalColumn: "id_producto");
                });

            migrationBuilder.CreateTable(
                name: "usuario",
                schema: "dbo",
                columns: table => new
                {
                    id_usuario = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_rol = table.Column<int>(type: "int", nullable: false),
                    nombre_completo = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    correo = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    password_hash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    fecha_creacion = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "SYSDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuario", x => x.id_usuario);
                    table.ForeignKey(
                        name: "FK_usuario_rol_id_rol",
                        column: x => x.id_rol,
                        principalSchema: "dbo",
                        principalTable: "rol",
                        principalColumn: "id_rol");
                });

            migrationBuilder.CreateTable(
                name: "hoja_ruta",
                schema: "dbo",
                columns: table => new
                {
                    id_hoja_ruta = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_vehiculo = table.Column<int>(type: "int", nullable: false),
                    id_conductor = table.Column<int>(type: "int", nullable: false),
                    id_usuario_creador = table.Column<int>(type: "int", nullable: false),
                    estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Pendiente"),
                    fecha_creacion = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "SYSDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hoja_ruta", x => x.id_hoja_ruta);
                    table.ForeignKey(
                        name: "FK_hoja_ruta_usuario_id_conductor",
                        column: x => x.id_conductor,
                        principalSchema: "dbo",
                        principalTable: "usuario",
                        principalColumn: "id_usuario");
                    table.ForeignKey(
                        name: "FK_hoja_ruta_usuario_id_usuario_creador",
                        column: x => x.id_usuario_creador,
                        principalSchema: "dbo",
                        principalTable: "usuario",
                        principalColumn: "id_usuario");
                    table.ForeignKey(
                        name: "FK_hoja_ruta_vehiculo_id_vehiculo",
                        column: x => x.id_vehiculo,
                        principalSchema: "dbo",
                        principalTable: "vehiculo",
                        principalColumn: "id_vehiculo");
                });

            migrationBuilder.CreateTable(
                name: "guia_envio",
                schema: "dbo",
                columns: table => new
                {
                    id_guia = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    codigo_guia = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: false),
                    id_cliente = table.Column<int>(type: "int", nullable: false),
                    id_almacen_origen = table.Column<int>(type: "int", nullable: false),
                    id_tipo_carga = table.Column<int>(type: "int", nullable: false),
                    id_hito_actual = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    id_hoja_ruta = table.Column<int>(type: "int", nullable: true),
                    id_usuario_registro = table.Column<int>(type: "int", nullable: false),
                    destinatario = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    direccion_destino = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    peso_kg = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    volumen_m3 = table.Column<decimal>(type: "decimal(10,3)", precision: 10, scale: 3, nullable: false),
                    fecha_registro = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "SYSDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_guia_envio", x => x.id_guia);
                    table.ForeignKey(
                        name: "FK_guia_envio_almacen_id_almacen_origen",
                        column: x => x.id_almacen_origen,
                        principalSchema: "dbo",
                        principalTable: "almacen",
                        principalColumn: "id_almacen");
                    table.ForeignKey(
                        name: "FK_guia_envio_cliente_id_cliente",
                        column: x => x.id_cliente,
                        principalSchema: "dbo",
                        principalTable: "cliente",
                        principalColumn: "id_cliente");
                    table.ForeignKey(
                        name: "FK_guia_envio_hito_id_hito_actual",
                        column: x => x.id_hito_actual,
                        principalSchema: "dbo",
                        principalTable: "hito",
                        principalColumn: "id_hito");
                    table.ForeignKey(
                        name: "FK_guia_envio_hoja_ruta_id_hoja_ruta",
                        column: x => x.id_hoja_ruta,
                        principalSchema: "dbo",
                        principalTable: "hoja_ruta",
                        principalColumn: "id_hoja_ruta");
                    table.ForeignKey(
                        name: "FK_guia_envio_tipo_carga_id_tipo_carga",
                        column: x => x.id_tipo_carga,
                        principalSchema: "dbo",
                        principalTable: "tipo_carga",
                        principalColumn: "id_tipo_carga");
                    table.ForeignKey(
                        name: "FK_guia_envio_usuario_id_usuario_registro",
                        column: x => x.id_usuario_registro,
                        principalSchema: "dbo",
                        principalTable: "usuario",
                        principalColumn: "id_usuario");
                });

            migrationBuilder.CreateTable(
                name: "checkpoint",
                schema: "dbo",
                columns: table => new
                {
                    id_checkpoint = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_guia = table.Column<int>(type: "int", nullable: false),
                    id_hito = table.Column<int>(type: "int", nullable: false),
                    id_almacen = table.Column<int>(type: "int", nullable: true),
                    id_usuario = table.Column<int>(type: "int", nullable: false),
                    fecha_hora = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "SYSDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_checkpoint", x => x.id_checkpoint);
                    table.ForeignKey(
                        name: "FK_checkpoint_almacen_id_almacen",
                        column: x => x.id_almacen,
                        principalSchema: "dbo",
                        principalTable: "almacen",
                        principalColumn: "id_almacen");
                    table.ForeignKey(
                        name: "FK_checkpoint_guia_envio_id_guia",
                        column: x => x.id_guia,
                        principalSchema: "dbo",
                        principalTable: "guia_envio",
                        principalColumn: "id_guia");
                    table.ForeignKey(
                        name: "FK_checkpoint_hito_id_hito",
                        column: x => x.id_hito,
                        principalSchema: "dbo",
                        principalTable: "hito",
                        principalColumn: "id_hito");
                    table.ForeignKey(
                        name: "FK_checkpoint_usuario_id_usuario",
                        column: x => x.id_usuario,
                        principalSchema: "dbo",
                        principalTable: "usuario",
                        principalColumn: "id_usuario");
                });

            migrationBuilder.CreateTable(
                name: "detalle_guia",
                schema: "dbo",
                columns: table => new
                {
                    id_guia = table.Column<int>(type: "int", nullable: false),
                    id_producto = table.Column<int>(type: "int", nullable: false),
                    cantidad = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_detalle_guia", x => new { x.id_guia, x.id_producto });
                    table.ForeignKey(
                        name: "FK_detalle_guia_guia_envio_id_guia",
                        column: x => x.id_guia,
                        principalSchema: "dbo",
                        principalTable: "guia_envio",
                        principalColumn: "id_guia");
                    table.ForeignKey(
                        name: "FK_detalle_guia_producto_id_producto",
                        column: x => x.id_producto,
                        principalSchema: "dbo",
                        principalTable: "producto",
                        principalColumn: "id_producto");
                });

            migrationBuilder.CreateTable(
                name: "entrega",
                schema: "dbo",
                columns: table => new
                {
                    id_entrega = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_guia = table.Column<int>(type: "int", nullable: false),
                    id_usuario = table.Column<int>(type: "int", nullable: false),
                    nombre_receptor = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    ruta_firma = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ruta_foto = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    fecha_hora = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "SYSDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_entrega", x => x.id_entrega);
                    table.ForeignKey(
                        name: "FK_entrega_guia_envio_id_guia",
                        column: x => x.id_guia,
                        principalSchema: "dbo",
                        principalTable: "guia_envio",
                        principalColumn: "id_guia");
                    table.ForeignKey(
                        name: "FK_entrega_usuario_id_usuario",
                        column: x => x.id_usuario,
                        principalSchema: "dbo",
                        principalTable: "usuario",
                        principalColumn: "id_usuario");
                });

            migrationBuilder.CreateTable(
                name: "incidencia",
                schema: "dbo",
                columns: table => new
                {
                    id_incidencia = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_guia = table.Column<int>(type: "int", nullable: false),
                    id_tipo_incidencia = table.Column<int>(type: "int", nullable: false),
                    id_usuario = table.Column<int>(type: "int", nullable: false),
                    descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ruta_foto = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    fecha_hora = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "SYSDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_incidencia", x => x.id_incidencia);
                    table.ForeignKey(
                        name: "FK_incidencia_guia_envio_id_guia",
                        column: x => x.id_guia,
                        principalSchema: "dbo",
                        principalTable: "guia_envio",
                        principalColumn: "id_guia");
                    table.ForeignKey(
                        name: "FK_incidencia_tipo_incidencia_id_tipo_incidencia",
                        column: x => x.id_tipo_incidencia,
                        principalSchema: "dbo",
                        principalTable: "tipo_incidencia",
                        principalColumn: "id_tipo_incidencia");
                    table.ForeignKey(
                        name: "FK_incidencia_usuario_id_usuario",
                        column: x => x.id_usuario,
                        principalSchema: "dbo",
                        principalTable: "usuario",
                        principalColumn: "id_usuario");
                });

            migrationBuilder.CreateTable(
                name: "movimiento_inventario",
                schema: "dbo",
                columns: table => new
                {
                    id_movimiento = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_almacen = table.Column<int>(type: "int", nullable: false),
                    id_producto = table.Column<int>(type: "int", nullable: false),
                    id_guia = table.Column<int>(type: "int", nullable: true),
                    id_usuario = table.Column<int>(type: "int", nullable: false),
                    tipo_movimiento = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    cantidad = table.Column<int>(type: "int", nullable: false),
                    fecha_hora = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "SYSDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_movimiento_inventario", x => x.id_movimiento);
                    table.ForeignKey(
                        name: "FK_movimiento_inventario_almacen_id_almacen",
                        column: x => x.id_almacen,
                        principalSchema: "dbo",
                        principalTable: "almacen",
                        principalColumn: "id_almacen");
                    table.ForeignKey(
                        name: "FK_movimiento_inventario_guia_envio_id_guia",
                        column: x => x.id_guia,
                        principalSchema: "dbo",
                        principalTable: "guia_envio",
                        principalColumn: "id_guia");
                    table.ForeignKey(
                        name: "FK_movimiento_inventario_producto_id_producto",
                        column: x => x.id_producto,
                        principalSchema: "dbo",
                        principalTable: "producto",
                        principalColumn: "id_producto");
                    table.ForeignKey(
                        name: "FK_movimiento_inventario_usuario_id_usuario",
                        column: x => x.id_usuario,
                        principalSchema: "dbo",
                        principalTable: "usuario",
                        principalColumn: "id_usuario");
                });

            migrationBuilder.CreateTable(
                name: "notificacion",
                schema: "dbo",
                columns: table => new
                {
                    id_notificacion = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_usuario = table.Column<int>(type: "int", nullable: false),
                    id_guia = table.Column<int>(type: "int", nullable: true),
                    tipo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    mensaje = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    leida = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    fecha_hora = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "SYSDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notificacion", x => x.id_notificacion);
                    table.ForeignKey(
                        name: "FK_notificacion_guia_envio_id_guia",
                        column: x => x.id_guia,
                        principalSchema: "dbo",
                        principalTable: "guia_envio",
                        principalColumn: "id_guia");
                    table.ForeignKey(
                        name: "FK_notificacion_usuario_id_usuario",
                        column: x => x.id_usuario,
                        principalSchema: "dbo",
                        principalTable: "usuario",
                        principalColumn: "id_usuario");
                });

            migrationBuilder.CreateIndex(
                name: "IX_almacen_nombre",
                schema: "dbo",
                table: "almacen",
                column: "nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_checkpoint_id_almacen",
                schema: "dbo",
                table: "checkpoint",
                column: "id_almacen");

            migrationBuilder.CreateIndex(
                name: "IX_checkpoint_id_guia_id_hito",
                schema: "dbo",
                table: "checkpoint",
                columns: new[] { "id_guia", "id_hito" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_checkpoint_id_hito",
                schema: "dbo",
                table: "checkpoint",
                column: "id_hito");

            migrationBuilder.CreateIndex(
                name: "IX_checkpoint_id_usuario",
                schema: "dbo",
                table: "checkpoint",
                column: "id_usuario");

            migrationBuilder.CreateIndex(
                name: "IX_cliente_nit",
                schema: "dbo",
                table: "cliente",
                column: "nit",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_detalle_guia_id_producto",
                schema: "dbo",
                table: "detalle_guia",
                column: "id_producto");

            migrationBuilder.CreateIndex(
                name: "IX_entrega_id_guia",
                schema: "dbo",
                table: "entrega",
                column: "id_guia",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_entrega_id_usuario",
                schema: "dbo",
                table: "entrega",
                column: "id_usuario");

            migrationBuilder.CreateIndex(
                name: "IX_guia_envio_codigo_guia",
                schema: "dbo",
                table: "guia_envio",
                column: "codigo_guia",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_guia_envio_id_almacen_origen",
                schema: "dbo",
                table: "guia_envio",
                column: "id_almacen_origen");

            migrationBuilder.CreateIndex(
                name: "IX_guia_envio_id_cliente",
                schema: "dbo",
                table: "guia_envio",
                column: "id_cliente");

            migrationBuilder.CreateIndex(
                name: "IX_guia_envio_id_hito_actual",
                schema: "dbo",
                table: "guia_envio",
                column: "id_hito_actual");

            migrationBuilder.CreateIndex(
                name: "IX_guia_envio_id_hoja_ruta",
                schema: "dbo",
                table: "guia_envio",
                column: "id_hoja_ruta");

            migrationBuilder.CreateIndex(
                name: "IX_guia_envio_id_tipo_carga",
                schema: "dbo",
                table: "guia_envio",
                column: "id_tipo_carga");

            migrationBuilder.CreateIndex(
                name: "IX_guia_envio_id_usuario_registro",
                schema: "dbo",
                table: "guia_envio",
                column: "id_usuario_registro");

            migrationBuilder.CreateIndex(
                name: "IX_hito_nombre",
                schema: "dbo",
                table: "hito",
                column: "nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_hito_orden",
                schema: "dbo",
                table: "hito",
                column: "orden",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_hoja_ruta_id_conductor",
                schema: "dbo",
                table: "hoja_ruta",
                column: "id_conductor");

            migrationBuilder.CreateIndex(
                name: "IX_hoja_ruta_id_usuario_creador",
                schema: "dbo",
                table: "hoja_ruta",
                column: "id_usuario_creador");

            migrationBuilder.CreateIndex(
                name: "IX_hoja_ruta_id_vehiculo",
                schema: "dbo",
                table: "hoja_ruta",
                column: "id_vehiculo");

            migrationBuilder.CreateIndex(
                name: "IX_incidencia_id_guia",
                schema: "dbo",
                table: "incidencia",
                column: "id_guia");

            migrationBuilder.CreateIndex(
                name: "IX_incidencia_id_tipo_incidencia",
                schema: "dbo",
                table: "incidencia",
                column: "id_tipo_incidencia");

            migrationBuilder.CreateIndex(
                name: "IX_incidencia_id_usuario",
                schema: "dbo",
                table: "incidencia",
                column: "id_usuario");

            migrationBuilder.CreateIndex(
                name: "IX_inventario_id_producto",
                schema: "dbo",
                table: "inventario",
                column: "id_producto");

            migrationBuilder.CreateIndex(
                name: "IX_movimiento_inventario_id_almacen",
                schema: "dbo",
                table: "movimiento_inventario",
                column: "id_almacen");

            migrationBuilder.CreateIndex(
                name: "IX_movimiento_inventario_id_guia",
                schema: "dbo",
                table: "movimiento_inventario",
                column: "id_guia");

            migrationBuilder.CreateIndex(
                name: "IX_movimiento_inventario_id_producto",
                schema: "dbo",
                table: "movimiento_inventario",
                column: "id_producto");

            migrationBuilder.CreateIndex(
                name: "IX_movimiento_inventario_id_usuario",
                schema: "dbo",
                table: "movimiento_inventario",
                column: "id_usuario");

            migrationBuilder.CreateIndex(
                name: "IX_notificacion_id_guia",
                schema: "dbo",
                table: "notificacion",
                column: "id_guia");

            migrationBuilder.CreateIndex(
                name: "IX_notificacion_id_usuario",
                schema: "dbo",
                table: "notificacion",
                column: "id_usuario");

            migrationBuilder.CreateIndex(
                name: "IX_producto_sku",
                schema: "dbo",
                table: "producto",
                column: "sku",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_rol_nombre",
                schema: "dbo",
                table: "rol",
                column: "nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tipo_carga_nombre",
                schema: "dbo",
                table: "tipo_carga",
                column: "nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tipo_incidencia_nombre",
                schema: "dbo",
                table: "tipo_incidencia",
                column: "nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_usuario_correo",
                schema: "dbo",
                table: "usuario",
                column: "correo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_usuario_id_rol",
                schema: "dbo",
                table: "usuario",
                column: "id_rol");

            migrationBuilder.CreateIndex(
                name: "IX_vehiculo_placa",
                schema: "dbo",
                table: "vehiculo",
                column: "placa",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "checkpoint",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "detalle_guia",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "entrega",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "incidencia",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "inventario",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "movimiento_inventario",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "notificacion",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "tipo_incidencia",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "producto",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "guia_envio",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "almacen",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "cliente",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "hito",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "hoja_ruta",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "tipo_carga",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "usuario",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "vehiculo",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "rol",
                schema: "dbo");
        }
    }
}
