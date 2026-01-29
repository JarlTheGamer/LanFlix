[Version] 
Class=IEXPRESS 
SEDVersion=3 
[Options] 
PackagePurpose=InstallApp 
ShowInstallProgramWindow=0 
HideExtractAnimation=1 
UseLongFileName=1 
InsideCompressed=0 
CAB_FixedSize=0 
CAB_ResvCodeSigning=0 
RebootMode=N 
InstallPrompt=Do you want to install Lanflix Server? 
DisplayLicense= 
FinishMessage=Lanflix Server installed successfully 
TargetName=F:\Programming\Flix\build-tools\server\Lanflix-Installer.exe 
FriendlyName=Lanflix Server 
AppLaunched=cmd /c start "" "Lanflix.WebApi.exe" 
PostInstallCmd=<None> 
AdminQuietInstCmd= 
UserQuietInstCmd= 
SourceFiles=SourceFiles 
[SourceFiles] 
SourceFiles0=F:\Programming\Flix\build-tools\server\build\win-x64 
[SourceFiles0] 
Lanflix.WebApi.exe= 
