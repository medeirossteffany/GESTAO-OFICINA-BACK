using GestaoOficina.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestaoOficina.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260408195000_AddTenantUsageAndPlanCheck")]
    public partial class AddTenantUsageAndPlanCheck : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE `Tenants` ADD COLUMN IF NOT EXISTS `Plan` longtext NOT NULL DEFAULT 'Basico';");

            migrationBuilder.Sql(@"
                SET @constraint_exists := (
                    SELECT COUNT(1)
                    FROM information_schema.table_constraints
                    WHERE table_schema = DATABASE()
                      AND table_name = 'Tenants'
                      AND constraint_name = 'CK_Tenants_Plan'
                );
                SET @add_constraint_sql := IF(
                    @constraint_exists = 0,
                    'ALTER TABLE `Tenants` ADD CONSTRAINT `CK_Tenants_Plan` CHECK (`Plan` IN (''Basico'',''Profissional'',''Premium''));',
                    'SELECT 1;'
                );
                PREPARE stmt FROM @add_constraint_sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
            ");

            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS `TenantUsages` (
                    `TenantId` int NOT NULL,
                    `CurrentUnits` int NOT NULL DEFAULT 0,
                    `CurrentUsers` int NOT NULL DEFAULT 0,
                    `CurrentCustomers` int NOT NULL DEFAULT 0,
                    `CurrentVehicles` int NOT NULL DEFAULT 0,
                    `CurrentServicesInMonth` int NOT NULL DEFAULT 0,
                    `ServicesMonthReference` datetime(6) NOT NULL,
                    `UpdatedAt` datetime(6) NOT NULL,
                    CONSTRAINT `PK_TenantUsages` PRIMARY KEY (`TenantId`),
                    CONSTRAINT `FK_TenantUsages_Tenants_TenantId` FOREIGN KEY (`TenantId`) REFERENCES `Tenants` (`Id`) ON DELETE CASCADE
                ) CHARACTER SET=utf8mb4;
            ");

            migrationBuilder.Sql(@"
                INSERT INTO `TenantUsages`
                (`TenantId`, `CurrentUnits`, `CurrentUsers`, `CurrentCustomers`, `CurrentVehicles`, `CurrentServicesInMonth`, `ServicesMonthReference`, `UpdatedAt`)
                SELECT
                    t.`Id` AS `TenantId`,
                    (SELECT COUNT(1) FROM `Units` u WHERE u.`TenantId` = t.`Id` AND u.`IsActive` = 1) AS `CurrentUnits`,
                    (SELECT COUNT(1) FROM `AspNetUsers` u WHERE u.`TenantId` = t.`Id` AND u.`IsActive` = 1) AS `CurrentUsers`,
                    (SELECT COUNT(1) FROM `Customers` c WHERE c.`TenantId` = t.`Id` AND c.`IsActive` = 1) AS `CurrentCustomers`,
                    (SELECT COUNT(1) FROM `Vehicles` v WHERE v.`TenantId` = t.`Id` AND v.`IsActive` = 1) AS `CurrentVehicles`,
                    (
                        SELECT COUNT(1)
                        FROM `ServiceOrders` so
                        WHERE so.`TenantId` = t.`Id`
                          AND so.`IsActive` = 1
                          AND so.`CreatedAt` >= UTC_DATE() - INTERVAL (DAY(UTC_DATE()) - 1) DAY
                          AND so.`CreatedAt` < (UTC_DATE() - INTERVAL (DAY(UTC_DATE()) - 1) DAY) + INTERVAL 1 MONTH
                    ) AS `CurrentServicesInMonth`,
                    UTC_DATE() - INTERVAL (DAY(UTC_DATE()) - 1) DAY AS `ServicesMonthReference`,
                    UTC_TIMESTAMP(6) AS `UpdatedAt`
                FROM `Tenants` t
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM `TenantUsages` tu
                    WHERE tu.`TenantId` = t.`Id`
                );
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS `TenantUsages`;");

            migrationBuilder.Sql(@"
                SET @constraint_exists := (
                    SELECT COUNT(1)
                    FROM information_schema.table_constraints
                    WHERE table_schema = DATABASE()
                      AND table_name = 'Tenants'
                      AND constraint_name = 'CK_Tenants_Plan'
                );
                SET @drop_constraint_sql := IF(
                    @constraint_exists = 1,
                    'ALTER TABLE `Tenants` DROP CHECK `CK_Tenants_Plan`;',
                    'SELECT 1;'
                );
                PREPARE stmt FROM @drop_constraint_sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
            ");
        }
    }
}
