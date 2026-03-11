$dllPath = "c:\Welber\2022\GitHubDesktop\corte_cor_ag\bin\Debug\net8.0\win-x64\Unimake.Business.DFe.dll"
$asm = [System.Reflection.Assembly]::LoadFrom($dllPath)
$asm.GetExportedTypes() | Where-Object { $_.FullName -like '*NACIONAL*' -or $_.FullName -match 'Consultar.*NFSe' -or $_.FullName -match 'RecepcaoEvento' } | Select-Object FullName
