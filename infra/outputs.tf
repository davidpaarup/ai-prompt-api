# outputs.tf

output "resource_group_name" {
  description = "Name of the created resource group"
  value       = azurerm_resource_group.main.name
}

output "container_registry_name" {
  description = "Name of the Azure Container Registry"
  value       = azurerm_container_registry.main.name
}

#output "container_registry_login_server" {
#  description = "Login server URL for the Azure Container Registry"
#  value       = azurerm_container_registry.main.login_server
#}

#output "container_registry_admin_username" {
#  description = "Admin username for the Azure Container Registry"
#  value       = azurerm_container_registry.main.admin_username
#  sensitive   = true
#}

#output "container_registry_admin_password" {
#  description = "Admin password for the Azure Container Registry"
#  value       = azurerm_container_registry.main.admin_password
#  sensitive   = true
#}

output "container_app_environment_name" {
  description = "Name of the Container Apps Environment"
  value       = azurerm_container_app_environment.main.name
}

output "container_app_name" {
  description = "Name of the Container App"
  value       = azurerm_container_app.api.name
}

output "container_app_url" {
  description = "URL of the deployed Container App"
  value       = "https://${azurerm_container_app.api.latest_revision_fqdn}"
}

#output "application_insights_name" {
#  description = "Name of the Application Insights instance"
#  value       = azurerm_application_insights.main.name
#}

#output "application_insights_instrumentation_key" {
#  description = "Instrumentation key for Application Insights"
#  value       = azurerm_application_insights.main.instrumentation_key
#  sensitive   = true
#}

#output "application_insights_connection_string" {
#  description = "Connection string for Application Insights"
#  value       = azurerm_application_insights.main.connection_string
#  sensitive   = true
#}

#output "key_vault_name" {
#  description = "Name of the Key Vault"
#  value       = azurerm_key_vault.main.name
#}

#output "key_vault_uri" {
#  description = "URI of the Key Vault"
#  value       = azurerm_key_vault.main.vault_uri
#}

#output "log_analytics_workspace_name" {
#  description = "Name of the Log Analytics Workspace"
#  value       = azurerm_log_analytics_workspace.main.name
#}

# GitHub Actions secrets format
#output "github_actions_secrets" {
#  description = "Values to add as GitHub Actions secrets"
#  value = {
#    AZURE_CONTAINER_REGISTRY    = azurerm_container_registry.main.login_server
#    AZURE_CONTAINER_APP         = azurerm_container_app.api.name
#    AZURE_RESOURCE_GROUP        = azurerm_resource_group.main.name
#    AZURE_CONTAINER_ENVIRONMENT = azurerm_container_app_environment.main.name
#    IMAGE_NAME                  = "api"
#  }
#}