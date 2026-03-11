$dllPath = "c:\Welber\2022\GitHubDesktop\corte_cor_ag\bin\Debug\net8.0\win-x64\Unimake.Business.DFe.dll"
$asm = [System.Reflection.Assembly]::LoadFrom($dllPath)
$types = $asm.GetExportedTypes()
$types | Where-Object { $_.FullName -like '*NFSe*' -and ($_.FullName -match 'Cancel' -or $_.FullName -match 'PedSit' -or $_.FullName -match 'Consul') } | Select-Object FullName
$types | Where-Object { $_.FullName -like '*Unimake.Business.DFe.Servicos.NFSe*' -and $_.FullName -match 'Nacional' } | Select-Object FullName
