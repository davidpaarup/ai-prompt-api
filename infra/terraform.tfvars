# terraform.tfvars.example
# Copy this file to terraform.tfvars and customize the values

project_name            = "semantic-kernel-api"
resource_group_name     = "ia-project"
location               = "East US"
container_registry_name = "testregistrypaarup"  # Must be globally unique
acr_sku                = "Basic"

# Container configuration
container_cpu    = 0.5
container_memory = "1.0Gi"
min_replicas     = 0    # Scales to zero when not in use
max_replicas     = 10   # Maximum scale out

# Environment
aspnet_environment = "Production"

# Tags
#tags = {
#  Environment = "Production"
#  Project     = "semantic-kernel-api"
#  ManagedBy   = "Terraform"
#  Owner       = "David Paarup"
#}