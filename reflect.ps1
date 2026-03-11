$dllPath = "c:\Welber\2022\GitHubDesktop\corte_cor_ag\bin\Debug\net8.0\win-x64\Unimake.Business.DFe.dll"
$asm = [System.Reflection.Assembly]::LoadFrom($dllPath)
Write-Host "--- E101101 ---"
$t1 = $asm.GetType('Unimake.Business.DFe.Xml.NFSe.NACIONAL.Eventos.E101101')
$t1.GetProperties() | Select-Object Name

Write-Host "--- InfPedReg ---"
$t2 = $asm.GetType('Unimake.Business.DFe.Xml.NFSe.NACIONAL.InfPedReg')
$t2.GetProperties() | Select-Object Name

Write-Host "--- Consulta.NFSe ---"
$t3 = $asm.GetType('Unimake.Business.DFe.Xml.NFSe.NACIONAL.Consulta.NFSe')
$t3.GetProperties() | Select-Object Name

Write-Host "--- Consulta.InfNFSe ---"
$t4 = $asm.GetType('Unimake.Business.DFe.Xml.NFSe.NACIONAL.Consulta.InfNFSe')
$t4.GetProperties() | Select-Object Name
