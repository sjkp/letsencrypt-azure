$tenantId = "37597dd5-5816-4d7a-99e8-b2e6c3f4d0c1"
$subscriptionId = "cd0d7179-80a0-47e6-a201-9c12de0bbd37"

$bytes = New-Object Byte[] 32
$rand = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$rand.GetBytes($bytes)
$rand.Dispose()
$key = [System.Convert]::ToBase64String($bytes)
Write-Host "service princpal password $key"
$app = New-AzureRmADApplication -DisplayName "LetsEncrypt.Azure" -HomePage "https://letsencrypt-azure" -IdentifierUris "https://letsencrypt-azure" -Password $key

$sp = New-AzureRmADServicePrincipal -ApplicationId $app.ApplicationId

Write-Host $sp.Id

$objectId = $sp.Id

$resourceGroupName = "LetsEncrypt.Azure"

New-AzureRmResourceGroup -Name $resourceGroupName -Location "NorthEurope" 

New-AzureRmResourceGroupDeployment -Name "test" -ResourceGroupName $resourceGroupName -TemplateParameterObject @{appName = "letsencryptfunctions"; keyVaultName = "letsencrypt-vault"; tenantId = $tenantId; objectId = $objectId} -TemplateFile .\..\Templates\letsencrypt.azure.core.json


