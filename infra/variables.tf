# variables.tf

variable "project_name" {
  description = "Name of the project used for resource naming"
  type        = string
  default     = "myapi"
}

variable "resource_group_name" {
  description = "Name of the Azure Resource Group"
  type        = string
  default     = "rg-myapi-prod"
}

variable "location" {
  description = "Azure region where resources will be created"
  type        = string
  default     = "East US"
}

variable "container_registry_name" {
  description = "Name of the Azure Container Registry (must be globally unique)"
  type        = string
  default     = "acrmyapi001"
  
  validation {
    condition     = can(regex("^[a-zA-Z0-9]*$", var.container_registry_name))
    error_message = "Container registry name can only contain alphanumeric characters."
  }
}

variable "acr_sku" {
  description = "SKU for Azure Container Registry"
  type        = string
  default     = "Basic"
  
  validation {
    condition     = contains(["Basic", "Standard", "Premium"], var.acr_sku)
    error_message = "ACR SKU must be Basic, Standard, or Premium."
  }
}

variable "initial_container_image" {
  description = "Initial container image to deploy"
  type        = string
  default     = "mcr.microsoft.com/azuredocs/containerapps-helloworld:latest"
}

variable "container_cpu" {
  description = "CPU allocation for the container (in cores)"
  type        = number
  default     = 0.5
  
  validation {
    condition     = contains([0.25, 0.5, 0.75, 1.0, 1.25, 1.5, 1.75, 2.0], var.container_cpu)
    error_message = "Container CPU must be one of: 0.25, 0.5, 0.75, 1.0, 1.25, 1.5, 1.75, 2.0."
  }
}

variable "container_memory" {
  description = "Memory allocation for the container"
  type        = string
  default     = "1.0Gi"
  
  validation {
    condition     = can(regex("^[0-9]+(\\.[0-9]+)?Gi$", var.container_memory))
    error_message = "Container memory must be specified in Gi format (e.g., 1.0Gi, 2.0Gi)."
  }
}

variable "min_replicas" {
  description = "Minimum number of container replicas"
  type        = number
  default     = 0
  
  validation {
    condition     = var.min_replicas >= 0 && var.min_replicas <= 1000
    error_message = "Min replicas must be between 0 and 1000."
  }
}

variable "max_replicas" {
  description = "Maximum number of container replicas"
  type        = number
  default     = 10
  
  validation {
    condition     = var.max_replicas >= 1 && var.max_replicas <= 1000
    error_message = "Max replicas must be between 1 and 1000."
  }
}

variable "aspnet_environment" {
  description = "ASP.NET Core environment"
  type        = string
  default     = "Production"
  
  validation {
    condition     = contains(["Development", "Staging", "Production"], var.aspnet_environment)
    error_message = "ASP.NET environment must be Development, Staging, or Production."
  }
}

#variable "tags" {
#  description = "Tags to apply to all resources"
#  type        = map(string)
#  default = {
#    Environment = "Production"
#    Project     = "MyAPI"
#    ManagedBy   = "Terraform"
#  }
#}