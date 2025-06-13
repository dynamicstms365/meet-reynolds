@maxLength(20)
@minLength(4)
@description('Used to generate names for all resources in this file')
param resourceBaseName string

@secure()
param botDisplayName string = 'Reynolds Teams Agent'

@secure()
param microsoftAppId string

@secure()
param microsoftAppPassword string

@secure()
param tenantId string

param webAppSKU string = 'B1'
param location string = resourceGroup().location
param serverfarmsName string = resourceBaseName
param webAppName string = resourceBaseName
param identityName string = resourceBaseName

// Create managed identity for Reynolds authentication
resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  location: location
  name: identityName
}

// Compute resources for Reynolds Web App
resource serverfarm 'Microsoft.Web/serverfarms@2021-02-01' = {
  kind: 'app'
  location: location
  name: serverfarmsName
  sku: {
    name: webAppSKU
  }
}

// Web App that hosts Reynolds with enterprise configuration
resource webApp 'Microsoft.Web/sites@2021-02-01' = {
  kind: 'app'
  location: location
  name: webAppName
  properties: {
    serverFarmId: serverfarm.id
    httpsOnly: true
    siteConfig: {
      alwaysOn: true
      netFrameworkVersion: 'v8.0'
      appSettings: [
        {
          name: 'WEBSITE_RUN_FROM_PACKAGE'
          value: '1'
        }
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Production'
        }
        {
          name: 'RUNNING_ON_AZURE'
          value: '1'
        }
        {
          name: 'MicrosoftAppId'
          value: microsoftAppId
        }
        {
          name: 'MicrosoftAppPassword'
          value: microsoftAppPassword
        }
        {
          name: 'TenantId'
          value: tenantId
        }
        {
          name: 'EnableTeamsIntegration'
          value: 'true'
        }
        {
          name: 'REYNOLDS_ORG_MODE'
          value: 'true'
        }
        {
          name: 'TARGET_ORGANIZATION'
          value: 'dynamicstms365'
        }
        {
          name: 'clientId'
          value: identity.properties.clientId
        }
        {
          name: 'tenantId'
          value: identity.properties.tenantId
        }
        {
          name: 'Teams:AppId'
          value: microsoftAppId
        }
        {
          name: 'Teams:AppPassword'
          value: microsoftAppPassword
        }
        {
          name: 'Teams:TenantId'
          value: tenantId
        }
        {
          name: 'Teams:BotUserEmail'
          value: 'reynolds@${tenantId}.onmicrosoft.com'
        }
      ]
      ftpsState: 'FtpsOnly'
    }
  }
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${identity.id}': {}
    }
  }
}

// Register Reynolds as a bot with the Bot Framework
module azureBotRegistration './botRegistration/azurebot.bicep' = {
  name: 'Reynolds-Bot-Registration'
  params: {
    resourceBaseName: resourceBaseName
    identityClientId: identity.properties.clientId
    identityResourceId: identity.id
    identityTenantId: identity.properties.tenantId
    botAppDomain: webApp.properties.defaultHostName
    botDisplayName: botDisplayName
  }
}

// Output values for deployment pipeline
output BOT_AZURE_APP_SERVICE_RESOURCE_ID string = webApp.id
output BOT_DOMAIN string = webApp.properties.defaultHostName
output BOT_ID string = identity.properties.clientId
output BOT_TENANT_ID string = identity.properties.tenantId
output REYNOLDS_ENDPOINT string = 'https://${webApp.properties.defaultHostName}'