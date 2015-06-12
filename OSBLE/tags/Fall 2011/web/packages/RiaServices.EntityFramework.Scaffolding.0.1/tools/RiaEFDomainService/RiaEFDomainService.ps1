[T4Scaffolding.Scaffolder(Description = "Enter a description of RiaEFDomainService here")][CmdletBinding()]
param(        
	[parameter(Mandatory = $true, ValueFromPipelineByPropertyName = $true)][string]$ModelType,
	[parameter(Mandatory = $true, ValueFromPipelineByPropertyName = $true)][string]$DbContextType,
	[switch]$UseFilePerModel=$false,
    [string]$Project,
	[string]$CodeLanguage,
	[string[]]$TemplateFolders,
	[switch]$Force = $false
)

# Mostly copied from the T4Scaffolding EFDbContext package/powershell script
# Ensure we can find the model type
$foundModelType = Get-ProjectType $ModelType -Project $Project  -BlockUi -ErrorAction SilentlyContinue
if (!$foundModelType) {
	Write-Error "Cannot find ModelType '$ModelType'"
	return 
}

# Find the DbContext class
$foundDbContextType = Get-ProjectType $DbContextType -Project $Project  -BlockUi -ErrorAction SilentlyContinue
if (!$foundDbContextType) { 
	Write-Error "Cannot find DbContextType '$DbContextType'"
	return 
}

$pluralName = Get-PluralizedWord $foundModelType.Name
$namespace = (Get-Project $Project).Properties.Item("DefaultNamespace").Value

$DbDomainServiceType = $DbContextType.Replace("Context", "") + "DomainService" #  
$foundDbDomainServiceType = Get-ProjectType $DbDomainServiceType -Project $Project -BlockUi -ErrorAction SilentlyContinue

if (!$foundDbDomainServiceType -or $UseFilePerModel) { 
	$outputPath = $DbDomainServiceType
	if ($UseFilePerModel) { $outputPath = $DbDomainServiceType + "." + $ModelType }
	$outputPath = Join-Path Services $outputPath
	# The filename extension will be added based on the template's <#@ Output Extension="..." #> directive

	# for some reason, the back-tick is required on the following lines
	Add-ProjectItemViaTemplate $outputPath -Template RiaEFDomainServiceTemplate `
		-Model @{ 		
			Namespace = $namespace;
			DbDomainService = $DbDomainServiceType;
			DbContextType = $DbContextType;
			DbContextTypeFullName = $foundDbContextType.FullName;
			EntityTypeName = $ModelType;
			EntityTypeFullName = $foundModelType.FullName;
			EntityTypeNamePluralized = $pluralName;
			} `
		-SuccessMessage "Added RiaEFDomainService at {0}" `
		-TemplateFolders $TemplateFolders -Project $Project -CodeLanguage $CodeLanguage -Force:$Force

	return
}
# Add the Type to the existing DomainService
$propertyName = "Get" +  $pluralName
Add-ClassMemberViaTemplate -Name $propertyName -CodeClass $foundDbDomainServiceType -Template RiaEFDomainServiceMember `
	-Model @{
		EntityType = $foundModelType;
		EntityTypeName = $ModelType;
		EntityTypeNamePluralized = $pluralName;
		} `
	-SuccessMessage "Added '$propertyName' to RiaEFDomainService '$($foundDbDomainServiceType.FullName)'" `
	-TemplateFolders $TemplateFolders -Project $Project -CodeLanguage $CodeLanguage
