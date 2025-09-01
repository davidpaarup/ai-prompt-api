# Configure the Azure Provider
terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~>3.0"
    }
  }
  required_version = ">= 1.0"
}

# Configure the Microsoft Azure Provider
provider "azurerm" {
  features {}
}

# Create Resource Group
resource "azurerm_resource_group" "main" {
  name     = var.resource_group_name
  location = var.location

  #tags = var.tags
}

# Create Log Analytics Workspace for Container Apps
#resource "azurerm_log_analytics_workspace" "main" {
#  name                = "${var.project_name}-law"
#  location            = azurerm_resource_group.main.location
#  resource_group_name = azurerm_resource_group.main.name
#  sku                 = "PerGB2018"
#  retention_in_days   = 30

  #tags = var.tags
#}

# Create Azure Container Registry
resource "azurerm_container_registry" "main" {
  name                = var.container_registry_name
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  sku                 = var.acr_sku
  admin_enabled       = true

  #tags = var.tags
}

# Create Container Apps Environment
resource "azurerm_container_app_environment" "main" {
  name                       = "${var.project_name}-env"
  location                   = azurerm_resource_group.main.location
  resource_group_name        = azurerm_resource_group.main.name
  #log_analytics_workspace_id = azurerm_log_analytics_workspace.main.id

  #tags = var.tags
}

# Create Container App
resource "azurerm_container_app" "api" {
  name                         = "${var.project_name}"
  container_app_environment_id = azurerm_container_app_environment.main.id
  resource_group_name          = azurerm_resource_group.main.name
  revision_mode                = "Single"

  template {
    container {
      name   = "api"
      image  = var.initial_container_image
      cpu    = var.container_cpu
      memory = var.container_memory

      env {
        name  = "ASPNETCORE_ENVIRONMENT"
        value = var.aspnet_environment
      }

      env {
        name  = "ASPNETCORE_URLS"
        value = "http://0.0.0.0:8080"
      }
    }

    min_replicas = var.min_replicas
    max_replicas = var.max_replicas
  }

  ingress {
    allow_insecure_connections = false
    external_enabled           = true
    target_port                = 8080

    traffic_weight {
      percentage      = 100
      latest_revision = true
    }
  }

  #tags = var.tags
}

# Create Application Insights for monitoring
#resource "azurerm_application_insights" "main" {
#  name                = "${var.project_name}-ai"
#  location            = azurerm_resource_group.main.location
#  resource_group_name = azurerm_resource_group.main.name
#  workspace_id        = azurerm_log_analytics_workspace.main.id
#  application_type    = "web"

  #tags = var.tags
#}

# Create Key Vault for storing secrets
#resource "azurerm_key_vault" "main" {
#  name                       = "${var.project_name}-kv-${random_string.suffix.result}"
#  location                   = azurerm_resource_group.main.location
#  resource_group_name        = azurerm_resource_group.main.name
#  tenant_id                  = data.azurerm_client_config.current.tenant_id
#  sku_name                   = "standard"
#  soft_delete_retention_days = 7

 # access_policy {
 #   tenant_id = data.azurerm_client_config.current.tenant_id
 #   object_id = data.azurerm_client_config.current.object_id

  #  secret_permissions = [
  #    "Get",
  #    "List",
  #    "Set",
  #    "Delete",
  #    "Recover",
  #    "Backup",
  #    "Restore"
  #  ]
  #}

  #tags = var.tags
#}

# Random string for unique naming
#resource "random_string" "suffix" {
#  length  = 4
#  special = false
#  upper   = false
#}

# Get current Azure client configuration
#data "azurerm_client_config" "current" {}

# Store Application Insights connection string in Key Vault
#resource "azurerm_key_vault_secret" "app_insights_connection_string" {
  #name         = "ApplicationInsights--ConnectionString"
  #value        = azurerm_application_insights.main.connection_string
  #key_vault_id = azurerm_key_vault.main.id

 # depends_on = [azurerm_key_vault.main]
#}

# Store ACR credentials in Key Vault
#resource "azurerm_key_vault_secret" "acr_username" {
  #name         = "ACR--Username"
  #value        = azurerm_container_registry.main.admin_username
  #key_vault_id = azurerm_key_vault.main.id

#  depends_on = [azurerm_key_vault.main]
#}

#resource "azurerm_key_vault_secret" "acr_password" {
#  name         = "ACR--Password"
#  value        = azurerm_container_registry.main.admin_password
#  key_vault_id = azurerm_key_vault.main.id

 # depends_on = [azurerm_key_vault.main]
#}