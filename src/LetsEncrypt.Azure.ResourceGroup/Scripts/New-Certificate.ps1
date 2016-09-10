$cert = New-SelfSignedCertificate -CertStoreLocation "cert:\CurrentUser\My" -Subject "CN=exampleapp" -KeySpec KeyExchange

$certContentEncoded = [System.Convert]::ToBase64String($cert.GetRawCertData())
Write-Host $certContentEncoded
$secret = ConvertTo-SecureString -String $certContentEncoded -AsPlainText –Force 
$secretContentType = 'application/x-pkcs12' 
Set-AzureKeyVaultSecret -VaultName sjkpvault2 -Name testCert -SecretValue $secret -ContentType $secretContentType # Change the Key Vault name and secret name