<#
.SYNOPSIS
    Manages an ACI app container with the specified parameters.

.PARAMETER CONFIG_PATH
    Specifies the path to the configuration file.

.PARAMETER COMMAND
    Specifies the command to run. Valid values are "run", "view", "restart", "stop", and "delete".

.PARAMETER REPLICA_COUNT
    Specifies the replica count.

.EXAMPLE
    COMMAND EXAMPLES
    ---------------------------
    .\RunContainerApps.ps1 -CONFIG_PATH .\config.json -COMMAND run  -REPLICA_COUNT 2
    Runs application container with the specified parameters.

    .\RunContainerApps.ps1 -CONFIG_PATH .\config.json -COMMAND view 
    Views application containers with the specified parameters.

    .\RunContainerApps.ps1 -CONFIG_PATH .\config.json -COMMAND restart 
    Restarts application containers with the specified parameters.

    .\RunContainerApps.ps1 -CONFIG_PATH .\config.json -COMMAND stop 
    Stops application containers with the specified parameters.

    .\RunContainerApps.ps1 -CONFIG_PATH .\config.json -COMMAND delete 
    Deletes application containers with the specified parameters.
    ---------------------------

.NOTES
    This script requires the Azure CLI to be installed and configured with the appropriate subscription.
#>

param(
    [Parameter(Mandatory = $true)][string]$CONFIG_PATH,
    [Parameter(Mandatory = $true)][string]$COMMAND,
    [Parameter(Mandatory = $false)][int]$REPLICA_COUNT
)

#  Summarize to user which args were passed.
Write-Host " "
Write-Host "--------------------------------------------------"
Write-Host "Running command '$COMMAND' with parameters:"
Write-Host "    CONFIG_PATH:           '$CONFIG_PATH'"
Write-Host "    REPLICA_COUNT:         '$(if ($REPLICA_COUNT -eq 0) { '' } else { $REPLICA_COUNT })'"
Write-Host "================================================="
Write-Host " "

# load the config file
$topLevelConfig = (Get-Content -Path $CONFIG_PATH -Raw | ConvertFrom-Json)
$config = $topLevelConfig.config
$containerConfig = $topLevelConfig.containerConfig

## ACI VARS ##

# use tags to get the resource group name for the ACI
if ($topLevelConfig.RG_VALUE) {
    $resourceGroup = (az group show -n $topLevelConfig.RG_VALUE | ConvertFrom-Json)
}
else {
    if (!$resourceGroup) {
        try {
            $resourceGroup = (az group list --tag "$($config.RG_TAG_NAME)=$($config.RG_TAG_VALUE)" | ConvertFrom-Json)[0]
        }
        catch {
            Write-Host "    Error: $resourceGroup"
            Write-Host "    Could not use tags to get resource group."
            exit
        }
    }
}

# use tags to get the ACR name and password
$acrName = ""
$acrPassword = ""
try {
    $acrName = (az resource list --tag "$($config.ACR_TAG_NAME)=$($config.ACR_TAG_VALUE)" | ConvertFrom-Json).name
    $acrPassword = (az acr credential show --name "$($acrName)" | ConvertFrom-Json).passwords[0].value
}
catch {
    Write-Host "    Error: $_"
    Write-Host "    Could not use tags to get the ACR name and password."
    exit
}

function Show-Usage {

    $allowedArguments = @(
        "-CONFIG_PATH",
        "-COMMAND [run -REPLICA_COUNT | view | restart | stop | delete ]"
    )

    Write-Host "Usage arguments and example:"
    foreach ($argument in $allowedArguments) {
        Write-Host "    $argument"
    }
    Write-Host "Example: .\RunContainerApps.ps1 -CONFIG_PATH .\config.json -COMMAND run -REPLICA_COUNT 2"
}

