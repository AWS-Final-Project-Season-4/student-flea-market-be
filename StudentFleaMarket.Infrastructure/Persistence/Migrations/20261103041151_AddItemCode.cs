using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentFleaMarket.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddItemCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Bước 1: thêm cột nullable trước — chưa đặt NOT NULL / UNIQUE vì
            // dữ liệu cũ chưa có giá trị.
            migrationBuilder.AddColumn<string>(
                name: "ItemCode",
                table: "Items",
                type: "character varying(8)",
                maxLength: 8,
                nullable: true);

            // Bước 2: sinh mã 8 ký tự (A-Z, 0-9) cho toàn bộ tin đăng đã có.
            //
            // Mỗi mã được sinh từ md5(random() || Id) rồi ánh xạ từng byte sang
            // bảng 36 ký tự (A-Z0-9) -> dùng đủ cả chữ cái lẫn chữ số.
            // Vòng lặp bảo đảm mã DUY NHẤT: cứ sinh tới khi không trùng mã
            // đã có trong bảng.
            migrationBuilder.Sql(
                """
                CREATE OR REPLACE FUNCTION tmp_sfm_gen_item_code(seed bytea)
                RETURNS text AS $$
                DECLARE
                    res text := '';
                    i integer;
                    b integer;
                    alpha constant text := 'ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789';
                BEGIN
                    FOR i IN 0..7 LOOP
                        -- get_byte trả về giá trị 0..255 -> mod 36 ra vị trí
                        -- trong bảng 36 ký tự (A-Z, 0-9).
                        b := get_byte(seed, i);
                        res := res || substr(alpha, (b % 36) + 1, 1);
                    END LOOP;
                    RETURN res;
                END;
                $$ LANGUAGE plpgsql;
                """);

            migrationBuilder.Sql(
                """
                DO $$
                DECLARE
                    r record;
                    c text;
                BEGIN
                    FOR r IN
                        SELECT "Id" FROM "Items"
                        WHERE "ItemCode" IS NULL OR "ItemCode" = ''
                        ORDER BY "Id"
                    LOOP
                        LOOP
                            c := tmp_sfm_gen_item_code(
                                    decode(md5(random()::text || r."Id"::text), 'hex'));
                            EXIT WHEN NOT EXISTS (
                                SELECT 1 FROM "Items" x
                                WHERE x."ItemCode" = c AND x."Id" <> r."Id"
                            );
                        END LOOP;
                        UPDATE "Items" SET "ItemCode" = c WHERE "Id" = r."Id";
                    END LOOP;
                END;
                $$;
                """);

            migrationBuilder.Sql("DROP FUNCTION IF EXISTS tmp_sfm_gen_item_code(bytea);");

            // Bước 3: siết lại thành NOT NULL + index UNIQUE toàn hệ thống.
            migrationBuilder.AlterColumn<string>(
                name: "ItemCode",
                table: "Items",
                type: "character varying(8)",
                maxLength: 8,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(8)",
                oldMaxLength: 8,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Items_ItemCode",
                table: "Items",
                column: "ItemCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Items_ItemCode",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "ItemCode",
                table: "Items");
        }
    }
}
