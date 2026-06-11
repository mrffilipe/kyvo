using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kyvo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MapApplicationClientListFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_external_identities_users_user_id",
                table: "external_identities");

            migrationBuilder.DropForeignKey(
                name: "FK_tenant_memberships_users_user_id",
                table: "tenant_memberships");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_invites_invited_by_user_id",
                table: "tenant_invites",
                column: "invited_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_auth_sessions_client_id",
                table: "auth_sessions",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "IX_auth_sessions_membership_id",
                table: "auth_sessions",
                column: "membership_id");

            migrationBuilder.CreateIndex(
                name: "IX_auth_sessions_tenant_id",
                table: "auth_sessions",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_auth_sessions_user_id",
                table: "auth_sessions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_membership_id",
                table: "audit_logs",
                column: "membership_id");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_user_id",
                table: "audit_logs",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_audit_logs_tenant_memberships_membership_id",
                table: "audit_logs",
                column: "membership_id",
                principalTable: "tenant_memberships",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_audit_logs_users_user_id",
                table: "audit_logs",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_auth_sessions_application_clients_client_id",
                table: "auth_sessions",
                column: "client_id",
                principalTable: "application_clients",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_auth_sessions_tenant_memberships_membership_id",
                table: "auth_sessions",
                column: "membership_id",
                principalTable: "tenant_memberships",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_auth_sessions_tenants_tenant_id",
                table: "auth_sessions",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_auth_sessions_users_user_id",
                table: "auth_sessions",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_external_identities_users_user_id",
                table: "external_identities",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tenant_invites_users_invited_by_user_id",
                table: "tenant_invites",
                column: "invited_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tenant_memberships_users_user_id",
                table: "tenant_memberships",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_audit_logs_tenant_memberships_membership_id",
                table: "audit_logs");

            migrationBuilder.DropForeignKey(
                name: "FK_audit_logs_users_user_id",
                table: "audit_logs");

            migrationBuilder.DropForeignKey(
                name: "FK_auth_sessions_application_clients_client_id",
                table: "auth_sessions");

            migrationBuilder.DropForeignKey(
                name: "FK_auth_sessions_tenant_memberships_membership_id",
                table: "auth_sessions");

            migrationBuilder.DropForeignKey(
                name: "FK_auth_sessions_tenants_tenant_id",
                table: "auth_sessions");

            migrationBuilder.DropForeignKey(
                name: "FK_auth_sessions_users_user_id",
                table: "auth_sessions");

            migrationBuilder.DropForeignKey(
                name: "FK_external_identities_users_user_id",
                table: "external_identities");

            migrationBuilder.DropForeignKey(
                name: "FK_tenant_invites_users_invited_by_user_id",
                table: "tenant_invites");

            migrationBuilder.DropForeignKey(
                name: "FK_tenant_memberships_users_user_id",
                table: "tenant_memberships");

            migrationBuilder.DropIndex(
                name: "IX_tenant_invites_invited_by_user_id",
                table: "tenant_invites");

            migrationBuilder.DropIndex(
                name: "IX_auth_sessions_client_id",
                table: "auth_sessions");

            migrationBuilder.DropIndex(
                name: "IX_auth_sessions_membership_id",
                table: "auth_sessions");

            migrationBuilder.DropIndex(
                name: "IX_auth_sessions_tenant_id",
                table: "auth_sessions");

            migrationBuilder.DropIndex(
                name: "IX_auth_sessions_user_id",
                table: "auth_sessions");

            migrationBuilder.DropIndex(
                name: "IX_audit_logs_membership_id",
                table: "audit_logs");

            migrationBuilder.DropIndex(
                name: "IX_audit_logs_user_id",
                table: "audit_logs");

            migrationBuilder.AddForeignKey(
                name: "FK_external_identities_users_user_id",
                table: "external_identities",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_tenant_memberships_users_user_id",
                table: "tenant_memberships",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