function Add-Containers {
    param(
        [Parameter(Mandatory = $true)][int]$StartingNameIndex,
        [Parameter(Mandatory = $true)][int]$Count
    )

    Write-Host " "
    Write-Host "--------------------------------------------------"
    Write-Host "Adding $($Count - $StartingNameIndex) container(s)" -BackgroundColor Yellow
    Write-Host "--------------------------------------------------"
    Write-Host " "

    # convert the config to this format:
    # "TENANT1=value1;"
    $containerEnvironmentVars = @()
    $containerConfig.environmentVars.PSObject.Properties | ForEach-Object {
        $containerEnvironmentVars += "$($_.Name)=$($_.Value)"
    }

    $containerSecureEnvironmentVars = @()
    $containerConfig.secureEnvironmentVars.PSObject.Properties | ForEach-Object {
        $containerSecureEnvironmentVars += "$($_.Name)=$($_.Value)"
    }

    # --assign-identity $using:config.AUTH_ID `
    # loop through the matching containers and start a create background job for each one
    $jobs = @()
    for ($i = $StartingNameIndex; $i -lt $Count; $i++) {
        $containerName = "$($config.CONTAINER_NAME_PREFIX)-" + $i
        $jobs += Start-Job -ScriptBlock {
            param($containerName)
            $response = az container create `
                -g $using:resourceGroup.name `
                --name $containerName `
                --image "$($using:acrName).azurecr.io/$($using:config.ACR_IMAGE)" `
                --location $using:config.LOCATION `
                --registry-login-server "$($using:acrName).azurecr.io" `
                --registry-username $using:acrName `
                --registry-password $using:acrPassword `
                --cpu $using:containerConfig.cpu `
                --memory $using:containerConfig.memory `
                --restart-policy $using:containerConfig.restartPolicy `
                --secure-environment-variables $using:containerSecureEnvironmentVars `
                --environment-variables $using:containerEnvironmentVars `
                --zone $using:config.ZONE

            if ($LastExitCode -ne 0) {
                throw "Error: $response"
            }

        } -ArgumentList $containerName

        Write-Host "Running Job - ContainerName: $($containerName)..."
    }

    # wait for all jobs to complete
    $failedJobs = $jobs | Wait-Job | Where-Object { $_.State -eq "Failed" }
    if ($failedJobs.Count -gt 0) {
        Write-Host "Failed jobs:"
        $failedJobs | Format-Table
        throw "Failed to add container(s)."
    }

    Write-Host "Container created... Done" -BackgroundColor Green
}

function Remove-Containers {
    param(
        [Parameter(Mandatory = $true)][int]$StartingNameIndex,
        [Parameter(Mandatory = $true)][string]$Count
    )

    Write-Host " "
    Write-Host "--------------------------------------------------"
    Write-Host "Deleting $($Count - $StartingNameIndex) container(s)..." -BackgroundColor Yellow
    Write-Host "--------------------------------------------------"
    Write-Host " "

    # loop through the matching containers and start a deletion background job for each one
    $jobs = @()
    for ($i = $StartingNameIndex; $i -lt $Count; $i++) {
        # calculate the container name based on the index
        $containerName = "$($config.CONTAINER_NAME_PREFIX)" + $i

        # start a background job to delete the container
        $jobs += Start-Job -ScriptBlock {
            param($containerName)
            # delete the container using the Azure CLI
            $response = az container delete --name $containerName --resource-group $using:resourceGroup.name --yes

            if ($LastExitCode -ne 0) {
                throw "Error: $response"
            }
        } -ArgumentList $containerName

        Write-Host "     Running Job - ContainerName: $($containerName)..."
    }

    # wait for all jobs to complete
    $failedJobs = $jobs | Wait-Job | Where-Object { $_.State -eq "Failed" }
    if ($failedJobs.Count -gt 0) {
        Write-Host "Failed jobs:"
        $failedJobs | Format-Table
        throw "Failed to $($Action) container(s)."
    }

    Write-Host "Container deletion done" -BackgroundColor Green
}

function Get-AllContainers {
    $response = az container list --resource-group $resourceGroup.name
    if ($LastExitCode -ne 0) {
        throw "Error: $response"
    }

    $containers = $response | ConvertFrom-Json
    if ($containers.Count -eq 0) {
        Write-Host "The Azure Container Instance has no containers." -BackgroundColor Yellow
        return @()
    }

    # checks if there are containers with the specified prefix
    $containers = $containers.Where({ $_.containers[0].name -ilike "$($config.CONTAINER_NAME_PREFIX)-*" })

    Write-Host "Found $($containers.Count) matching container(s)..."

    # update the instance view for each container using a background job for each one and make sure the original container gets updated
    $jobs = @()
    for ($i = 0; $i -lt $containers.Count; $i++) {
        $container = $containers[$i].containers[0]
        $jobs += Start-Job -ScriptBlock {
            param($container)
            $containerGroup = az container show --name $container.name --resource-group $using:resourceGroup.name | ConvertFrom-Json
            $container.instanceView = $containerGroup.containers[0].instanceView
            return $container
        } -ArgumentList $container

        Write-Host "    Running Job - ContainerName: $($container.name)..."
    }

    $successfulJobs = @()
    $failedJobs = @()
    $jobs | Wait-Job | ForEach-Object {
        if ($_.State -eq "Failed") {
            $failedJobs += $_
        }
        else {
            $successfulJobs += $_ | Receive-Job
        }
    }

    if ($failedJobs.Count -gt 0) {
        Write-Host "Failed jobs:"
        $failedJobs | Format-Table
        throw "Failed to get instance view for containers."
    }

    # update the containers with the instance view
    for ($i = 0; $i -lt $successfulJobs.Count; $i++) {
        $containers[$i].containers[0].instanceView = $successfulJobs[$i].instanceView
    }
    return $containers
}

function Update-Containers {
    param(
        [Parameter(Mandatory = $true)][string]$Action,
        [Parameter(Mandatory = $false)][array]$containers = @()
    )

    if ($containers.Count -le 0) {
        Write-Host "    No containers were $($Action)ed."
        exit
    }

    Write-Host " "
    Write-Host "--------------------------------------------------"
    Write-Host "$($Action)ing $($containers.Count) container(s)..." -BackgroundColor Yellow
    Write-Host "--------------------------------------------------"
    Write-Host " "

    # loop through the matching containers and start a background job for each one
    $jobs = @()
    for ($i = 0; $i -lt $containers.Count; $i++) {
        $container = $containers[$i]
        $jobs += Start-Job -ScriptBlock {
            param($container)
            $response = ""
            switch ($using:Action) {
                "delete" {
                    $response = az container delete --name $container.name --resource-group $using:resourceGroup.name --yes
                }
                "stop" {
                    $response = az container stop --name $container.name --resource-group $using:resourceGroup.name
                }
                "restart" {
                    $response = az container restart --name $container.name --resource-group $using:resourceGroup.name
                }
            }
            if ($LastExitCode -ne 0) {
                throw "Error: $response"
            }
        } -ArgumentList $container
        Write-Host "     Running Job - ContainerName: $($container.name)..."
    }

    # wait for all jobs to complete
    $failedJobs = $jobs | Wait-Job | Where-Object { $_.State -eq "Failed" }
    if ($failedJobs.Count -gt 0) {
        Write-Host "Failed jobs:"
        $failedJobs | Format-Table
        throw "Failed to $($Action) container(s)."
    }

    Write-Host " "
    Write-Host "All containers $($Action)ed." -BackgroundColor Green

}

switch ($COMMAND) {
    "delete" {
        $containers = @(Get-AllContainers)
        Update-Containers -Action "delete" -containers $containers
    }
    "stop" {
        $containers = @(Get-AllContainers)
        Update-Containers -Action "stop" -containers $containers
    }
    "restart" {
        $containers = @(Get-AllContainers)
        Update-Containers -Action "restart" -containers $containers
    }
    "view" {
        $containers = @(Get-AllContainers)

        if ($containers.Count -eq 0) {
            Write-Host "    No containers found. Please check the name and try again."
            exit
        }

        # create a table that shows the state of each container
        $table = @()
        for ($i = 0; $i -lt $containers.Count; $i++) {

            $table += [pscustomobject]@{
                ContainerName = $containers[$i].name
                Status        = $containers[$i].provisioningState
                RestartPol    = $containers[$i].restartPolicy
                Location      = $containers[$i].location
                Image         = $containers[$i].containers[0].image
                State         = $containers[$i].containers[0].instanceView.currentState.state
                Restarts      = $containers[$i].containers[0].instanceView.restartCount
            }
        }
        $table | Format-Table
    }

    "run" {
        $containers = @(Get-AllContainers)
        
        $diff = $REPLICA_COUNT - $containers.Length

        if ($diff -gt 0) {
            # add containers
            Add-Containers -StartingNameIndex $currentCount -Count $diff
        }
        elseif ($diff -lt 0) {
            # delete containers
            Remove-Containers -StartingNameIndex $requestCount -Count ($containers.Length - $REPLICA_COUNT)
        }
        else {
            Write-Host "No changes were made."
        }
    }
    default {
        Write-Host "Unknown command: $COMMAND"
        Show-Usage
    }
}
