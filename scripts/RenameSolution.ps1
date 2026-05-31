param([string]$cmdarg = "")
#Contributed by Kpro#3271

## if script does not run due to Execution_Policies and not being digitally signed, you can unblock it with:
## Unblock-File -Path .\scripts\RenameSolution.ps1

# uncomment to enable logging
Start-Transcript -OutputDirectory "C:\transcripts\"

# make script more robust and set working directory to root-directory of repository
# will also be a safeguard against multiple invocations (e.g. after failed/interrupted attempt)
# or when script will be copied to another place
$scriptDir = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent
$projectDir = Split-Path -Path $scriptDir -Parent
$projectName = Split-Path -Path $projectDir -leaf

# check a few files and directories
$required = @(
	(Join-Path $projectDir 'README.md'),
	(Join-Path $projectDir 'LICENSE'),
	(Join-Path $projectDir 'JotunnModStub.sln'),
	(Join-Path $projectDir 'JotunnModStub')
)

$allExist = $true
foreach ($p in $required)
{
	if (-not (Test-Path $p)) { $allExist = $false; break }
}

if ($allExist) {
	Push-Location
	Set-Location -Path $projectDir
} else {
	Write-Host "File ($p) missing, script was already executed? Please restart from clean checkout"
	exit 1
}

Write-Host ""
Write-Host ""
Write-Host "WELCOME TO JOTUNNMODSTUB RENAMING UTILITY"
Write-Host "-----------------------------------------"
Write-Host ""
Write-Host "This script will do the following:"
Write-Host ""
Write-Host "Step 1. Change the file names, folder names, and project references from the JotunnModStub to your custom solution name."
Write-Host ""
Write-Host "Step 2. Change the DoPrebuild.props file."
Write-Host ""
Write-Host "Step 3. Create the Environment.props file."
Write-Host ""
Write-Host "-----------------------------------------"
Write-Host ""

if ($cmdarg -eq "-nocopy") {
	$Name = $projectName
} else {
	# Check for rename and copy use-case
	Write-Host "Step 1. Choose one of the following options:"
	Write-Host ""
	Write-Host "	Option 1. Create a solution in this folder named '$projectName'"
	Write-Host ""
	Write-Host "	Option 2. Copy this folder to a new folder with a solution name I will choose"
	Write-Host ""
	$yn_name = Read-Host "Select (1/2)?"
	if ($yn_name -eq "1") {
		$Name = $projectName
	} else {
		Write-Host ""
		Write-Host "Got it. A copy of the folder $projectName will be created in a new folder and solution name"
		$Name = Read-Host "Enter a new name for the solution."
		if ($Name -eq $projectName) {
			Write-Host "Name equals current directory $projectName. Aborting."
			# Restore original working directory
			Pop-Location
			Exit 1
		}
		Write-Host ""

		$parentDir = Split-Path -Path $projectDir -Parent
		$destinationDir = Join-Path $parentDir $Name

		if (Test-Path $destinationDir) {
			Read-Host "Target directory $destinationDir already exists, please hit Enter to proceed, files will be overwritten"
		}

		# Copy modstub folder
		New-Item -ItemType Directory -Path $destinationDir -Force | Out-Null
		robocopy $projectDir $destinationDir /E /XD .git .vs .idea

		Set-Location $destinationDir
		Write-Host "Current location set to: $(Get-Location)"
	}
}

# Rename folders and files

if ($Name -ne "") {
	Write-Host "     . . . Renaming files and folders to '$Name' . . ."
	Move-Item -Path JotunnModStub -Destination $Name
	$unity = $Name + "Unity"
	Move-Item -Path JotunnModStubUnity -Destination $unity
	Get-ChildItem -Path . -File -Recurse |
		Where-Object { $_.Name -ne "JotunnModStub.zip" } |
		ForEach-Object {
			Rename-Item `
				-Path $_.PSPath `
				-NewName $_.Name.Replace("JotunnModStub", $Name)
		}
} else {
	Write-Host "Error: empty solution name"
	Read-Host "Enter to exit"
	# Restore original working directory
	Pop-Location
	Exit 0
}

# Rename internal references
$msg = "     . . . Replacing internal references to 'JotunnModStub' with '$Name' . . ."
Write-Host $msg

# in these files JotunnModStub will be replaced with the new solutionname
$filestochange = @(
	("README.md"),
	("$Name.sln"),
	("$Name\$Name.cs"),
	("$Name\$Name.csproj"),
	("$Name\Properties\AssemblyInfo.cs")
)

foreach ($p in $filestochange) {
	if (Test-Path $p) {
		((Get-Content -path $p -Raw) -replace 'JotunnModStub',$Name) | Set-Content -Path $p
	}
}

# project content is now replaced, adapt build Environment

# setting DoPrebuild.props to true
Write-Host ""
Write-Host "Step 2. . . . setting DoPrebuid.props <ExecutePrebuild> to true..."
((Get-Content -path DoPreBuild.props -Raw) -replace 'False','True') | Set-Content -Path DoPreBuild.props

