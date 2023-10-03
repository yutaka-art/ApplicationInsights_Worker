param environmentName string = 'zenn-9e6580d41f8f62'
param storageAccountName string = 'st${replace(environmentName, '-', '')}'
param logAnalyticsWorkspaceName string = 'logs-${environmentName}'
param appInsightsName string = 'appi-${environmentName}'
param acrName string = 'cr${replace(environmentName, '-', '')}'
param hostingPlanName string = 'plan-${environmentName}'
param functionAppName string = 'func-${environmentName}'
param location string = resourceGroup().location

// LogAnalyticsWorkspace
resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2020-08-01' = {
  name: logAnalyticsWorkspaceName
  location: location
  properties: any({
    retentionInDays: 30
    features: {
      searchVersion: 1
      legacy: 0
      enableLogAccessUsingOnlyResourcePermissions: true
    }
    sku: {
      name: 'PerGB2018'
    }
  })
}


// Application Insights
resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  kind: 'web'
  properties: { 
    Application_Type: 'web'
    WorkspaceResourceId:logAnalyticsWorkspace.id
  }
}

// Storage account
var strageSku = 'Standard_LRS'

resource storageAccount 'Microsoft.Storage/storageAccounts@2021-04-01' = {
  name: storageAccountName
  location: location
  kind: 'Storage'
  sku:{
    name: strageSku
  }
}

// Azure Container Registry
param acrSku string = 'Basic'

resource acrResource 'Microsoft.ContainerRegistry/registries@2023-01-01-preview' = {
  name: acrName
  location: location
  sku: {
    name: acrSku
  }
  properties: {
    adminUserEnabled: true
  }
}

// Function App Plan
resource hostingPlan  'Microsoft.Web/serverfarms@2021-03-01' = {
  name: hostingPlanName
  location: location
  sku: {
    name: 'EP1'
    tier: 'ElasticPremium'
    size: 'EP1'
  }
  properties: {
    reserved: true // for Linux
  }
}

// Function App
resource functionApp 'Microsoft.Web/sites@2021-03-01' = {
  name: functionAppName
  location: location
  kind: 'functionapp,linux,container'
  properties: {
    serverFarmId: hostingPlan.id
    siteConfig: {
      appSettings: [
        {
          name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
          value: appInsights.properties.InstrumentationKey
        }
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccountName};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storageAccount.listKeys().keys[0].value}'
        }
        {
          name: 'DOCKER_ENABLE_CI'
          value: 'true'
        }
        {
          name: 'DOCKER_REGISTRY_SERVER_PASSWORD'
          value: ''
        }
        {
          name: 'DOCKER_REGISTRY_SERVER_URL'
          value: 'https://${acrName}.azurecr.io'
        }
        {
          name: 'DOCKER_REGISTRY_SERVER_USERNAME'
          value: '${acrName}'
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccountName};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storageAccount.listKeys().keys[0].value}'
        }
        {
          name: 'WEBSITES_ENABLE_APP_SERVICE_STORAGE'
          value: 'false'
        }
        {
          name: 'TZ'
          value: 'Asia/Tokyo'
        }
        {
          name: 'STORAGE_CONNECT_STRING'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccountName};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storageAccount.listKeys().keys[0].value}'
        }
        {
          name: 'KQL_APPLICATION_ID'
          value: 'a78281cd-5c91-40ce-807e-1b47452ea16f'
        }
        {
          name: 'KQL_APPLICATION_API_KEY'
          value: '1hxiiqwavakwklgpqcejussrnfn3lkl5sv8mdh3b'
        }
        {
          name: 'KQL_MAIN_QUERY_REGION'
          value: 'traces | project timestamp, message, client_StateOrProvince, client_City'
        }
        {
          name: 'KQL_ORDER_BY_REGION'
          value: ' | order by timestamp desc'
        }
        {
          name: 'KQL_WHERE_REGION'
          value: ' | where timestamp >= datetime(TARGET_FROM) and timestamp <= datetime(TARGET_TO) and message contains \'AlphaProcessAsync\''
        }
        {
          name: 'KQL_WHERE_REGION_TIME_SPAN'
          value: '-12'
        }
        {
          name: 'BLOB_CONTAINER_NAME_OPE_LOG'
          value: 'aggregate'
        }
      ]
      linuxFxVersion: 'DOCKER|mcr.microsoft.com/azure-cognitive-services/decision/anomaly-detector:latest'
      ftpsState: 'FtpsOnly'
      minTlsVersion: '1.2'
    }
    httpsOnly: true
  }
}
