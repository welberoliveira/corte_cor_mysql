$dllPath = "c:\Welber\2022\GitHubDesktop\corte_cor_ag\bin\Debug\net8.0\win-x64\Unimake.Business.DFe.dll"
$asm = [System.Reflection.Assembly]::LoadFrom($dllPath)

Write-Host "--- E101101 ---"
$t1 = $asm.GetType('Unimake.Business.DFe.Xml.NFSe.NACIONAL.Eventos.E101101')
$t1.GetProperties() | Select-Object Name

Write-Host "--- Consulta.DPS ---"
$t2 = $asm.GetType('Unimake.Business.DFe.Xml.NFSe.NACIONAL.Consulta.DPS')
$t2.GetProperties() | Select-Object Name

Write-Host "--- Consulta.InfDPS ---"
$t3 = $asm.GetType('Unimake.Business.DFe.Xml.NFSe.NACIONAL.Consulta.InfDPS')
$t3.GetProperties() | Select-Object Name