Write-Host ""

$yn_copyprops = "n"
# Test whether Environment.props exists in parent directory
if (Test-Path -Path ..\Environment.props) {	
	Write-Host ""
	$yn_copyprops = Read-Host "Step 3 . . . . An Environment.Props file is dectected in the parent directory. Copy this file to solution directory (y/n)?"
}
switch -Regex ($yn_copyprops) {
	'^y$'	{ Copy-Item "..\Environment.props" -Destination "."; break }
	'.*'	{
		Write-Host ""
		Write-Host "Step 3 . . . . You must create an Environment.props file inside the solution directory PRIOR to building the solution."
		Write-Host ""
		$yn_createprops = Read-Host " -- Would you like to create an Environment.props file now (y/n)"
		if ($yn_createprops -eq "y")
		{
			# Initially assume typical install location
			$TypicalInstallFolder = "c:\Program Files (x86)\Steam\steamapps\common\Valheim"

			Write-Host ""
			# Test for existence of typical install
			if (Test-Path -Path $TypicalInstallFolder) {
				$ValheimFolder = $TypicalInstallFolder
				Write-Host " -- Steam installation found - please confirm your Valheim install folder"
			} else {
				$ValheimFolder = "$env:SystemDrive"
				Write-Host " -- No Steam installation found - please select your Valheim install folder"
			}
			
			function Get-InstallFolder($label="", $initialDirectory="") {
				[void] [System.Reflection.Assembly]::LoadWithPartialName('System.Windows.Forms')
				
				$OpenFolderDialog = New-Object System.Windows.Forms.FolderBrowserDialog
				$OpenFolderDialog.SelectedPath = $initialDirectory
				$OpenFolderDialog.Description = $label
				$OpenFolderDialog.rootfolder = "MyComputer"
				[void] $OpenFolderDialog.ShowDialog()
				return $OpenFolderDialog.SelectedPath
			}

			# Ask user to verify install folder
			$verifiedinstallfolder = Get-InstallFolder -label "Select Valheim Install Folder" -initialDirectory $ValheimFolder
			$bepinexdll = Join-Path $verifiedinstallfolder "BepInEx\core\BepInEx.dll"

			if (Test-Path -Path $bepinexdll) {
				# BepInEx installed in game folder, deploying there
				$deploypath = '$(VALHEIM_INSTALL)\BepInEx\plugins'
				$bepinex_section = ''
			} else {
				while (-not (Test-Path -Path $bepinexdll)) {
					# need selection of mod manager profile path

					Write-Host ""
					Write-Host "BepInEx.dll not found at $bepinexdll"
					Write-Host "It seems a mod manager is used and the dll is inside the mod manager profile"
					Write-Host "Please select BepInEx folder inside of active mod manager profile"
					Write-Host ""

					$bepinexpath = Get-InstallFolder -label "Select BepInEx folder in mod manager profile" -initialDirectory "$env:APPDATA\Roaming"
					$bepinexdll = Join-Path $bepinexpath "core\BepInEx.dll"
				}
				$bepinex_section = @"
		<!-- Path to BepInEx directory to access dlls -->
		<BEPINEX_PATH>$bepinexpath</BEPINEX_PATH>
"@
				$deploypath = '$(BEPINEX_PATH)\plugins'
			}

			Write-Host ""
			Write-Host ""

			# Build Environment.props
			Set-Content -Path .\Environment.props -Encoding UTF8 -Value @"
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="Current" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
	<PropertyGroup>
		<!-- Needs to be your path to the base Valheim folder -->
		<VALHEIM_INSTALL>$verifiedinstallfolder</VALHEIM_INSTALL>
$bepinex_section
		<!-- This is the folder where your build gets copied to when using the post-build automations -->
		<MOD_DEPLOYPATH>$deploypath</MOD_DEPLOYPATH>
	</PropertyGroup>
</Project>
"@
			
			# Copy Environment.props to parent folder for reuse
			Write-Host "Copying Environment.props to parent folder for future reuse"
			Copy-Item ".\Environment.props" -Destination "..\"
			Write-Host ""
			Write-Host "Deploy-Path is set to $deploypath"
			Write-Host ""

		}
	}
}

Write-Host ""
Write-Host ""
Write-Host "Success"
Write-Host "-------"
Write-Host ""
Write-Host "The process is complete."
Write-Host "Note that, as stated in the wiki, the compiler will generate reference errors the first time you build the solution."
Write-Host "This is normal. Close VS2019/2022. Reopen the solution. Build. The errors should be resolved."
Write-Host ""
Write-Host "You might want to continue with updating the JotunnLib package"
Write-Host "For instructions, see https://www.nuget.org/packages/JotunnLib"
Write-Host ""
Read-Host "Hit Enter to Exit"
Write-Host ""
Write-Host ""

# Restore original working directory
Pop-Location
Exit 0
