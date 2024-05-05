param([Parameter(Mandatory = $true)][string] $version)
$ErrorActionPreference = "Stop"
$appName = "aiochatbot"
$path = "src"

$acr = (az resource list --tag asm-resource-id=shared-container-registry | ConvertFrom-Json)
if (!$acr) {
    throw "Unable to find eligible platform container registry!"
}
if ($acr.Length -eq 0) {
    throw "Unable to find 'ANY' eligible platform container registry!"
}

$AcrName = $acr.Name

# Login to ACR
# az acr login --name $AcrName -> NOT SUPPORTED in docker
az acr login -n $AcrName --expose-token
if ($LastExitCode -ne 0) {
    throw "An error has occured. Unable to login to acr."
}

$imageName = "$appName`:$version"

$shouldBuild = $true
$tags = az acr repository show-tags --name $AcrName --repository $appName | ConvertFrom-Json
if ($tags) {
    if ($tags.Contains($version)) {
        $shouldBuild = $false
    }
}

if ($shouldBuild -eq $true) {
    # Build your app with ACR build command
    az acr build --image $imageName -r $AcrName --file ./$path/Dockerfile .
    
    if ($LastExitCode -ne 0) {
        throw "An error has occured. Unable to build image."
    }
}
